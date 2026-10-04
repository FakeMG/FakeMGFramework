using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class TargetAvailableCompletionCondition : ITutorialCompletionCondition
    {
        [SerializeField] private TutorialTargetKeySO _targetKey;

        public bool IsAlreadySatisfied(TutorialContext context)
        {
            return IsTargetAvailable(context);
        }

        public async UniTask WaitUntilSatisfiedAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => IsTargetAvailable(context), cancellationToken: cancellationToken);
        }

        private bool IsTargetAvailable(TutorialContext context)
        {
            return context.TargetRegistry.TryGet(_targetKey, out ITutorialTarget target) && target.IsAvailable;
        }
    }
}
