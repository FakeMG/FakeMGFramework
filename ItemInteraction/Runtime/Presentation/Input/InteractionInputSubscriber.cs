using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace FakeMG.ItemInteraction
{
    public sealed class InteractionInputSubscriber : MonoBehaviour
    {
        [SerializeField] private InputActionReference _primaryUseActionReference;
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private Transform _interactionOrigin;
        [SerializeField] private ItemActionSO _emptyHandActionSO;

        private InteractionInputState _inputState;
        private IActiveItemSource _activeItemSource;
        private IInteractionInputBlocker _inputBlocker;
        private IReadOnlyList<IWorldInteractionPointerInputSource> _worldPointerInputSources;
        private IReadOnlyList<IUsableItemInteractionPointerInputSource> _usableItemPointerInputSources;
        private IInteractionPointerInputSource _activePointerInputSource;
        private bool _isInjected;
        private bool _isSubscribed;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_isInjected)
            {
                Subscribe();
            }
        }

        private void Update()
        {
            if (!_isInjected)
            {
                return;
            }

            if (!_inputState.HasActiveInteraction)
            {
                _inputState.Tick(Time.deltaTime);
                return;
            }

            if (!TryReadValidPointerPositions(out Vector2 pointerScreenPositionPixels, out Vector2 itemTargetScreenPositionPixels))
            {
                _inputState.Cancel();
                _activePointerInputSource = null;
                return;
            }
            _inputState.MovePointer(
                _worldCamera.ScreenPointToRay(itemTargetScreenPositionPixels), itemTargetScreenPositionPixels,
                _worldCamera.ScreenPointToRay(pointerScreenPositionPixels), pointerScreenPositionPixels);
            _inputState.Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            Unsubscribe();
            _inputState?.Cancel();
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(InteractionInputState inputState,
            IActiveItemSource activeItemSource,
            IInteractionInputBlocker inputBlocker,
            IReadOnlyList<IWorldInteractionPointerInputSource> worldPointerInputSources,
            IReadOnlyList<IUsableItemInteractionPointerInputSource> usableItemPointerInputSources)
        {
            _inputState = inputState;
            _activeItemSource = activeItemSource;
            _inputBlocker = inputBlocker;
            _worldPointerInputSources = worldPointerInputSources;
            _usableItemPointerInputSources = usableItemPointerInputSources;
            _isInjected = true;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(InputActionReference primaryUseActionReference, Camera worldCamera, Transform interactionOrigin)
        {
            _primaryUseActionReference = primaryUseActionReference;
            _worldCamera = worldCamera;
            _interactionOrigin = interactionOrigin;
        }
#endif

        #endregion

        #region Private Methods

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            _primaryUseActionReference.action.started += BeginDesktopUse;
            _primaryUseActionReference.action.canceled += EndDesktopUse;
            foreach (IWorldInteractionPointerInputSource pointerInputSource in _worldPointerInputSources)
            {
                pointerInputSource.OnPressed += BeginWorldPointerUse;
                pointerInputSource.OnReleased += EndPointerUse;
            }

            foreach (IUsableItemInteractionPointerInputSource pointerInputSource in _usableItemPointerInputSources)
            {
                pointerInputSource.OnPressed += BeginUsableItemPointerUse;
                pointerInputSource.OnReleased += EndPointerUse;
            }
            _inputBlocker.OnInputBlockedChanged += CancelUseWhenInputIsBlocked;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _primaryUseActionReference.action.started -= BeginDesktopUse;
            _primaryUseActionReference.action.canceled -= EndDesktopUse;
            foreach (IWorldInteractionPointerInputSource pointerInputSource in _worldPointerInputSources)
            {
                pointerInputSource.OnPressed -= BeginWorldPointerUse;
                pointerInputSource.OnReleased -= EndPointerUse;
            }

            foreach (IUsableItemInteractionPointerInputSource pointerInputSource in _usableItemPointerInputSources)
            {
                pointerInputSource.OnPressed -= BeginUsableItemPointerUse;
                pointerInputSource.OnReleased -= EndPointerUse;
            }
            _inputBlocker.OnInputBlockedChanged -= CancelUseWhenInputIsBlocked;
            _isSubscribed = false;
        }

        private void BeginDesktopUse(InputAction.CallbackContext context)
        {
            if (context.control.device is not Touchscreen)
            {
                BeginUse();
            }
        }

        private void EndDesktopUse(InputAction.CallbackContext context)
        {
            if (context.control.device is not Touchscreen)
            {
                _inputState.Release();
            }
        }

        private void BeginWorldPointerUse(IInteractionPointerInputSource pointerInputSource)
        {
            if (_activeItemSource.ActiveItem is IUsableItem)
            {
                BeginWorldOnlyPointerUse(pointerInputSource);
                return;
            }

            BeginPointerUse(pointerInputSource);
        }

        private void BeginWorldOnlyPointerUse(IInteractionPointerInputSource pointerInputSource)
        {
            _activePointerInputSource = pointerInputSource;
            if (_inputBlocker.IsInputBlocked)
            {
                return;
            }

            if (!TryReadValidPointerPositions(out Vector2 pointerScreenPositionPixels, out _)) return;

            _inputState.TryBeginWorldInteraction(
                _worldCamera.ScreenPointToRay(pointerScreenPositionPixels),
                _interactionOrigin.position,
                pointerScreenPositionPixels);
        }

        private void BeginUsableItemPointerUse(IInteractionPointerInputSource pointerInputSource)
        {
            if (_activeItemSource.ActiveItem != null && _activeItemSource.ActiveItem is not IUsableItem)
            {
                return;
            }

            BeginPointerUse(pointerInputSource);
        }

        private void BeginPointerUse(IInteractionPointerInputSource pointerInputSource)
        {
            _activePointerInputSource = pointerInputSource;
            BeginUse();
        }

        private void EndPointerUse(IInteractionPointerInputSource pointerInputSource)
        {
            if (_activePointerInputSource != pointerInputSource)
            {
                return;
            }

            _activePointerInputSource = null;
            _inputState.Release();
        }

        private void BeginUse()
        {
            if (_inputBlocker.IsInputBlocked)
            {
                return;
            }

            IInteractionItem selectedItem = _activeItemSource.ActiveItem;
            ItemActionSO actionSO = (selectedItem as IUsableItem)?.PrimaryActionSO;
            if (!TryReadValidPointerPositions(out Vector2 pointerScreenPositionPixels, out Vector2 itemTargetScreenPositionPixels))
            {
                return;
            }

            bool isShootInput = _activePointerInputSource is IUsableItemInteractionPointerInputSource ||
                (_activePointerInputSource == null && selectedItem == null);
            if (isShootInput)
            {
                _inputState.PressItem(selectedItem, selectedItem == null ? _emptyHandActionSO : actionSO,
                    _worldCamera.ScreenPointToRay(itemTargetScreenPositionPixels), _interactionOrigin.position, itemTargetScreenPositionPixels);
                return;
            }

            _inputState.Press(
                selectedItem, actionSO,
                _worldCamera.ScreenPointToRay(itemTargetScreenPositionPixels),
                _worldCamera.ScreenPointToRay(pointerScreenPositionPixels),
                _interactionOrigin.position, itemTargetScreenPositionPixels, pointerScreenPositionPixels);
        }

        private void CancelUseWhenInputIsBlocked(bool isInputBlocked)
        {
            if (isInputBlocked)
            {
                _inputState.Cancel();
            }
        }

        private Vector2 GetPointerScreenPositionPixels()
        {
            return _activePointerInputSource?.IsPressed == true
                ? _activePointerInputSource.ScreenPositionPixels
                : Mouse.current?.position.ReadValue() ?? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private bool TryReadValidPointerPositions(out Vector2 pointerScreenPositionPixels, out Vector2 itemTargetScreenPositionPixels)
        {
            pointerScreenPositionPixels = GetPointerScreenPositionPixels();
            itemTargetScreenPositionPixels = ResolveItemTargetScreenPositionPixels(pointerScreenPositionPixels);
            if (float.IsFinite(pointerScreenPositionPixels.x) && float.IsFinite(pointerScreenPositionPixels.y)
                && float.IsFinite(itemTargetScreenPositionPixels.x) && float.IsFinite(itemTargetScreenPositionPixels.y))
            {
                return true;
            }

            Echo.Warning("Ignoring interaction because the pointer or item target screen position is not finite.", context: this);
            return false;
        }

        private Vector2 ResolveItemTargetScreenPositionPixels(Vector2 pointerScreenPositionPixels)
        {
            if (_activeItemSource.ActiveItem is not IUsableItem usableItem)
            {
                return pointerScreenPositionPixels;
            }

            return usableItem.PrimaryActionSO.TargetingProfileSO.ResolveScreenPositionPixels(
                pointerScreenPositionPixels, new Vector2(Screen.width, Screen.height));
        }

        #endregion
    }
}
