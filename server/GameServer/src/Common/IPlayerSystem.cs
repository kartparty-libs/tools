using Google.Protobuf;

public interface IPlayerSystem
{
    public void Initializer(bool i_bIsNewPlayer);
    public void DayRefresh();
    public void SaveData();
    public void Update(int i_nMillisecondDelay);
    public void Delete();
    public IMessage GetResMsgBody();
    public void OnHandle(long i_nCurrTime, long i_nLastHandleTime, long i_nIntervalTime);
    public bool IsChangeData();
    public void OnChangeData();
    public void OnNewSeason(int i_nSeasonId);
}