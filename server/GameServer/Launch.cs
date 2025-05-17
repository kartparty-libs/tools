
public class Launch
{
    /// <summary>
    /// 逻辑启动器
    /// </summary>
    public static LogicLaunch LogicLaunch = new LogicLaunch();

    /// <summary>
    /// DB服务器
    /// </summary>
    public static DBServer DBServer = new DBServer();

    /// <summary>
    /// WebSocket服务器
    /// </summary>
    public static WebSocketServer WebSocketServer = new WebSocketServer();

    /// <summary>
    /// 是否启动服务器
    /// </summary>
    public static volatile bool Start = false;

    /// <summary>
    /// 是否关闭服务器
    /// </summary>
    public static volatile bool Close = false;

    static void Main(string[] args)
    {
        Debug.Instance.LogInfo("服务器开始启动");
        ServerConfig.Initializer();
        RegisterProtocol.Register();

        // DBServer启动
        DBServer.Initializer();

        // LogicLaunch启动
        LogicLaunch.Initializer();
        Thread kernelServerUpdateThread = new Thread(() =>
        {
            LogicLaunch.Update();
        });
        kernelServerUpdateThread.Start();

        // WebSocketServer启动
        bool webSocketInitializer = false;
        foreach (var item in ServerConfig.ServerOpens)
        {
            int port = ServerConfig.GetToInt("port_websocket", item.Key);
            if (port > 0)
            {
                if (!webSocketInitializer)
                {
                    WebSocketServer.Initializer();
                    webSocketInitializer = true;
                }
                Thread webSocketServerrThread = new Thread(() =>
                {
                    WebSocketServer.RunServerAsync(port).Wait();
                });
                webSocketServerrThread.Start();
            }
        }

        Thread.Sleep(2000);

        LogicLaunch.Start();

        Debug.Instance.LogInfo("服务器启动完毕");

        Start = true;
        while (!Close)
        {
            Thread.Sleep(1000);
        };

        Debug.Instance.LogInfo("服务器开始关闭");

        // 优先断开Network链接
        WebSocketServer.Close();

        // 等待ks update完成
        kernelServerUpdateThread.Join();

        LogicLaunch.Close();
        DBServer.Close();

        Thread.Sleep(2000);

        Debug.Instance.LogInfo("服务器关闭完成");
        System.Environment.Exit(0);
    }
}