using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.SaveLoad
{
    public interface IGlobalSaveDocument : ISaveable
    {
        string DocumentId { get; }
        string FileName { get; }
        ISaveDataStoreProfile StorageProfile { get; }
    }

    public interface IGlobalSaveInitializer
    {
        UniTask<GlobalSaveInitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
    }

    public interface IGlobalDocumentSaveRequester
    {
        UniTask<GlobalDocumentSaveResult> SaveAsync(string documentId, CancellationToken cancellationToken = default);
    }

    public interface IGlobalSaveManager : IGlobalSaveInitializer, IGlobalDocumentSaveRequester
    {
    }

    public interface IWorldAutoSaveRequester
    {
        bool HasActiveWorld { get; }
        UniTask<WorldSaveResult> TriggerAutoSaveAsync(CancellationToken cancellationToken = default);
    }

    public interface IWorldManualSaveRequester
    {
        UniTask<WorldSaveResult> SaveManualAsync(CancellationToken cancellationToken = default);
    }

    public interface IWorldSaveQueries
    {
        string ActiveWorldId { get; }
        bool IsSaving { get; }
        IReadOnlyList<WorldSummary> GetWorlds();
        IReadOnlyList<WorldSnapshotSummary> GetSnapshots(string worldId);
    }

    public interface IWorldStartupContext : IWorldSaveQueries
    {
        UniTask<WorldCreationResult> CreateWorldAsync(string displayName, CancellationToken cancellationToken = default);
        UniTask<WorldOperationResult> OpenWorldAsync(string worldId, CancellationToken cancellationToken = default);
    }

    public interface IWorldLifecycleCommands : IWorldStartupContext
    {
        UniTask<WorldOperationResult> LoadSnapshotAsync(
            string worldId,
            string snapshotFileName,
            CancellationToken cancellationToken = default);
        UniTask<WorldOperationResult> DeleteWorldAsync(string worldId, CancellationToken cancellationToken = default);

        /// <summary>Deletes one snapshot file of a world. The world itself, and its other snapshots, are kept.</summary>
        UniTask<WorldOperationResult> DeleteSnapshotAsync(
            string worldId,
            string snapshotFileName,
            CancellationToken cancellationToken = default);

        /// <summary>Changes a world's display name. Its snapshots and last-played time are kept.</summary>
        UniTask<WorldOperationResult> RenameWorldAsync(
            string worldId,
            string displayName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ends the play session of the active world, after any save already queued. Until a world is opened or created
        /// again every save, autosaves included, is refused, so leaving gameplay can never save the torn-down scene.
        /// The world's files are kept.
        /// </summary>
        UniTask<WorldOperationResult> CloseActiveWorldAsync(CancellationToken cancellationToken = default);
    }

    public interface IWorldSaveManager :
        IWorldAutoSaveRequester,
        IWorldManualSaveRequester,
        IWorldLifecycleCommands,
        IAsyncSaveParticipantRegistry,
        IDisposable
    {
    }

    public interface IAsyncSaveParticipantRegistry
    {
        bool RegisterAsyncSaveParticipant(IAsyncSaveParticipant participant);
        void UnregisterAsyncSaveParticipant(IAsyncSaveParticipant participant);
    }

    public interface ISaveTimeProvider
    {
        DateTime GetUtcNow();
    }

}
