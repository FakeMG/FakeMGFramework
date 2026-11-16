using FakeMG.Framework;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class TargetClickCompletionCondition : ITutorialCompletionCondition
    {
        [SerializeField] private TutorialTargetKeySO _targetKey;

        public bool IsAlreadySatisfied(TutorialContext context) => false;

        public async UniTask WaitUntilSatisfiedAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            if (!context.TargetRegistry.TryGet(_targetKey, out ITutorialTarget target))
            {
                Echo.Error($"Tutorial completion target '{(_targetKey == null ? "<none>" : _targetKey.name)}' is not registered; the step cannot complete on it.");
                await UniTask.Never(cancellationToken);
                return;
            }

            var completionSource = new UniTaskCompletionSource();
            TutorialTargetClickSubscription subscription = TutorialTargetClickSubscription.Subscribe(target, CompleteWhenClicked);

            if (!subscription.IsSubscribed)
            {
                Echo.Error($"Tutorial completion target '{_targetKey.name}' exposes no activation event or Button to complete on.");
                await UniTask.Never(cancellationToken);
                return;
            }

            try
            {
                await completionSource.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                subscription.Unsubscribe();
            }

            void CompleteWhenClicked()
            {
                completionSource.TrySetResult();
            }
        }
    }
}
