
using Microsoft.Extensions.Configuration;

public partial class ServerConfig
{
    /// <summary>
    /// 服务器配置
    /// </summary>
    private static IConfigurationRoot m_pServerConfig;

    public static void Initializer()
    {
        m_pServerConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine("config", "ServerConfig.json"), optional: false, reloadOnChange: true)
            .Build();

        Debug.Instance.LogInfo("ServerConfig Initializer ServerConfig.json");

        InitializeExtend();
    }

    public static int GetToInt(string key)
    {
        return m_pServerConfig.GetSection(key).Get<int>();
    }
    public static string GetToString(string key)
    {
        return m_pServerConfig[key];
    }
    public static bool GetToBoolean(string key)
    {
        return m_pServerConfig.GetSection(key).Get<bool>();
    }
    public static int[] GetToIntArray(string key)
    {
        return m_pServerConfig.GetSection(key).Get<int[]>();
    }
    public static string[] GetToStringArray(string key)
    {
        return m_pServerConfig.GetSection(key).Get<string[]>();
    }
    public static T0 GetValue<T0>(string key)
    {
        return m_pServerConfig.GetSection(key).Get<T0>();
    }
}