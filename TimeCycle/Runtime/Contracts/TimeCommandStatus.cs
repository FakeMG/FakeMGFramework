namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Reports how a requested time command ended.
    /// </summary>
    public enum TimeCommandStatus
    {
        Completed,
        Replaced,
        Cancelled,
        Rejected,
    }
}
