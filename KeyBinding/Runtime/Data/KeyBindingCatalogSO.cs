using System;
using System.Collections.Generic;
using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding
{
    /// <summary>An action is listed only for the device families that have a binding for it.</summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.KEY_BINDING + "/Key Binding Catalog")]
    public sealed class KeyBindingCatalogSO : ScriptableObject
    {
        [InfoBox("Leave out actions bound to Escape, Start or the left and right mouse buttons: those stay reserved for menus and clicks.")]
        [SerializeField] private List<KeyBindingSection> _sections = new();

        public IReadOnlyList<KeyBindingSection> Sections => _sections;
    }

    [Serializable]
    public sealed class KeyBindingSection
    {
        [SerializeField] private string _title;
        [SerializeField] private List<RebindableAction> _actions = new();

        public string Title => _title;
        public IReadOnlyList<RebindableAction> Actions => _actions;
    }

    [Serializable]
    public sealed class RebindableAction
    {
        [SerializeField] private string _label;
        [Tooltip("Shown beside the list while the action is focused.")]
        [SerializeField, TextArea] private string _description;
        [Required, SerializeField] private InputActionReference _actionReference;
        [Tooltip("For a composite such as WASD movement, the part to rebind, e.g. 'up'. Empty for a plain action.")]
        [SerializeField] private string _compositePartName;

        public string Label => _label;
        public string Description => _description;
        public InputAction Action => _actionReference.action;
        public string CompositePartName => _compositePartName;
        public bool IsCompositePart => !string.IsNullOrEmpty(_compositePartName);
    }
}
