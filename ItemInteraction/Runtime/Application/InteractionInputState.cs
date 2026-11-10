using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.ItemInteraction
{
    public sealed class InteractionInputState
    {
        private readonly InteractionControllerResolver _controllerResolver;
        private readonly IReadOnlyList<IWorldInteractionController> _worldInteractionControllers;

        private IInteractionController _activeController;
        private IWorldInteractionController _activeWorldInteractionController;
        private bool _isInputPressed;
        private bool _requiresFreshPress;
        private bool _areNormalActionsEnabled = true;

        public bool IsInputPressed => _isInputPressed;
        public bool RequiresFreshPress => _requiresFreshPress;
        public bool HasActiveInteraction => _activeController?.IsInteractionActive == true ||
            _activeWorldInteractionController?.IsInteractionActive == true;

        public InteractionInputState(InteractionControllerResolver controllerResolver)
            : this(controllerResolver, Array.Empty<IWorldInteractionController>())
        {
        }

        [Inject]
        public InteractionInputState(
            InteractionControllerResolver controllerResolver,
            IReadOnlyList<IWorldInteractionController> worldInteractionControllers)
        {
            _controllerResolver = controllerResolver;
            _worldInteractionControllers = SortWorldInteractionControllers(worldInteractionControllers);
        }

        #region Public Methods

        // No one uses this overload
        public bool Press(
            IInteractionItem selectedItem,
            ItemActionSO actionSO,
            Ray targetRay,
            Vector3 interactionOriginPositionMeters,
            Vector2 screenPositionPixels)
        {
            return Press(selectedItem, actionSO, targetRay, targetRay, interactionOriginPositionMeters, screenPositionPixels, screenPositionPixels);
        }

        public bool Press(IInteractionItem selectedItem, ItemActionSO actionSO, Ray itemTargetRay,
            Ray worldTargetRay, Vector3 interactionOriginPositionMeters, Vector2 itemTargetScreenPositionPixels,
            Vector2 worldTargetScreenPositionPixels)
        {
            return BeginPress(selectedItem, actionSO, itemTargetRay, worldTargetRay, interactionOriginPositionMeters,
                itemTargetScreenPositionPixels, worldTargetScreenPositionPixels, true);
        }

        public bool PressItem(IInteractionItem selectedItem, ItemActionSO actionSO, Ray targetRay,
            Vector3 interactionOriginPositionMeters, Vector2 screenPositionPixels)
        {
            return BeginPress(selectedItem, actionSO, targetRay, targetRay, interactionOriginPositionMeters,
                screenPositionPixels, screenPositionPixels, false);
        }

        public bool TryBeginWorldInteraction(Ray worldTargetRay, Vector3 interactionOriginPositionMeters, Vector2 worldTargetScreenPositionPixels)
        {
            if (!_areNormalActionsEnabled)
            {
                Echo.Warning("Ignoring world interaction press because normal character actions are unavailable.");
                return false;
            }

            if (_isInputPressed || _requiresFreshPress || HasActiveInteraction)
            {
                Echo.Warning("Ignoring world interaction press because input is already held or a fresh press is required.");
                return false;
            }

            _isInputPressed = true;
            if (TryBeginWorldInteractionInternal(worldTargetRay, interactionOriginPositionMeters, worldTargetScreenPositionPixels))
            {
                return true;
            }

            _isInputPressed = false;
            return false;
        }

        public void MovePointer(Ray targetRay, Vector2 screenPositionPixels)
        {
            MovePointer(targetRay, screenPositionPixels, targetRay, screenPositionPixels);
        }

        public void MovePointer(
            Ray itemTargetRay,
            Vector2 itemTargetScreenPositionPixels,
            Ray worldTargetRay,
            Vector2 worldTargetScreenPositionPixels)
        {
            if (_activeWorldInteractionController != null)
            {
                _activeWorldInteractionController.MovePointer(worldTargetRay, worldTargetScreenPositionPixels);
                return;
            }

            _activeController?.MovePointer(itemTargetRay, itemTargetScreenPositionPixels);
        }

        public void Tick(float deltaSeconds)
        {
            if (_activeWorldInteractionController != null)
            {
                if (deltaSeconds < 0f)
                {
                    Echo.Warning($"Ignoring negative world interaction delta {deltaSeconds} seconds.");
                    return;
                }

                _activeWorldInteractionController.Tick(deltaSeconds);
                ClearCompletedWorldInteractionController();
                return;
            }

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
            ClearCompletedController();
        }

        public void Release()
        {
            _isInputPressed = false;
            _requiresFreshPress = false;
            _activeWorldInteractionController?.ReleaseInteraction();
            _activeController?.ReleaseInteraction();
            ClearCompletedController();
        }

        public void ChangeSelection()
        {
            _activeWorldInteractionController?.CancelInteraction();
            _activeController?.CancelInteraction();
            ClearActiveController();
            if (_isInputPressed)
            {
                _requiresFreshPress = true;
            }
        }

        public void Cancel()
        {
            _activeWorldInteractionController?.CancelInteraction();
            _activeController?.CancelInteraction();
            ClearActiveController();
            _isInputPressed = false;
            _requiresFreshPress = false;
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

        private bool BeginPress(IInteractionItem selectedItem, ItemActionSO actionSO, Ray itemTargetRay,
            Ray worldTargetRay, Vector3 interactionOriginPositionMeters, Vector2 itemTargetScreenPositionPixels,
            Vector2 worldTargetScreenPositionPixels, bool canInteractWithWorld)
        {
            if (!_areNormalActionsEnabled)
            {
                // Popup controls legitimately share the primary pointer action. The popup's
                // CanvasGroup owns the click; gameplay input simply stays inactive here.
                return false;
            }

            if (_isInputPressed || _requiresFreshPress || HasActiveInteraction)
            {
                Echo.Warning("Ignoring interaction press because input is already held or a fresh press is required.");
                return false;
            }

            _isInputPressed = true;
            if (canInteractWithWorld && TryBeginWorldInteractionInternal(
                    worldTargetRay, interactionOriginPositionMeters, worldTargetScreenPositionPixels))
            {
                return true;
            }

            if (!_controllerResolver.TryResolve(selectedItem, actionSO, out _activeController))
            {
                _isInputPressed = false;
                return false;
            }

            if (!_activeController.TryBeginInteraction(
                    selectedItem, actionSO, itemTargetRay, interactionOriginPositionMeters, itemTargetScreenPositionPixels))
            {
                Echo.Warning("The resolved interaction controller rejected the interaction start.");
                ClearActiveController();
                return false;
            }

            if (!_activeController.IsInteractionActive)
            {
                Echo.Error("The interaction controller accepted a start but did not become active.");
                ClearActiveController();
                return false;
            }

            return true;
        }

        private void ClearCompletedController()
        {
            ClearCompletedWorldInteractionController();
            if (_activeController?.IsInteractionActive == false)
            {
                ClearActiveController();
            }
        }

        private bool TryBeginWorldInteractionInternal(Ray targetRay, Vector3 interactionOriginPositionMeters, Vector2 screenPositionPixels)
        {
            for (int controllerIndex = 0; controllerIndex < _worldInteractionControllers.Count; controllerIndex++)
            {
                IWorldInteractionController controller = _worldInteractionControllers[controllerIndex];
                if (!controller.TryBeginInteraction(targetRay, interactionOriginPositionMeters, screenPositionPixels))
                {
                    continue;
                }

                if (!controller.IsInteractionActive)
                {
                    Echo.Error("A world interaction controller accepted a start but did not become active.");
                    controller.CancelInteraction();
                    return false;
                }

                _activeWorldInteractionController = controller;
                return true;
            }

            return false;
        }

        private void ClearCompletedWorldInteractionController()
        {
            if (_activeWorldInteractionController?.IsInteractionActive == false)
            {
                _activeWorldInteractionController = null;
            }
        }

        private void ClearActiveController()
        {
            _activeController = null;
            _activeWorldInteractionController = null;
        }

        private static IReadOnlyList<IWorldInteractionController> SortWorldInteractionControllers(
            IReadOnlyList<IWorldInteractionController> controllers)
        {
            List<IWorldInteractionController> sortedControllers = new(controllers ?? Array.Empty<IWorldInteractionController>());
            sortedControllers.Sort(WorldInteractionControllerComparer.Instance);
            return sortedControllers;
        }

        private sealed class WorldInteractionControllerComparer : IComparer<IWorldInteractionController>
        {
            public static readonly WorldInteractionControllerComparer Instance = new();

            public int Compare(IWorldInteractionController first, IWorldInteractionController second)
            {
                return second.Priority.CompareTo(first.Priority);
            }
        }

        #endregion
    }
}
