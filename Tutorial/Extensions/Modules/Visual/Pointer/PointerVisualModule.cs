using FakeMG.Framework;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Shows a pointer aimed at a UI target resolved by key, so it can point at runtime UI
    /// such as a button inside a popup. The pointer hides as soon as the player interacts
    /// with the target, whichever click source the target exposes.
    /// </summary>
    [Serializable]
    public sealed class PointerVisualModule : TutorialVisualModuleBase<TutorialPointerView>
    {
        [SerializeField] private TutorialTargetKeySO _targetKey;

        private TutorialTargetClickSubscription _clickSubscription;

        protected override void ConfigureView(TutorialPointerView view, TutorialContext context)
        {
            if (!context.TargetRegistry.TryGet(_targetKey, out ITutorialTarget target))
            {
                throw new InvalidOperationException(
                    $"Tutorial pointer target '{(_targetKey == null ? "<none>" : _targetKey.name)}' is not registered.");
            }

            view.PointAt(ResolveTargetRect(context, _targetKey), context.VisualRoot);

            _clickSubscription = TutorialTargetClickSubscription.Subscribe(target, HidePointerWhenTargetActivated);
            if (!_clickSubscription.IsSubscribed)
            {
                Echo.Warning($"Tutorial pointer target '{_targetKey.name}' has no activation event or Button; the pointer will hide when the step ends.");
            }
        }

        protected override void OnBeforeViewDestroyed()
        {
            _clickSubscription?.Unsubscribe();
            _clickSubscription = null;
        }

        private void HidePointerWhenTargetActivated()
        {
            HideViewAsync(CancellationToken.None).Forget();
        }
    }
}
