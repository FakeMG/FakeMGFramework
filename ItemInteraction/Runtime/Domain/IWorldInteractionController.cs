using UnityEngine;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Handles world targets which must claim the shared Character primary-use gesture before
    /// inventory item interaction is resolved.
    /// </summary>
    public interface IWorldInteractionController
    {
        int Priority { get; }
        bool IsInteractionActive { get; }
        bool TryBeginInteraction(Ray targetRay, Vector3 interactionOriginPositionMeters,
            Vector2 screenPositionPixels);
        void MovePointer(Ray targetRay, Vector2 screenPositionPixels);
        void Tick(float deltaSeconds);
        void ReleaseInteraction();
        void CancelInteraction();
    }
}
