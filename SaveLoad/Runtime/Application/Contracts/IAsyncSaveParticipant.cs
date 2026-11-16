using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.SaveLoad
{
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
