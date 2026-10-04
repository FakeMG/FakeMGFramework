using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FakeMG.Numbers;
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

        // The rolling number tween runs over a double for smooth motion; the exact GameNumber
        // target is applied on completion so arbitrary-magnitude counts stay precise.
        public UniTask AnimateAsync(GameNumber fromCount, GameNumber targetCount, Action<GameNumber> applyCount)
        {
            KillActiveTween();

            if (fromCount == targetCount)
            {
                applyCount?.Invoke(targetCount);
                return UniTask.CompletedTask;
            }

            double displayedCount = fromCount.ToDouble();
            _activeCountTween = DOTween.To(
                    () => displayedCount,
                    value =>
                    {
                        displayedCount = value;
                        applyCount?.Invoke(GameNumber.FromDouble(value));
                    },
                    targetCount.ToDouble(),
                    _countTweenDurationSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    applyCount?.Invoke(targetCount);
                    ClearActiveTweenReference();
                })
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
