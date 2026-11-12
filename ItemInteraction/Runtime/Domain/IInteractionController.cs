using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public interface IInteractionController
    {
        bool IsInteractionActive { get; }

        /// <param name="selectedItem">Never null: an empty hand is its owner's empty-hand item.</param>
        bool Supports(IInteractionItem selectedItem, ItemActionSO actionSO);
        bool TryBeginInteraction(
            IInteractionItem selectedItem,
            ItemActionSO actionSO,
            Ray targetRay,
            Vector3 interactionOriginPositionMeters,
            Vector2 screenPositionPixels);
        void MovePointer(Ray targetRay, Vector2 screenPositionPixels);
        void Tick(float deltaSeconds);
        void ReleaseInteraction();
        void CancelInteraction();
    }
}
