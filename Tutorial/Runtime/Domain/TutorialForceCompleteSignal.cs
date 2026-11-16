using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
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
