using FakeMG.GridBuilding;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Uses the active placement grid's world bounds as the camera movement boundary.
    /// </summary>
    public class GridCameraBoundsProvider : CameraBoundsProvider
    {
        [SerializeField] private GridManager _gridManager;

        #region Public Methods

        public override Bounds GetBoundsMeters()
        {
            return _gridManager.GetWorldBoundsMeters();
        }

        #endregion
    }
}
