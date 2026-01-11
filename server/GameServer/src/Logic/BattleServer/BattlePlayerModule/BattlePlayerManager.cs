using DotNetty.Transport.Channels;
using Proto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Numerics;

/// <summary>
/// 战斗服角色管理器
/// </summary>
public class BattlePlayerManager : BaseManager<BattlePlayerManager>
{
    /// <summary>
    /// 玩家Id基数
    /// </summary>
    public static int ServerPlayerIdBaseValue = 10000000;

    /// <summary>
    /// 玩家id自增
    /// </summary>
    private static long m_nPlayerIndex = 0;

    /// <summary>
    /// 战斗角色列表
    /// </summary>
    private ConcurrentDictionary<long, BattlePlayer> m_pBattlePlayers = new ConcurrentDictionary<long, BattlePlayer>();

    /// <summary>
    /// 战斗角色列表
    /// </summary>
    private ConcurrentDictionary<string, BattlePlayer> m_pBattlePlayerByAccounts = new ConcurrentDictionary<string, BattlePlayer>();

    /// <summary>
    /// 战斗角色待删除列表
    /// </summary>
    private List<BattlePlayer> m_tRemovePlayers = new List<BattlePlayer>();

    public BattlePlayerManager()
    {
        m_nUpdateIntervalTime = 10000;
    }

    public override void Initializer(DataRow i_pGlobalInfo, bool i_bIsFirstOpenServer)
    {
        base.Initializer(i_pGlobalInfo, i_bIsFirstOpenServer);
        m_nPlayerIndex = Convert.ToInt64(i_pGlobalInfo["playerindex"]);
    }

    public override void Update(int i_nMillisecondDelay)
    {
        base.Update(i_nMillisecondDelay);

        long currTime = UtilityMethod.GetUnixTimeMilliseconds();
        foreach (var item in m_pBattlePlayers)
        {
            if (currTime - item.Value.GetLastHandleTime() > 3600000)//
            {
                m_tRemovePlayers.Add(item.Value);
            }
        }
        

        if (m_tRemovePlayers.Count > 0)
        {
            foreach (var item in m_tRemovePlayers)
            {
                RemoveBattlePlayer(item.GetPlayerInstId());
            }
            m_tRemovePlayers.Clear();
        }

        GC.Collect(2,GCCollectionMode.Forced);
    }

    public void UpdatePlayerStateSync()
    {
        if (m_pBattlePlayers.Count == 0)
        {
            return;
        }
        Dictionary<long, BattlePlayer> battlePlayers = m_pBattlePlayers.ToDictionary();
        foreach (var item in battlePlayers)
        {
            RoomManager.Instance.GetRoom(item.Value.RoomInstId)?.UpdatePlayerStateSync(item.Value);
        }
        foreach (var item in battlePlayers)
        {
            item.Value.ClearChangeLastBattlePlayerStateData();
        }
    }
    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 获取新实例Id
    /// </summary>
    /// <returns></returns>
    public long GetNewPlayerInstId()
    {
        long playerInstId = 0;
        lock (this)
        {
            m_nPlayerIndex = m_nPlayerIndex + 1;
            playerInstId = m_nPlayerIndex + ServerConfig.ServerId * ServerPlayerIdBaseValue;
        }
        Launch.DBServer.UpdateData(ServerTypeEnum.eBattleServer, SqlTableName.globalinfo, new Dictionary<string, object>() { { "playerindex", m_nPlayerIndex } }, "serverid", ServerConfig.ServerId);
        return playerInstId;
    }

    /// <summary>
    /// 获取战斗角色
    /// </summary>
    /// <param name="i_nPlayerId"></param>
    /// <returns></returns>
    public BattlePlayer GetBattlePlayer(long i_nPlayerId, IChannelHandlerContext i_pContext = null)
    {
        m_pBattlePlayers.TryGetValue(i_nPlayerId, out BattlePlayer battlePlayer);
        if (i_pContext != null)
        {
            battlePlayer?.SetContext(i_pContext);
            battlePlayer?.UpdateLastHandleTime();
        }
        return battlePlayer;
    }

    /// <summary>
    /// 登入战斗角色
    /// </summary>
    /// <returns></returns>
    public PlayerCallbackData LoginBattlePlayer(string i_sAccount, string i_sPassword, IChannelHandlerContext i_pContext)
    {
        PlayerCallbackData playerCallbackData = new PlayerCallbackData();
        if (m_pBattlePlayerByAccounts.TryGetValue(i_sAccount, out BattlePlayer battlePlayer))
        {
            if (battlePlayer.GetPassword() != i_sPassword)
            {
                playerCallbackData.resCodeEnum = ResCodeEnum.PassworWrongd;
                return playerCallbackData;
            }

            RoomManager.Instance.PlayerLeaveRoom(battlePlayer);

            playerCallbackData.player = battlePlayer;
        }
        else
        {
            DataTable data = Launch.DBServer.SelectData(ServerTypeEnum.eBattleServer, SqlTableName.player, "account", i_sAccount);
            if (data == null)
            {
                playerCallbackData.resCodeEnum = ResCodeEnum.ServerBusy;
                return playerCallbackData;
            }
            else if (data.Rows.Count == 0)
            {
                playerCallbackData.resCodeEnum = ResCodeEnum.AccountNotRegistered;
                return playerCallbackData;
            }
            else
            {
                DataRow dataRow = data.Rows[0];
                PlayerData playerData = new PlayerData();
                playerData.account = Convert.ToString(dataRow["account"]);
                playerData.playerInstId = Convert.ToInt64(dataRow["playerinstid"]);
                playerData.name = Convert.ToString(dataRow["playername"]);
                playerData.password = Convert.ToString(dataRow["password"]);
                playerData.createtime = Convert.ToInt64(dataRow["createtime"]);

                if (playerData.password != i_sPassword)
                {
                    playerCallbackData.resCodeEnum = ResCodeEnum.PassworWrongd;
                    return playerCallbackData;
                }

                playerCallbackData.player = this.CreateBattlePlayer(playerData);
            }
        }

        playerCallbackData.resCodeEnum = ResCodeEnum.Succeed;
        playerCallbackData.player?.SetContext(i_pContext);
        playerCallbackData.player?.UpdateLastHandleTime();
        Debug.Instance.Log("LoginBattlePlayer " + i_sAccount);
        return playerCallbackData;
    }

    /// <summary>
    /// 创建战斗角色
    /// </summary>
    /// <param name="i_pPlayerData"></param>
    /// <returns></returns>
    public BattlePlayer CreateBattlePlayer(PlayerData i_pPlayerData)
    {
        if (m_pBattlePlayerByAccounts.TryGetValue(i_pPlayerData.account, out BattlePlayer battlePlayer))
        {
            return battlePlayer;
        }
        battlePlayer = m_pBattlePlayers.GetOrAdd(i_pPlayerData.playerInstId, (key) => { return new BattlePlayer(); });
        m_pBattlePlayerByAccounts.TryAdd(i_pPlayerData.account, battlePlayer);
        battlePlayer.Initializer(i_pPlayerData);
        return battlePlayer;
    }

    public PlayerCallbackData CreateNewBattlePlayer(string i_sAccount, string i_sPassword, string i_sName)
    {
        PlayerCallbackData playerCallbackData = new PlayerCallbackData();

        // 账号密码含非法字符
        if (!UtilityMethod.VerifyConventionByAccount(i_sAccount))
        {
            playerCallbackData.resCodeEnum = ResCodeEnum.InvalidAccount;
            return playerCallbackData;
        }

        // 账号密码含非法字符
        if (!UtilityMethod.VerifyConventionByAccount(i_sPassword))
        {
            playerCallbackData.resCodeEnum = ResCodeEnum.InvalidAccount;
            return playerCallbackData;
        }

        // 名字含非法字符
        if (!UtilityMethod.VerifyConventionByName(i_sName))
        {
            playerCallbackData.resCodeEnum = ResCodeEnum.InvalidName;
            return playerCallbackData;
        }

        // 账号重复
        if (m_pBattlePlayerByAccounts.TryGetValue(i_sAccount, out BattlePlayer battlePlayer))
        {
            playerCallbackData.resCodeEnum = ResCodeEnum.AccountDuplication;
            return playerCallbackData;
        }

        DataTable data = Launch.DBServer.SelectData(ServerTypeEnum.eBattleServer, SqlTableName.player, "account", i_sAccount);
        if (data == null)
        {
            playerCallbackData.resCodeEnum = ResCodeEnum.ServerBusy;
            return playerCallbackData;
        }
        else if (data.Rows.Count > 0)
        {
            playerCallbackData.resCodeEnum = ResCodeEnum.AccountDuplication;
            return playerCallbackData;
        }

        long playerInstId = GetNewPlayerInstId();

        PlayerData playerData = new PlayerData()
        {
            account = i_sAccount,
            playerInstId = playerInstId,
            password = i_sPassword,
            name = i_sName,
            createtime = UtilityMethod.GetUnixTimeMilliseconds()
        };

        Dictionary<string, object> columnValues = new Dictionary<string, object>();
        columnValues.Add("account", playerData.account);
        columnValues.Add("playerinstid", playerData.playerInstId);
        columnValues.Add("playername", playerData.name);
        columnValues.Add("password", playerData.password);
        columnValues.Add("createtime", playerData.createtime);
        Launch.DBServer.InsertData(ServerTypeEnum.eBattleServer, SqlTableName.player, columnValues);

        playerCallbackData.player = CreateBattlePlayer(playerData);
        playerCallbackData.resCodeEnum = ResCodeEnum.Succeed;
        return playerCallbackData;
    }

    /// <summary>
    /// 删除战斗角色
    /// </summary>
    /// <param name="i_nPlayerId"></param>
    public void RemoveBattlePlayer(long i_nPlayerId)
    {
        m_pBattlePlayers.TryRemove(i_nPlayerId, out BattlePlayer battlePlayer);
        m_pBattlePlayerByAccounts.TryRemove(battlePlayer.GetName(), out battlePlayer);
        RoomManager.Instance.PlayerLeaveRoom(battlePlayer);
    }

    /// <summary>
    /// 创建机器人
    /// </summary>
    /// <param name="i_nRobotPlayerId"></param>
    /// <param name="i_nRoomOwnerPlayerId"></param>
    /// <returns></returns>
    public BattlePlayer CreateRobotPlayer(long i_nRobotPlayerId, long i_nRoomOwnerPlayerId)
    {
        PlayerData playerData = new PlayerData();
        playerData.playerInstId = i_nRobotPlayerId;
        BattlePlayer roomOwnerPlayer = GetBattlePlayer(i_nRoomOwnerPlayerId);
        BattlePlayer battlePlayer = new BattlePlayer();
        battlePlayer.Initializer(playerData);
        battlePlayer.IsRobot = true;
        return battlePlayer;
    }
}

/// <summary>
/// 玩家数据结构体
/// </summary>
public struct PlayerData()
{
    public string account;
    public long playerInstId;
    public string name;
    public string password;
    public long createtime;
}

/// <summary>
/// 玩家返回数据
/// </summary>
public struct PlayerCallbackData()
{
    public ResCodeEnum resCodeEnum;
    public BattlePlayer player;
}