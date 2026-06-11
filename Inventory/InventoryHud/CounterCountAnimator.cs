using System;
using System.Numerics;
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

        // The rolling number tween runs over a double for smooth motion; the exact BigInteger
        // target is applied on completion so arbitrary-magnitude counts stay precise.
        public UniTask AnimateAsync(BigInteger fromCount, BigInteger targetCount, Action<BigInteger> applyCount)
        {
            KillActiveTween();

            if (fromCount == targetCount)
            {
                applyCount?.Invoke(targetCount);
                return UniTask.CompletedTask;
            }

            double displayedCount = (double)fromCount;
            _activeCountTween = DOTween.To(
                    () => displayedCount,
                    value =>
                    {
                        displayedCount = value;
                        applyCount?.Invoke(new BigInteger(value));
                    },
                    (double)targetCount,
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
