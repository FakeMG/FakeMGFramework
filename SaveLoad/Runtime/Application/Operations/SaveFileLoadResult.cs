namespace FakeMG.SaveLoad
{
    public enum SaveFileLoadStatus
    {
        Success = 0,
        DefaultsAppliedBecauseMissing = 1,
        RecoveredBackup = 2,
        Missing = 3,
        Rejected = 4,
        Failed = 5,
        Cancelled = 6,
    }

    public readonly struct SaveFileLoadResult
    {
        public SaveFileLoadStatus Status { get; }
        public string FailureReason { get; }
        public string LoadedFilePath { get; }
        public bool Succeeded => Status is SaveFileLoadStatus.Success or
            SaveFileLoadStatus.DefaultsAppliedBecauseMissing or SaveFileLoadStatus.RecoveredBackup;

        public SaveFileLoadResult(
            SaveFileLoadStatus status,
            string failureReason = "",
            string loadedFilePath = "")
        {
            Status = status;
            FailureReason = failureReason ?? string.Empty;
            LoadedFilePath = loadedFilePath ?? string.Empty;
        }
    }

    public enum SaveFileWriteStatus
    {
        Success = 0,
        Failed = 1,
        Cancelled = 2,
        CommittedWithParticipantCompletionFailure = 3,
    }

    public readonly struct SaveFileWriteResult
    {
        public SaveFileWriteStatus Status { get; }
        public string FilePath { get; }
        public string FailureReason { get; }
        public bool DidCommitFile => Status is SaveFileWriteStatus.Success or
            SaveFileWriteStatus.CommittedWithParticipantCompletionFailure;
        public bool Succeeded => Status == SaveFileWriteStatus.Success;

        public SaveFileWriteResult(SaveFileWriteStatus status, string filePath, string failureReason = "")
        {
            Status = status;
            FilePath = filePath ?? string.Empty;
            FailureReason = failureReason ?? string.Empty;
        }
    }
}
