namespace FakeMG.TimeCycle
{
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
