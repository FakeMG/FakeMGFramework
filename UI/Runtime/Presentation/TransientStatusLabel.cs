using DG.Tweening;
using TMPro;
using UnityEngine;

namespace FakeMG.Framework.UI
{
    public sealed class TransientStatusLabel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _label;

        [Header("Tone")]
        [SerializeField] private Color _confirmationColor = new(0.431f, 0.620f, 0.290f);
        [SerializeField] private Color _errorColor = new(0.753f, 0.337f, 0.227f);

        [Header("Timing")]
        [SerializeField, Min(0.5f)] private float _visibleSeconds = 4f;
        [SerializeField, Min(0f)] private float _fadeInSeconds = 0.2f;
        [SerializeField, Min(0f)] private float _fadeOutSeconds = 0.4f;

        [Header("Error Shake")]
        [SerializeField] private Vector2 _errorPunchPixels = new(8f, 0f);
        [SerializeField, Min(0f)] private float _errorPunchSeconds = 0.35f;

        private Sequence _sequence;
        private Vector2 _shownPositionPixels;

        #region Unity Lifecycle

        private void Awake()
        {
            _label.alpha = 0f;
            _shownPositionPixels = _label.rectTransform.anchoredPosition;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        #endregion

        #region Public Methods

        public void ShowConfirmation(string message)
        {
            PlayMessage(message, _confirmationColor);
        }

        public void ShowError(string message)
        {
            PlayMessage(message, _errorColor)
                .Insert(0f, _label.rectTransform.DOPunchAnchorPos(_errorPunchPixels, _errorPunchSeconds, 8));
        }

        #endregion

        #region Private Methods

        /// <remarks>The returned sequence starts on the next tween update, so callers may still insert into it.</remarks>
        private Sequence PlayMessage(string message, Color color)
        {
            _sequence?.Kill();
            // A killed error shake would otherwise leave the label wherever the punch had moved it.
            _label.rectTransform.anchoredPosition = _shownPositionPixels;
            _label.text = message;
            _label.color = new Color(color.r, color.g, color.b, _label.alpha);
            _sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true)
                .Insert(0f, _label.DOFade(1f, _fadeInSeconds))
                .Insert(_fadeInSeconds + _visibleSeconds, _label.DOFade(0f, _fadeOutSeconds));
            return _sequence;
        }

        #endregion
    }
}
