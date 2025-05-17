
using Proto;

/// <summary>
/// 注册定义
/// </summary>
public class RegisterDefine
{
    /// <summary>
    /// 管理器注册
    /// </summary>
    public static Dictionary<ServerTypeEnum, HashSet<IBaseManager>> ManagerRegister = new Dictionary<ServerTypeEnum, HashSet<IBaseManager>>()
    {
        [ServerTypeEnum.eBattleServer] = new HashSet<IBaseManager>()
        {
            WebSocketMessageManager.Instance ,
            BattlePlayerManager.Instance ,
            RoomManager.Instance ,
            SendPlayerStateManager.Instance ,
        },
    };

    /// <summary>
    /// Http路由注册
    /// </summary>
    public static HashSet<string> HttpRoutingRegister = new HashSet<string>()
    {
    };

    /// <summary>
    /// 协议注册
    /// </summary>
    public static Dictionary<ServerTypeEnum, Action> ProtocolRegister = new Dictionary<ServerTypeEnum, Action>()
    {
        [ServerTypeEnum.eBattleServer] = () => { RegisterProtocol.RegisterBattleServer(); },
    };
}