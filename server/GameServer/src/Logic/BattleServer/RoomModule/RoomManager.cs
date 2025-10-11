using Proto;
using System.Collections.Generic;
using System.Data;

/// <summary>
/// 房间管理器
/// </summary>
public class RoomManager : BaseManager<RoomManager>
{
    /// <summary>
    /// 房间递增实例Id
    /// </summary>
    private long m_nRoomMaxInstId = 1;

    /// <summary>
    /// 房间集合
    /// </summary>
    private Dictionary<long, BaseRoom> m_pRooms = new Dictionary<long, BaseRoom>();

    /// <summary>
    /// 房间集合 基于 地图配置Id
    /// </summary>
    private Dictionary<int, List<BaseRoom>> m_pRoomsByCfgId = new Dictionary<int, List<BaseRoom>>();

    /// <summary>
    /// 房间删除列表
    /// </summary>
    private List<long> m_tRemoveRoomList = new List<long>();

    public RoomManager()
    {
    }

    public override void Initializer(DataRow i_pGlobalInfo, bool i_bIsFirstOpenServer)
    {
        base.Initializer(i_pGlobalInfo, i_bIsFirstOpenServer);
        for (int i = 1; i <= 5; i++)
        {
            CreateRoom(i);
        }
    }

    public override void Update(int i_nMillisecondDelay)
    {
        base.Update(i_nMillisecondDelay);

        foreach (var item in m_pRooms)
        {
            item.Value.Update(i_nMillisecondDelay);
            //if (item.Value.GetRoomCurrentPlayerCount() == 0)
            //{
            //    m_tRemoveRoomList.Add(item.Key);
            //}
        }

        //if (m_tRemoveRoomList.Count > 0)
        //{
        //    foreach (var instId in m_tRemoveRoomList)
        //    {
        //        RemoveRoom(instId);
        //    }
        //    m_tRemoveRoomList.Clear();
        //}
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 获取新房间实例Id
    /// </summary>
    /// <returns></returns>
    public long GetNewRoomInstId() => m_nRoomMaxInstId++;

    /// <summary>
    /// 获取房间实例
    /// </summary>
    /// <param name="i_nRoomInstId"></param>
    /// <returns></returns>
    public BaseRoom GetRoom(long i_nRoomInstId)
    {
        m_pRooms.TryGetValue(i_nRoomInstId, out BaseRoom room);
        return room;
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    /// <param name="i_nMapCfgId"></param>
    /// <returns></returns>
    public BaseRoom CreateRoom(int i_nMapCfgId)
    {
        List<BaseRoom> rooms;
        if (!m_pRoomsByCfgId.TryGetValue(i_nMapCfgId, out  rooms))
        {
            rooms = new List<BaseRoom>();
            m_pRoomsByCfgId.Add(i_nMapCfgId, rooms);
        }
        BaseRoom room = null;
        foreach (var item in rooms)
        {
            if (!item.IsMaxPlayer())
            {
                room = item;
                break;
            }
        }
        if (room != null)
        {
            return room;
        }
        long roomInstId = GetNewRoomInstId();
        room = new MatchRoom(roomInstId);
        room.Initializer(i_nMapCfgId);

        m_pRooms.TryAdd(roomInstId, room);
        rooms.Add(room);
        return room;
    }

    public void RemoveRoom(long i_nRoomInstId)
    {
        if (m_pRooms.TryGetValue(i_nRoomInstId, out BaseRoom room))
        {
            m_pRooms.Remove(i_nRoomInstId);
        }
    }

    /// <summary>
    /// 请求加入房间
    /// </summary>
    /// <param name="i_nMapCfgId"></param>
    /// <param name="i_pBattlePlayer"></param>
    public void PlayerEnterRoom(int i_nMapCfgId, BattlePlayer i_pBattlePlayer)
    {
        BaseRoom room = CreateRoom(i_nMapCfgId);
        room?.PlayerEnterRoom(i_pBattlePlayer);
    }

    /// <summary>
    /// 请求离开房间
    /// </summary>
    /// <param name="i_pBattlePlayer"></param>
    public void PlayerLeaveRoom(BattlePlayer i_pBattlePlayer)
    {
        GetRoom(i_pBattlePlayer.RoomInstId)?.PlayerLeaveRoom(i_pBattlePlayer);
    }

    /// <summary>
    /// 玩家加载地图完成
    /// </summary>
    public void PlayerLoadMapComplete(BattlePlayer i_pBattlePlayer)
    {
        GetRoom(i_pBattlePlayer.RoomInstId)?.PlayerLoadMapComplete(i_pBattlePlayer);
    }

    /// <summary>
    /// 玩家状态同步
    /// </summary>
    public void PlayerStateSync(BattlePlayer i_pBattlePlayer, BattlePlayerStateData i_pBattlePlayerStateData)
    {
        GetRoom(i_pBattlePlayer.RoomInstId)?.PlayerStateSync(i_pBattlePlayer, i_pBattlePlayerStateData);
    }

    /// <summary>
    /// 广播地图玩家
    /// </summary>
    public void SendPlayer(BattlePlayer i_pBattlePlayer, string i_sAccount, bool i_bIsIgnoreSelf, ResMsgClientData i_pResMsgClientData)
    {
        if (i_sAccount.Equals(""))
        {
            if (i_bIsIgnoreSelf)
            {
                GetRoom(i_pBattlePlayer.RoomInstId)?.SendAllPlayerToClient(i_pResMsgClientData, i_pBattlePlayer.GetPlayerInstId());
            }
            else
            {
                GetRoom(i_pBattlePlayer.RoomInstId)?.SendAllPlayerToClient(i_pResMsgClientData);
            }
        }
        else
        {
            if (!i_bIsIgnoreSelf)
            {
                GetRoom(i_pBattlePlayer.RoomInstId)?.SendPlayerToClient(i_pResMsgClientData, i_pBattlePlayer.GetAccount());
            }
            GetRoom(i_pBattlePlayer.RoomInstId)?.SendPlayerToClient(i_pResMsgClientData, i_sAccount);
        }
    }
}