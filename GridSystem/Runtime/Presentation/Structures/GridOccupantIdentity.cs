using UnityEngine;

namespace FakeMG.GridSystem
{
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
