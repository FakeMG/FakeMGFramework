using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI
{
    public class HudAdditivePulseAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _growDurationSeconds = 0.08f;
        [SerializeField] private float _shrinkDurationSeconds = 0.12f;
        [SerializeField] private float _pulseScaleDelta01 = 0.18f;

        private Sequence _activePulseSequence;
        private Vector3 _baseScale;

        #region Unity Lifecycle

        private void Awake()
        {
            Debug.Assert(_targetTransform != null, $"{nameof(_targetTransform)} is not assigned on {name}.");
            _baseScale = _targetTransform.localScale;
        }

        private void OnDisable()
        {
            ResetScaleImmediatelyAndKillTweens();
        }

        private void OnDestroy()
        {
            ResetScaleImmediatelyAndKillTweens();
        }

        #endregion

        #region Public Methods

        public void PlayAdditivePulse()
        {
            KillActivePulse();
            StartPulseFromCurrentScale();
        }

        public void ResetToBaseScale()
        {
            _targetTransform.localScale = _baseScale;
        }

        #endregion

        #region Private Methods

        private void StartPulseFromCurrentScale()
        {
            Vector3 pulseScaleDelta = Vector3.one * _pulseScaleDelta01;
            Vector3 remainingGrowScaleDelta = _baseScale + pulseScaleDelta - _targetTransform.localScale;

            _activePulseSequence = DOTween.Sequence();
            _activePulseSequence
                .SetId(this)
                .SetUpdate(true)
                .SetLink(_targetTransform.gameObject)
                .Append(_targetTransform
                    .DOBlendableScaleBy(remainingGrowScaleDelta, _growDurationSeconds)
                    .SetEase(Ease.OutQuad)
                    .SetLink(_targetTransform.gameObject))
                .Append(_targetTransform
                    .DOBlendableScaleBy(-pulseScaleDelta, _shrinkDurationSeconds)
                    .SetEase(Ease.InQuad)
                    .SetLink(_targetTransform.gameObject))
                .OnComplete(RestoreScaleWhenPulseCompletes)
                .OnKill(ClearActivePulseSequenceReferenceWhenKilled);
        }

        private void RestoreScaleWhenPulseCompletes()
        {
            _activePulseSequence = null;
            ResetToBaseScale();
        }

        private void ResetScaleImmediatelyAndKillTweens()
        {
            KillActivePulse();
            ResetToBaseScale();
        }

        private void KillActivePulse()
        {
            if (_activePulseSequence != null && _activePulseSequence.IsActive())
            {
                _activePulseSequence.Kill();
            }

            DOTween.Kill(this);
            _activePulseSequence = null;
        }

        private void ClearActivePulseSequenceReferenceWhenKilled()
        {
            _activePulseSequence = null;
        }

        #endregion
    }
}
