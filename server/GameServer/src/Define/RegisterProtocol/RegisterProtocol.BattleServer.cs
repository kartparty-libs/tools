using DotNetty.Transport.Channels;
using Google.Protobuf;
using Proto;

public static partial class RegisterProtocol
{
    /// <summary>
    /// 战斗服协议注册
    /// </summary>
    public static void RegisterBattleServer()
    {
        AddProtocol(typeof(ReqMsgPing), Ping, ReqMsgPing.Parser.ParseFrom);
        AddProtocol(typeof(ReqMsgReconnect), Reconnect, ReqMsgReconnect.Parser.ParseFrom);

        AddProtocol(typeof(ReqMsgLoginBattlePlayer), LoginPlayer, ReqMsgLoginBattlePlayer.Parser.ParseFrom);
        AddProtocol(typeof(ReqMsgCreateBattlePlayer), CreateBattlePlayer, ReqMsgCreateBattlePlayer.Parser.ParseFrom);
        AddProtocol(typeof(ReqMsgPlayerEnterRoom), PlayerEnterRoom, ReqMsgPlayerEnterRoom.Parser.ParseFrom);
        AddProtocol(typeof(ReqMsgPlayerLeaveRoom), PlayerLeaveRoom, ReqMsgPlayerLeaveRoom.Parser.ParseFrom);

        AddProtocol(typeof(ReqMsgPlayerLoadMapComplete), PlayerLoadMapComplete, ReqMsgPlayerLoadMapComplete.Parser.ParseFrom);
        AddProtocol(typeof(ReqMsgPlayerStateSync), PlayerStateSync, ReqMsgPlayerStateSync.Parser.ParseFrom);

        AddProtocol(typeof(ReqMsgSendPlayer), SendPlayer, ReqMsgSendPlayer.Parser.ParseFrom);

    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    private static ResMsgPing ResMsgPing = new ResMsgPing();
    /// <summary>
    /// Ping
    /// </summary>
    private static void Ping(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        resMsgClientData.AddMessageData(ResMsgPing);
        WebSocketJsonResponse(context, resMsgClientData);
    }

    /// <summary>
    /// 断线重连
    /// </summary>
    private static void Reconnect(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();

        ResMsgReconnect resMsgReconnect = new ResMsgReconnect();

        BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(msgServerHeader.PlayerInstId, context);
        if (battlePlayer != null)
        {
            if (battlePlayer.RoomInstId != 0)
            {
                BaseRoom room = RoomManager.Instance.GetRoom(battlePlayer.RoomInstId);
                if (room != null)
                {
                    resMsgReconnect.RoomData = room.GetRoomData();
                }
            }
        }

        resMsgClientData.AddMessageData(resMsgReconnect);
        WebSocketJsonResponse(context, resMsgClientData);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 登入玩家
    /// </summary>
    private static void LoginPlayer(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        if (!(reqMsg is ReqMsgLoginBattlePlayer rqMsgLoginBattlePlayer))
        {
            return;
        }
        PlayerCallbackData playerCallbackData = BattlePlayerManager.Instance.LoginBattlePlayer(rqMsgLoginBattlePlayer.Account, rqMsgLoginBattlePlayer.Password, context);
        ResMsgLoginBattlePlayer resMsgLoginBattlePlayer = new ResMsgLoginBattlePlayer()
        {
            ResCodeEnum = playerCallbackData.resCodeEnum,
            BattlePlayerData = playerCallbackData.player?.GetBattlePlayerData(),
        };
        resMsgClientData.AddMessageData(resMsgLoginBattlePlayer);
        WebSocketJsonResponse(context, resMsgClientData);
    }

    /// <summary>
    /// 创建战斗服角色
    /// </summary>
    private static void CreateBattlePlayer(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        if (!(reqMsg is ReqMsgCreateBattlePlayer reqMsgCreateBattlePlayer))
        {
            return;
        }

        PlayerCallbackData playerCallbackData = BattlePlayerManager.Instance.CreateNewBattlePlayer(reqMsgCreateBattlePlayer.Account, reqMsgCreateBattlePlayer.Password, reqMsgCreateBattlePlayer.PlayerName);
        ResMsgCreateBattlePlayer resMsgCreateBattlePlayer = new ResMsgCreateBattlePlayer()
        {
            ResCodeEnum = playerCallbackData.resCodeEnum,
            BattlePlayerData = playerCallbackData.player?.GetBattlePlayerData()
        };
        resMsgClientData.AddMessageData(resMsgCreateBattlePlayer);
        WebSocketJsonResponse(context, resMsgClientData);
    }

    /// <summary>
    /// 请求加入房间
    /// </summary>
    private static void PlayerEnterRoom(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        if (!(reqMsg is ReqMsgPlayerEnterRoom reqMsgPlayerEnterRoom))
        {
            return;
        }

        BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(msgServerHeader.PlayerInstId, context);
        if (battlePlayer != null)
        {
            RoomManager.Instance.PlayerEnterRoom(reqMsgPlayerEnterRoom.MapCfgId, battlePlayer);
        }
    }

    /// <summary>
    /// 请求离开房间
    /// </summary>
    private static void PlayerLeaveRoom(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        if (!(reqMsg is ReqMsgPlayerLeaveRoom reqMsgPlayerLeaveRoom))
        {
            return;
        }

        BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(msgServerHeader.PlayerInstId, context);
        if (battlePlayer != null)
        {
            RoomManager.Instance.PlayerLeaveRoom(battlePlayer);
        }
    }

    /// <summary>
    /// 玩家加载地图完成通知
    /// </summary>
    private static void PlayerLoadMapComplete(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        if (!(reqMsg is ReqMsgPlayerLoadMapComplete reqMsgPlayerLoadMapComplete))
        {
            return;
        }

        BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(msgServerHeader.PlayerInstId, context);
        if (battlePlayer != null)
        {
            RoomManager.Instance.PlayerLoadMapComplete(battlePlayer);
        }
    }

    /// <summary>
    /// 玩家状态同步
    /// </summary>
    private static void PlayerStateSync(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        if (!(reqMsg is ReqMsgPlayerStateSync reqMsgPlayerStateSync))
        {
            return;
        }

        BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(msgServerHeader.PlayerInstId, context);
        if (battlePlayer != null)
        {
            RoomManager.Instance.PlayerStateSync(battlePlayer, reqMsgPlayerStateSync.BattlePlayerStateData);
        }
    }

    /// <summary>
    /// 广播地图玩家
    /// </summary>
    private static void SendPlayer(IChannelHandlerContext context, MsgServerHeader msgServerHeader, IMessage reqMsg)
    {
        ResMsgClientData resMsgClientData = new ResMsgClientData();
        if (!(reqMsg is ReqMsgSendPlayer reqMsgSendPlayer))
        {
            return;
        }

        BattlePlayer battlePlayer = BattlePlayerManager.Instance.GetBattlePlayer(msgServerHeader.PlayerInstId, context);
        if (battlePlayer != null)
        {
            ResMsgSendPlayer resMsgSendPlayer = new ResMsgSendPlayer();
            resMsgSendPlayer.Json = reqMsgSendPlayer.Json;
            resMsgClientData.AddMessageData(resMsgSendPlayer);
            RoomManager.Instance.SendPlayer(battlePlayer, reqMsgSendPlayer.Account, reqMsgSendPlayer.IsIgnoreSelf, resMsgClientData);
        }
    }
}