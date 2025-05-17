using Proto;

/// <summary>
/// 战斗服玩家
/// 房间相关数据
/// </summary>
public partial class BattlePlayer
{
    /// <summary>
    /// 房间实例Id
    /// </summary>
    public long RoomInstId = 0;

    /// <summary>
    /// 是否加载地图完成
    /// </summary>
    public bool IsLoadMapComplete = false;

    /// <summary>
    /// 是否是机器人
    /// </summary>
    public bool IsRobot = false;

    /// <summary>
    /// 视野内的玩家
    /// </summary>
    public HashSet<long> AoiPlayers = new HashSet<long>();

    public void InitializerRoom(PlayerData i_pPlayerData)
    {
    }

    /// <summary>
    /// 加入房间通知
    /// </summary>
    /// <param name="i_sRoomCode"></param>
    public void OnPlayerEnterRoom(BaseRoom i_pRoom)
    {
        if (RoomInstId > 0)
        {
            BaseRoom room = RoomManager.Instance.GetRoom(RoomInstId);
            if (room != null)
            {
                room.PlayerLeaveRoom(this);
            }
        }
        RoomInstId = i_pRoom.GetInstId();

        if (IsRobot)
        {
            IsLoadMapComplete = true;
        }
        else
        {
            IsLoadMapComplete = false;

            ResMsgClientData resMsgClientData = new ResMsgClientData();
            ResMsgPlayerEnterRoom resMsgPlayerEnterRoom = new ResMsgPlayerEnterRoom()
            {
                RoomData = i_pRoom.GetRoomData()
            };
            resMsgClientData.AddMessageData(resMsgPlayerEnterRoom);
            SendToClient(resMsgClientData);
        }
    }

    /// <summary>
    /// 离开房间通知
    /// </summary>
    public void OnPlayerLeaveRoom(bool i_bIsKick = false)
    {
        IsLoadMapComplete = false;
        RoomInstId = 0;
        m_pLastBattlePlayerStateData = null;
        m_bIsChangeLastBattlePlayerStateData = false;
        if (AoiPlayers.Count > 0)
        {
            ResMsgClientData resMsgClientData1 = new ResMsgClientData();
            ResMsgPlayerLeaveAoi resMsgPlayerLeaveAoi = new ResMsgPlayerLeaveAoi();
            resMsgPlayerLeaveAoi.A.Add(m_nPlayerInstId);
            resMsgClientData1.AddMessageData(resMsgPlayerLeaveAoi);
            foreach (var instId in AoiPlayers)
            {
                BattlePlayer player = BattlePlayerManager.Instance.GetBattlePlayer(instId);
                player?.SendToClient(resMsgClientData1);
            }
            AoiPlayers.Clear();
        }

        ResMsgClientData resMsgClientData2 = new ResMsgClientData();
        ResMsgPlayerLeaveRoom resMsgPlayerEnterRoom = new ResMsgPlayerLeaveRoom();
        resMsgClientData2.AddMessageData(resMsgPlayerEnterRoom);
        SendToClient(resMsgClientData2);
    }

    /// <summary>
    /// 玩家加载地图完成通知
    /// </summary>
    public void OnPlayerLoadMapComplete()
    {
        IsLoadMapComplete = true;
    }
}