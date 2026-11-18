using System;
using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.InputHints;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding
{
    /// <summary>
    /// Devices whose bindings are listed and rebound together, such as keyboard and mouse, or any gamepad. The Controls list
    /// shows the family the player is using, judged by the glyph set the input hints draw.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.KEY_BINDING + "/Binding Device Family")]
    public sealed class BindingDeviceFamilySO : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] private string _displayName;
        [Tooltip("Shown in a binding slot while it waits for input, e.g. 'Press a key'.")]
        [SerializeField] private string _listeningPrompt;

        [Space]
        [Header("Glyphs")]
        [Tooltip("The Controls list switches to this family while the input hints draw one of these sets.")]
        [SerializeField] private List<InputGlyphSetSO> _glyphSetSOs = new();

        [Space]
        [Header("Bindings")]
        [Tooltip("The binding group, i.e. control scheme, whose bindings this family lists, e.g. 'Keyboard&Mouse'. An empty " +
                 "binding, kept for a key the player may add, belongs to a family only through its group.")]
        [SerializeField] private string _bindingGroup;
        [Tooltip("How many bindings of one action are listed, e.g. a primary and a secondary key. Author an empty binding in " +
                 "the family's group for each slot an action leaves free, so the player can fill it.")]
        [SerializeField, Min(1)] private int _slotsPerAction = 1;

        [Space]
        [Header("Rebinding")]
        [Tooltip("Controls a new binding may use; children of a listed control are included.")]
        [SerializeField] private string[] _rebindableControlPaths = Array.Empty<string>();
        [Tooltip("Controls never captured as a new binding, e.g. sticks for a button, which drift.")]
        [SerializeField] private string[] _excludedControlPaths = Array.Empty<string>();
        [Tooltip("Stops listening for a new binding and keeps the old one.")]
        [SerializeField] private string _cancelControlPath;

        public string DisplayName => _displayName;
        public string ListeningPrompt => _listeningPrompt;
        public string BindingGroup => _bindingGroup;
        public int SlotsPerAction => _slotsPerAction;
        public IReadOnlyList<string> ExcludedControlPaths => _excludedControlPaths;
        public string CancelControlPath => _cancelControlPath;

        public string CancelControlName =>
            InputControlPath.ToHumanReadableString(_cancelControlPath, InputControlPath.HumanReadableStringOptions.OmitDevice);

        #region Public Methods

        public bool IsDrawnWith(InputGlyphSetSO glyphSetSO)
        {
            return _glyphSetSOs.Contains(glyphSetSO);
        }

        public bool IsRebindable(InputControl control)
        {
            foreach (string rebindableControlPath in _rebindableControlPaths)
            {
                if (InputControlPath.MatchesPrefix(rebindableControlPath, control))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Owns(InputBinding binding)
        {
            return InputBinding.MaskByGroup(_bindingGroup).Matches(binding);
        }

        #endregion
    }
}
