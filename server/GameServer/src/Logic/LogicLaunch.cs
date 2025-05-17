using System.Data;
using System.Diagnostics;

/// <summary>
/// 核心
/// </summary>
public class LogicLaunch
{
    /// <summary>
    /// 管理器哈希列表
    /// </summary>
    private HashSet<IBaseManager> m_tManagerHashSet = new HashSet<IBaseManager>();

    public LogicLaunch() { }

    public void Register()
    {
        foreach (var item in ServerConfig.ServerOpens)
        {
            foreach (IBaseManager manager in RegisterDefine.ManagerRegister[item.Key])
            {
                manager.BelongServerType = item.Key;
                m_tManagerHashSet.Add(manager);
            }
        }
    }

    public void Initializer()
    {
        Debug.Instance.LogInfo("LogicLaunch Initializer Start");

        Register();

        foreach (var item in m_tManagerHashSet)
        {
            Debug.Instance.LogInfo($"LogicLaunch {item.GetType().Name} Initializer Start");

            DataTable data = Launch.DBServer.SelectData(item.BelongServerType, SqlTableName.globalinfo, "serverid", ServerConfig.ServerId);
            DataRow dataRow = null;
            if (data != null && data.Rows.Count > 0)
            {
                dataRow = data.Rows[0];
            }
            bool isNew = Launch.DBServer.IsNewServer(item.BelongServerType);
            item.Initializer(dataRow, isNew);

            Debug.Instance.LogInfo($"LogicLaunch {item.GetType().Name} Initializer Succeed");
        }
        Debug.Instance.LogInfo("LogicLaunch Initializer Succeed");
    }

    public void Start()
    {
        foreach (var item in m_tManagerHashSet)
        {
            item.Start();
        }
    }

    public void Update()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        long lastElapsedMilliseconds = 0;

        while (!Launch.Close)
        {
            if (Launch.Start)
            {
                long currentElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                long millisecondDelay = currentElapsedMilliseconds - lastElapsedMilliseconds;

                lastElapsedMilliseconds = currentElapsedMilliseconds;

                foreach (var item in m_tManagerHashSet)
                {
                    item.NotIntervalUpdate((int)millisecondDelay);
                }
            }

            // 控制延迟：如果 millisecondDelay 小于某个阈值，可以稍作休眠
            if (stopwatch.ElapsedMilliseconds - lastElapsedMilliseconds < 1)
            {
                Thread.Sleep(1); // 休眠1毫秒，避免忙等待
            }
        }

        stopwatch.Stop();
    }

    public void Close()
    {
        foreach (var item in m_tManagerHashSet)
        {
            item.Delete();
        }
    }
}