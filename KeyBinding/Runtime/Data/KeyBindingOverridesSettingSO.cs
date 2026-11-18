using FakeMG.Framework;
using FakeMG.Settings;
using UnityEngine;

namespace FakeMG.KeyBinding
{
    /// <summary>Input System override JSON; empty means every action uses its authored bindings.</summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.KEY_BINDING + "/Key Binding Overrides Setting")]
    public sealed class KeyBindingOverridesSettingSO : SettingDefinitionGenericSO<string>
    {
    }
}
