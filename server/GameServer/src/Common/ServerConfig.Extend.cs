
using Microsoft.Extensions.Configuration;

public partial class ServerConfig
{
    /// <summary>
    /// 服务器Id
    /// </summary>
    public static int ServerId;

    /// <summary>
    /// 服务器环境
    /// </summary>
    public static EnvironmentEnum Environment = EnvironmentEnum.development;

    /// <summary>
    /// 是否输出普通日志
    /// </summary>
    public static bool IsDebug = true;

    /// <summary>
    /// 扩展服务器配置
    /// </summary>
    private static Dictionary<ServerTypeEnum, IConfigurationRoot> m_pServerConfigExtends = new Dictionary<ServerTypeEnum, IConfigurationRoot>();

    /// <summary>
    /// 服务器启动列表
    /// </summary>
    public static Dictionary<ServerTypeEnum, string> ServerOpens = new Dictionary<ServerTypeEnum, string>();

    /// <summary>
    /// 服务器向服务器请求URL
    /// </summary>
    public static Dictionary<ServerTypeEnum, string> StoSURL = new Dictionary<ServerTypeEnum, string>();

    /// <summary>
    /// 客户端向服务器请求URL
    /// </summary>
    public static Dictionary<ServerTypeEnum, string> CtoSURL = new Dictionary<ServerTypeEnum, string>();

    /// <summary>
    /// 战斗服务器分组
    /// </summary>
    public static Dictionary<BattleServerGroupEnum, List<string[]>> BattleServerURL = new Dictionary<BattleServerGroupEnum, List<string[]>>();

    public static void InitializeExtend()
    {
        Environment = (EnvironmentEnum)GetToInt("environment");
        Debug.Instance.LogInfo($"ServerConfig Initializer Environment -> {Environment.ToString()}");

        m_pServerConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine("config", "ServerConfig.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine("config", $"serverconfig_{Environment.ToString()}", "ServerConfig.json"), optional: false, reloadOnChange: true)
            .Build();

        ServerId = GetToInt("serverid");
        Debug.Instance.LogInfo($"ServerConfig Initializer ServerId -> {ServerId}");

        IsDebug = GetToBoolean("isdebug");
        Debug.Instance.LogInfo($"ServerConfig Initializer IsDebug -> {IsDebug.ToString()}");

        Dictionary<string, string[]> serverOpens = GetValue<Dictionary<string, string[]>>("server_open");
        Dictionary<ServerTypeEnum, string> opens = new Dictionary<ServerTypeEnum, string>();
        Dictionary<ServerTypeEnum, string> stos = new Dictionary<ServerTypeEnum, string>();
        Dictionary<ServerTypeEnum, string> ctos = new Dictionary<ServerTypeEnum, string>();
        Dictionary<ServerTypeEnum, IConfigurationRoot> serverConfigExtends = new Dictionary<ServerTypeEnum, IConfigurationRoot>();

        foreach (var item in serverOpens)
        {
            ServerTypeEnum serverTypeEnum = (ServerTypeEnum)Convert.ToInt32(item.Key);
            if (item.Value[3] == "1")
            {
                IConfigurationRoot configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(Path.Combine("config", $"serverconfig_{Environment.ToString()}", item.Value[0]), optional: false, reloadOnChange: true)
                    .Build();
                serverConfigExtends.Add(serverTypeEnum, configurationBuilder);

                opens.Add(serverTypeEnum, item.Value[1]);
            }
            stos.Add(serverTypeEnum, item.Value[1]);
            ctos.Add(serverTypeEnum, item.Value[2]);

            Debug.Instance.LogInfo($"ServerConfig Initializer Server -> {item.Value[0]}");
            Debug.Instance.LogInfo($"ServerConfig Initializer StoSURL -> {item.Value[1]}");
            Debug.Instance.LogInfo($"ServerConfig Initializer CtoSURL -> {item.Value[2]}");

            lock (ServerOpens)
            {
                ServerOpens = new Dictionary<ServerTypeEnum, string>(opens);
            }
            lock (StoSURL)
            {
                StoSURL = new Dictionary<ServerTypeEnum, string>(stos);
            }
            lock (CtoSURL)
            {
                CtoSURL = new Dictionary<ServerTypeEnum, string>(ctos);
            }
            lock (m_pServerConfigExtends)
            {
                m_pServerConfigExtends = new Dictionary<ServerTypeEnum, IConfigurationRoot>(serverConfigExtends);
            }
        }

        Dictionary<string, List<string[]>> battleServerUrl = GetValue<Dictionary<string, List<string[]>>>("battle_server_url");
        Dictionary<BattleServerGroupEnum, List<string[]>> battleServerUrls = new Dictionary<BattleServerGroupEnum, List<string[]>>();
        if (battleServerUrl != null)
        {
            foreach (var item in battleServerUrl)
            {
                BattleServerGroupEnum battleServerGroupEnum = (BattleServerGroupEnum)Convert.ToInt32(item.Key);
                battleServerUrls.Add(battleServerGroupEnum, new List<string[]>(item.Value));
                lock (BattleServerURL)
                {
                    BattleServerURL = new Dictionary<BattleServerGroupEnum, List<string[]>>(battleServerUrls);
                }
            }
        }
    }

    public static string GetStoSURL(ServerTypeEnum serverTypeEnum)
    {
        if (StoSURL.TryGetValue(serverTypeEnum, out string url))
        {
            return url;
        }
        return "";
    }


    public static string GetCtoSURL(ServerTypeEnum serverTypeEnum)
    {
        if (CtoSURL.TryGetValue(serverTypeEnum, out string url))
        {
            return url;
        }
        return "";
    }

    public static int GetToInt(string key, ServerTypeEnum serverTypeEnum)
    {
        if (m_pServerConfigExtends.TryGetValue(serverTypeEnum, out IConfigurationRoot config))
        {
            return config.GetSection(key).Get<int>();
        }
        return default;
    }
    public static string GetToString(string key, ServerTypeEnum serverTypeEnum)
    {
        if (m_pServerConfigExtends.TryGetValue(serverTypeEnum, out IConfigurationRoot config))
        {
            return config[key];
        }
        return default;
    }
    public static bool GetToBoolean(string key, ServerTypeEnum serverTypeEnum)
    {
        if (m_pServerConfigExtends.TryGetValue(serverTypeEnum, out IConfigurationRoot config))
        {
            return config.GetSection(key).Get<bool>();
        }
        return false;
    }
    public static int[] GetToIntArray(string key, ServerTypeEnum serverTypeEnum)
    {
        if (m_pServerConfigExtends.TryGetValue(serverTypeEnum, out IConfigurationRoot config))
        {
            return config.GetSection(key).Get<int[]>();
        }
        return default;
    }
    public static string[] GetToStringArray(string key, ServerTypeEnum serverTypeEnum)
    {
        if (m_pServerConfigExtends.TryGetValue(serverTypeEnum, out IConfigurationRoot config))
        {
            return config.GetSection(key).Get<string[]>();
        }
        return default;
    }
    public static T0 GetValue<T0>(string key, ServerTypeEnum serverTypeEnum)
    {
        if (m_pServerConfigExtends.TryGetValue(serverTypeEnum, out IConfigurationRoot config))
        {
            return config.GetSection(key).Get<T0>();
        }
        return default;
    }
}