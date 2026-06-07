using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.Framework.Editor
{
    [InitializeOnLoad]
    public static class MissingUnityEventChecker
    {
        private const string AUTO_CHECK_EDITOR_PREFS_KEY = "FakeMG.Framework.Editor.MissingUnityEventChecker.AutoCheck";

        private const string CHECK_OPEN_SCENES_MENU_PATH = FakeMGEditorMenus.CHECK_MISSING_UNITY_EVENTS + "/Check Open Scenes";
        private const string CHECK_PROJECT_ASSETS_MENU_PATH = FakeMGEditorMenus.CHECK_MISSING_UNITY_EVENTS + "/Check Project Assets";
        private const string CHECK_ALL_MENU_PATH = FakeMGEditorMenus.CHECK_MISSING_UNITY_EVENTS + "/Check All";
        private const string AUTO_CHECK_MENU_PATH = FakeMGEditorMenus.CHECK_MISSING_UNITY_EVENTS + "/Auto Check Open Scenes After Compile";

        private static readonly Type[] EmptyParameterTypes = Array.Empty<Type>();

        static MissingUnityEventChecker()
        {
            EditorApplication.delayCall += RunAutoCheckAfterCompilation;
        }

        private static bool AutoCheckEnabled
        {
            get => EditorPrefs.GetBool(AUTO_CHECK_EDITOR_PREFS_KEY, false);
            set => EditorPrefs.SetBool(AUTO_CHECK_EDITOR_PREFS_KEY, value);
        }

        private static void RunAutoCheckAfterCompilation()
        {
            if (!AutoCheckEnabled)
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            CheckOpenScenes();
        }

        [MenuItem(CHECK_OPEN_SCENES_MENU_PATH)]
        public static void CheckOpenScenes()
        {
            CheckResult result = new();

            GameObject[] allGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GameObject gameObject in allGameObjects)
            {
                CheckGameObjectAndChildrenInOpenScene(gameObject, result);
            }

            LogSummary("Open Scene UnityEvent Check", result);
        }

        [MenuItem(CHECK_PROJECT_ASSETS_MENU_PATH)]
        public static void CheckProjectAssets()
        {
            CheckResult result = new();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string prefabGuid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefabRoot == null)
                    continue;

                CheckPrefabAsset(prefabRoot, path, result);
            }

            LogSummary("Project Asset UnityEvent Check", result);
        }

        [MenuItem(CHECK_ALL_MENU_PATH)]
        public static void CheckAll()
        {
            CheckResult result = new();

            GameObject[] allGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GameObject gameObject in allGameObjects)
            {
                CheckGameObjectAndChildrenInOpenScene(gameObject, result);
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string prefabGuid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefabRoot == null)
                    continue;

                CheckPrefabAsset(prefabRoot, path, result);
            }

            LogSummary("All UnityEvent Check", result);
        }

        [MenuItem(AUTO_CHECK_MENU_PATH)]
        private static void ToggleAutoCheck()
        {
            AutoCheckEnabled = !AutoCheckEnabled;
            Menu.SetChecked(AUTO_CHECK_MENU_PATH, AutoCheckEnabled);

            Echo.Log($"Missing UnityEvent auto-check is now {(AutoCheckEnabled ? "enabled" : "disabled")}.");
        }

        [MenuItem(AUTO_CHECK_MENU_PATH, true)]
        private static bool ToggleAutoCheckValidate()
        {
            Menu.SetChecked(AUTO_CHECK_MENU_PATH, AutoCheckEnabled);
            return true;
        }

        private static void CheckGameObjectAndChildrenInOpenScene(GameObject gameObject, CheckResult result)
        {
            CheckMissingScripts(gameObject, gameObject, "Open Scene", result);

            Component[] components = gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                CheckComponent(component, "Open Scene", result);
            }
        }

        private static void CheckPrefabAsset(GameObject prefabRoot, string assetPath, CheckResult result)
        {
            Transform[] children = prefabRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                GameObject gameObject = child.gameObject;

                CheckMissingScripts(gameObject, prefabRoot, assetPath, result);

                Component[] components = gameObject.GetComponents<Component>();

                foreach (Component component in components)
                {
                    if (component == null)
                        continue;

                    CheckComponent(component, assetPath, result);
                }
            }
        }

        private static void CheckMissingScripts(
            GameObject gameObject,
            UnityEngine.Object logContext,
            string location,
            CheckResult result)
        {
            int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

            if (missingScriptCount <= 0)
                return;

            result.IssuesFound += missingScriptCount;

            Echo.Warning(
                $"Missing script found. Count: {missingScriptCount}. GameObject: '{GetGameObjectPath(gameObject)}'. Location: {location}",
                context: logContext);
        }

        private static void CheckComponent(Component component, string location, CheckResult result)
        {
            using SerializedObject serializedObject = new(component);

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;

                SerializedProperty property = iterator.Copy();

                if (!IsUnityEventProperty(property))
                    continue;

                CheckUnityEventProperty(property, component, location, result);

                // Avoid walking into UnityEvent internal serialized fields.
                enterChildren = false;
            }
        }

        private static bool IsUnityEventProperty(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Generic)
                return false;

            SerializedProperty persistentCalls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            return persistentCalls is { isArray: true };
        }

        private static void CheckUnityEventProperty(
            SerializedProperty unityEventProperty,
            Component owner,
            string location,
            CheckResult result)
        {
            SerializedProperty callsProperty = unityEventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");

            if (callsProperty == null || !callsProperty.isArray)
                return;

            Type[] dynamicParameterTypes = GetUnityEventDynamicParameterTypes(unityEventProperty);

            for (int i = 0; i < callsProperty.arraySize; i++)
            {
                SerializedProperty callProperty = callsProperty.GetArrayElementAtIndex(i);

                result.PersistentListenersChecked++;

                CheckPersistentCall(
                    callProperty,
                    unityEventProperty,
                    owner,
                    location,
                    i,
                    dynamicParameterTypes,
                    result);
            }
        }

        private static void CheckPersistentCall(
            SerializedProperty callProperty,
            SerializedProperty unityEventProperty,
            Component owner,
            string location,
            int listenerIndex,
            Type[] dynamicParameterTypes,
            CheckResult result)
        {
            SerializedProperty targetProperty = callProperty.FindPropertyRelative("m_Target");
            SerializedProperty methodNameProperty = callProperty.FindPropertyRelative("m_MethodName");
            SerializedProperty modeProperty = callProperty.FindPropertyRelative("m_Mode");
            SerializedProperty callStateProperty = callProperty.FindPropertyRelative("m_CallState");

            UnityEngine.Object target = targetProperty?.objectReferenceValue;
            string methodName = methodNameProperty?.stringValue;

            PersistentListenerMode mode = modeProperty != null
                ? (PersistentListenerMode)modeProperty.enumValueIndex
                : PersistentListenerMode.EventDefined;

            UnityEventCallState callState = callStateProperty != null
                ? (UnityEventCallState)callStateProperty.enumValueIndex
                : UnityEventCallState.RuntimeOnly;

            string eventPath = unityEventProperty.propertyPath;

            if (callState == UnityEventCallState.Off)
            {
                // Disabled calls are still serialized.
                // Usually they do not need to block validation.
                return;
            }

            if (target == null)
            {
                result.IssuesFound++;

                Echo.Warning(
                    $"Missing UnityEvent target. Event: '{eventPath}', Listener Index: {listenerIndex}, Owner: {owner.GetType().Name}, GameObject: '{GetGameObjectPath(owner.gameObject)}', Location: {location}",
                    context: owner.gameObject);

                return;
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                result.IssuesFound++;

                Echo.Warning(
                    $"Missing UnityEvent method. Event: '{eventPath}', Listener Index: {listenerIndex}, Target: '{target.name}', Owner: {owner.GetType().Name}, GameObject: '{GetGameObjectPath(owner.gameObject)}', Location: {location}",
                    context: owner.gameObject);

                return;
            }

            Type[] parameterTypes = GetParameterTypesForMode(callProperty, mode, dynamicParameterTypes);

            bool isValid = UnityEventBase.GetValidMethodInfo(target, methodName, parameterTypes) != null;

            if (isValid)
                return;

            result.IssuesFound++;

            string parameterDisplay = parameterTypes.Length == 0
                ? "void"
                : string.Join(", ", Array.ConvertAll(parameterTypes, type => type?.Name ?? "Unknown"));

            Echo.Warning(
                $"Invalid UnityEvent method. Event: '{eventPath}', Listener Index: {listenerIndex}, Target Type: {target.GetType().FullName}, Method: {methodName}({parameterDisplay}), Mode: {mode}, Owner: {owner.GetType().Name}, GameObject: '{GetGameObjectPath(owner.gameObject)}', Location: {location}",
                context: owner.gameObject);
        }

        private static Type[] GetParameterTypesForMode(
            SerializedProperty callProperty,
            PersistentListenerMode mode,
            Type[] dynamicParameterTypes)
        {
            return mode switch
            {
                PersistentListenerMode.EventDefined => dynamicParameterTypes,
                PersistentListenerMode.Void => EmptyParameterTypes,
                PersistentListenerMode.Object => GetObjectArgumentParameterTypes(callProperty),
                PersistentListenerMode.Int => new[] { typeof(int) },
                PersistentListenerMode.Float => new[] { typeof(float) },
                PersistentListenerMode.String => new[] { typeof(string) },
                PersistentListenerMode.Bool => new[] { typeof(bool) },
                _ => EmptyParameterTypes
            };
        }

        private static Type[] GetObjectArgumentParameterTypes(SerializedProperty callProperty)
        {
            SerializedProperty argumentTypeProperty =
                callProperty.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName");

            string argumentTypeName = argumentTypeProperty?.stringValue;

            if (string.IsNullOrWhiteSpace(argumentTypeName))
                return new[] { typeof(UnityEngine.Object) };

            Type argumentType = Type.GetType(argumentTypeName);

            if (argumentType == null)
                return new[] { typeof(UnityEngine.Object) };

            return new[] { argumentType };
        }

        private static Type[] GetUnityEventDynamicParameterTypes(SerializedProperty unityEventProperty)
        {
            object unityEventObject = GetManagedValue(unityEventProperty);

            if (unityEventObject == null)
                return EmptyParameterTypes;

            Type unityEventType = unityEventObject.GetType();

            while (unityEventType != null)
            {
                if (unityEventType.IsGenericType)
                {
                    Type genericDefinition = unityEventType.GetGenericTypeDefinition();

                    if (genericDefinition == typeof(UnityEvent<>) ||
                        genericDefinition == typeof(UnityEvent<,>) ||
                        genericDefinition == typeof(UnityEvent<,,>) ||
                        genericDefinition == typeof(UnityEvent<,,,>))
                    {
                        return unityEventType.GetGenericArguments();
                    }
                }

                if (unityEventType == typeof(UnityEvent))
                    return EmptyParameterTypes;

                unityEventType = unityEventType.BaseType;
            }

            return EmptyParameterTypes;
        }

        private static object GetManagedValue(SerializedProperty property)
        {
            object currentObject = property.serializedObject.targetObject;

            string[] pathParts = property.propertyPath
                .Replace(".Array.data[", "[")
                .Split('.');

            foreach (string pathPart in pathParts)
            {
                if (currentObject == null)
                    return null;

                if (pathPart.Contains("["))
                {
                    string fieldName = pathPart[..pathPart.IndexOf("[", StringComparison.Ordinal)];
                    int index = int.Parse(pathPart[
                        (pathPart.IndexOf("[", StringComparison.Ordinal) + 1)..pathPart.IndexOf("]", StringComparison.Ordinal)]);

                    currentObject = GetFieldOrPropertyValue(currentObject, fieldName);
                    currentObject = GetIndexedValue(currentObject, index);
                }
                else
                {
                    currentObject = GetFieldOrPropertyValue(currentObject, pathPart);
                }
            }

            return currentObject;
        }

        private static object GetFieldOrPropertyValue(object source, string name)
        {
            if (source == null)
                return null;

            Type type = source.GetType();

            while (type != null)
            {
                System.Reflection.FieldInfo field = type.GetField(
                    name,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (field != null)
                    return field.GetValue(source);

                System.Reflection.PropertyInfo property = type.GetProperty(
                    name,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (property != null)
                    return property.GetValue(source);

                type = type.BaseType;
            }

            return null;
        }

        private static object GetIndexedValue(object source, int index)
        {
            if (source is not System.Collections.IEnumerable enumerable)
                return null;

            System.Collections.IEnumerator enumerator = enumerable.GetEnumerator();

            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext())
                    return null;
            }

            return enumerator.Current;
        }

        private static string GetGameObjectPath(GameObject gameObject)
        {
            if (gameObject == null)
                return "<null>";

            Stack<string> pathParts = new();

            Transform current = gameObject.transform;

            while (current != null)
            {
                pathParts.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", pathParts);
        }

        private static void LogSummary(string title, CheckResult result)
        {
            if (result.IssuesFound == 0)
            {
                if (result.PersistentListenersChecked == 0)
                {
                    Echo.Log($"{title} Complete: No persistent UnityEvent listeners found.");
                    return;
                }

                Echo.Log($"{title} Complete: All {result.PersistentListenersChecked} persistent UnityEvent listener(s) are valid.");
            }
            else
            {
                Echo.Warning(
                    $"{title} Complete: {result.IssuesFound} issue(s) found. Persistent UnityEvent listener(s) checked: {result.PersistentListenersChecked}.");
            }
        }

        private sealed class CheckResult
        {
            public int PersistentListenersChecked;
            public int IssuesFound;
        }
    }
}
