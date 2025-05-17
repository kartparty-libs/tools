using System.Data;

public interface IBaseManager
{
    public ServerTypeEnum BelongServerType { get; set; }
    public void Initializer(DataRow i_pGlobalInfo, bool i_bIsFirstOpenServer);
    public void Start();
    public void NotIntervalUpdate(int i_nMillisecondDelay);
    public void Update(int i_nMillisecondDelay);
    public void Delete();
    public void SaveData();
}