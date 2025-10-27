using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Toggle
{
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        private const float MIN_VALUE = 0f;
        private const float MAX_VALUE = 1f;

        [Header("Visual Components")]
        [SerializeField] private RectTransform _fillRect;
        [SerializeField] private RectTransform _handleRect;

        [Header("Animation")]
        [SerializeField, Range(0, 1f)] private float _animationDuration = 0.1f;
        [SerializeField] private AnimationCurve _slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine _animateSliderCoroutine;

        public event Action<bool> OnValueChanged;

        private ToggleSwitchGroupManager _toggleSwitchGroupManager;

        // Visual update tracking from Unity's Slider
        private DrivenRectTransformTracker _visualTracker;
        private Image _fillImage;
        private Transform _fillTransform;
        private RectTransform _fillContainerRect;
        private Transform _handleTransform;
        private RectTransform _handleContainerRect;

        public bool IsOn { get; private set; }

        private bool _previousValue;
        private float _currentNormalizedValue;

        protected void OnEnable()
        {
            UpdateCachedReferences();
            SetVisualState(IsOn ? MAX_VALUE : MIN_VALUE);
        }

        protected void OnDisable()
        {
            _visualTracker.Clear();
        }

        private void UpdateCachedReferences()
        {
            if (_fillRect && _fillRect != (RectTransform)transform)
            {
                _fillTransform = _fillRect.transform;
                _fillImage = _fillRect.GetComponent<Image>();
                if (_fillTransform.parent)
                    _fillContainerRect = _fillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                _fillRect = null;
                _fillContainerRect = null;
                _fillImage = null;
            }

            if (_handleRect && _handleRect != (RectTransform)transform)
            {
                _handleTransform = _handleRect.transform;
                if (_handleTransform.parent)
                    _handleContainerRect = _handleTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                _handleRect = null;
                _handleContainerRect = null;
            }
        }

        // Extracted from Unity's Slider.UpdateVisuals()
        private void UpdateVisuals()
        {
            _visualTracker.Clear();

            if (_fillContainerRect)
            {
                _visualTracker.Add(this, _fillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (_fillImage && _fillImage.type == Image.Type.Filled)
                {
                    _fillImage.fillAmount = _currentNormalizedValue;
                }
                else
                {
                    anchorMax.x = _currentNormalizedValue; // Assuming left-to-right direction
                }

                _fillRect.anchorMin = anchorMin;
                _fillRect.anchorMax = anchorMax;
            }

            if (_handleContainerRect)
            {
                _visualTracker.Add(this, _handleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin.x = anchorMax.x = _currentNormalizedValue; // Assuming left-to-right direction
                _handleRect.anchorMin = anchorMin;
                _handleRect.anchorMax = anchorMax;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }

        private void Toggle()
        {
            if (_toggleSwitchGroupManager)
            {
                _toggleSwitchGroupManager.ToggleGroup(this);
            }
            else
            {
                SetStateAndStartAnimation(!IsOn);
            }
        }

        public void SetStateAndStartAnimation(bool state)
        {
            _previousValue = IsOn;
            IsOn = state;

            if (_previousValue != IsOn)
            {
                OnValueChanged?.Invoke(IsOn);
            }

            if (_animateSliderCoroutine != null)
            {
                StopCoroutine(_animateSliderCoroutine);
            }

            _animateSliderCoroutine = StartCoroutine(AnimateSlider());
        }

        public void SetStateWithoutAnimation(bool state)
        {
            _previousValue = IsOn;
            IsOn = state;

            if (_previousValue != IsOn)
            {
                OnValueChanged?.Invoke(IsOn);
            }

            SetVisualState(IsOn ? MAX_VALUE : MIN_VALUE);
        }

        private IEnumerator AnimateSlider()
        {
            float startValue = _currentNormalizedValue;
            float endValue = IsOn ? MAX_VALUE : MIN_VALUE;

            float time = 0f;
            if (_animationDuration > 0f)
            {
                while (time < _animationDuration)
                {
                    time += Time.deltaTime;

                    float lerpFactor = _slideEase.Evaluate(time / _animationDuration);
                    float currentValue = Mathf.Lerp(startValue, endValue, lerpFactor);

                    SetVisualState(currentValue);

                    yield return null;
                }
            }

            SetVisualState(endValue);
        }

        private void SetVisualState(float normalizedValue)
        {
            _currentNormalizedValue = Mathf.Clamp01(normalizedValue);
            UpdateVisuals();
        }

        public void SetupForManager(ToggleSwitchGroupManager manager)
        {
            _toggleSwitchGroupManager = manager;
        }

        public void ToggleByGroupManager(bool valueToSetTo)
        {
            SetStateAndStartAnimation(valueToSetTo);
        }
    }
}