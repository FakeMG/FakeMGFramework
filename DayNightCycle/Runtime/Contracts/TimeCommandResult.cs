namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Carries the final status of one time command request.
    /// </summary>
    public readonly struct TimeCommandResult
    {
        public long CommandId { get; }
        public TimeCommandStatus Status { get; }

        public TimeCommandResult(long commandId, TimeCommandStatus status)
        {
            CommandId = commandId;
            Status = status;
        }
    }
}
