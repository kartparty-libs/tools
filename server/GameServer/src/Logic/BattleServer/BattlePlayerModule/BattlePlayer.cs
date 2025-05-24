using System;
using DotNetty.Transport.Channels;
using Proto;

/// <summary>
/// 战斗服角色
/// </summary>
public partial class BattlePlayer
{
    /// <summary>
    /// 角色Id
    /// </summary>
    private long m_nPlayerInstId = 0;

    /// <summary>
    /// 玩家名字
    /// </summary>
    private string m_sName = "";

    /// <summary>
    /// 账号
    /// </summary>
    private string m_sAccount = "";

    /// <summary>
    /// 密码
    /// </summary>
    private string m_sPassword = "";

    /// <summary>
    /// WebSocket交互对象
    /// </summary>
    private IChannelHandlerContext m_pContext;

    /// <summary>
    /// 最后操作时间戳
    /// </summary>
    private long m_nLastHandleTime;

    /// <summary>
    /// 最近一次同步数据
    /// </summary>
    protected BattlePlayerStateData m_pLastBattlePlayerStateData;
    protected bool m_bIsChangeLastBattlePlayerStateData = false;

    /// <summary>
    /// 客户端数据缓存
    /// </summary>
    private BattlePlayerData m_pBattlePlayerData = new BattlePlayerData();

    public BattlePlayer()
    {
    }

    public void Initializer(PlayerData i_pPlayerData)
    {
        m_nPlayerInstId = i_pPlayerData.playerInstId;
        m_sName = i_pPlayerData.name;
        m_sAccount = i_pPlayerData.account;
        m_sPassword = i_pPlayerData.password;

        UpdateLastHandleTime();

        InitializerRoom(i_pPlayerData);
    }

    public void Update(int i_nMillisecondDelay)
    {
    }

    public void Delete()
    {
    }

    public BattlePlayerData GetBattlePlayerData()
    {
        m_pBattlePlayerData.A = m_nPlayerInstId;
        m_pBattlePlayerData.B = m_sName;
        m_pBattlePlayerData.C = m_sAccount;
        //m_pBattlePlayerData.LastBattlePlayerStateData = m_pLastBattlePlayerStateData;
        return m_pBattlePlayerData;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 获取玩家Id
    /// </summary>
    /// <returns></returns>
    public long GetPlayerInstId() => m_nPlayerInstId;

    /// <summary>
    /// 获取玩家名字
    /// </summary>
    /// <returns></returns>
    public string GetName() => m_sName;

    /// <summary>
    /// 获取玩家账号
    /// </summary>
    /// <returns></returns>
    public string GetAccount() => m_sAccount;
    
    /// <summary>
    /// 获取密码
    /// </summary>
    /// <returns></returns>
    public string GetPassword() => m_sPassword;

    /// <summary>
    /// 获取最后操作时间戳
    /// </summary>
    /// <returns></returns>
    public long GetLastHandleTime() => m_nLastHandleTime;

    /// <summary>
    /// 刷新最后操作时间戳
    /// </summary>
    public void UpdateLastHandleTime()
    {
        m_nLastHandleTime = UtilityMethod.GetUnixTimeMilliseconds();
    }

    /// <summary>
    /// 设置WebSocket交互对象
    /// </summary>
    /// <param name="i_pContext"></param>
    public void SetContext(IChannelHandlerContext i_pContext)
    {
        if (m_pContext != null && m_pContext.Channel.Id != i_pContext.Channel.Id)
        {
            m_pContext.CloseAsync();
        }
        if (m_pContext == null)
        {
            i_pContext.Channel.GetAttribute(WebSocketServerHandler.WebSocketUserDataAttributeKey)?.Remove();
            WebSocketUserData webSocketUserData = new WebSocketUserData() { roleId = m_nPlayerInstId };
            i_pContext.Channel.GetAttribute(WebSocketServerHandler.WebSocketUserDataAttributeKey).Set(webSocketUserData);
        }
        m_pContext = i_pContext;
    }

    public BattlePlayerStateData GetLastBattlePlayerStateData() => m_pLastBattlePlayerStateData;
    public void SetLastBattlePlayerStateData(BattlePlayerStateData i_pBattlePlayerStateData, bool i_bIsChangeLastBattlePlayerStateData)
    {
        m_pLastBattlePlayerStateData = i_pBattlePlayerStateData;
        m_bIsChangeLastBattlePlayerStateData = true;
    }

    public bool IsChangeLastBattlePlayerStateData() => m_bIsChangeLastBattlePlayerStateData;
    public void ClearChangeLastBattlePlayerStateData()
    {
        m_bIsChangeLastBattlePlayerStateData = false;
    }
    /// <summary>
    /// 清理WebSocket交互对象
    /// </summary>
    public void ClearContext()
    {
        m_pContext.CloseAsync();
        m_pContext = null;
    }

    /// <summary>
    /// 发送消息至客户端
    /// </summary>
    /// <param name="m_pResponseMessageData"></param>
    public void SendToClient(ResMsgClientData m_pResponseMessageData)
    {
        if (m_pContext == null)
        {
            return;
        }
        if (m_pContext.Removed)
        {
            m_pContext.CloseAsync();
            m_pContext = null;
            return;
        }
        RegisterProtocol.WebSocketJsonResponse(m_pContext, m_pResponseMessageData);
    }
}