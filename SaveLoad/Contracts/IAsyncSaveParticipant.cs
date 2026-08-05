using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Extends a normal Saveable with asynchronous preparation and restoration work. Large systems
    /// use this boundary to persist external payloads before the small metadata file commits,
    /// while existing synchronous Saveable components continue working without modification.
    /// </summary>
    public interface IAsyncSaveParticipant
    {
        //TODO: manual order is fragile and tedious; consider a more robust approach to participant ordering.
        int SaveOrder { get; }

        UniTask PrepareSaveAsync(SaveOperationContext context, CancellationToken cancellationToken);

        UniTask ApplyLoadedStateAsync(LoadOperationContext context, CancellationToken cancellationToken);

        UniTask CompleteSaveAsync(SaveOperationContext context, bool didMetadataCommit, CancellationToken cancellationToken);
    }
}
