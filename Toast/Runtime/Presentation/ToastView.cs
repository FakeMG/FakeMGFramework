using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FakeMG.Toast
{
    /// <summary>
    /// Applies message text and color, measures the wrapped text, clamps the visible height
    /// and reports the final height so the manager can stack toasts. Width is owned by the prefab layout.
    /// </summary>
    public class ToastView : MonoBehaviour
    {
        [Required]
        [SerializeField] private RectTransform _rectTransform;
        [Required]
        [SerializeField] private TMP_Text _text;
        [Required]
        [SerializeField] private ToastAnimator _animator;
        [SerializeField] private float _verticalPaddingPixels = 16f;

        public ToastAnimator Animator => _animator;
        public RectTransform RectTransform => _rectTransform;

        #region Public
        /// <summary>Sets text and color, resizes the view to the clamped text height. The GameObject must be active.</summary>
        /// <returns>Final visible height in pixels, for stack positioning.</returns>
        public float ApplyMessage(string text, Color textColor, float maxHeightPixels, int maxLineCount)
        {
            _text.text = text;
            _text.color = textColor;
            // Layout has not run this frame; without this, preferredHeight and lineCount are stale.
            _text.ForceMeshUpdate();

            float clampedTextHeightPixels = CalculateClampedTextHeight(maxHeightPixels, maxLineCount);
            float totalHeightPixels = clampedTextHeightPixels + _verticalPaddingPixels;
            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, totalHeightPixels);
            return totalHeightPixels;
        }

        public void ResetForPool()
        {
            _text.text = string.Empty;
            _animator.SetVisualsToHiddenState();
            gameObject.SetActive(false);
        }
        #endregion

        #region Private
        private float CalculateClampedTextHeight(float maxHeightPixels, int maxLineCount)
        {
            float heightPixels = _text.preferredHeight;

            // Font metrics instead of textInfo.lineCount: textInfo is stale on the activation frame,
            // which made the line clamp silently skip.
            if (maxLineCount > 0)
            {
                float maxLinesHeightPixels = GetLineHeightPixels() * maxLineCount;
                heightPixels = Mathf.Min(heightPixels, maxLinesHeightPixels);
            }

            if (maxHeightPixels > 0f)
            {
                heightPixels = Mathf.Min(heightPixels, maxHeightPixels);
            }

            return heightPixels;
        }

        private float GetLineHeightPixels()
        {
            var faceInfo = _text.font.faceInfo;
            return _text.fontSize * (faceInfo.lineHeight / faceInfo.pointSize);
        }
        #endregion
    }
}
