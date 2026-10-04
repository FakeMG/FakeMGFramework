using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Per-step force-complete signal. The runner arms a fresh signal at the start of
    /// every step; a debug tool trips it to complete the current step as if the player
    /// satisfied it. Steps await <see cref="WaitAsync"/> alongside their real conditions.
    /// </summary>
    public sealed class TutorialForceCompleteSignal
    {
        private UniTaskCompletionSource _source = new();

        public void ArmForStep()
        {
            _source = new UniTaskCompletionSource();
        }

        public void ForceCompleteCurrentStep()
        {
            _source.TrySetResult();
        }

        public async UniTask WaitAsync(CancellationToken cancellationToken)
        {
            await _source.Task.AttachExternalCancellation(cancellationToken);
        }
    }
}
