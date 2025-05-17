using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Proto;
using System.Text;
using static DotNetty.Codecs.Http.HttpResponseStatus;
using static DotNetty.Codecs.Http.HttpVersion;


public sealed class WebSocketServerHandler : SimpleChannelInboundHandler<object>
{
    public static AttributeKey<WebSocketUserData> WebSocketUserDataAttributeKey = AttributeKey<WebSocketUserData>.NewInstance("WebSocketUserData");

    public static string WebsocketPath = $"/battleserver_{ServerConfig.ServerId}/websocket";

    private WebSocketServerHandshaker m_pHandshaker;

    protected override void ChannelRead0(IChannelHandlerContext context, object msg)
    {
        //Debug.Instance.Log($"ChannelRead0");
        if (msg is IFullHttpRequest request)
        {
            this.HandleHttpRequest(context, request);
        }
        else if (msg is WebSocketFrame frame)
        {
            this.HandleWebSocketFrame(context, frame);
        }
    }

    public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

    void HandleHttpRequest(IChannelHandlerContext context, IFullHttpRequest req)
    {
        //Debug.Instance.Log($"HandleHttpRequest req.Uri = {req.Uri}");
        if (!req.Result.IsSuccess)
        {
            SendHttpResponse(context, req, new DefaultFullHttpResponse(Http11, BadRequest));
            return;
        }

        if (!Equals(req.Method, DotNetty.Codecs.Http.HttpMethod.Get))
        {
            SendHttpResponse(context, req, new DefaultFullHttpResponse(Http11, Forbidden));
            return;
        }

        if (req.Uri == WebsocketPath)
        {
            var wsFactory = new WebSocketServerHandshakerFactory(
                GetWebSocketLocation(req), null, true, 5 * 1024 * 1024);
            this.m_pHandshaker = wsFactory.NewHandshaker(req);
            if (this.m_pHandshaker == null)
            {
                WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(context.Channel);
            }
            else
            {
                this.m_pHandshaker.HandshakeAsync(context.Channel, req);
            }
            //Debug.Instance.Log($"HandleHttpRequest {Thread.CurrentThread.ManagedThreadId}  context.Channel.Id = {context.Channel.Id}");
        }
    }

    void HandleWebSocketFrame(IChannelHandlerContext context, WebSocketFrame frame)
    {
        if (frame is CloseWebSocketFrame)
        {
            this.m_pHandshaker.CloseAsync(context.Channel, (CloseWebSocketFrame)frame.Retain());
        }
        else if (frame is PingWebSocketFrame)
        {
            context.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
        }
        else if (frame is PongWebSocketFrame)
        {
            context.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
        }
        else if (frame is TextWebSocketFrame textFrame)
        {
            byte[] content = new byte[textFrame.Content.ReadableBytes];
            if (content.Length > 0)
            {
                textFrame.Content.ReadBytes(content);
                List<IMessage> messages = UtilityMethod.JsonToMessage(content);
                //Debug.Instance.Log($"TextWebSocketFrame " + messages[1].GetType().Name);
                WebSocketMessageManager.Instance.AddMessage(context, messages);
            }
        }
        else if (frame is BinaryWebSocketFrame binaryFrame)
        {
            byte[] content = new byte[binaryFrame.Content.ReadableBytes];
            binaryFrame.Content.ReadBytes(content);
            List<IMessage> messages = GlobalDefine.ProtoManager.FromBytes(content);
            //Debug.Instance.Log($"BinaryWebSocketFrame " + messages[1].GetType().Name);
            WebSocketMessageManager.Instance.AddMessage(context, messages);
            return;
        }
        //Debug.Instance.Log($"HandleWebSocketFrame {frame.GetType().Name} {Thread.CurrentThread.ManagedThreadId}  context.Channel.Id = {context.Channel.Id}");
    }

    static void SendHttpResponse(IChannelHandlerContext context, IFullHttpRequest req, IFullHttpResponse res)
    {
        if (res.Status.Code != 200)
        {
            IByteBuffer buf = Unpooled.CopiedBuffer(Encoding.UTF8.GetBytes(res.Status.ToString()));
            res.Content.WriteBytes(buf);
            buf.Release();
            HttpUtil.SetContentLength(res, res.Content.ReadableBytes);
        }

        Task task = context.Channel.WriteAndFlushAsync(res);
        if (!HttpUtil.IsKeepAlive(req) || res.Status.Code != 200)
        {
            task.ContinueWith((t, c) => ((IChannelHandlerContext)c).CloseAsync(), context, TaskContinuationOptions.ExecuteSynchronously);
        }
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
    {
        WebSocketUserData webSocketUserData = context.Channel.GetAttribute(WebSocketUserDataAttributeKey).Get();
        if (webSocketUserData != null)
        {
            BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(webSocketUserData.roleId);
            if (battlePlayer != null)
            {
                List<IMessage> messages = new List<IMessage>();
                MsgServerHeader msgServerHeader = new MsgServerHeader()
                {
                    PlayerInstId = battlePlayer.GetPlayerInstId()
                };

                ReqMsgPlayerLeaveRoom reqMsgPlayerLeaveRoom = new ReqMsgPlayerLeaveRoom();
                messages.Add(msgServerHeader);
                messages.Add(reqMsgPlayerLeaveRoom);
                WebSocketMessageManager.Instance.AddMessage(context, messages);
            }
        }
        context.CloseAsync();
        Debug.Instance.Log($"ExceptionCaught {Thread.CurrentThread.ManagedThreadId}  {e.Message}  context.Channel.Id = {context.Channel.Id}", e);
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        WebSocketUserData webSocketUserData = context.Channel.GetAttribute(WebSocketUserDataAttributeKey).Get();
        if (webSocketUserData != null)
        {
            BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(webSocketUserData.roleId);
            if (battlePlayer != null)
            {
                List<IMessage> messages = new List<IMessage>();
                MsgServerHeader msgServerHeader = new MsgServerHeader()
                {
                    PlayerInstId = battlePlayer.GetPlayerInstId()
                };

                ReqMsgPlayerLeaveRoom reqMsgPlayerLeaveRoom = new ReqMsgPlayerLeaveRoom();
                messages.Add(msgServerHeader);
                messages.Add(reqMsgPlayerLeaveRoom);
                WebSocketMessageManager.Instance.AddMessage(context, messages);
            }
        }
        context.CloseAsync();
        Debug.Instance.Log($"ChannelInactive {Thread.CurrentThread.ManagedThreadId}  context.Channel.Id = {context.Channel.Id}");
    }

    static string GetWebSocketLocation(IFullHttpRequest req)
    {
        bool result = req.Headers.TryGet(HttpHeaderNames.Host, out ICharSequence value);
        string location = value.ToString() + WebsocketPath;

        if (false)
        {
            return "wss://" + location;
        }
        else
        {
            return "ws://" + location;
        }
    }
}

/// <summary>
/// 用户数据
/// </summary>
public class WebSocketUserData
{
    public long roleId;
}