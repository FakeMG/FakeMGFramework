using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.FakeMGFramework
{
    /// <summary>
    /// Base class for all items in the game
    /// </summary>
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Item")]
    public class ItemBaseSO : SerializedScriptableObject
    {
        [Header("Item Info")]
        [Required]
        [SerializeField] private string id;
        [SerializeField] private string itemName;
        [SerializeField, TextArea(3, 5)] protected string description;
        [PreviewField(75, ObjectFieldAlignment.Left)]
        [SerializeField] private AssetReferenceT<Sprite> iconSpriteAsset;
        [SerializeField] private Dictionary<ItemBaseSO, int> _price = new();

        public string ID => id;
        public string ItemName => itemName;
        public string Description => description;
        public AssetReferenceT<Sprite> IconSpriteAsset => iconSpriteAsset;
        public Dictionary<ItemBaseSO, int> Price => _price;

#if UNITY_EDITOR
        [Button("Set ID from Name")]
        private void SetIDFromName()
        {
            id = itemName.Replace(" ", "_").ToLowerInvariant();
        }

        [Button("Set ID from File Name")]
        private void SetIDFromFileName()
        {
            if (string.IsNullOrEmpty(name)) return;
            id = name.Replace(" ", "_").ToLowerInvariant();
        }

        [Button("Set Name from File Name")]
        private void SetNameFromFileName()
        {
            if (string.IsNullOrEmpty(name)) return;
            itemName = name.Replace("_", " ");
        }
#endif
    }
}