
using System.Data;

public class SendPlayerStateManager : BaseManager<SendPlayerStateManager>
{
    public SendPlayerStateManager()
    {
        m_nUpdateIntervalTime = 100;
    }

    public override void Initializer(DataRow i_pGlobalInfo, bool i_bIsFirstOpenServer)
    {
        base.Initializer(i_pGlobalInfo, i_bIsFirstOpenServer);
    }

    public override void Update(int i_nMillisecondDelay)
    {
        base.Update(i_nMillisecondDelay);

        //Debug.Instance.LogWarn($"Start UpdatePlayerStateSync");
        BattlePlayerManager.Instance.UpdatePlayerStateSync();
        //Debug.Instance.LogWarn($"End UpdatePlayerStateSync  {i_nMillisecondDelay} ");
        //GC.Collect();
    }
}