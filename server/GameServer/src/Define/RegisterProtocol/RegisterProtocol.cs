using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Proto;
using Serilog;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

public static partial class RegisterProtocol
{
    public delegate void ProtocolDelegate(IChannelHandlerContext context, MsgServerHeader msgHeader, IMessage reqMsg);

    public static Dictionary<Type, ProtocolDelegate> Protocols = new Dictionary<Type, ProtocolDelegate>();

    private static readonly ThreadLocalCache Cache = new ThreadLocalCache();
    private sealed class ThreadLocalCache : FastThreadLocal<AsciiString>
    {
        protected override AsciiString GetInitialValue()
        {
            DateTime dateTime = DateTime.UtcNow;
            return AsciiString.Cached($"{dateTime.DayOfWeek}, {dateTime:dd MMM yyyy HH:mm:ss z}");
        }
    }

    public static readonly AsciiString TypePlain = AsciiString.Cached("text/plain");
    public static readonly AsciiString TypeJson = AsciiString.Cached("application/json");
    public static readonly AsciiString TypeBinary = AsciiString.Cached("application/octet-stream");
    public static readonly AsciiString ServerName = AsciiString.Cached("Netty");
    public static readonly AsciiString ContentTypeEntity = HttpHeaderNames.ContentType;
    public static readonly AsciiString DateEntity = HttpHeaderNames.Date;
    public static readonly AsciiString ContentLengthEntity = HttpHeaderNames.ContentLength;
    public static readonly AsciiString ServerEntity = HttpHeaderNames.Server;
    public static volatile ICharSequence date = Cache.Value;

    /// <summary>
    /// 注册
    /// </summary>
    public static void Register()
    {
        // 消息头注册
        GlobalDefine.ProtoManager.AddMapping<MsgServerHeader>(MsgServerHeader.Parser.ParseFrom);
        GlobalDefine.ProtoManager.AddMapping<MsgClientHeader>(MsgClientHeader.Parser.ParseFrom);

        // 各服协议注册
        foreach (var item in ServerConfig.ServerOpens)
        {
            RegisterDefine.ProtocolRegister[item.Key].Invoke();
        }
    }

    /// <summary>
    /// 注册Protocol协议
    /// </summary>
    /// <param name="i_pProtocol"></param>
    /// <param name="i_fProcessRequestDelegate"></param>
    /// <param name="i_fParser"></param>
    public static void AddProtocol(Type i_pProtocol, ProtocolDelegate i_fProcessRequestDelegate, Func<byte[], int, int, IMessage> i_fParser = null)
    {
        GlobalDefine.ProtoManager.AddMapping(i_pProtocol, i_fParser);
        Protocols.Add(i_pProtocol, i_fProcessRequestDelegate);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // Http响应

    /// <summary>
    /// Http 简单字符串响应
    /// </summary>
    public static void HttpPlainResponse(IChannelHandlerContext context, string resrequest, ICharSequence redirectUrl = null)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(resrequest);
        int plaintextLen = plaintext.Length;
        IByteBuffer plaintextContentBuffer = Unpooled.UnreleasableBuffer(Unpooled.DirectBuffer().WriteBytes(plaintext));
        AsciiString plaintextClheaderValue = AsciiString.Cached($"{plaintextLen}");
        HttpCommonResponse(context, plaintextContentBuffer, TypePlain, plaintextClheaderValue, redirectUrl);
    }

    /// <summary>
    /// Http Json字符串响应
    /// </summary>
    public static void HttpJsonResponse(IChannelHandlerContext context, ResJsonData responseJsonData, ICharSequence redirectUrl = null)
    {
        string jsonstring = responseJsonData.GetResponseJsonData();
        byte[] jsontext = Encoding.UTF8.GetBytes(jsonstring);
        int jsontextLen = jsontext.Length;
        IByteBuffer jsontextContentBuffer = Unpooled.WrappedBuffer(jsontext);
        AsciiString JsontextClheaderValue = AsciiString.Cached($"{jsontextLen}");
        HttpCommonResponse(context, jsontextContentBuffer, TypeJson, JsontextClheaderValue, redirectUrl);
    }

    /// <summary>
    /// Http 二进制响应
    /// </summary>
    public static void HttpBinaryResponse(IChannelHandlerContext context, ResMsgClientData responseMessageData, ICharSequence redirectUrl = null)
    {
        byte[] sendMessages = responseMessageData.GetSendMessages();
        int len = sendMessages.Length;
        IByteBuffer contentBuffer = Unpooled.WrappedBuffer(sendMessages);
        AsciiString clheaderValue = AsciiString.Cached($"{len}");
        HttpCommonResponse(context, contentBuffer, TypeBinary, clheaderValue, redirectUrl);
    }

    /// <summary>
    /// Http 通用响应
    /// </summary>
    public static void HttpCommonResponse(IChannelHandlerContext context, IByteBuffer buf, ICharSequence contentType, ICharSequence contentLength, ICharSequence redirectUrl = null)
    {
        var response = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.OK, buf, false);
        HttpHeaders headers = response.Headers;
        headers.Set(ContentTypeEntity, contentType);
        headers.Set(ServerEntity, ServerName);
        headers.Set(DateEntity, date);
        headers.Set(ContentLengthEntity, contentLength); headers.Set(HttpHeaderNames.AccessControlAllowOrigin, "*");

        if (redirectUrl != null)
        {
            headers.Set(HttpHeaderNames.Location, redirectUrl);
            response.SetStatus(HttpResponseStatus.Found);
        }

        context.WriteAsync(response);
    }


    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // WebSocket响应

    /// <summary>
    /// WebSocket 二进制响应
    /// </summary>
    public static void WebSocketBinaryResponse(IChannelHandlerContext context, ResMsgClientData responseMessageData)
    {
        var msg = new BinaryWebSocketFrame(Unpooled.WrappedBuffer(responseMessageData.GetSendMessages())).Retain();
        context.WriteAndFlushAsync(msg).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                ReferenceCountUtil.SafeRelease(msg);
            }
        });
    }

    private static ConcurrentQueue<IByteBuffer> ByteBufferPools = new ConcurrentQueue<IByteBuffer>();
    public static bool IsSend = true;
    /// <summary>
    /// WebSocket json响应
    /// </summary>
    public static void WebSocketJsonResponse(IChannelHandlerContext context, ResMsgClientData responseMessageData)
    {
        try
        {
            string json = responseMessageData.GetSendMessagesToJson();
            WebSocketFrame frame = new TextWebSocketFrame(json);
            context.WriteAndFlushAsync(frame);
        }
        catch (Exception ex)
        {
            // 记录异常日志
            Debug.Instance.LogWarn($"Error sending WebSocket response: {ex.Message}");
        }

        //try
        //{
        //    var jsonResponse = responseMessageData.GetSendMessagesToJson();
        //    byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonResponse);
        //    int messageSize = jsonBytes.Length;
        //    //Debug.Instance.LogWarn($"Sending WebSocket message of size: {messageSize} bytes");
        //IByteBuffer buf = context.Allocator.Buffer(jsonBytes.Length);
        //    buf.WriteBytes(jsonBytes);
        //    var msg = new TextWebSocketFrame(jsonResponse);
        //    context.WriteAndFlushAsync(msg).ContinueWith(t =>
        //    {
        //        //if (t.IsCompletedSuccessfully)
        //        //{
        //            ReferenceCountUtil.SafeRelease(msg);
        //        //}
        //    });
        //}
        //catch (Exception ex)
        //{
        //    // 记录异常日志
        //    Debug.Instance.LogWarn($"Error sending WebSocket response: {ex.Message}");
        //}

        //try
        //{
        //    var jsonResponse = responseMessageData.GetSendMessagesToJson();
        //    //byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonResponse);
        //    //int messageSize = jsonBytes.Length;
        //    // 打印发送的包体大小
        //    //Debug.Instance.LogWarn($"Sending WebSocket message of size: {messageSize} bytes");
        //    var buf = Unpooled.CopiedBuffer(jsonResponse, Encoding.UTF8);
        //    var msg = new TextWebSocketFrame(buf);
        //    context.WriteAndFlushAsync(msg).ContinueWith(t =>
        //    {
        //        //if (t.IsCompletedSuccessfully)
        //        //{
        //        //ReferenceCountUtil.SafeRelease(buf);
        //        //ReferenceCountUtil.SafeRelease(msg);
        //        //}
        //    });
        //}
        //catch (Exception ex)
        //{
        //    // 记录异常日志
        //    Debug.Instance.LogWarn($"Error sending WebSocket response: {ex.Message}");
        //}
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // 通用GM协议

    /// <summary>
    /// 关闭服务器
    /// </summary>
    private static void GMCloseServer(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData responseMessageData = new ResMsgClientData();
        if (msgServerHeader.PlayerInstId != -10000)
        {
            HttpBinaryResponse(context, responseMessageData);
            return;
        }
        Launch.Close = true;
        responseMessageData.SetCode(ResCodeEnum.Succeed);
        HttpBinaryResponse(context, responseMessageData);
    }
}