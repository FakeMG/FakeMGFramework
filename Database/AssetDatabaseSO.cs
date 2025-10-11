using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Framework.Database
{
    public abstract class AssetDatabaseSO<T> : SerializedScriptableObject where T : ItemSO
    {
        [SerializeField] protected AssetLabelReference label;

        [DictionaryDrawerSettings(KeyLabel = "ID", ValueLabel = "Asset Reference")]
        [SerializeField] protected Dictionary<string, T> Items = new();

        public T GetReference(string id)
        {
            Items.TryGetValue(id, out var reference);
            return reference;
        }

        public List<T> GetAllItems()
        {
            return new List<T>(Items.Values);
        }

#if UNITY_EDITOR
        [Button]
        private void RebuildDatabase()
        {
            Editor.DatabaseRebuilderUtility.BuildDatabase(label, this, Items);
        }
#endif
    }
}