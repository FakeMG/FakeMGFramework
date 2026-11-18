using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.ButtonSelection
{
    /// <summary>
    /// Draws a frame behind the selected object. The frame is drawn where it sits in the hierarchy, so a popup that covers
    /// the screen needs its own frame placed behind its buttons, e.g. as the first child of its panel. A selected object
    /// inside a scroll view's <see cref="RectMask2D"/> clips the frame to that mask, as the object itself is clipped.
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
        private RectMask2D _lastSelectedMask;
        private MaskableGraphic[] _frameGraphics;
        private float _appearThresholdSquared;

        #region Unity Lifecycle

        private void Awake()
        {
            _appearThresholdSquared = _appearThreshold * _appearThreshold;
            _frameGraphics = _frameRectTransform.GetComponentsInChildren<MaskableGraphic>(true);
        }

        private void Update()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected != _lastSelected)
            {
                _lastSelected = selected;
                _lastSelectedRect = selected ? selected.GetComponent<RectTransform>() : null;
                _lastSelectedMask = selected ? selected.GetComponentInParent<RectMask2D>() : null;
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
            ClipToSelectedObjectMask();
        }

        #endregion

        #region Private Methods

        /// <remarks>The mask's rect and the frame's clip rect share one space only while both sit under the same root canvas.</remarks>
        private void ClipToSelectedObjectMask()
        {
            bool isClipped = _lastSelectedMask && _lastSelectedMask.isActiveAndEnabled;
            Rect clipRect = isClipped ? _lastSelectedMask.canvasRect : default;
            foreach (MaskableGraphic frameGraphic in _frameGraphics)
            {
                frameGraphic.SetClipRect(clipRect, isClipped);
            }
        }

        #endregion
    }
}