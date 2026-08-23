using System;
using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    [Serializable]
    public sealed class WorldManifest
    {
        public string WorldId;
        public string DisplayName;
        public DateTime CreatedTimestampUtc;
        public DateTime LastPlayedTimestampUtc;
    }

    public sealed class WorldSummary
    {
        public string WorldId { get; }
        public string DisplayName { get; }
        public DateTime CreatedTimestampUtc { get; }
        public DateTime LastPlayedTimestampUtc { get; }

        public WorldSummary(WorldManifest manifest)
        {
            WorldId = manifest.WorldId;
            DisplayName = manifest.DisplayName;
            CreatedTimestampUtc = manifest.CreatedTimestampUtc;
            LastPlayedTimestampUtc = manifest.LastPlayedTimestampUtc;
        }
    }

    public sealed class WorldSnapshotSummary
    {
        public string WorldId { get; }
        public string FileName { get; }
        public SaveFileKind SaveKind { get; }
        public DateTime TimestampUtc { get; }

        public WorldSnapshotSummary(ValidatedSaveFileInfo saveFileInfo)
        {
            WorldId = saveFileInfo.OwnerId;
            FileName = saveFileInfo.SaveFileName;
            SaveKind = saveFileInfo.SaveKind;
            TimestampUtc = saveFileInfo.TimestampUtc;
        }
    }

    public readonly struct WorldCreationResult
    {
        public bool Succeeded { get; }
        public string FailureReason { get; }
        public WorldSummary World { get; }

        private WorldCreationResult(bool succeeded, string failureReason, WorldSummary world)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            World = world;
        }

        public static WorldCreationResult Success(WorldSummary world)
        {
            return new WorldCreationResult(true, string.Empty, world);
        }

        public static WorldCreationResult Failure(string failureReason)
        {
            return new WorldCreationResult(false, failureReason, null);
        }
    }

    public enum WorldOperationStatus
    {
        Success = 0,
        Rejected = 1,
        Failed = 2,
        Cancelled = 3,
    }

    public readonly struct WorldOperationResult
    {
        public WorldOperationStatus Status { get; }
        public string FailureReason { get; }
        public bool Succeeded => Status == WorldOperationStatus.Success;

        private WorldOperationResult(WorldOperationStatus status, string failureReason)
        {
            Status = status;
            FailureReason = failureReason ?? string.Empty;
        }

        public static WorldOperationResult Success()
        {
            return new WorldOperationResult(WorldOperationStatus.Success, string.Empty);
        }

        public static WorldOperationResult Failure(string failureReason)
        {
            return new WorldOperationResult(WorldOperationStatus.Failed, failureReason);
        }

        public static WorldOperationResult Rejected(string failureReason)
        {
            return new WorldOperationResult(WorldOperationStatus.Rejected, failureReason);
        }

        public static WorldOperationResult Cancelled(string failureReason)
        {
            return new WorldOperationResult(WorldOperationStatus.Cancelled, failureReason);
        }
    }

    public enum WorldSaveStatus
    {
        Success = 0,
        NoActiveWorld = 1,
        SnapshotFailed = 2,
        SnapshotCommittedManifestFailed = 3,
        ParticipantCompletionFailed = 4,
        Cancelled = 5,
    }

    public readonly struct WorldSaveResult
    {
        public WorldSaveStatus Status { get; }
        public string SnapshotFilePath { get; }
        public string FailureReason { get; }
        public bool DidPersistSnapshot { get; }
        public bool Succeeded => Status == WorldSaveStatus.Success;

        public WorldSaveResult(
            WorldSaveStatus status,
            string snapshotFilePath,
            string failureReason,
            bool didPersistSnapshot)
        {
            Status = status;
            SnapshotFilePath = snapshotFilePath ?? string.Empty;
            FailureReason = failureReason ?? string.Empty;
            DidPersistSnapshot = didPersistSnapshot;
        }
    }

    public readonly struct GlobalDocumentSaveResult
    {
        public string DocumentId { get; }
        public SaveFileWriteResult FileResult { get; }
        public bool Succeeded => FileResult.Succeeded;
        public string FailureReason => FileResult.FailureReason;

        public GlobalDocumentSaveResult(string documentId, SaveFileWriteResult fileResult)
        {
            DocumentId = documentId;
            FileResult = fileResult;
        }
    }

    public readonly struct GlobalSaveInitializationResult
    {
        public bool Succeeded { get; }
        public IReadOnlyList<string> FailureReasons { get; }

        public GlobalSaveInitializationResult(bool succeeded, IReadOnlyList<string> failureReasons)
        {
            Succeeded = succeeded;
            FailureReasons = failureReasons ?? Array.Empty<string>();
        }
    }
}
