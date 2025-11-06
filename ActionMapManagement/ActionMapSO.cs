using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.ActionMapManagement
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ACTION_MAP_MANAGEMENT + "/ActionMapSO")]
    public class ActionMapSO : ScriptableObject
    {
        [SerializeField]
        [ValidateInput(nameof(ValidateNamingPattern))]
        private string _actionMapName;

        public string ActionMapName => _actionMapName;

        private const string SUFFIX = " Action Map";

        private bool ValidateNamingPattern(string value, ref string errorMessage)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            string expectedAssetName = value + SUFFIX;
            if (name == expectedAssetName)
                return true;

            errorMessage = $"Action map name and asset name don't match the pattern.\nAsset name should be '{value}{SUFFIX}'";
            return false;
        }

        [Button]
        private void SetMapNameFromAssetName()
        {
            if (string.IsNullOrEmpty(name))
                return;

            // Remove " Action Map" suffix from asset name
            if (name.EndsWith(SUFFIX))
            {
                _actionMapName = name.Substring(0, name.Length - SUFFIX.Length);
            }
            else
            {
                _actionMapName = name;
            }
        }

#if UNITY_EDITOR
        [Button]
        private void SetAssetNameFromMapName()
        {
            if (string.IsNullOrEmpty(_actionMapName)) return;

            // Add " Action Map" suffix to create asset name
            string newAssetName = _actionMapName + SUFFIX;
            if (name == newAssetName) return;

            UnityEditor.AssetDatabase.RenameAsset(UnityEditor.AssetDatabase.GetAssetPath(this), newAssetName);
        }
#endif
    }
}