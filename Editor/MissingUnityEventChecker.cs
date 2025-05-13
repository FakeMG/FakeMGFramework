using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.Framework.Editor {
    [InitializeOnLoad]
    public class MissingUnityEventChecker : MonoBehaviour {
        static MissingUnityEventChecker() {
            // Add a delayed call after compilation completes
            EditorApplication.delayCall += RunCheckAfterCompilation;
        }

        private static void RunCheckAfterCompilation() {
            // Check if we are in the Editor and not in play mode
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                CheckMissingUnityEvents();
            }
        }

        [MenuItem("Tools/Check Missing Unity Events In Scene", priority = 200)]
        public static void CheckMissingUnityEvents() {
            GameObject[] allGameObjects =
                FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (GameObject gameObj in allGameObjects) {
                Component[] components = gameObj.GetComponents<Component>();

                foreach (Component component in components) {
                    if (component == null) continue;

                    FieldInfo[] fields = component.GetType()
                        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (FieldInfo field in fields) {
                        if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType)) {
                            if (field.GetValue(component) is UnityEventBase unityEvent) {
                                Check(unityEvent, component);
                            }
                        }
                    }
                }
            }
        }

        private static void Check(UnityEventBase unityEventBase, Component component) {
            int eventCount = unityEventBase.GetPersistentEventCount();

            for (int i = 0; i < eventCount; i++) {
                UnityEngine.Object target = unityEventBase.GetPersistentTarget(i);
                string methodName = unityEventBase.GetPersistentMethodName(i);

                if (target == null) {
                    Debug.LogWarning("Object doesn't exist: " + target.name, component.gameObject);
                    continue;
                }

                string objectFullNameWithNamespace = target.GetType().FullName;
                if (!ClassExist(objectFullNameWithNamespace)) {
                    Debug.LogWarning("Class doesn't exist: " + objectFullNameWithNamespace, component.gameObject);
                    continue;
                }

                if (FunctionExistAsPublicInTarget(target, methodName)) {
                    continue;
                }

                if (FunctionExistAsPrivateInTarget(target, methodName)) {
                    Debug.LogWarning(
                        "Function is private: " + methodName + " - " + objectFullNameWithNamespace + ": "
                        + component,
                        component.gameObject);
                } else {
                    Debug.LogWarning(
                        "Function doesn't exist: " + methodName + " - " + objectFullNameWithNamespace + ": "
                        + component,
                        component.gameObject);
                }
            }
        }

        private static bool ClassExist(string className) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type type = assembly.GetType(className);
                if (type != null) {
                    return true;
                }
            }

            return false;
        }

        private static bool FunctionExistAsPublicInTarget(UnityEngine.Object target, string functionName) {
            Type type = target.GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            return methods.Any(method => method.Name == functionName);
        }

        private static bool FunctionExistAsPrivateInTarget(UnityEngine.Object target, string functionName) {
            Type type = target.GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            return methods.Any(method => method.Name == functionName);
        }
    }
}