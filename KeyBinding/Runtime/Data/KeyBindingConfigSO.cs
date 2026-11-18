using System.Collections.Generic;
using FakeMG.ActionMapManagement;
using FakeMG.Framework;
using FakeMG.InputHints;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.KEY_BINDING + "/Key Binding Config")]
    public sealed class KeyBindingConfigSO : ScriptableObject
    {
        [Header("Actions")]
        [Tooltip("The project-wide actions that overrides are applied to.")]
        [Required, SerializeField] private InputActionAsset _inputActions;
        [Required, SerializeField] private KeyBindingCatalogSO _keyBindingCatalogSO;
        [Tooltip("Maps that are never live together may share keys; every other pair of maps may not.")]
        [Required, SerializeField] private ActionMapConflictsPairsSO _actionMapConflictsPairsSO;

        [Space]
        [Header("Devices")]
        [Tooltip("The first family is listed while the input hints draw a glyph set no family claims.")]
        [Required, SerializeField] private List<BindingDeviceFamilySO> _bindingDeviceFamilySOs = new();

        [Space]
        [Header("Persistence")]
        [Required, SerializeField] private KeyBindingOverridesSettingSO _keyBindingOverridesSettingSO;

        public InputActionAsset InputActions => _inputActions;
        public KeyBindingCatalogSO KeyBindingCatalogSO => _keyBindingCatalogSO;
        public ActionMapConflictsPairsSO ActionMapConflictsPairsSO => _actionMapConflictsPairsSO;
        public IReadOnlyList<BindingDeviceFamilySO> BindingDeviceFamilySOs => _bindingDeviceFamilySOs;
        public KeyBindingOverridesSettingSO KeyBindingOverridesSettingSO => _keyBindingOverridesSettingSO;

        #region Public Methods

        public BindingDeviceFamilySO FindDeviceFamilyDrawnWith(InputGlyphSetSO glyphSetSO)
        {
            foreach (BindingDeviceFamilySO deviceFamilySO in _bindingDeviceFamilySOs)
            {
                if (deviceFamilySO.IsDrawnWith(glyphSetSO))
                {
                    return deviceFamilySO;
                }
            }

            return _bindingDeviceFamilySOs[0];
        }

        #endregion
    }
}
