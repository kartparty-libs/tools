
using System.Numerics;
using Proto;

/// <summary>
/// 匹配比赛房间
/// </summary>
public class MatchRoom : BaseRoom
{
    private MapGrid m_pMapGrid;
    public MatchRoom(long i_nInstId) : base(i_nInstId)
    {
        m_pMapGrid = new();
    }

    /// <summary>
    /// 玩家离开房间
    /// </summary>
    /// <param name="i_pBattlePlayer"></param>
    /// <param name="i_bIsKick"></param>
    public override void PlayerLeaveRoom(BattlePlayer i_pBattlePlayer, bool i_bIsKick = false)
    {
        m_pMapGrid.Remove(i_pBattlePlayer.GetPlayerInstId());
        base.PlayerLeaveRoom(i_pBattlePlayer, i_bIsKick);
    }

    /// <summary>
    /// 房间数据改变通知
    /// </summary>
    //public override void OnChangeRoomData()
    //{
    //    if (m_tPlayers.Count == 0)
    //    {
    //        return;
    //    }
    //    foreach (var player in m_tPlayers)
    //    {
    //        ResMsgClientData resMsgClientData = new ResMsgClientData();
    //        RoomData roomData = GetRoomData();

    //        roomData.Players.Clear();
    //        roomData.Players.AddRange(m_pMapGrid.Get(player.GetPlayerInstId()));
    //        ResMsgRoomDataChange resMsgBodyRoomDataChange = new ResMsgRoomDataChange()
    //        {
    //            RoomData = GetRoomData(),
    //        };
    //        resMsgClientData.AddMessageData(resMsgBodyRoomDataChange);
    //        player.SendToClient(i_pResponseMessageData);
    //    }
    //}

    /// <summary>
    /// 玩家状态同步 覆写
    /// </summary>
    public override void PlayerStateSync(BattlePlayer i_pBattlePlayer, BattlePlayerStateData i_pBattlePlayerStateData)
    {
        i_pBattlePlayer.SetLastBattlePlayerStateData(i_pBattlePlayerStateData, true);
    }

    //public override void SendResMsgClientData(BattlePlayer i_pBattlePlayer, ResMsgClientData i_pResMsgClientData, BattlePlayerStateData i_pBattlePlayerStateData)
    //{
    //    List<long> players = m_pMapGrid.Add(i_pBattlePlayerStateData.Position.X, i_pBattlePlayerStateData.Position.Y, i_pBattlePlayer.GetPlayerInstId());
    //    if (players != null)
    //    {
    //        foreach (var instId in players)
    //        {
    //            BattlePlayer player = BattlePlayerManager.Instance.GetBattlePlayer(instId);
    //            if (i_pBattlePlayer.GetPlayerInstId() != instId)
    //            {
    //                player.SendToClient(i_pResMsgClientData);
    //            }
    //        }
    //    }
    //}
    public override void UpdatePlayerStateSync(BattlePlayer i_pBattlePlayer)
    {
        var BattlePlayerStateData = i_pBattlePlayer.GetLastBattlePlayerStateData();
        if (BattlePlayerStateData == null)
        {
            return;
        }
        long myInstId = i_pBattlePlayer.GetPlayerInstId();
        int oldGridId = m_pMapGrid.GetGridIdByInstId(myInstId);
        HashSet<long> players = m_pMapGrid.Add(BattlePlayerStateData.B[0], BattlePlayerStateData.B[1], myInstId).ToHashSet();
        int newGridId = m_pMapGrid.GetGridIdByInstId(myInstId);
        if (oldGridId != newGridId)
        {
            if (players != null)
            {
                ResMsgClientData resMsgClientData1 = new ResMsgClientData();
                ResMsgPlayerEnterAoi resMsgPlayerEnterAoi1 = new ResMsgPlayerEnterAoi();
                resMsgClientData1.AddMessageData(resMsgPlayerEnterAoi1);
                resMsgPlayerEnterAoi1.A.Add(i_pBattlePlayer.GetBattlePlayerData());
                resMsgPlayerEnterAoi1.B.Add(BattlePlayerStateData);

                ResMsgClientData resMsgClientData2 = new ResMsgClientData();
                ResMsgPlayerEnterAoi resMsgPlayerEnterAoi2 = new ResMsgPlayerEnterAoi();
                resMsgClientData2.AddMessageData(resMsgPlayerEnterAoi2);

                foreach (var instId in players)
                {
                    if (instId != myInstId && !i_pBattlePlayer.AoiPlayers.Contains(instId))
                    {
                        // 通知其他玩家 我进入了他的视野
                        BattlePlayer player = BattlePlayerManager.Instance.GetBattlePlayer(instId);
                        if (player != null)
                        {
                            //Debug.Instance.Log($"{myInstId} 通知 {instId} 进入了他的视野");
                            player.SendToClient(resMsgClientData1);

                            resMsgPlayerEnterAoi2.A.Add(player.GetBattlePlayerData());
                            resMsgPlayerEnterAoi2.B.Add(player.GetLastBattlePlayerStateData());
                        }
                    }
                }

                // 通知我 视野进入了这些玩家
                if (resMsgPlayerEnterAoi2.A.Count > 0)
                {
                    i_pBattlePlayer.SendToClient(resMsgClientData2);
                    foreach (var item in resMsgPlayerEnterAoi2.A)
                    {
                        //Debug.Instance.Log($"{myInstId} 视野进入玩家 {item.PlayerInstId}");
                    }
                }
            }

            ResMsgClientData resMsgClientData3 = new ResMsgClientData();
            ResMsgPlayerLeaveAoi resMsgPlayerLeaveAoi3 = new ResMsgPlayerLeaveAoi();
            resMsgClientData3.AddMessageData(resMsgPlayerLeaveAoi3);
            resMsgPlayerLeaveAoi3.A.Add(myInstId);

            ResMsgClientData resMsgClientData4 = new ResMsgClientData();
            ResMsgPlayerLeaveAoi resMsgPlayerLeaveAoi4 = new ResMsgPlayerLeaveAoi();
            resMsgClientData4.AddMessageData(resMsgPlayerLeaveAoi4);

            foreach (var instId in i_pBattlePlayer.AoiPlayers)
            {
                if (instId != myInstId && (players == null || !players.Contains(instId)))
                {
                    // 通知其他玩家 我离开了他的视野
                    BattlePlayer player = BattlePlayerManager.Instance.GetBattlePlayer(instId);
                    if (player != null)
                    {
                        //Debug.Instance.Log($"{myInstId} 通知 {instId} 离开了他的视野");
                        player.SendToClient(resMsgClientData3);

                        resMsgPlayerLeaveAoi4.A.Add(player.GetPlayerInstId());
                    }
                }
            }

            // 通知我 视野离开了这些玩家
            if (resMsgPlayerLeaveAoi4.A.Count > 0)
            {
                i_pBattlePlayer.SendToClient(resMsgClientData4);

                foreach (var item in resMsgPlayerLeaveAoi4.A)
                {
                    //Debug.Instance.Log($"{myInstId} 视野离开玩家 {item}");
                }
            }

            i_pBattlePlayer.AoiPlayers.Clear();
            if (players != null)
            {
                i_pBattlePlayer.AoiPlayers = players;
            }
        }

        ResMsgClientData resMsgClientData5 = new ResMsgClientData();
        ResMsgPlayerStateSync resMsgBodyPlayerStateSync5 = new ResMsgPlayerStateSync();
        if (players != null)
        {
            foreach (var instId in players)
            {
                BattlePlayer player = BattlePlayerManager.Instance.GetBattlePlayer(instId);
                if (i_pBattlePlayer.GetPlayerInstId() != instId)
                {
                    BattlePlayerStateData battlePlayerStateData = player.GetLastBattlePlayerStateData();
                    if (battlePlayerStateData != null && player.IsChangeLastBattlePlayerStateData())
                    {
                        resMsgBodyPlayerStateSync5.BattlePlayerStateDatas.Add(battlePlayerStateData);
                    }
                }
            }
            if (resMsgBodyPlayerStateSync5.BattlePlayerStateDatas.Count > 0)
            {
                resMsgClientData5.AddMessageData(resMsgBodyPlayerStateSync5);
                i_pBattlePlayer.SendToClient(resMsgClientData5);
            }
        }
    }
}