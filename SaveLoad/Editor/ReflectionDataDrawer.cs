using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    /// <summary>
    /// Draws and edits arbitrary serialized objects via reflection in IMGUI.
    /// Returns true from DrawObject if any field was modified.
    /// </summary>
    public static class ReflectionDataDrawer
    {
        private static readonly HashSet<string> ExpandedPaths = new();

        public static bool DrawObject(object obj, string path = "")
        {
            if (obj == null)
            {
                EditorGUILayout.HelpBox("Value is null.", MessageType.Info);
                return false;
            }

            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            bool changed = false;

            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(NonSerializedAttribute), true))
                    continue;

                string fieldPath = string.IsNullOrEmpty(path)
                    ? field.Name
                    : $"{path}.{field.Name}";

                object value = field.GetValue(obj);
                object newValue = DrawField(field.Name, field.FieldType, value, fieldPath, out bool fieldChanged);

                if (fieldChanged)
                {
                    field.SetValue(obj, newValue);
                    changed = true;
                }
            }

            return changed;
        }

        private static object DrawField(
            string label,
            Type fieldType,
            object value,
            string path,
            out bool changed)
        {
            changed = false;

            if (fieldType == typeof(int))
                return DrawPrimitive(label, (int)value, EditorGUILayout.IntField, out changed);

            if (fieldType == typeof(float))
                return DrawPrimitive(label, (float)value, EditorGUILayout.FloatField, out changed);

            if (fieldType == typeof(double))
            {
                double old = (double)value;
                double next = EditorGUILayout.DoubleField(label, old);
                changed = !old.Equals(next);
                return next;
            }

            if (fieldType == typeof(long))
            {
                long old = (long)value;
                long next = EditorGUILayout.LongField(label, old);
                changed = old != next;
                return next;
            }

            if (fieldType == typeof(bool))
            {
                bool old = (bool)value;
                bool next = EditorGUILayout.Toggle(label, old);
                changed = old != next;
                return next;
            }

            if (fieldType == typeof(string))
            {
                string old = (string)value ?? string.Empty;
                string next = EditorGUILayout.TextField(label, old);
                changed = old != next;
                return next;
            }

            if (fieldType == typeof(Vector2))
                return DrawPrimitive(label, (Vector2)value, EditorGUILayout.Vector2Field, out changed);

            if (fieldType == typeof(Vector3))
                return DrawPrimitive(label, (Vector3)value, EditorGUILayout.Vector3Field, out changed);

            if (fieldType == typeof(Vector4))
                return DrawPrimitive(label, (Vector4)value, EditorGUILayout.Vector4Field, out changed);

            if (fieldType == typeof(Vector2Int))
                return DrawPrimitive(label, (Vector2Int)value, EditorGUILayout.Vector2IntField, out changed);

            if (fieldType == typeof(Vector3Int))
                return DrawPrimitive(label, (Vector3Int)value, EditorGUILayout.Vector3IntField, out changed);

            if (fieldType == typeof(Quaternion))
            {
                Quaternion old = (Quaternion)value;
                Vector4 asVec = new(old.x, old.y, old.z, old.w);
                Vector4 next = EditorGUILayout.Vector4Field(label, asVec);
                Quaternion result = new(next.x, next.y, next.z, next.w);
                changed = old != result;
                return result;
            }

            if (fieldType == typeof(Color))
            {
                Color old = (Color)value;
                Color next = EditorGUILayout.ColorField(label, old);
                changed = old != next;
                return next;
            }

            if (fieldType == typeof(DateTime))
            {
                DateTime old = (DateTime)value;
                string text = EditorGUILayout.TextField(label, old.ToString("O"));
                if (DateTime.TryParse(text, out DateTime parsed) && parsed != old)
                {
                    changed = true;
                    return parsed;
                }
                return old;
            }

            if (fieldType.IsEnum)
            {
                Enum old = (Enum)value;
                Enum next = EditorGUILayout.EnumPopup(label, old);
                changed = !Equals(old, next);
                return next;
            }

            if (typeof(IList).IsAssignableFrom(fieldType))
                return DrawList(label, (IList)value, fieldType, path, out changed);

            if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
                return DrawNestedObject(label, value, path, out changed);

            EditorGUILayout.LabelField(label, $"(unsupported type: {fieldType.Name})");
            return value;
        }

        private static T DrawPrimitive<T>(
            string label, T value, Func<string, T, GUILayoutOption[], T> drawer, out bool changed)
        {
            T next = drawer(label, value, Array.Empty<GUILayoutOption>());
            changed = !Equals(value, next);
            return next;
        }

        private static object DrawNestedObject(string label, object value, string path, out bool changed)
        {
            changed = false;

            if (value == null)
            {
                EditorGUILayout.LabelField(label, "(null)");
                return value;
            }

            bool expanded = ExpandedPaths.Contains(path);
            bool newExpanded = EditorGUILayout.Foldout(expanded, label, true);

            if (newExpanded != expanded)
            {
                if (newExpanded) ExpandedPaths.Add(path);
                else ExpandedPaths.Remove(path);
            }

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                changed = DrawObject(value, path);
                EditorGUI.indentLevel--;
            }

            return value;
        }

        private static object DrawList(
            string label, IList list, Type fieldType, string path, out bool changed)
        {
            changed = false;

            if (list == null)
            {
                EditorGUILayout.LabelField(label, "(null list)");
                return list;
            }

            bool expanded = ExpandedPaths.Contains(path);
            bool newExpanded = EditorGUILayout.Foldout(expanded, $"{label} [{list.Count}]", true);

            if (newExpanded != expanded)
            {
                if (newExpanded) ExpandedPaths.Add(path);
                else ExpandedPaths.Remove(path);
            }

            if (!newExpanded) return list;

            EditorGUI.indentLevel++;

            Type elementType = ResolveListElementType(fieldType);
            int removeIndex = -1;

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string elementPath = $"{path}[{i}]";
                object element = list[i];
                object newElement = DrawField($"[{i}]", elementType, element, elementPath, out bool elementChanged);

                if (elementChanged)
                {
                    list[i] = newElement;
                    changed = true;
                }

                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                list.RemoveAt(removeIndex);
                changed = true;
            }

            if (GUILayout.Button($"Add Element to {label}"))
            {
                object newElement = CreateDefaultInstance(elementType);
                list.Add(newElement);
                changed = true;
            }

            EditorGUI.indentLevel--;
            return list;
        }

        private static Type ResolveListElementType(Type listType)
        {
            if (listType.IsArray)
                return listType.GetElementType();

            if (listType.IsGenericType)
                return listType.GetGenericArguments()[0];

            return typeof(object);
        }

        private static object CreateDefaultInstance(Type type)
        {
            if (type == typeof(string))
                return string.Empty;

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearExpandedState()
        {
            ExpandedPaths.Clear();
        }
    }
}
