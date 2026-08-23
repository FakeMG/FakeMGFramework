using System.Collections.Generic;
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
        string ParticipantId { get; }
        // TODO: Each participant has a fixed set of dependencies, which makes it harder to reuse participants in different contexts.
        // Consider making this a method that takes the context as an argument.
        IReadOnlyCollection<string> RunsAfterParticipantIds { get; }

        UniTask PrepareSaveAsync(SaveOperationContext context, CancellationToken cancellationToken);

        UniTask ApplyLoadedStateAsync(LoadOperationContext context, CancellationToken cancellationToken);

        UniTask RollBackLoadedStateAsync(LoadOperationContext context, CancellationToken cancellationToken);

        UniTask CompleteSaveAsync(SaveOperationContext context, bool didMetadataCommit, CancellationToken cancellationToken);
    }
}
