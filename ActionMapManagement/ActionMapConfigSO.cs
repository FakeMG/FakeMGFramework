using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.Framework.ActionMapManagement
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/ActionMapConfigSO")]
    public class ActionMapConfigSO : ScriptableObject
    {
        [SerializeField] private InputActionAsset _inputAsset;
        [ActionMapName]
        [SerializeField] private string _actionMapName;
        [SerializeField] private List<ConflictMap> _conflictsWith = new();

        public string ActionMapName => _actionMapName; // Public getter

        public List<string> ConflictsWith =>
            _conflictsWith.ConvertAll(c => c.ActionMapName); // Runtime getter for string list

        public bool IsAlwaysEnabled;
    }

    [Serializable]
    public class ConflictMap
    {
        [ActionMapName]
        public string ActionMapName;
    }
}