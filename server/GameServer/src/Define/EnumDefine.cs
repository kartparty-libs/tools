/// <summary>
/// 服务器类型
/// </summary>
public enum ServerTypeEnum
{
    /// <summary>
    /// 战斗服
    /// </summary>
    eBattleServer = 0,
}

/// <summary>
/// 服务器环境枚举
/// </summary>
public enum EnvironmentEnum
{
    /// <summary>
    /// 开发环境
    /// </summary>
    development = 0,
    /// <summary>
    /// 沙箱环境
    /// </summary>
    sandbox = 1,
    /// <summary>
    /// 生产环境
    /// </summary>
    production = 2,
}

/// <summary>
/// 战斗服分组
/// </summary>
public enum BattleServerGroupEnum
{
    /// <summary>
    /// 私有战斗服分组
    /// </summary>
    ePrivate = 0,
}

/// <summary>
/// 数据库表名
/// </summary>
public static class SqlTableName
{
    public const string globalinfo = nameof(globalinfo);

    public const string player = nameof(player);
}

/// <summary>
/// 数据库操作类型
/// </summary>
public enum SqlHandleEnum
{
    eInsert,
    eUpdate,
    eDelete,
}

/// <summary>
/// 战斗服玩家状态枚举
/// </summary>
public enum BattlePlayerStateEnum
{
    /// <summary>
    /// 在线
    /// </summary>
    eOnLine = 0,
    /// <summary>
    /// 离线
    /// </summary>
    eOffLine = 1,
    /// <summary>
    /// 匹配中
    /// </summary>
    eInMatch = 2,
    /// <summary>
    /// 战斗中
    /// </summary>
    eInBattle = 3,
}

/// <summary>
/// 房间类型枚举
/// </summary>
public enum RoomTypeEnum
{
    /// <summary>
    /// 匹配比赛房间
    /// </summary>
    eMatchRoom = 0,
}