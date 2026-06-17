using System;
using DG.Tweening;
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
        [SerializeField] private Ease _slideEase = Ease.InOutSine;

        [Header("Extensible Visuals")]
        [SerializeField] private MonoBehaviour[] _visualBehaviours;

        public event Action<bool> OnValueChanged;

        public bool IsOn { get; private set; }

        private ToggleSwitchGroupManager _toggleSwitchGroupManager;

        private DrivenRectTransformTracker _visualTracker;

        private Image _fillImage;
        private Transform _fillTransform;
        private RectTransform _fillContainerRect;

        private Transform _handleTransform;
        private RectTransform _handleContainerRect;

        private IToggleSwitchVisual[] _visuals;

        private bool _previousValue;
        private float _currentNormalizedValue;
        private Tween _slideTween;

        protected void OnEnable()
        {
            CacheVisualBehaviours();
            UpdateCachedReferences();

            SetVisualState(IsOn ? MAX_VALUE : MIN_VALUE, true);
            ApplyExtendedVisuals(true);
        }

        protected void OnDisable()
        {
            _slideTween?.Kill();
            _slideTween = null;

            _visualTracker.Clear();
        }

        private void CacheVisualBehaviours()
        {
            if (_visualBehaviours == null || _visualBehaviours.Length == 0)
            {
                _visuals = Array.Empty<IToggleSwitchVisual>();
                return;
            }

            int validCount = 0;

            for (int i = 0; i < _visualBehaviours.Length; i++)
            {
                if (_visualBehaviours[i] is IToggleSwitchVisual)
                    validCount++;
            }

            _visuals = new IToggleSwitchVisual[validCount];

            int index = 0;

            for (int i = 0; i < _visualBehaviours.Length; i++)
            {
                if (_visualBehaviours[i] is IToggleSwitchVisual visual)
                {
                    _visuals[index] = visual;
                    index++;
                }
            }
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
                    anchorMax.x = _currentNormalizedValue;
                }

                _fillRect.anchorMin = anchorMin;
                _fillRect.anchorMax = anchorMax;
            }

            if (_handleContainerRect)
            {
                _visualTracker.Add(this, _handleRect, DrivenTransformProperties.Anchors);

                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                anchorMin.x = _currentNormalizedValue;
                anchorMax.x = _currentNormalizedValue;

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
                return;
            }

            SetStateAndStartAnimation(!IsOn);
        }

        public void SetStateAndStartAnimation(bool state)
        {
            SetState(state, false);
        }

        public void SetStateWithoutAnimation(bool state)
        {
            SetState(state, true);
        }

        private void SetState(bool state, bool instant)
        {
            _previousValue = IsOn;
            IsOn = state;

            if (_previousValue != IsOn)
                OnValueChanged?.Invoke(IsOn);

            float targetValue = IsOn ? MAX_VALUE : MIN_VALUE;

            _slideTween?.Kill();
            _slideTween = null;

            ApplyExtendedVisuals(instant);

            if (instant || _animationDuration <= 0f)
            {
                SetVisualState(targetValue, true);
                return;
            }

            _slideTween = DOTween
                .To(
                    () => _currentNormalizedValue,
                    value => SetVisualState(value, false),
                    targetValue,
                    _animationDuration
                )
                .SetEase(_slideEase)
                .SetLink(gameObject);
        }

        private void SetVisualState(float normalizedValue, bool instant)
        {
            _currentNormalizedValue = Mathf.Clamp01(normalizedValue);

            UpdateVisuals();
            ApplyExtendedVisuals(instant);
        }

        private void ApplyExtendedVisuals(bool instant)
        {
            if (_visuals == null)
                return;

            for (int i = 0; i < _visuals.Length; i++)
            {
                _visuals[i]?.Apply(IsOn, _currentNormalizedValue, instant);
            }
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