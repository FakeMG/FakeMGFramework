using System.Collections.Generic;
using FakeMG.Framework;

namespace FakeMG.ItemInteraction
{
    public sealed class InteractionControllerResolver
    {
        private readonly IReadOnlyList<IInteractionController> _interactionControllers;

        public InteractionControllerResolver(IReadOnlyList<IInteractionController> interactionControllers)
        {
            _interactionControllers = interactionControllers;
        }

        #region Public Methods

        public bool TryResolve(IInteractionItem selectedItem, ItemActionSO actionSO, out IInteractionController resolvedInteractionController)
        {
            resolvedInteractionController = null;
            int matchingControllerCount = 0;
            foreach (IInteractionController interactionController in _interactionControllers)
            {
                if (!interactionController.Supports(selectedItem, actionSO))
                {
                    continue;
                }

                matchingControllerCount++;
                resolvedInteractionController = interactionController;
            }

            string actionName = actionSO != null ? actionSO.name : "default action";
            if (matchingControllerCount == 0)
            {
                Echo.Warning($"Cannot begin {actionName} for {selectedItem.DisplayName} because no interaction controller supports it.");
                return false;
            }

            if (matchingControllerCount > 1)
            {
                Echo.Warning(
                    $"Cannot begin {actionName} for {selectedItem.DisplayName} because {matchingControllerCount} controllers "
                    + "support it; exactly one is required.");
                resolvedInteractionController = null;
                return false;
            }

            return true;
        }

        #endregion
    }
}
