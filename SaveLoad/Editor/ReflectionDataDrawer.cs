using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    public static class ReflectionDataDrawer
    {
        private static readonly HashSet<string> ExpandedPaths = new();
        private static readonly Dictionary<Type, FieldInfo[]> EditableFieldsByType = new();

        public static bool DrawRootValue(Type valueType, ref object value, string path = "root")
        {
            object nextValue = DrawField("Value", valueType, value, path, out bool changed);
            if (changed)
            {
                value = nextValue;
            }

            return changed;
        }

        public static object CreateDefaultValue(Type type)
        {
            return CreateDefaultInstance(type);
        }

        public static bool DrawObject(object obj, string path = "")
        {
            if (obj == null)
            {
                EditorGUILayout.HelpBox("Value is null.", MessageType.Info);
                return false;
            }

            Type type = obj.GetType();
            FieldInfo[] fields = GetEditableFields(type);

            bool changed = false;

            foreach (FieldInfo field in fields)
            {
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

        private static FieldInfo[] GetEditableFields(Type type)
        {
            if (EditableFieldsByType.TryGetValue(type, out FieldInfo[] fields))
            {
                return fields;
            }

            fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !field.IsDefined(typeof(NonSerializedAttribute), true))
                .Where(field => !typeof(Delegate).IsAssignableFrom(field.FieldType))
                .ToArray();

            EditableFieldsByType[type] = fields;
            return fields;
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
                return DrawPrimitive(label, value is int intValue ? intValue : default, EditorGUILayout.IntField, out changed);

            if (fieldType == typeof(float))
                return DrawPrimitive(label, value is float floatValue ? floatValue : default, EditorGUILayout.FloatField, out changed);

            if (fieldType == typeof(double))
            {
                double old = value is double doubleValue ? doubleValue : default;
                double next = EditorGUILayout.DoubleField(label, old);
                changed = !old.Equals(next);
                return next;
            }

            if (fieldType == typeof(long))
            {
                long old = value is long longValue ? longValue : default;
                long next = EditorGUILayout.LongField(label, old);
                changed = old != next;
                return next;
            }

            if (fieldType == typeof(bool))
            {
                bool old = value is bool boolValue && boolValue;
                bool next = EditorGUILayout.Toggle(label, old);
                changed = old != next;
                return next;
            }

            if (fieldType == typeof(string))
            {
                string old = value as string ?? string.Empty;
                string next = EditorGUILayout.TextField(label, old);
                changed = old != next;
                return next;
            }

            if (typeof(IDictionary).IsAssignableFrom(fieldType))
                return DrawDictionary(label, value as IDictionary, fieldType, path, out changed);

            if (fieldType == typeof(Vector2))
                return DrawPrimitive(label, value is Vector2 vector2 ? vector2 : default, EditorGUILayout.Vector2Field, out changed);

            if (fieldType == typeof(Vector3))
                return DrawPrimitive(label, value is Vector3 vector3 ? vector3 : default, EditorGUILayout.Vector3Field, out changed);

            if (fieldType == typeof(Vector4))
                return DrawPrimitive(label, value is Vector4 vector4 ? vector4 : default, EditorGUILayout.Vector4Field, out changed);

            if (fieldType == typeof(Vector2Int))
                return DrawPrimitive(label, value is Vector2Int vector2Int ? vector2Int : default, EditorGUILayout.Vector2IntField, out changed);

            if (fieldType == typeof(Vector3Int))
                return DrawPrimitive(label, value is Vector3Int vector3Int ? vector3Int : default, EditorGUILayout.Vector3IntField, out changed);

            if (fieldType == typeof(Quaternion))
            {
                Quaternion old = value is Quaternion quaternion ? quaternion : default;
                Vector4 asVec = new(old.x, old.y, old.z, old.w);
                Vector4 next = EditorGUILayout.Vector4Field(label, asVec);
                Quaternion result = new(next.x, next.y, next.z, next.w);
                changed = old != result;
                return result;
            }

            if (fieldType == typeof(Color))
            {
                Color old = value is Color color ? color : default;
                Color next = EditorGUILayout.ColorField(label, old);
                changed = old != next;
                return next;
            }

            if (fieldType == typeof(DateTime))
            {
                DateTime old = value is DateTime dateTime ? dateTime : default;
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
                Enum old = value as Enum ?? (Enum)Activator.CreateInstance(fieldType);
                Enum next = EditorGUILayout.EnumPopup(label, old);
                changed = !Equals(old, next);
                return next;
            }

            if (typeof(IList).IsAssignableFrom(fieldType))
                return DrawList(label, value as IList, fieldType, path, out changed);

            if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
                return DrawNestedObject(label, value, path, out changed);

            EditorGUILayout.LabelField(label, $"(unsupported type: {fieldType.Name})");
            return value;
        }

        private static T DrawPrimitive<T>(
            string label,
            T value,
            Func<string, T, GUILayoutOption[], T> drawer,
            out bool changed)
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

        private static object DrawDictionary(
            string label,
            IDictionary dictionary,
            Type fieldType,
            string path,
            out bool changed)
        {
            changed = false;

            if (dictionary == null)
            {
                EditorGUILayout.LabelField(label, "(null dictionary)");
                return dictionary;
            }

            bool expanded = ExpandedPaths.Contains(path);
            bool newExpanded = EditorGUILayout.Foldout(expanded, $"{label} [{dictionary.Count}]", true);

            if (newExpanded != expanded)
            {
                if (newExpanded) ExpandedPaths.Add(path);
                else ExpandedPaths.Remove(path);
            }

            if (!newExpanded)
                return dictionary;

            EditorGUI.indentLevel++;
            ResolveDictionaryTypes(fieldType, dictionary, out Type keyType, out Type declaredValueType);

            object keyToRemove = null;
            List<object> keys = new();
            foreach (DictionaryEntry entry in dictionary)
            {
                keys.Add(entry.Key);
            }

            for (int i = 0; i < keys.Count; i++)
            {
                object entryKey = keys[i];
                object entryValue = dictionary[entryKey];
                Type entryValueType = entryValue?.GetType() ?? declaredValueType;

                EditorGUILayout.BeginHorizontal();

                object nextValue = DrawField(
                    entryKey?.ToString() ?? "(null)",
                    entryValueType,
                    entryValue,
                    $"{path}.{entryKey}",
                    out bool entryChanged);

                if (entryChanged)
                {
                    dictionary[entryKey] = nextValue;
                    changed = true;
                }

                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    keyToRemove = entryKey;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (keyToRemove != null)
            {
                dictionary.Remove(keyToRemove);
                changed = true;
            }

            EditorGUI.indentLevel--;
            return dictionary;
        }

        private static object DrawList(
            string label,
            IList list,
            Type fieldType,
            string path,
            out bool changed)
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

            if (!newExpanded)
                return list;

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

        private static void ResolveDictionaryTypes(Type fieldType, IDictionary dictionary, out Type keyType, out Type valueType)
        {
            if (TryGetDictionaryTypes(fieldType, out keyType, out valueType))
                return;

            if (dictionary != null && TryGetDictionaryTypes(dictionary.GetType(), out keyType, out valueType))
                return;

            keyType = typeof(object);
            valueType = typeof(object);
        }

        private static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type[] genericArguments = type.GetGenericArguments();
                keyType = genericArguments[0];
                valueType = genericArguments[1];
                return true;
            }

            Type[] interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type @interface = interfaces[i];
                if (!@interface.IsGenericType)
                    continue;

                if (@interface.GetGenericTypeDefinition() != typeof(IDictionary<,>))
                    continue;

                Type[] genericArguments = @interface.GetGenericArguments();
                keyType = genericArguments[0];
                valueType = genericArguments[1];
                return true;
            }

            keyType = typeof(object);
            valueType = typeof(object);
            return false;
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

            if (type.IsArray)
                return Array.CreateInstance(type.GetElementType(), 0);

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
