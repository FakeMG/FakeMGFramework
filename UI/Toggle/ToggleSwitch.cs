using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.UI.Toggle
{
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        private const float MIN_VALUE = 0f;
        private const float MAX_VALUE = 1f;

        [Header("Visual Components")]
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private RectTransform handleRect;

        [Header("Animation")]
        [SerializeField, Range(0, 1f)] private float animationDuration = 0.1f;
        [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine _animateSliderCoroutine;

        [Header("Events")]
        [SerializeField] private UnityEvent<bool> onValueChanged;

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
            if (fillRect && fillRect != (RectTransform)transform)
            {
                _fillTransform = fillRect.transform;
                _fillImage = fillRect.GetComponent<Image>();
                if (_fillTransform.parent)
                    _fillContainerRect = _fillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                fillRect = null;
                _fillContainerRect = null;
                _fillImage = null;
            }

            if (handleRect && handleRect != (RectTransform)transform)
            {
                _handleTransform = handleRect.transform;
                if (_handleTransform.parent)
                    _handleContainerRect = _handleTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                handleRect = null;
                _handleContainerRect = null;
            }
        }

        // Extracted from Unity's Slider.UpdateVisuals()
        private void UpdateVisuals()
        {
            _visualTracker.Clear();

            if (_fillContainerRect)
            {
                _visualTracker.Add(this, fillRect, DrivenTransformProperties.Anchors);
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

                fillRect.anchorMin = anchorMin;
                fillRect.anchorMax = anchorMax;
            }

            if (_handleContainerRect)
            {
                _visualTracker.Add(this, handleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin.x = anchorMax.x = _currentNormalizedValue; // Assuming left-to-right direction
                handleRect.anchorMin = anchorMin;
                handleRect.anchorMax = anchorMax;
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
                onValueChanged?.Invoke(IsOn);
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
                onValueChanged?.Invoke(IsOn);
            }

            SetVisualState(IsOn ? MAX_VALUE : MIN_VALUE);
        }

        private IEnumerator AnimateSlider()
        {
            float startValue = _currentNormalizedValue;
            float endValue = IsOn ? MAX_VALUE : MIN_VALUE;

            float time = 0f;
            if (animationDuration > 0f)
            {
                while (time < animationDuration)
                {
                    time += Time.deltaTime;

                    float lerpFactor = slideEase.Evaluate(time / animationDuration);
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