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
        private delegate object ScalarDrawer(string label, object value, out bool didChange);

        private static readonly HashSet<string> _expandedPaths = new();
        private static readonly Dictionary<Type, FieldInfo[]> _editableFieldsByType = new();
        private static readonly IReadOnlyDictionary<Type, ScalarDrawer> _scalarDrawersByType =
            new Dictionary<Type, ScalarDrawer>
            {
                [typeof(int)] = DrawInt,
                [typeof(float)] = DrawFloat,
                [typeof(double)] = DrawDouble,
                [typeof(long)] = DrawLong,
                [typeof(bool)] = DrawBool,
                [typeof(string)] = DrawString,
                [typeof(Vector2)] = DrawVector2,
                [typeof(Vector3)] = DrawVector3,
                [typeof(Vector4)] = DrawVector4,
                [typeof(Vector2Int)] = DrawVector2Int,
                [typeof(Vector3Int)] = DrawVector3Int,
                [typeof(Quaternion)] = DrawQuaternion,
                [typeof(Color)] = DrawColor,
                [typeof(DateTime)] = DrawDateTime,
            };

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

        public static bool DrawObject(object valueObject, string path = "")
        {
            if (valueObject == null)
            {
                EditorGUILayout.HelpBox("Value is null.", MessageType.Info);
                return false;
            }

            Type type = valueObject.GetType();
            FieldInfo[] fields = GetEditableFields(type);

            bool changed = false;

            foreach (FieldInfo field in fields)
            {
                string fieldPath = string.IsNullOrEmpty(path)
                    ? field.Name
                    : $"{path}.{field.Name}";

                object value = field.GetValue(valueObject);
                object newValue = DrawField(field.Name, field.FieldType, value, fieldPath, out bool fieldChanged);

                if (fieldChanged)
                {
                    field.SetValue(valueObject, newValue);
                    changed = true;
                }
            }

            return changed;
        }

        private static FieldInfo[] GetEditableFields(Type type)
        {
            if (_editableFieldsByType.TryGetValue(type, out FieldInfo[] fields))
            {
                return fields;
            }

            fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !field.IsDefined(typeof(NonSerializedAttribute), true))
                .Where(field => !typeof(Delegate).IsAssignableFrom(field.FieldType))
                .ToArray();

            _editableFieldsByType[type] = fields;
            return fields;
        }

        private static object DrawField(
            string label,
            Type fieldType,
            object value,
            string path,
            out bool changed)
        {
            if (TryDrawPrimitiveField(label, fieldType, value, out object primitiveValue, out changed))
            {
                return primitiveValue;
            }

            if (typeof(IDictionary).IsAssignableFrom(fieldType))
            {
                return DrawDictionary(label, value as IDictionary, fieldType, path, out changed);
            }

            if (fieldType.IsArray)
            {
                return DrawArray(label, value as Array, fieldType, path, out changed);
            }

            if (typeof(IList).IsAssignableFrom(fieldType))
            {
                return DrawList(label, value as IList, fieldType, path, out changed);
            }

            if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
            {
                return DrawNestedObject(label, value, path, out changed);
            }

            changed = false;
            EditorGUILayout.LabelField(label, $"(unsupported type: {fieldType.Name})");
            return value;
        }

        private static bool TryDrawPrimitiveField(
            string label,
            Type fieldType,
            object value,
            out object drawnValue,
            out bool changed)
        {
            if (_scalarDrawersByType.TryGetValue(fieldType, out ScalarDrawer scalarDrawer))
            {
                drawnValue = scalarDrawer(label, value, out changed);
                return true;
            }

            if (fieldType.IsEnum)
            {
                Enum old = value as Enum ?? (Enum)Activator.CreateInstance(fieldType);
                Enum next = EditorGUILayout.EnumPopup(label, old);
                changed = !Equals(old, next);
                drawnValue = next;
                return true;
            }

            changed = false;
            drawnValue = null;
            return false;
        }

        private static object DrawInt(string label, object value, out bool didChange)
        {
            return DrawPrimitive(
                label,
                value is int intValue ? intValue : default,
                EditorGUILayout.IntField,
                out didChange);
        }

        private static object DrawFloat(string label, object value, out bool didChange)
        {
            return DrawPrimitive(
                label,
                value is float floatValue ? floatValue : default,
                EditorGUILayout.FloatField,
                out didChange);
        }

        private static object DrawDouble(string label, object value, out bool didChange)
        {
            double previousValue = value is double doubleValue ? doubleValue : default;
            double nextValue = EditorGUILayout.DoubleField(label, previousValue);
            didChange = !previousValue.Equals(nextValue);
            return nextValue;
        }

        private static object DrawLong(string label, object value, out bool didChange)
        {
            long previousValue = value is long longValue ? longValue : default;
            long nextValue = EditorGUILayout.LongField(label, previousValue);
            didChange = previousValue != nextValue;
            return nextValue;
        }

        private static object DrawBool(string label, object value, out bool didChange)
        {
            bool wasEnabled = value is bool boolValue && boolValue;
            bool isEnabled = EditorGUILayout.Toggle(label, wasEnabled);
            didChange = wasEnabled != isEnabled;
            return isEnabled;
        }

        private static object DrawString(string label, object value, out bool didChange)
        {
            string previousValue = value as string ?? string.Empty;
            string nextValue = EditorGUILayout.TextField(label, previousValue);
            didChange = previousValue != nextValue;
            return nextValue;
        }

        private static object DrawVector2(string label, object value, out bool didChange)
        {
            return DrawPrimitive(
                label,
                value is Vector2 vectorValue ? vectorValue : default,
                EditorGUILayout.Vector2Field,
                out didChange);
        }

        private static object DrawVector3(string label, object value, out bool didChange)
        {
            return DrawPrimitive(
                label,
                value is Vector3 vectorValue ? vectorValue : default,
                EditorGUILayout.Vector3Field,
                out didChange);
        }

        private static object DrawVector4(string label, object value, out bool didChange)
        {
            return DrawPrimitive(
                label,
                value is Vector4 vectorValue ? vectorValue : default,
                EditorGUILayout.Vector4Field,
                out didChange);
        }

        private static object DrawVector2Int(string label, object value, out bool didChange)
        {
            return DrawPrimitive(
                label,
                value is Vector2Int vectorValue ? vectorValue : default,
                EditorGUILayout.Vector2IntField,
                out didChange);
        }

        private static object DrawVector3Int(string label, object value, out bool didChange)
        {
            return DrawPrimitive(
                label,
                value is Vector3Int vectorValue ? vectorValue : default,
                EditorGUILayout.Vector3IntField,
                out didChange);
        }

        private static object DrawQuaternion(string label, object value, out bool didChange)
        {
            Quaternion previousValue = value is Quaternion quaternionValue ? quaternionValue : default;
            Vector4 editableValue = new(
                previousValue.x,
                previousValue.y,
                previousValue.z,
                previousValue.w);
            Vector4 nextEditableValue = EditorGUILayout.Vector4Field(label, editableValue);
            Quaternion nextValue = new(
                nextEditableValue.x,
                nextEditableValue.y,
                nextEditableValue.z,
                nextEditableValue.w);
            didChange = previousValue != nextValue;
            return nextValue;
        }

        private static object DrawColor(string label, object value, out bool didChange)
        {
            Color previousValue = value is Color colorValue ? colorValue : default;
            Color nextValue = EditorGUILayout.ColorField(label, previousValue);
            didChange = previousValue != nextValue;
            return nextValue;
        }

        private static object DrawDateTime(string label, object value, out bool didChange)
        {
            DateTime previousValue = value is DateTime dateTimeValue ? dateTimeValue : default;
            string editedTimestamp = EditorGUILayout.TextField(label, previousValue.ToString("O"));
            if (DateTime.TryParse(editedTimestamp, out DateTime parsedValue) && parsedValue != previousValue)
            {
                didChange = true;
                return parsedValue;
            }

            didChange = false;
            return previousValue;
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

            bool expanded = _expandedPaths.Contains(path);
            bool newExpanded = EditorGUILayout.Foldout(expanded, label, true);

            if (newExpanded != expanded)
            {
                if (newExpanded) _expandedPaths.Add(path);
                else _expandedPaths.Remove(path);
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

            bool expanded = _expandedPaths.Contains(path);
            bool newExpanded = EditorGUILayout.Foldout(expanded, $"{label} [{dictionary.Count}]", true);

            if (newExpanded != expanded)
            {
                if (newExpanded) _expandedPaths.Add(path);
                else _expandedPaths.Remove(path);
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

            bool expanded = _expandedPaths.Contains(path);
            bool newExpanded = EditorGUILayout.Foldout(expanded, $"{label} [{list.Count}]", true);

            if (newExpanded != expanded)
            {
                if (newExpanded) _expandedPaths.Add(path);
                else _expandedPaths.Remove(path);
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
                Type runtimeElementType = element?.GetType() ?? elementType;
                object newElement = DrawField($"[{i}]", runtimeElementType, element, elementPath, out bool elementChanged);

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

        private static object DrawArray(
            string label,
            Array array,
            Type fieldType,
            string path,
            out bool changed)
        {
            changed = false;

            if (array == null)
            {
                EditorGUILayout.LabelField(label, "(null array)");
                return array;
            }

            bool expanded = _expandedPaths.Contains(path);
            bool newExpanded = EditorGUILayout.Foldout(expanded, $"{label} [{array.Length}]", true);

            if (newExpanded != expanded)
            {
                if (newExpanded) _expandedPaths.Add(path);
                else _expandedPaths.Remove(path);
            }

            if (!newExpanded)
            {
                return array;
            }

            EditorGUI.indentLevel++;

            Type elementType = fieldType.GetElementType() ?? typeof(object);
            int removeIndex = -1;

            for (int i = 0; i < array.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string elementPath = $"{path}[{i}]";
                object element = array.GetValue(i);
                Type runtimeElementType = element?.GetType() ?? elementType;
                object newElement = DrawField($"[{i}]", runtimeElementType, element, elementPath, out bool elementChanged);

                if (elementChanged)
                {
                    array.SetValue(newElement, i);
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
                array = RemoveArrayElement(array, elementType, removeIndex);
                changed = true;
            }

            if (GUILayout.Button($"Add Element to {label}"))
            {
                array = AppendArrayElement(array, elementType);
                changed = true;
            }

            EditorGUI.indentLevel--;
            return array;
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

        private static Array RemoveArrayElement(Array array, Type elementType, int removeIndex)
        {
            Array resizedArray = Array.CreateInstance(elementType, array.Length - 1);
            int nextIndex = 0;

            for (int i = 0; i < array.Length; i++)
            {
                if (i == removeIndex)
                {
                    continue;
                }

                resizedArray.SetValue(array.GetValue(i), nextIndex);
                nextIndex++;
            }

            return resizedArray;
        }

        private static Array AppendArrayElement(Array array, Type elementType)
        {
            Array resizedArray = Array.CreateInstance(elementType, array.Length + 1);

            for (int i = 0; i < array.Length; i++)
            {
                resizedArray.SetValue(array.GetValue(i), i);
            }

            resizedArray.SetValue(CreateDefaultInstance(elementType), array.Length);
            return resizedArray;
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
            _expandedPaths.Clear();
        }
    }
}
