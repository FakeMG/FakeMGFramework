using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// A single check the player must satisfy for a step to complete. Steps may compose
    /// multiple required conditions that must all be satisfied together. The context lets
    /// a condition resolve runtime targets by id.
    /// </summary>
    public interface ITutorialCompletionCondition
    {
        bool IsAlreadySatisfied(TutorialContext context);
        UniTask WaitUntilSatisfiedAsync(TutorialContext context, CancellationToken cancellationToken);
    }
}
