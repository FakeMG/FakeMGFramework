using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// A code-defined unit of tutorial behavior. Running the step validates required
    /// dependencies, activates modules, waits for completion, and cleans up, returning
    /// the outcome (completed, completed-with-branch, or skipped).
    /// </summary>
    public interface ITutorialStep
    {
        StepId Id { get; }
        UniTask<TutorialStepResult> ExecuteAsync(TutorialContext context, CancellationToken cancellationToken);
    }
}
