using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.Framework.ActionMapManagement
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/ActionMapConfigSO")]
    public class ActionMapConfigSO : ScriptableObject
    {
        [SerializeField] private InputActionAsset inputAsset;
        [ActionMapName]
        [SerializeField] private string actionMapName;
        [SerializeField] private List<ConflictMap> conflictsWith = new();

        public string ActionMapName => actionMapName; // Public getter

        public List<string> ConflictsWith =>
            conflictsWith.ConvertAll(c => c.actionMapName); // Runtime getter for string list

        public bool isAlwaysEnabled;
    }

    [Serializable]
    public class ConflictMap
    {
        [ActionMapName]
        public string actionMapName;
    }
}