using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Toggle
{
    /// <summary>Slides the handle along its parent and stretches the fill up to it.</summary>
    /// <remarks>The anchors it moves are marked as driven, so the editor does not save them into the prefab.</remarks>
    public sealed class ToggleSwitchRectVisual : MonoBehaviour, IToggleSwitchVisual
    {
        [Tooltip("A Filled image is filled up to the handle; any other image is stretched to it.")]
        [Required, SerializeField] private Image _fillImage;
        [Required, SerializeField] private RectTransform _handleRect;

        private DrivenRectTransformTracker _drivenAnchorsTracker;

        private bool IsFillFilled => _fillImage.type == Image.Type.Filled;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (!IsFillFilled)
            {
                _drivenAnchorsTracker.Add(this, _fillImage.rectTransform, DrivenTransformProperties.Anchors);
            }

            _drivenAnchorsTracker.Add(this, _handleRect, DrivenTransformProperties.Anchors);
        }

        private void OnDisable()
        {
            _drivenAnchorsTracker.Clear();
        }

        #endregion

        #region Public Methods

        public void Apply(bool isOn, float normalizedValue, bool instant)
        {
            if (IsFillFilled)
            {
                _fillImage.fillAmount = normalizedValue;
            }
            else
            {
                _fillImage.rectTransform.anchorMin = Vector2.zero;
                _fillImage.rectTransform.anchorMax = new Vector2(normalizedValue, 1f);
            }

            _handleRect.anchorMin = new Vector2(normalizedValue, 0f);
            _handleRect.anchorMax = new Vector2(normalizedValue, 1f);
        }

        #endregion
    }
}
