// using System.Collections.Generic;
// using FakeMGFramework.SOEventSystem.EventChannel;
// using UnityEditor;
// using UnityEngine;
//
// namespace FakeMGFramework.SOEventSystem.Editor {
//     [CustomEditor(typeof(EventChannelSOBase), true)]
//     public class EventChannelReferenceFinder : UnityEditor.Editor {
//         private readonly Dictionary<GameObject, List<Component>> _referencingComponents = new();
//         private readonly Dictionary<Component, UnityEditor.Editor> _componentEditors = new();
//         private readonly Dictionary<GameObject, bool> _foldoutStates = new();
//         private readonly Dictionary<Component, bool> _expandedStates = new();
//         private bool _hasSearched;
//
//         public override void OnInspectorGUI() {
//             // Draw default inspector UI
//             base.OnInspectorGUI();
//
//             // Find references
//             if (!_hasSearched) {
//                 FindReferencesInScene((EventChannelSOBase)target);
//
//                 foreach (var componentKeyValuePair in _referencingComponents) {
//                     _foldoutStates.Add(componentKeyValuePair.Key, true);
//                     foreach (var component in componentKeyValuePair.Value) {
//                         _componentEditors.Add(component, CreateEditor(component));
//                         _expandedStates.Add(component, true);
//                     }
//                 }
//             }
//
//             if (_hasSearched) {
//                 GUILayout.Space(10);
//                 GUILayout.Label("Components referencing this ScriptableObject:");
//
//                 if (_referencingComponents.Count == 0) {
//                     GUILayout.Label("No references found in the scene.");
//                 } else {
//                     // Draw each referencing component's inspector
//                     foreach (var componentKeyValuePair in _referencingComponents) {
//                         GameObject gameObject = componentKeyValuePair.Key;
//                         EditorGUILayout.Space();
//
//                         _foldoutStates[gameObject] =
//                             EditorGUILayout.Foldout(_foldoutStates[gameObject], gameObject.name, true);
//
//                         if (!_foldoutStates[componentKeyValuePair.Key]) {
//                             continue;
//                         }
//
//                         foreach (var component in componentKeyValuePair.Value) {
//                             var editor = _componentEditors[component];
//
//                             if (!gameObject.activeSelf) {
//                                 _expandedStates[component] = false;
//                             }
//
//                             _expandedStates[component] =
//                                 EditorGUILayout.InspectorTitlebar(_expandedStates[component], component);
//
//                             if (_expandedStates[component] && editor && component is Behaviour { enabled: true }) {
//                                 editor.OnInspectorGUI();
//                             }
//                         }
//
//                         if (GUILayout.Button("Ping GameObject")) {
//                             EditorGUIUtility.PingObject(gameObject);
//                         }
//                     }
//                 }
//             }
//         }
//
//         private void FindReferencesInScene(ScriptableObject scriptableObject) {
//             // Clear previous results
//             _referencingComponents.Clear();
//             _hasSearched = true;
//
//             // Get all components in the scene
//             var allComponents = FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
//             foreach (var component in allComponents) {
//                 if (!component) continue;
//
//                 var serializedObjectComponent = new SerializedObject(component);
//                 var property = serializedObjectComponent.GetIterator();
//
//                 while (property.NextVisible(true)) {
//                     if (property.propertyType == SerializedPropertyType.ObjectReference &&
//                         property.objectReferenceValue == scriptableObject) {
//                         if (!_referencingComponents.TryGetValue(component.gameObject, out var referencingComponent)) {
//                             var components = new List<Component> { component };
//                             _referencingComponents.Add(component.gameObject, components);
//                         } else {
//                             referencingComponent.Add(component);
//                         }
//
//                         break;
//                     }
//                 }
//             }
//
//             // Refresh the inspector to show results
//             Repaint();
//         }
//
//         private void OnDisable() {
//             // Clean up all created editors when the inspector is closed or recompiled
//             foreach (var editor in _componentEditors.Values) {
//                 DestroyImmediate(editor);
//             }
//         }
//     }
// }

