
using Google.Protobuf;
using Proto;

public class ReqMsgServerData
{
    private List<IMessage> m_pMessages = new List<IMessage>();
    private MsgServerHeader m_pMsgServerHeader;

    public ReqMsgServerData()
    {
        m_pMsgServerHeader = new MsgServerHeader();
        m_pMessages.Add(m_pMsgServerHeader);
    }

    public void AddMessageData(IMessage i_pMessage)
    {
        if (i_pMessage == null)
        {
            return;
        }
        m_pMessages.Add(i_pMessage);
    }

    public void AddMessageData(List<IMessage> i_pMessages)
    {
        if (i_pMessages == null || i_pMessages.Count == 0)
        {
            return;
        }
        foreach (var item in i_pMessages)
        {
            m_pMessages.Add(item);
        }
    }

    public byte[] GetSendMessages()
    {
        return GlobalDefine.ProtoManager.SendMessages(m_pMessages);
    }
}
