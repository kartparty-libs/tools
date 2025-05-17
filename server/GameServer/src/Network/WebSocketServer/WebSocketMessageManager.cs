using DotNetty.Transport.Channels;
using Google.Protobuf;
using Proto;
using System.Collections.Concurrent;
using System.Data;

/// <summary>
/// WebSocket消息管理器
/// 消息单线程处理
/// </summary>
public class WebSocketMessageManager : BaseManager<WebSocketMessageManager>
{
    struct MessageData
    {
        public IChannelHandlerContext context;
        public List<IMessage> messages;
    }

    /// <summary>
    /// 消息队列
    /// </summary>
    private ConcurrentQueue<MessageData> m_tMessageDatas = new ConcurrentQueue<MessageData>();

    public WebSocketMessageManager()
    {
        m_nUpdateIntervalTime = 10;
    }

    public override void Initializer(DataRow i_pGlobalInfo, bool i_bIsFirstOpenServer)
    {
        base.Initializer(i_pGlobalInfo, i_bIsFirstOpenServer);
    }

    public override void Update(int i_nMillisecondDelay)
    {
        base.Update(i_nMillisecondDelay);
        if (!m_tMessageDatas.IsEmpty)
        {
            List<MessageData> messageDatas;
            lock (this)
            {
                messageDatas = m_tMessageDatas.ToList();
                m_tMessageDatas.Clear();
            }

            foreach (var item in messageDatas)
            {
                if (item.messages != null && item.messages.Count > 1 && RegisterProtocol.Protocols.TryGetValue(item.messages[1].GetType(), out RegisterProtocol.ProtocolDelegate? __delegate))
                {
                    MsgServerHeader msgServerHeader = item.messages[0] as MsgServerHeader;
                    __delegate(item.context, msgServerHeader, item.messages[1]);
                }
            }
            messageDatas.Clear();
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 添加消息
    /// </summary>
    /// <param name="context"></param>
    /// <param name="messages"></param>
    public void AddMessage(IChannelHandlerContext context, List<IMessage> messages)
    {
        m_tMessageDatas.Enqueue(new MessageData()
        {
            context = context,
            messages = messages
        });
    }
}