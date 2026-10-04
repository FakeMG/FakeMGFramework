using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Provides a manually configured rectangular camera boundary on the ground plane.
    /// </summary>
    public class RectCameraBoundsProvider : CameraBoundsProvider
    {
        [SerializeField] private Vector2 _centerMeters;
        [SerializeField] private Vector2 _sizeMeters = new(20f, 20f);

        #region Public Methods

        public override Bounds GetBoundsMeters()
        {
            // Inspector XY values map to world XZ because camera movement occurs on the ground plane.
            return new Bounds(
                new Vector3(_centerMeters.x, 0f, _centerMeters.y),
                new Vector3(_sizeMeters.x, 1f, _sizeMeters.y));
        }

        #endregion
    }
}
