using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Provides the world-space region within which the camera's visible ground area must remain.
    /// </summary>
    public abstract class CameraBoundsProvider : MonoBehaviour
    {
        #region Public Methods

        public abstract Bounds GetBoundsMeters();

        #endregion
    }
}
