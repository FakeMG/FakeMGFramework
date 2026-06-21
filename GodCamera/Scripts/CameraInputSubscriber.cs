using System;
using FakeMG.ActionMapManagement;
using FakeMG.Framework;
using FakeMG.Framework.EventBus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Owns camera input bindings and translates raw Input System callbacks into camera commands.
    /// </summary>
    public class CameraInputSubscriber : MonoBehaviour
    {
        private const float LEGACY_MOUSE_SCROLL_STEP_UNITS = 120f;
        private const float LARGE_MOUSE_SCROLL_DELTA_THRESHOLD = 10f;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference _moveActionReference;
        [SerializeField] private InputActionReference _zoomActionReference;
        [SerializeField] private InputActionReference _pointerMovementActionReference;
        [SerializeField] private InputActionReference _dragPanActionReference;
        [SerializeField] private InputActionReference _rotateLeftActionReference;
        [SerializeField] private InputActionReference _rotateRightActionReference;
        [SerializeField] private ActionMapSO _cameraActionMapSO;

        private float _pendingPointerZoomSteps;
        private bool _isPointerDragPanStartPending;

        public event Action OnPointerDragPanStarted;
        public event Action OnPointerDragPanStopped;
        public event Action<int> OnRotationStepRequested;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _zoomActionReference.action.performed += QueuePointerZoomWhenPerformed;
            _dragPanActionReference.action.started += StartPointerDragPanWhenPressed;
            _dragPanActionReference.action.canceled += StopPointerDragPanWhenReleased;
            _rotateLeftActionReference.action.performed += RotateLeftWhenPerformed;
            _rotateRightActionReference.action.performed += RotateRightWhenPerformed;

            EventBus<EnableActionMapEvent>.Raise(new EnableActionMapEvent
            {
                ActionMap = _cameraActionMapSO
            });
        }

        private void LateUpdate()
        {
            StartPendingPointerDragPan();
        }

        private void OnDisable()
        {
            _zoomActionReference.action.performed -= QueuePointerZoomWhenPerformed;
            _dragPanActionReference.action.started -= StartPointerDragPanWhenPressed;
            _dragPanActionReference.action.canceled -= StopPointerDragPanWhenReleased;
            _rotateLeftActionReference.action.performed -= RotateLeftWhenPerformed;
            _rotateRightActionReference.action.performed -= RotateRightWhenPerformed;

            _pendingPointerZoomSteps = 0f;
            _isPointerDragPanStartPending = false;
            OnPointerDragPanStopped?.Invoke();

            EventBus<DisableActionMapEvent>.Raise(new DisableActionMapEvent
            {
                ActionMap = _cameraActionMapSO
            });
        }

        #endregion

        #region Public Methods

        public Vector2 ReadMoveInput()
        {
            return _moveActionReference.action.ReadValue<Vector2>();
        }

        public Vector2 ReadPointerPositionPixels()
        {
            return _pointerMovementActionReference.action.ReadValue<Vector2>();
        }

        public float ReadControllerZoomInput()
        {
            InputAction zoomAction = _zoomActionReference.action;
            InputControl activeControl = zoomAction.activeControl;
            if (activeControl == null || activeControl.device is not Gamepad)
            {
                return 0f;
            }

            return zoomAction.ReadValue<float>();
        }

        public float ConsumePointerZoomSteps()
        {
            float pointerZoomSteps = _pendingPointerZoomSteps;
            _pendingPointerZoomSteps = 0f;

            if (IsPointerOverUi())
            {
                return 0f;
            }

            return pointerZoomSteps;
        }

        #endregion

        #region Private Methods

        private void QueuePointerZoomWhenPerformed(InputAction.CallbackContext context)
        {
            if (!IsPointerInput(context))
            {
                return;
            }

            _pendingPointerZoomSteps += NormalizePointerZoomSteps(context.ReadValue<float>());
        }

        private void StartPointerDragPanWhenPressed(InputAction.CallbackContext context)
        {
            if (!IsPointerInput(context))
            {
                Echo.Warning(
                    $"Camera pointer drag received unsupported input from {context.control?.displayName}.",
                    context: this);
                return;
            }

            _isPointerDragPanStartPending = true;
        }

        private void StopPointerDragPanWhenReleased(InputAction.CallbackContext context)
        {
            _isPointerDragPanStartPending = false;
            OnPointerDragPanStopped?.Invoke();
        }

        private void RotateLeftWhenPerformed(InputAction.CallbackContext context)
        {
            OnRotationStepRequested?.Invoke(-1);
        }

        private void RotateRightWhenPerformed(InputAction.CallbackContext context)
        {
            OnRotationStepRequested?.Invoke(1);
        }

        private void StartPendingPointerDragPan()
        {
            if (!_isPointerDragPanStartPending)
            {
                return;
            }

            _isPointerDragPanStartPending = false;
            if (IsPointerOverUi())
            {
                return;
            }

            OnPointerDragPanStarted?.Invoke();
        }

        private float NormalizePointerZoomSteps(float rawZoomValue)
        {
            // Windows can report one wheel notch as 120 while other devices already report normalized steps.
            return Mathf.Abs(rawZoomValue) >= LARGE_MOUSE_SCROLL_DELTA_THRESHOLD
                ? rawZoomValue / LEGACY_MOUSE_SCROLL_STEP_UNITS
                : rawZoomValue;
        }

        private bool IsPointerInput(InputAction.CallbackContext context)
        {
            return context.control != null && context.control.device is Mouse;
        }

        private bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        #endregion
    }
}
