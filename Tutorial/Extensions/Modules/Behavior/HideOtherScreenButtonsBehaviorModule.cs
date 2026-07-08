using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class HideOtherScreenButtonsBehaviorModule : ITutorialModule
    {
        [SerializeField] private bool _isRequired = true;
        [SerializeField] private RectTransform _screenSpaceCanvasRoot;
        [SerializeField] private List<TutorialTargetKeySO> _visibleTargetKeys = new();
        [SerializeField] private List<RectTransform> _alwaysVisibleRoots = new();

        private TutorialSelectableVisibilityController _visibilityController;

        public bool BlocksCompletion => false;
        public bool IsRequired => _isRequired;

        [Inject]
        public void Construct(TutorialSelectableVisibilityController visibilityController)
        {
            _visibilityController = visibilityController;
        }

        public UniTask<bool> ActivateAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            RectTransform canvasRoot = ResolveCanvasRoot(context);
            if (canvasRoot == null)
            {
                Echo.Error("Cannot hide gameplay buttons because no screen-space Canvas root is assigned.");
                return UniTask.FromResult(false);
            }

            List<Transform> visibleRoots = ResolveVisibleRoots(context);
            _visibilityController.ApplyVisibility(canvasRoot, visibleRoots, _alwaysVisibleRoots);

            return UniTask.FromResult(true);
        }

        private RectTransform ResolveCanvasRoot(TutorialContext context)
        {
            return _screenSpaceCanvasRoot != null ? _screenSpaceCanvasRoot : context.HiddenUIRoot;
        }

        public UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        private List<Transform> ResolveVisibleRoots(TutorialContext context)
        {
            var visibleRoots = new List<Transform>(_visibleTargetKeys.Count);
            for (int keyIndex = 0; keyIndex < _visibleTargetKeys.Count; keyIndex++)
            {
                TutorialTargetKeySO key = _visibleTargetKeys[keyIndex];
                if (context.TargetRegistry.TryGet(key, out ITutorialTarget target) && !target.IsDestroyed() &&
                    target.InteractionTransform != null)
                {
                    visibleRoots.Add(target.InteractionTransform);
                    continue;
                }

                Echo.Warning($"Visible tutorial target '{(key == null ? "<none>" : key.name)}' is not registered; no button will be preserved for it.");
            }

            return visibleRoots;
        }
    }
}
