using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Framework
{
    /// <summary>
    /// Base class for all items in the game
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/ItemSO")]
    public class ItemSO : ScriptableObject
    {
        [Header("Item Info")]
        [Required]
        [SerializeField] private string _id;
        [SerializeField] private string _itemName;
        [SerializeField, TextArea(3, 5)] protected string _description;
        [PreviewField(75, ObjectFieldAlignment.Left)]
        [SerializeField] private AssetReferenceSprite _iconSpriteAsset;
        [SerializeField] private AssetReferenceGameObject _prefabAsset;

        public string ID => _id;
        public string ItemName => _itemName;
        public string Description => _description;
        public AssetReferenceSprite IconSpriteAsset => _iconSpriteAsset;
        public AssetReferenceGameObject PrefabAsset => _prefabAsset;

#if UNITY_EDITOR
        [Button("Set ID From Name")]
        private void SetIDFromName()
        {
            _id = _itemName.Replace(" ", "_").ToLowerInvariant();
        }

        [Button]
        private void SetFileNameFromName()
        {
            if (string.IsNullOrEmpty(_itemName))
            {
                Debug.LogWarning("Item name is empty, cannot set file name.");
                return;
            }

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath)) return;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (fileName != _itemName)
            {
                UnityEditor.AssetDatabase.RenameAsset(assetPath, _itemName);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }

        [Button("Set ID From File Name")]
        private void SetIDFromFileName()
        {
            if (string.IsNullOrEmpty(name)) return;
            _id = name.Replace(" ", "_").ToLowerInvariant();
        }

        [Button]
        private void SetNameFromFileName()
        {
            if (string.IsNullOrEmpty(name)) return;
            _itemName = name.Replace("_", " ");
        }
#endif
    }
}