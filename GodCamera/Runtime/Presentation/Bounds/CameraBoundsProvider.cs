using UnityEngine;

namespace FakeMG.GodCamera
{
    public abstract class CameraBoundsProvider : MonoBehaviour
    {
        #region Public Methods

        public abstract Bounds GetBoundsMeters();

        #endregion
    }
}
