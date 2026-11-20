using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace FakeMG.Framework.UI.Toggle
{
    /// <summary>An on/off switch whose look comes entirely from its <see cref="IToggleSwitchVisual"/> components.</summary>
    /// <remarks>
    /// Pair it with a Selectable on the same object for keyboard and gamepad: navigation focuses that, and Submit reaches this.
    /// Not a Selectable itself, which runs in edit mode and would let the visuals rewrite the prefab.
    /// </remarks>
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        private const float OFF_POSITION01 = 0f;
        private const float ON_POSITION01 = 1f;

        [Header("Animation")]
        [FormerlySerializedAs("_animationDuration")]
        [SerializeField, Range(0, 1f)] private float _animationDurationSeconds = 0.1f;
        [SerializeField] private Ease _slideEase = Ease.InOutSine;

        [Header("Visuals")]
        [Tooltip("Components implementing IToggleSwitchVisual, e.g. ToggleSwitchRectVisual for the sliding fill and handle.")]
        [SerializeField] private MonoBehaviour[] _visualBehaviours;

        private IToggleSwitchVisual[] _visuals;
        private ToggleSwitchGroupManager _groupManager;
        private float _position01;
        private Tween _slideTween;

        public bool IsOn { get; private set; }

        public event Action<bool> OnValueChanged;

        private float TargetPosition01 => IsOn ? ON_POSITION01 : OFF_POSITION01;

        #region Unity Lifecycle

        private void Awake()
        {
            _visuals = Array.ConvertAll(_visualBehaviours, behaviour => (IToggleSwitchVisual)behaviour);
        }

        private void OnEnable()
        {
            ShowPosition(TargetPosition01, isInstant: true);
        }

        private void OnDisable()
        {
            KillSlide();
        }

        #endregion

        #region Public Methods

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Toggle();
        }

        public void SetStateAndStartAnimation(bool isOn)
        {
            SetState(isOn, isInstant: false);
        }

        /// <remarks>Raises <see cref="OnValueChanged"/> like an animated change does.</remarks>
        public void SetStateWithoutAnimation(bool isOn)
        {
            SetState(isOn, isInstant: true);
        }

        public void SetupForManager(ToggleSwitchGroupManager manager)
        {
            _groupManager = manager;
        }

        public void ToggleByGroupManager(bool isOn)
        {
            SetStateAndStartAnimation(isOn);
        }

        #endregion

        #region Private Methods

        private void Toggle()
        {
            if (_groupManager)
            {
                _groupManager.ToggleGroup(this);
                return;
            }

            SetStateAndStartAnimation(!IsOn);
        }

        private void SetState(bool isOn, bool isInstant)
        {
            bool hasChanged = IsOn != isOn;
            IsOn = isOn;
            if (hasChanged)
            {
                OnValueChanged?.Invoke(IsOn);
            }

            KillSlide();
            if (isInstant || _animationDurationSeconds <= 0f)
            {
                ShowPosition(TargetPosition01, isInstant: true);
                return;
            }

            ShowPosition(_position01, isInstant: false);
            _slideTween = DOTween.To(GetPosition01, ShowSlidingPosition, TargetPosition01, _animationDurationSeconds)
                .SetEase(_slideEase)
                .SetLink(gameObject);
        }

        private float GetPosition01()
        {
            return _position01;
        }

        private void ShowSlidingPosition(float position01)
        {
            ShowPosition(position01, isInstant: false);
        }

        private void ShowPosition(float position01, bool isInstant)
        {
            _position01 = Mathf.Clamp01(position01);
            foreach (IToggleSwitchVisual visual in _visuals)
            {
                visual.Apply(IsOn, _position01, isInstant);
            }
        }

        private void KillSlide()
        {
            _slideTween?.Kill();
            _slideTween = null;
        }

        #endregion
    }
}
