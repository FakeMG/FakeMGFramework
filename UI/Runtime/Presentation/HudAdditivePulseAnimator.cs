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
        private int _pendingPulseCount;

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
            _pendingPulseCount++;
            if (_activePulseSequence == null || !_activePulseSequence.IsActive())
            {
                StartPulseSequenceWhenIdle();
            }
        }

        public void ResetToBaseScale()
        {
            _targetTransform.localScale = _baseScale;
        }

        #endregion

        #region Private Methods

        private void StartPulseSequenceWhenIdle()
        {
            Vector3 pulseScaleDelta = Vector3.one * _pulseScaleDelta01;

            _activePulseSequence = DOTween.Sequence();
            _activePulseSequence
                .SetId(this)
                .SetLink(_targetTransform.gameObject)
                .Append(_targetTransform
                    .DOBlendableScaleBy(pulseScaleDelta, _growDurationSeconds)
                    .SetEase(Ease.OutQuad)
                    .SetLink(_targetTransform.gameObject))
                .Append(_targetTransform
                    .DOBlendableScaleBy(-pulseScaleDelta, _shrinkDurationSeconds)
                    .SetEase(Ease.InQuad)
                    .SetLink(_targetTransform.gameObject))
                .OnComplete(HandlePulseSequenceCompleted)
                .OnKill(ClearActivePulseSequenceReferenceWhenKilled);
        }

        private void HandlePulseSequenceCompleted()
        {
            _pendingPulseCount = Mathf.Max(0, _pendingPulseCount - 1);
            if (_pendingPulseCount > 0)
            {
                StartPulseSequenceWhenIdle();
                return;
            }

            _activePulseSequence = null;
            ResetToBaseScale();
        }

        private void ResetScaleImmediatelyAndKillTweens()
        {
            _pendingPulseCount = 0;

            if (_activePulseSequence != null && _activePulseSequence.IsActive())
            {
                _activePulseSequence.Kill();
            }

            DOTween.Kill(this);
            _activePulseSequence = null;
            ResetToBaseScale();
        }

        private void ClearActivePulseSequenceReferenceWhenKilled()
        {
            _activePulseSequence = null;
        }

        #endregion
    }
}
