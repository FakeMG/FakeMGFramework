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

            // Find the inputAsset from the parent object
            SerializedProperty inputAssetProp = property.serializedObject.FindProperty("inputAsset");
            InputActionAsset inputAsset = inputAssetProp?.objectReferenceValue as InputActionAsset;

            if (!inputAsset)
            {
                EditorGUI.LabelField(position, label.text, "Assign InputActionAsset in config");
                EditorGUI.EndProperty();
                return;
            }

            // Get all action map names
            string[] mapNames = inputAsset.actionMaps.Select(map => map.name).ToArray();
            if (mapNames.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No action maps found");
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