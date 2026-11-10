using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class HeldItemPresentation
    {
        private readonly IReadOnlyList<IHeldItemSelectionObserver> _selectionObservers;

        public HeldItemPresentation(IReadOnlyList<IHeldItemSelectionObserver> selectionObservers)
        {
            _selectionObservers = selectionObservers;
        }

        #region Public Methods

        public void ApplyHandPoseAndNotifyShown(IUsableItem item, GameObject heldItemInstance)
        {
            heldItemInstance.transform.SetLocalPositionAndRotation(
                item.RightHandLocalPositionMeters,
                Quaternion.Euler(item.RightHandLocalRotationDegrees));

            foreach (IHeldItemSelectionObserver selectionObserver in _selectionObservers)
            {
                selectionObserver.HandleHeldItemShown(item, heldItemInstance);
            }
        }

        public void NotifyHidden(GameObject heldItemInstance)
        {
            foreach (IHeldItemSelectionObserver selectionObserver in _selectionObservers)
            {
                selectionObserver.HandleHeldItemHidden(heldItemInstance);
            }
        }

        #endregion
    }
}
