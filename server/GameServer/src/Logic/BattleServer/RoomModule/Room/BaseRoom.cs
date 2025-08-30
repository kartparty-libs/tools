using DotNetty.Codecs.Http;
using Proto;

/// <summary>
/// 房间基类
/// </summary>
public abstract class BaseRoom
{
    /// <summary>
    /// 房间实例Id
    /// </summary>
    private long m_nInstId;

    /// <summary>
    /// 地图配置Id
    /// </summary>
    private int m_nMapCfgId;

    /// <summary>
    /// 房间玩家人数上限
    /// </summary>
    private int m_nRoomMaxPlayerCount;

    /// <summary>
    /// 房间玩家列表
    /// </summary>
    protected List<BattlePlayer> m_tPlayers = new List<BattlePlayer>();

    /// <summary>
    /// 玩家数量
    /// </summary>
    private int m_nPlayerCount = 0;

    /// <summary>
    /// 机器人数量
    /// </summary>
    private int m_nRobotCount = 0;

    /// <summary>
    /// 客户端数据缓存
    /// </summary>
    private RoomData m_pRoomData = new RoomData(); 

    /// <summary>
    /// 机器人Id自增
    /// </summary>
    private long m_nRobotMaxInstId = 1;

    public BaseRoom(long i_nInstId)
    {
        m_nInstId = i_nInstId;
    }

    public virtual void Initializer(int i_nMapCfgId)
    {
        m_nMapCfgId = i_nMapCfgId;
        m_nRoomMaxPlayerCount = ServerConfig.GetToInt("map_max_player");
    }

    public virtual void Update(int i_nMillisecondDelay)
    {
    }

    public virtual void Delete()
    {
    }

    public virtual RoomData GetRoomData()
    {
        m_pRoomData.InstId = m_nInstId;
        m_pRoomData.MapCfgId = m_nMapCfgId;
        m_pRoomData.RoomMaxPlayerCount = m_nRoomMaxPlayerCount;

        //m_pRoomData.Players.Clear();
        //foreach (BattlePlayer player in m_tPlayers)
        //{
        //    m_pRoomData.Players.Add(player.GetBattlePlayerData());
        //}

        return m_pRoomData;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 获取房间实例Id
    /// </summary>
    /// <returns></returns>
    public long GetInstId() => m_nInstId;

    /// <summary>
    /// 获取房间地图配置Id
    /// </summary>
    /// <returns></returns>
    public int GetMapCfgId() => m_nMapCfgId;

    /// <summary>
    /// 获取房间当前人数
    /// </summary>
    /// <returns></returns>
    public int GetRoomCurrentPlayerCount() => m_tPlayers.Count;

    /// <summary>
    /// 获取房间当前真实人数
    /// </summary>
    /// <returns></returns>
    public int GetRoomCurrentRealPlayerCount() => m_nPlayerCount;

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 是否满人
    /// </summary>
    /// <returns></returns>
    public bool IsMaxPlayer()
    {
        return m_nPlayerCount >= m_nRoomMaxPlayerCount;
    }

    /// <summary>
    /// 是否可以进入房间
    /// </summary>
    /// <param name="i_nRoleId"></param>
    /// <returns></returns>
    public virtual bool IsEnterRoom(long i_nRoleId)
    {
        if (m_tPlayers.Count >= m_nRoomMaxPlayerCount)
        {
            return false;
        }
        foreach (var player in m_tPlayers)
        {
            if (player.GetPlayerInstId() == i_nRoleId)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 玩家进入房间
    /// </summary>
    /// <param name="i_pBattlePlayer"></param>
    public virtual void PlayerEnterRoom(BattlePlayer i_pBattlePlayer)
    {
        if (!IsEnterRoom(i_pBattlePlayer.GetPlayerInstId()))
        {
            return;
        }

        m_tPlayers.Add(i_pBattlePlayer);

        if (i_pBattlePlayer.IsRobot)
        {
            m_nRobotCount++;
        }
        else
        {
            m_nPlayerCount++;
        }

        i_pBattlePlayer.OnPlayerEnterRoom(this);
        //OnChangeRoomData();

        Debug.Instance.Log($"PlayerEnterRoom -> RoomId = {m_nInstId}  RoleId = {i_pBattlePlayer.GetPlayerInstId()}  Name = {i_pBattlePlayer.GetName()}");
    }

    /// <summary>
    /// 玩家离开房间
    /// </summary>
    /// <param name="i_pBattlePlayer"></param>
    /// <param name="i_bIsKick"></param>
    public virtual void PlayerLeaveRoom(BattlePlayer i_pBattlePlayer, bool i_bIsKick = false)
    {
        for (int i = m_tPlayers.Count - 1; i >= 0; i--)
        {
            if (m_tPlayers[i].GetPlayerInstId() == i_pBattlePlayer.GetPlayerInstId())
            {
                m_tPlayers.RemoveAt(i);
                break;
            }
        }

        if (i_pBattlePlayer.IsRobot)
        {
            m_nRobotCount--;
        }
        else
        {
            m_nPlayerCount--;
        }

        i_pBattlePlayer.OnPlayerLeaveRoom(i_bIsKick);
        //OnChangeRoomData();

        Debug.Instance.Log($"PlayerLeaveRoom -> RoomId = {m_nInstId}  RoleId = {i_pBattlePlayer.GetPlayerInstId()}  Name = {i_pBattlePlayer.GetName()}");
    }

    /// <summary>
    /// 玩家加载地图完成通知
    /// </summary>
    public void PlayerLoadMapComplete(BattlePlayer i_pBattlePlayer)
    {
        i_pBattlePlayer.OnPlayerLoadMapComplete();
    }

    /// <summary>
    /// 玩家状态同步
    /// </summary>
    public virtual void PlayerStateSync(BattlePlayer i_pBattlePlayer, BattlePlayerStateData i_pBattlePlayerStateData)
    {
    }

    /// <summary>
    /// 玩家完成比赛同步
    /// </summary>
    public void PlayerCompleteGame(BattlePlayer i_pBattlePlayer, int i_nTime, long i_nRoleId)
    {
    }

    /// <summary>
    /// 房间数据改变通知
    /// </summary>
    public virtual void OnChangeRoomData()
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        ResMsgRoomDataChange resMsgBodyRoomDataChange = new ResMsgRoomDataChange()
        {
            RoomData = GetRoomData(),
        };
        resMsgClientData.AddMessageData(resMsgBodyRoomDataChange);
        SendAllPlayerToClient(resMsgClientData);
    }

    /// <summary>
    /// 广播房间玩家至客户端
    /// </summary>
    /// <param name="i_pResMsgData"></param>
    /// <param name="i_nIgnoreRoleId"></param>
    public void SendAllPlayerToClient(ResMsgClientData i_pResMsgData, long i_nIgnoreRoleId = -1)
    {
        if (m_tPlayers.Count == 0)
        {
            return;
        }
        foreach (var player in m_tPlayers)
        {
            if (i_nIgnoreRoleId != player.GetPlayerInstId())
            {
                player.SendToClient(i_pResMsgData);
            }
        }
    }

    /// <summary>
    /// 广播房间玩家至客户端
    /// </summary>
    /// <param name="i_pResMsgData"></param>
    /// <param name="i_sAccount"></param>
    public void SendPlayerToClient(ResMsgClientData i_pResMsgData, string i_sAccount)
    {

        foreach (var player in m_tPlayers)
        {
            if (i_sAccount == player.GetAccount())
            {
                player.SendToClient(i_pResMsgData);
            }
        }
    }

    //public virtual void SendResMsgClientData(BattlePlayer i_pBattlePlayer, ResMsgClientData i_pResMsgClientData, BattlePlayerStateData i_pBattlePlayerStateData)
    //{
    //}

    public virtual void UpdatePlayerStateSync(BattlePlayer i_pBattlePlayer)
    {
    }
}