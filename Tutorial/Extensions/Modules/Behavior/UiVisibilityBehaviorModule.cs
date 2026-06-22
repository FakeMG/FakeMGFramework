using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Hides a set of UI canvas groups for the duration of a step and restores their
    /// previous visibility on deactivation, both with a smooth fade. Captures each
    /// group's prior alpha so restoration returns to the exact previous state.
    /// </summary>
    [Serializable]
    public sealed class UiVisibilityBehaviorModule : ITutorialModule
    {
        [SerializeField] private bool _isRequired;
        [SerializeField] private float _fadeDurationSeconds = 0.25f;
        [SerializeField] private List<CanvasGroup> _groupsToHide = new();

        private readonly List<float> _previousAlphas = new();

        public bool BlocksCompletion => true;
        public bool IsRequired => _isRequired;

        public async UniTask ActivateAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            _previousAlphas.Clear();

            var fadeTasks = new List<UniTask>(_groupsToHide.Count);
            for (int i = 0; i < _groupsToHide.Count; i++)
            {
                CanvasGroup group = _groupsToHide[i];
                _previousAlphas.Add(group.alpha);
                group.interactable = false;
                group.blocksRaycasts = false;
                fadeTasks.Add(group.DOFade(0f, _fadeDurationSeconds).ToUniTask(cancellationToken: cancellationToken));
            }

            await UniTask.WhenAll(fadeTasks);
        }

        public async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            var fadeTasks = new List<UniTask>(_groupsToHide.Count);
            for (int i = 0; i < _groupsToHide.Count; i++)
            {
                CanvasGroup group = _groupsToHide[i];
                float previousAlpha = _previousAlphas[i];
                fadeTasks.Add(group.DOFade(previousAlpha, _fadeDurationSeconds).ToUniTask(cancellationToken: cancellationToken));
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            await UniTask.WhenAll(fadeTasks);
        }
    }
}
