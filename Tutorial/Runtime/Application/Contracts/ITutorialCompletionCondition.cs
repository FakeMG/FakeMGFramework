using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
    public interface ITutorialCompletionCondition
    {
        bool IsAlreadySatisfied(TutorialContext context);
        UniTask WaitUntilSatisfiedAsync(TutorialContext context, CancellationToken cancellationToken);
    }
}
