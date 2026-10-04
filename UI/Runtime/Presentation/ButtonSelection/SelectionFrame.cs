using UnityEngine;
using UnityEngine.EventSystems;

namespace FakeMG.Framework.UI.ButtonSelection
{
    public class SelectionFrame : MonoBehaviour
    {
        // All canvases should use the same scale and settings to ensure proper frame positioning and sizing.
        // If canvases have different scaling, the selection frame may appear misaligned or incorrectly sized relative to the selected object.
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _frameRectTransform;
        [SerializeField] private Vector2 _padding = new(10f, 10f);
        [SerializeField] private float _scaleAnimationSpeed = 15f;
        [SerializeField] private float _appearThreshold = 5f;

        private bool _isAppearing;
        private GameObject _lastSelected;
        private RectTransform _lastSelectedRect;
        private Transform _canvasTransform;
        private float _appearThresholdSquared;

        private void Awake()
        {
            _canvasTransform = _canvas.transform;
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

            if (!_lastSelectedRect)
            {
                _frameRectTransform.sizeDelta = Vector2.zero;
                _frameRectTransform.localScale = Vector3.zero;
                _isAppearing = false;
                return;
            }

            float canvasScale = _lastSelectedRect.lossyScale.x / _canvasTransform.lossyScale.x;
            Vector2 targetSize = (_lastSelectedRect.sizeDelta + _padding) * canvasScale;

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

            _frameRectTransform.position = _lastSelectedRect.position;
            _frameRectTransform.sizeDelta = targetSize;
        }
    }
}