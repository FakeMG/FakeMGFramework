using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.Framework.ActionMapManagement.Editor
{
    [CustomPropertyDrawer(typeof(ActionMapNameAttribute))]
    public class ActionMapNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect valueRect = new(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, position.height);

            GUIStyle errorStyle = new(EditorStyles.label);
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontStyle = FontStyle.Bold;

            // Find the inputAsset from project-wide settings
            InputActionAsset inputAsset = InputSystem.actions;

            if (!inputAsset)
            {
                EditorGUI.LabelField(labelRect, label.text);
                EditorGUI.LabelField(valueRect, "⚠ Assign project-wide InputActionAsset", errorStyle);

                EditorGUI.EndProperty();
                return;
            }

            // Get all action map names
            string[] mapNames = inputAsset.actionMaps.Select(map => map.name).ToArray();
            if (mapNames.Length == 0)
            {
                EditorGUI.LabelField(labelRect, label.text);
                EditorGUI.LabelField(valueRect, "⚠ No action maps found", errorStyle);

                EditorGUI.EndProperty();
                return;
            }

            // Find current index
            int selectedIndex = Mathf.Max(0, Array.IndexOf(mapNames, property.stringValue));

            // Draw dropdown
            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, mapNames);
            property.stringValue = mapNames[selectedIndex];

            EditorGUI.EndProperty();
        }
    }
}