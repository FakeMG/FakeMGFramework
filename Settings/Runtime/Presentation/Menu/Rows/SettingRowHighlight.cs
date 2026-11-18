using DG.Tweening;
using UnityEngine;

namespace FakeMG.Settings
{
    public sealed class SettingRowHighlight : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _highlight;

        [Space]
        [Header("Motion")]
        [SerializeField, Range(0f, 1f)] private float _focusedAlpha01 = 1f;
        [SerializeField, Min(0f)] private float _fadeInSeconds = 0.12f;
        [SerializeField, Min(0f)] private float _fadeOutSeconds = 0.08f;

        #region Unity Lifecycle

        private void OnDisable()
        {
            _highlight.DOKill();
            _highlight.alpha = 0f;
        }

        #endregion

        #region Public Methods

        public void ShowFocused()
        {
            _highlight.DOKill();
            _highlight.DOFade(_focusedAlpha01, _fadeInSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
        }

        public void ShowUnfocused()
        {
            _highlight.DOKill();
            _highlight.DOFade(0f, _fadeOutSeconds).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject);
        }

        #endregion
    }
}
