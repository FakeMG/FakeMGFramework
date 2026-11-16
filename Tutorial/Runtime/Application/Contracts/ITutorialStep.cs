using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
    public interface ITutorialStep
    {
        StepId Id { get; }
        UniTask<TutorialStepResult> ExecuteAsync(TutorialContext context, CancellationToken cancellationToken);
    }
}
