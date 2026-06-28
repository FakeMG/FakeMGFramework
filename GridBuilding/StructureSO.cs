using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.GridBuilding
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.GRID_BUILDING + "/StructureSO")]
    public class StructureSO : IdentitySO
    {
        [SerializeField] private AssetReferenceGameObject _structureAsset;

        public AssetReferenceGameObject StructureAsset => _structureAsset;
    }
}
