using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Carries the stable placement instance id and the placed definition on a runtime structure GameObject,
    /// so occupant components can read their definition without duplicating it on the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridOccupantIdentity : MonoBehaviour
    {
        public string InstanceId { get; private set; }
        public StructureSO StructureSO { get; private set; }

        #region Public Methods

        public void Initialize(string instanceId, StructureSO structureSO)
        {
            InstanceId = instanceId;
            StructureSO = structureSO;
        }

        #endregion
    }
}
