using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Something a tracking pointer can aim at. Resolved every frame, so the anchor can
    /// move, animate, or disappear (a closed popup, a target behind the camera) and the
    /// pointer follows or hides accordingly.
    /// </summary>
    public interface ITutorialPointerAnchor
    {
        /// <summary>
        /// Returns false when the anchor is not currently visible to the player.
        /// </summary>
        bool TryGetScreenPositionPixels(out Vector2 screenPositionPixels);
    }
}
