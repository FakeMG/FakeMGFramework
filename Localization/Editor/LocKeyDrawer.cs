using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace FakeMG.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LocKeyAttribute))]
    public class LocKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);

            Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            string currentKey = property.stringValue;
            bool isMissing = !IsValidKey(currentKey) && !string.IsNullOrEmpty(currentKey);

            var originalColor = GUI.backgroundColor;
            if (isMissing) GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);

            string displayString = string.IsNullOrEmpty(currentKey) ? "None" : currentKey;
            if (GUI.Button(buttonRect, new GUIContent(displayString), EditorStyles.popup))
            {
                var dropdown = new LocKeyDropdown(new AdvancedDropdownState(), property);
                dropdown.Show(buttonRect);
            }

            GUI.backgroundColor = originalColor;
            EditorGUI.EndProperty();
        }

        private bool IsValidKey(string key)
        {
            var fields = LocKeysResolver.GetKeyFields();
            return fields.Any(f => f.Name == key);
        }
    }

    public class LocKeyDropdown : AdvancedDropdown
    {
        private SerializedProperty _targetProperty;

        public LocKeyDropdown(AdvancedDropdownState state, SerializedProperty target) : base(state)
        {
            _targetProperty = target;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Localization Keys");

            var grouped = LocKeysResolver.GetKeyFieldsGrouped();
            if (grouped.Count > 0)
            {
                foreach (var (tableName, fields) in grouped)
                {
                    var tableGroup = new AdvancedDropdownItem(tableName);

                    foreach (var field in fields)
                    {
                        tableGroup.AddChild(new AdvancedDropdownItem(field.Name));
                    }

                    root.AddChild(tableGroup);
                }
            }
            else
            {
                root.AddChild(new AdvancedDropdownItem("Error: Loc.Keys not found"));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            _targetProperty.serializedObject.Update();
            _targetProperty.stringValue = item.name;
            _targetProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}