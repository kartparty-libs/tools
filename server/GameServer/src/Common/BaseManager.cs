using System.Data;

/// <summary>
/// 管理器基类
/// </summary>
public class BaseManager<T> : Singleton<T>, IBaseManager where T : new()
{
    public ServerTypeEnum BelongServerType { get; set; }
    private int m_nCurrUpdateIntervalTime = 0;
    protected int m_nUpdateIntervalTime = 200;

    public virtual void Initializer(DataRow i_pGlobalInfo, bool i_bIsFirstOpenServer)
    {
    }

    public virtual void Start()
    {
    }

    public virtual void NotIntervalUpdate(int i_nMillisecondDelay)
    {
        m_nCurrUpdateIntervalTime += i_nMillisecondDelay;
        if (m_nCurrUpdateIntervalTime > m_nUpdateIntervalTime)
        {
            Update(m_nCurrUpdateIntervalTime);
            m_nCurrUpdateIntervalTime -= m_nUpdateIntervalTime;
        }
    }

    public virtual void Update(int i_nMillisecondDelay)
    {
    }

    public virtual void Delete()
    {
        SaveData();
    }

    public virtual void SaveData()
    {
    }
}
