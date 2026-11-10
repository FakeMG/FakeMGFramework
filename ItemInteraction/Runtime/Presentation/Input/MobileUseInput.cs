using System;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace FakeMG.ItemInteraction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(-100)]
    public sealed class MobileUseInput :
        OnScreenControl,
        IUsableItemInteractionPointerInputSource,
        IPointerDownHandler,
        IInitializePotentialDragHandler,
        IDragHandler, IPointerUpHandler, IEndDragHandler
    {
        private const int NO_POINTER_ID = int.MinValue;

        [InputControl(layout = "Vector2")]
        [SerializeField] private string _controlPath;
        [SerializeField, Min(0f)] private float _lookSensitivityMultiplier = 1f;

        private int _activePointerId = NO_POINTER_ID;
        private Vector2 _lookDeltaPixels;
        private bool _shouldResetLookControl;

        protected override string controlPathInternal
        {
            get => _controlPath;
            set => _controlPath = value;
        }

        public bool IsPressed { get; private set; }
        public Vector2 ScreenPositionPixels { get; private set; }

        public event Action<IInteractionPointerInputSource> OnPressed;
        public event Action<IInteractionPointerInputSource> OnReleased;

        #region Unity Lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetInputWithoutRelease();
            if (EventSystem.current == null)
            {
                Echo.Error("Mobile Use Input requires an active EventSystem. Disabling the input component.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_shouldResetLookControl)
            {
                return;
            }

            _lookDeltaPixels = Vector2.zero;
            _shouldResetLookControl = false;
            SendValueToControl(Vector2.zero);
        }

        protected override void OnDisable()
        {
            ReleaseActivePointer();
            base.OnDisable();
            ResetInputWithoutRelease();
        }

        #endregion

        #region Public Methods

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsPressed)
            {
                return;
            }

            _activePointerId = eventData.pointerId;
            ScreenPositionPixels = eventData.position;
            _lookDeltaPixels = Vector2.zero;
            _shouldResetLookControl = false;
            IsPressed = true;
            SendValueToControl(Vector2.zero);
            OnPressed?.Invoke(this);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!OwnsPointer(eventData))
            {
                return;
            }

            ScreenPositionPixels = eventData.position;
            _lookDeltaPixels += eventData.delta;
            _shouldResetLookControl = true;
            SendValueToControl(_lookDeltaPixels * _lookSensitivityMultiplier);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (OwnsPointer(eventData))
            {
                ReleaseActivePointer();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (OwnsPointer(eventData) && !eventData.eligibleForClick)
            {
                ReleaseActivePointer();
            }
        }

        #endregion

        #region Private Methods

        private bool OwnsPointer(PointerEventData eventData)
        {
            return IsPressed && eventData.pointerId == _activePointerId;
        }

        private void ReleaseActivePointer()
        {
            if (!IsPressed)
            {
                return;
            }

            bool shouldResetLookControlNextUpdate = _shouldResetLookControl;
            ResetInputWithoutRelease();
            if (shouldResetLookControlNextUpdate)
            {
                _shouldResetLookControl = true;
            }
            else
            {
                SendValueToControl(Vector2.zero);
            }

            OnReleased?.Invoke(this);
        }

        private void ResetInputWithoutRelease()
        {
            _activePointerId = NO_POINTER_ID;
            ScreenPositionPixels = Vector2.zero;
            _lookDeltaPixels = Vector2.zero;
            _shouldResetLookControl = false;
            IsPressed = false;
        }

        #endregion
    }
}
