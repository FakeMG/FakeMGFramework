using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.GridSystem
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.GRID_SYSTEM + "/StructureSO")]
    public class StructureSO : IdentitySO
    {
        [SerializeField] private AssetReferenceGameObject _structureAsset;

        public AssetReferenceGameObject StructureAsset => _structureAsset;
    }

}
