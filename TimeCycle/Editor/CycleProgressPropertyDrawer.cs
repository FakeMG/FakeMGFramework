#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FakeMG.TimeCycle.Editor
{
    /// <summary>
    /// Draws normalized authored positions with their current runtime-second equivalent.
    /// </summary>
    [CustomPropertyDrawer(typeof(CycleProgressAttribute))]
    public sealed class CycleProgressPropertyDrawer : PropertyDrawer
    {
        private const double MAX_PROGRESS_01 = 0.999999999d;

        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect progressPosition = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            double authoredProgress01 = EditorGUI.DoubleField(progressPosition, label, property.doubleValue);
            property.doubleValue = Math.Clamp(authoredProgress01, 0d, MAX_PROGRESS_01);

            Rect resolvedTimePosition = new(
                position.x + EditorGUIUtility.labelWidth,
                position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(resolvedTimePosition, CreateResolvedTimeLabel(property));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        #endregion

        #region Private Methods

        private static string CreateResolvedTimeLabel(SerializedProperty property)
        {
            SerializedProperty cycleDurationProperty = property.serializedObject.FindProperty("_cycleDurationSeconds");
            if (cycleDurationProperty == null || cycleDurationProperty.doubleValue <= 0d)
            {
                return "Resolved time: unavailable";
            }

            double resolvedTimeSeconds = property.doubleValue * cycleDurationProperty.doubleValue;
            int resolvedHours = (int)(resolvedTimeSeconds / 3600d);
            int resolvedMinutes = (int)(resolvedTimeSeconds % 3600d / 60d);
            int resolvedSeconds = (int)(resolvedTimeSeconds % 60d);
            return $"Resolved time: {resolvedHours:00}:{resolvedMinutes:00}:{resolvedSeconds:00}";
        }

        #endregion
    }
}
#endif
