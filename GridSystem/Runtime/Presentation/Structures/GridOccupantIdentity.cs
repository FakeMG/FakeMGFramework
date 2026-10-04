using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Carries the stable placement instance id on a runtime structure GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridOccupantIdentity : MonoBehaviour
    {
        public string InstanceId { get; private set; }

        #region Public Methods

        public void Initialize(string instanceId)
        {
            InstanceId = instanceId;
        }

        #endregion
    }
}
