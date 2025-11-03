using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Framework.Database
{
    public abstract class DatabaseSO<T> : SerializedScriptableObject where T : ScriptableObject, IIdentifiable
    {
        [SerializeField] protected AssetLabelReference _label;

        [DictionaryDrawerSettings(KeyLabel = "ID", ValueLabel = "Asset Reference")]
        [SerializeField] protected Dictionary<string, T> _items = new();

        public T GetAssetByID(string id)
        {
            _items.TryGetValue(id, out T reference);
            return reference;
        }

        public IReadOnlyList<T> GetAllAssets()
        {
            return new List<T>(_items.Values);
        }

#if UNITY_EDITOR
        [Button]
        private void RebuildDatabase()
        {
            Editor.DatabaseRebuilderUtility.BuildDatabase(_label, this, _items);
        }
#endif
    }
}
