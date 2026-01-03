
using Google.Protobuf;
using Proto;
using System.Collections.Generic;

public class ResMsgClientData
{
    private static long ID = 0;
    private static object lockobj = new object();

    private List<IMessage> m_pMessages = new List<IMessage>();
    private MsgClientHeader m_pMsgClientHeader;
    private Dictionary<string, IMessage> m_pTemp = new Dictionary<string, IMessage>();

    public ResMsgClientData()
    {
        m_pMsgClientHeader = new MsgClientHeader();
        m_pMsgClientHeader.Code = ResCodeEnum.Succeed;
        m_pMsgClientHeader.Date = UtilityMethod.GetUnixTimeMilliseconds();
        lock (lockobj)
        {
            m_pMsgClientHeader.Id = ID++;
        }
        m_pMessages.Add(m_pMsgClientHeader);
        //Debug.Instance.LogWarn("sendid = " + m_pMsgClientHeader.Id);
    }

    public ResCodeEnum GetCode()
    {
        return m_pMsgClientHeader.Code;
    }

    public void SetCode(ResCodeEnum i_eResCodeEnum)
    {
        m_pMsgClientHeader.Code = i_eResCodeEnum;
    }

    public void SetDate(long i_nDate)
    {
        m_pMsgClientHeader.Date = i_nDate;
    }

    public void AddMessageData(IMessage i_pMessage)
    {
        if (i_pMessage == null)
        {
            return;
        }
        m_pMessages.Add(i_pMessage);
        SetDate(UtilityMethod.GetUnixTimeMilliseconds());
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
        SetDate(UtilityMethod.GetUnixTimeMilliseconds());
    }

    public byte[] GetSendMessages()
    {
        return GlobalDefine.ProtoManager.SendMessages(m_pMessages);
    }
    public string GetSendMessagesToJson()
    {
        m_pTemp.Clear();
        for (int i = 0; i < m_pMessages.Count; i++)
        {
            m_pTemp.Add(m_pMessages[i].GetType().Name, m_pMessages[i]);
        }
        return UtilityMethod.JsonSerializeObject(m_pTemp);
    }
}
