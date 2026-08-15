using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Exposes save requests without coupling callers to the scene-owned SaveLoadSystem component.
    /// A caller cancellation token cancels only that caller's wait; it never cancels the shared save.
    /// </summary>
    public interface ISaveRequester
    {
        UniTask<bool> SaveGameAsync(CancellationToken cancellationToken = default);
        UniTask<bool> TriggerAutoSaveAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Registers asynchronous save participants explicitly across VContainer and scene hierarchy
    /// boundaries. Register and unregister must be paired by the participant's lifecycle owner.
    /// </summary>
    public interface IAsyncSaveParticipantRegistry
    {
        bool RegisterAsyncSaveParticipant(IAsyncSaveParticipant participant);
        void UnregisterAsyncSaveParticipant(IAsyncSaveParticipant participant);
    }

    /// <summary>
    /// Defines the path, label, and retention behavior for a save request kind. Adding a
    /// new request kind requires a policy implementation rather than branches in the coordinator.
    /// </summary>
    public interface ISaveRequestPolicy
    {
        SaveFileKind SaveKind { get; }
        string DisplayName { get; }
        string CreateSaveFilePath(string saveDirectoryPath, string fixedSaveFilePath, DateTime timestampUtc);
        void ApplyRetention(string saveDirectoryPath, int maximumAutoSaveCount);
    }
}
