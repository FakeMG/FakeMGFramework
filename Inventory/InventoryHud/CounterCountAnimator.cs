using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    public sealed class CounterCountAnimator : MonoBehaviour
    {
        [SerializeField] private float _countTweenDurationSeconds = 0.12f;

        private Tween _activeCountTween;

        #region Unity Lifecycle

        private void OnDisable()
        {
            KillActiveTween();
        }

        private void OnDestroy()
        {
            KillActiveTween();
        }

        #endregion

        #region Public Methods

        public UniTask AnimateAsync(int fromCount, int targetCount, Action<int> applyCount)
        {
            KillActiveTween();

            if (fromCount == targetCount)
            {
                applyCount?.Invoke(targetCount);
                return UniTask.CompletedTask;
            }

            int displayedCount = fromCount;
            _activeCountTween = DOTween.To(
                    () => displayedCount,
                    value =>
                    {
                        displayedCount = value;
                        applyCount?.Invoke(value);
                    },
                    targetCount,
                    _countTweenDurationSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(ClearActiveTweenReference)
                .OnKill(ClearActiveTweenReference);

            return _activeCountTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        #endregion

        #region Private Methods

        private void KillActiveTween()
        {
            if (_activeCountTween != null && _activeCountTween.IsActive())
            {
                _activeCountTween.Kill();
            }

            _activeCountTween = null;
        }

        private void ClearActiveTweenReference()
        {
            _activeCountTween = null;
        }

        #endregion
    }
}
