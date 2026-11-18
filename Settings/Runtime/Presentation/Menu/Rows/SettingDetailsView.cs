using DG.Tweening;
using TMPro;
using UnityEngine;

namespace FakeMG.Settings
{
    /// <remarks>All motion runs on unscaled time: a pause menu holds the simulation paused.</remarks>
    public sealed class SettingDetailsView : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The whole panel, background included, hidden while no setting is selected.")]
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private CanvasGroup _content;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;

        [Space]
        [Header("Motion")]
        [Tooltip("New details fade in from this alpha, so switching rows reads as a change without blinking.")]
        [SerializeField, Range(0f, 1f)] private float _switchStartAlpha01 = 0.3f;
        [SerializeField, Min(0f)] private float _fadeInSeconds = 0.15f;
        [SerializeField, Min(0f)] private float _fadeOutSeconds = 0.1f;

        #region Unity Lifecycle

        private void OnDisable()
        {
            _panel.DOKill();
            _content.DOKill();
            _panel.alpha = 0f;
        }

        #endregion

        #region Public Methods

        /// <param name="row">Null hides the panel.</param>
        public void Show(ISettingRow row)
        {
            _panel.DOKill();
            _content.DOKill();
            if (row == null)
            {
                _panel.DOFade(0f, _fadeOutSeconds).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject);
                return;
            }

            _title.text = row.Title;
            _description.text = row.Description;
            _panel.DOFade(1f, _fadeInSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            _content.alpha = Mathf.Min(_content.alpha, _switchStartAlpha01);
            _content.DOFade(1f, _fadeInSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
        }

        #endregion
    }
}
