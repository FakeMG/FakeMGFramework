using FakeMG.Framework;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Completion condition satisfied when the player interacts with a tutorial target
    /// resolved by key, whether that target raises a custom activation event or is a plain
    /// UI Button. Works with runtime UI such as a button inside a popup.
    /// </summary>
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
