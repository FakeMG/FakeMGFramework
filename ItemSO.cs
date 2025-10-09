#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Framework
{
    /// <summary>
    /// Base class for all items in the game
    /// </summary>
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Item")]
    public class ItemSO : ScriptableObject
    {
        [Header("Item Info")]
        [Required]
        [SerializeField] private string id;
        [SerializeField] private string itemName;
        [SerializeField, TextArea(3, 5)] protected string description;
        [PreviewField(75, ObjectFieldAlignment.Left)]
        [SerializeField] private AssetReferenceSprite iconSpriteAsset;
        [SerializeField] private AssetReferenceGameObject prefabAsset;

        public string ID => id;
        public string ItemName => itemName;
        public string Description => description;
        public AssetReferenceSprite IconSpriteAsset => iconSpriteAsset;
        public AssetReferenceGameObject PrefabAsset => prefabAsset;

#if UNITY_EDITOR
        [Button("Set ID From Name")]
        private void SetIDFromName()
        {
            id = itemName.Replace(" ", "_").ToLowerInvariant();
        }

        [Button]
        private void SetFileNameFromName()
        {
            if (string.IsNullOrEmpty(itemName))
            {
                Debug.LogWarning("Item name is empty, cannot set file name.");
                return;
            }

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath)) return;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (fileName != itemName)
            {
                UnityEditor.AssetDatabase.RenameAsset(assetPath, itemName);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }

        [Button("Set ID From File Name")]
        private void SetIDFromFileName()
        {
            if (string.IsNullOrEmpty(name)) return;
            id = name.Replace(" ", "_").ToLowerInvariant();
        }

        [Button]
        private void SetNameFromFileName()
        {
            if (string.IsNullOrEmpty(name)) return;
            itemName = name.Replace("_", " ");
        }
#endif
    }
}