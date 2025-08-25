using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    [CreateAssetMenu(fileName = "Popup", menuName = "UI/Popup")]
    public class PopupSO : ScriptableObject
    {
        [Header("Asset Reference")]
        [Required]
        [SerializeField] private AssetReferenceT<GameObject> popupPrefabAsset;

        public AssetReferenceT<GameObject> PopupPrefabAsset => popupPrefabAsset;
    }
}
