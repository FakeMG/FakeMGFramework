using FakeMG.GridSystem;
using UnityEngine;

namespace FakeMG.GodCamera
{
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
