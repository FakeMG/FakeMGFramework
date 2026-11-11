using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class InteractionInputState : IInteractionSelectionLifecycle
    {
        private readonly InteractionControllerResolver _controllerResolver;
        private IInteractionController _activeController;
        private bool _areNormalActionsEnabled = true;

        public bool IsInputPressed { get; private set; }
        public bool RequiresFreshPress { get; private set; }
        public bool HasActiveInteraction => _activeController?.IsInteractionActive == true;
        public bool CanAcceptPress => _areNormalActionsEnabled && !IsInputPressed && !RequiresFreshPress && !HasActiveInteraction;

        public InteractionInputState(InteractionControllerResolver controllerResolver)
        {
            _controllerResolver = controllerResolver;
        }

        #region Public Methods

        public bool PressItem(
            IInteractionItem selectedItem,
            ItemActionSO actionSO,
            Ray targetRay,
            Vector3 interactionOriginPositionMeters,
            Vector2 screenPositionPixels)
        {
            if (!CanAcceptPress)
            {
                return false;
            }

            if (!_controllerResolver.TryResolve(selectedItem, actionSO, out _activeController))
            {
                return false;
            }

            IsInputPressed = true;
            if (!_activeController.TryBeginInteraction(
                    selectedItem, actionSO, targetRay, interactionOriginPositionMeters, screenPositionPixels))
            {
                Echo.Warning("The resolved interaction controller rejected the interaction start.");
                _activeController = null;
                return false;
            }

            if (!HasActiveInteraction)
            {
                Echo.Error("The interaction controller accepted a start but did not become active.");
                _activeController = null;
                return false;
            }

            return true;
        }

        public void MovePointer(Ray targetRay, Vector2 screenPositionPixels)
        {
            _activeController?.MovePointer(targetRay, screenPositionPixels);
        }

        public void Tick(float deltaSeconds)
        {
            if (_activeController == null)
            {
                return;
            }

            if (deltaSeconds < 0f)
            {
                Echo.Warning($"Ignoring negative interaction delta {deltaSeconds} seconds.");
                return;
            }

            _activeController.Tick(deltaSeconds);
            ClearCompletedInteraction();
        }

        public void Release()
        {
            IsInputPressed = false;
            RequiresFreshPress = false;
            _activeController?.ReleaseInteraction();
            ClearCompletedInteraction();
        }

        public void ChangeSelection()
        {
            CancelActiveInteraction();
            RequiresFreshPress = IsInputPressed;
        }

        public void Cancel()
        {
            CancelActiveInteraction();
            IsInputPressed = false;
            RequiresFreshPress = false;
        }

        public void SetNormalActionsEnabled(bool areEnabled)
        {
            if (_areNormalActionsEnabled == areEnabled)
            {
                return;
            }

            _areNormalActionsEnabled = areEnabled;
            if (!areEnabled)
            {
                Cancel();
            }
        }

        #endregion

        #region Private Methods

        private void ClearCompletedInteraction()
        {
            if (!HasActiveInteraction)
            {
                _activeController = null;
            }
        }

        private void CancelActiveInteraction()
        {
            IInteractionController controller = _activeController;
            _activeController = null;
            controller?.CancelInteraction();
        }

        #endregion
    }
}
