using System.Collections.Generic;
using FakeMG.ActionMapManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding.Tests
{
    /// <summary>Builds a keyboard and mouse setup like a game's: actions in a control scheme that requires both devices.</summary>
    internal sealed class KeyBindingTestFactory
    {
        public const string KEYBOARD_MOUSE_GROUP = "Keyboard&Mouse";
        public const string MAP_NAME = "Gameplay";

        private readonly List<Object> _createdObjects = new();

        public InputActionAsset InputActions { get; }

        public KeyBindingTestFactory()
        {
            InputActions = Track(ScriptableObject.CreateInstance<InputActionAsset>());
            InputActions.AddControlScheme(KEYBOARD_MOUSE_GROUP).WithRequiredDevice("<Keyboard>").WithRequiredDevice("<Mouse>");
            InputActions.AddActionMap(MAP_NAME);
        }

        #region Public Methods

        /// <summary>An action with a primary key and an empty secondary slot, both in the keyboard and mouse group.</summary>
        public InputAction AddAction(string name, string primaryControlPath)
        {
            InputAction action = InputActions.FindActionMap(MAP_NAME).AddAction(name, InputActionType.Button, expectedControlLayout: "Button");
            action.AddBinding(primaryControlPath, groups: KEYBOARD_MOUSE_GROUP);
            action.AddBinding("", groups: KEYBOARD_MOUSE_GROUP);
            return action;
        }

        public BindingDeviceFamilySO CreateKeyboardMouseFamily(params string[] rebindableControlPaths)
        {
            var familySO = Track(ScriptableObject.CreateInstance<BindingDeviceFamilySO>());
            var serializedFamily = new SerializedObject(familySO);
            serializedFamily.FindProperty("_displayName").stringValue = "Keyboard & Mouse";
            serializedFamily.FindProperty("_bindingGroup").stringValue = KEYBOARD_MOUSE_GROUP;
            serializedFamily.FindProperty("_slotsPerAction").intValue = 2;
            serializedFamily.FindProperty("_cancelControlPath").stringValue = "<Keyboard>/escape";
            SetStrings(serializedFamily.FindProperty("_rebindableControlPaths"), rebindableControlPaths);
            SetStrings(serializedFamily.FindProperty("_excludedControlPaths"), new[] { "<Keyboard>/anyKey" });
            serializedFamily.ApplyModifiedPropertiesWithoutUndo();
            return familySO;
        }

        public KeyBindingConfigSO CreateConfig(BindingDeviceFamilySO familySO, params InputAction[] actions)
        {
            var configSO = Track(ScriptableObject.CreateInstance<KeyBindingConfigSO>());
            var serializedConfig = new SerializedObject(configSO);
            serializedConfig.FindProperty("_inputActions").objectReferenceValue = InputActions;
            serializedConfig.FindProperty("_keyBindingCatalogSO").objectReferenceValue = CreateCatalog(actions);
            serializedConfig.FindProperty("_actionMapConflictsPairsSO").objectReferenceValue =
                Track(ScriptableObject.CreateInstance<ActionMapConflictsPairsSO>());
            SerializedProperty families = serializedConfig.FindProperty("_bindingDeviceFamilySOs");
            families.arraySize = 1;
            families.GetArrayElementAtIndex(0).objectReferenceValue = familySO;
            serializedConfig.FindProperty("_keyBindingOverridesSettingSO").objectReferenceValue = CreateOverridesSetting();
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return configSO;
        }

        public void DestroyAll()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        #endregion

        #region Private Methods

        private KeyBindingCatalogSO CreateCatalog(InputAction[] actions)
        {
            var catalogSO = Track(ScriptableObject.CreateInstance<KeyBindingCatalogSO>());
            var serializedCatalog = new SerializedObject(catalogSO);
            SerializedProperty sections = serializedCatalog.FindProperty("_sections");
            sections.arraySize = 1;
            SerializedProperty section = sections.GetArrayElementAtIndex(0);
            section.FindPropertyRelative("_title").stringValue = MAP_NAME;
            SerializedProperty listedActions = section.FindPropertyRelative("_actions");
            listedActions.arraySize = actions.Length;
            for (int i = 0; i < actions.Length; i++)
            {
                SerializedProperty listedAction = listedActions.GetArrayElementAtIndex(i);
                listedAction.FindPropertyRelative("_label").stringValue = actions[i].name;
                listedAction.FindPropertyRelative("_actionReference").objectReferenceValue = Track(InputActionReference.Create(actions[i]));
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            return catalogSO;
        }

        private KeyBindingOverridesSettingSO CreateOverridesSetting()
        {
            var settingSO = Track(ScriptableObject.CreateInstance<KeyBindingOverridesSettingSO>());
            var serializedSetting = new SerializedObject(settingSO);
            serializedSetting.FindProperty("_settingId").stringValue = "test.keyBindingOverrides";
            serializedSetting.ApplyModifiedPropertiesWithoutUndo();
            return settingSO;
        }

        private static void SetStrings(SerializedProperty array, string[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private T Track<T>(T createdObject) where T : Object
        {
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        #endregion
    }
}
