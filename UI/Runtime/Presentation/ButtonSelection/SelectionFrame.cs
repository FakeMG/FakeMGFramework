using UnityEngine;
using UnityEngine.EventSystems;

namespace FakeMG.Framework.UI.ButtonSelection
{
    /// <summary>
    /// Draws a frame behind the selected object. The frame is drawn where it sits in the hierarchy, so a popup that covers
    /// the screen needs its own frame placed behind its buttons, e.g. as the first child of its panel.
    /// </summary>
    public class SelectionFrame : MonoBehaviour
    {
        [Tooltip("Keep its pivot centred: the frame is centred on the selected object, whatever that object's pivot.")]
        [SerializeField] private RectTransform _frameRectTransform;
        [SerializeField] private Vector2 _padding = new(10f, 10f);
        [SerializeField] private float _scaleAnimationSpeed = 15f;
        [SerializeField] private float _appearThreshold = 5f;

        private bool _isAppearing;
        private GameObject _lastSelected;
        private RectTransform _lastSelectedRect;
        private float _appearThresholdSquared;

        #region Unity Lifecycle

        private void Awake()
        {
            _appearThresholdSquared = _appearThreshold * _appearThreshold;
        }

        private void Update()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected != _lastSelected)
            {
                _lastSelected = selected;
                _lastSelectedRect = selected ? selected.GetComponent<RectTransform>() : null;
            }

            // The EventSystem keeps a selection whose panel was hidden, e.g. the button that opened an empty panel.
            if (!_lastSelectedRect || !_lastSelectedRect.gameObject.activeInHierarchy)
            {
                _frameRectTransform.sizeDelta = Vector2.zero;
                _frameRectTransform.localScale = Vector3.zero;
                _isAppearing = false;
                return;
            }

            // Measured against the frame's parent rather than the frame, whose own scale animates while it appears.
            float selectedToFrameScale = _lastSelectedRect.lossyScale.x / _frameRectTransform.parent.lossyScale.x;
            Vector2 targetSizePixels = (_lastSelectedRect.rect.size + _padding) * selectedToFrameScale;

            bool isSizeNearZero = _frameRectTransform.sizeDelta.sqrMagnitude < _appearThresholdSquared;
            if (isSizeNearZero && !_isAppearing)
            {
                _isAppearing = true;
                _frameRectTransform.localScale = Vector3.zero;
            }

            if (_isAppearing)
            {
                _frameRectTransform.localScale = Vector3.Lerp(
                    _frameRectTransform.localScale,
                    Vector3.one,
                    Time.unscaledDeltaTime * _scaleAnimationSpeed
                );

                if (_frameRectTransform.localScale.x >= 0.99f)
                {
                    _frameRectTransform.localScale = Vector3.one;
                    _isAppearing = false;
                }
            }

            _frameRectTransform.position = _lastSelectedRect.TransformPoint(_lastSelectedRect.rect.center);
            _frameRectTransform.sizeDelta = targetSizePixels;
        }

        #endregion
    }
}