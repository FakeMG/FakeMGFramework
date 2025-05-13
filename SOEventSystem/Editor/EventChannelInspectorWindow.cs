using System.Collections.Generic;
using System.Reflection;
using FakeMG.FakeMGFramework.SOEventSystem.EventChannel;
using FakeMG.FakeMGFramework.SOEventSystem.PayloadAdapter;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeMG.SOEventSystem.Editor {
    public class EventChannelInspectorWindow : EditorWindow {
        [MenuItem("Window/Event Channel Inspector")]
        public static void ShowWindow() {
            GetWindow<EventChannelInspectorWindow>("Event Channel Inspector");
        }

        private TreeViewState _treeViewState;
        private EventChannelTreeView _treeView;
        private ScriptableObject _selectedChannel;
        private Vector2 _scrollPosition;

        // Caches for optimization
        private List<MonoBehaviour> _cachedListeners;
        private List<MonoBehaviour> _cachedCallers;
        private ScriptableObject _lastSelectedChannel;
        private readonly Dictionary<MonoBehaviour, SerializedObject> _listenerSerializedObjects = new();
        private readonly Dictionary<MonoBehaviour, bool> _listenerFoldouts = new();

        // Resize state
        private float _treeViewHeight = 200;
        private const float MIN_TREE_VIEW_HEIGHT = 50;
        private const float MAX_TREE_VIEW_HEIGHT = 500;
        private const float SPLITTER_WIDTH = 5f;
        private bool _isDragging;
        private Rect _splitterRect;

        private void OnEnable() {
            _treeViewState ??= new TreeViewState();
            _treeView = new EventChannelTreeView(_treeViewState);
            _treeView.OnChannelSelected += OnChannelSelected;
            ClearCaches();
        }

        private void OnDisable() {
            ClearCaches();
        }

        private void ClearCaches() {
            _cachedListeners = null;
            _cachedCallers = null;
            _lastSelectedChannel = null;
            _listenerSerializedObjects.Clear();
            _listenerFoldouts.Clear();
        }

        private void OnChannelSelected(ScriptableObject channel) {
            if (_selectedChannel != channel) {
                _selectedChannel = channel;
                _cachedListeners = null;
                _cachedCallers = null;
                _listenerSerializedObjects.Clear();
                _listenerFoldouts.Clear();
            }
            Repaint();
        }

        private void OnGUI() {
            // Top bar for label and refresh button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Event Channels", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(80))) {
                Refresh();
            }
            EditorGUILayout.EndHorizontal();

            // Calculate the total available height, accounting for the top bar
            float topBarHeight = EditorGUIUtility.singleLineHeight + 5; // Label height + spacing
            float totalHeight = position.height - topBarHeight;

            // Ensure TreeView height is within bounds
            _treeViewHeight = Mathf.Clamp(_treeViewHeight, MIN_TREE_VIEW_HEIGHT, MAX_TREE_VIEW_HEIGHT);

            // TreeView section
            Rect treeRect = new Rect(0, topBarHeight, position.width, _treeViewHeight);
            _treeView.OnGUI(treeRect);

            // Splitter
            _splitterRect = new Rect(0, topBarHeight + _treeViewHeight, position.width, SPLITTER_WIDTH);
            EditorGUIUtility.AddCursorRect(_splitterRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && _splitterRect.Contains(Event.current.mousePosition)) {
                _isDragging = true;
            }
            if (_isDragging) {
                if (Event.current.type == EventType.MouseDrag) {
                    float newHeight = _treeViewHeight + Event.current.delta.y;
                    _treeViewHeight = Mathf.Clamp(newHeight, MIN_TREE_VIEW_HEIGHT, MAX_TREE_VIEW_HEIGHT);
                    Repaint();
                } else if (Event.current.type == EventType.MouseUp) {
                    _isDragging = false;
                }
            }
            EditorGUI.DrawRect(_splitterRect, new Color(0.3f, 0.3f, 0.3f)); // Draw splitter line

            // Details section
            if (_selectedChannel) {
                Rect detailsRect = new Rect(0, topBarHeight + _treeViewHeight + SPLITTER_WIDTH, position.width, totalHeight - _treeViewHeight - SPLITTER_WIDTH);
                _scrollPosition = GUI.BeginScrollView(detailsRect, _scrollPosition, new Rect(0, 0, position.width - 20, 1000));

                GUILayout.Space(10);
                GUILayout.Label($"Selected Channel: {_selectedChannel.name}", EditorStyles.boldLabel);

                GUILayout.Label("Listeners", EditorStyles.boldLabel);
                DisplayListeners();

                GUILayout.Label("Callers", EditorStyles.boldLabel);
                DisplayCallers();

                GUI.EndScrollView();
            }

            if (_lastSelectedChannel != _selectedChannel) {
                UpdateCaches();
                _lastSelectedChannel = _selectedChannel;
            }
        }

        private void Refresh() {
            ClearCaches();
            AssetDatabase.Refresh();
            _treeView.Reload();
            Repaint();
        }

        private void UpdateCaches() {
            if (!_selectedChannel) return;

            _cachedListeners = FindListenerComponents(_selectedChannel);
            _cachedCallers = FindCallerComponents(_selectedChannel);

            foreach (var listener in _cachedListeners) {
                _listenerFoldouts.TryAdd(listener, true);
                if (!_listenerSerializedObjects.ContainsKey(listener)) {
                    _listenerSerializedObjects[listener] = new SerializedObject(listener);
                }
            }
        }

        private void DisplayListeners() {
            if (_cachedListeners == null || _cachedListeners.Count == 0) {
                EditorGUILayout.LabelField("No listeners found.");
                return;
            }

            GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
            linkStyle.normal.textColor = new Color(0.0f, 0.5f, 1.0f);
            linkStyle.hover.textColor = new Color(0.0f, 0.7f, 1.0f);

            foreach (var listener in _cachedListeners) {
                if (!listener) continue;

                string displayName = $"{listener.gameObject.name} ({listener.GetType().Name})";
                Rect rect = EditorGUILayout.GetControlRect();
                if (GUI.Button(rect, displayName, linkStyle)) {
                    Selection.activeObject = listener.gameObject;
                    EditorGUIUtility.PingObject(listener.gameObject);
                }

                _listenerFoldouts[listener] = EditorGUILayout.Foldout(_listenerFoldouts[listener], "Details", true);
                if (_listenerFoldouts[listener]) {
                    EditorGUI.indentLevel++;
                    var serializedListener = _listenerSerializedObjects[listener];
                    serializedListener.Update();
                    SerializedProperty iterator = serializedListener.GetIterator();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren)) {
                        if (iterator.name == "m_Script") continue;
                        EditorGUILayout.PropertyField(iterator, true);
                        enterChildren = false;
                    }
                    serializedListener.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();
            }
        }

        private void DisplayCallers() {
            if (_cachedCallers == null || _cachedCallers.Count == 0) {
                EditorGUILayout.LabelField("No callers found.");
                return;
            }

            GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
            linkStyle.normal.textColor = new Color(0.0f, 0.5f, 1.0f);
            linkStyle.hover.textColor = new Color(0.0f, 0.7f, 1.0f);

            foreach (var caller in _cachedCallers) {
                if (!caller) continue;
                string displayName = $"{caller.gameObject.name} ({caller.GetType().Name})";
                Rect rect = EditorGUILayout.GetControlRect();
                if (GUI.Button(rect, displayName, linkStyle)) {
                    Selection.activeObject = caller.gameObject;
                    EditorGUIUtility.PingObject(caller.gameObject);
                }
            }
        }

        private List<MonoBehaviour> FindListenerComponents(ScriptableObject so) {
            List<MonoBehaviour> listeners = new List<MonoBehaviour>();
            string assetPath = AssetDatabase.GetAssetPath(so);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) {
                    foreach (var go in scene.GetRootGameObjects()) {
                        listeners.AddRange(GetListenersInGameObject(go, so));
                    }
                }
            }

            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
            foreach (string prefabGuid in prefabGUIDs) {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab) {
                    listeners.AddRange(GetListenersInGameObject(prefab, so));
                }
            }

            return listeners;
        }

        private List<MonoBehaviour> FindCallerComponents(ScriptableObject so) {
            List<MonoBehaviour> callers = new List<MonoBehaviour>();
            string assetPath = AssetDatabase.GetAssetPath(so);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) {
                    foreach (var go in scene.GetRootGameObjects()) {
                        callers.AddRange(GetCallersInGameObject(go, so));
                    }
                }
            }

            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
            foreach (string prefabGuid in prefabGUIDs) {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab) {
                    callers.AddRange(GetCallersInGameObject(prefab, so));
                }
            }

            return callers;
        }

        private List<MonoBehaviour> GetListenersInGameObject(GameObject go, ScriptableObject so) {
            List<MonoBehaviour> listeners = new List<MonoBehaviour>();
            var components = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components) {
                if (!comp) continue;
                var type = comp.GetType();
                bool isListener = false;
                var baseType = type;
                while (baseType != null && baseType != typeof(MonoBehaviour)) {
                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(GenericPayloadAdapter<>)
                        || baseType == typeof(VoidPayloadAdapter)) {
                        isListener = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }

                if (isListener) {
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields) {
                        if (field.FieldType.IsAssignableFrom(so.GetType())) {
                            var value = field.GetValue(comp) as ScriptableObject;
                            if (value == so) {
                                listeners.Add(comp);
                                break;
                            }
                        }
                    }
                }
            }
            return listeners;
        }

        private List<MonoBehaviour> GetCallersInGameObject(GameObject go, ScriptableObject so) {
            List<MonoBehaviour> callers = new List<MonoBehaviour>();
            var components = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components) {
                if (!comp) continue;
                var type = comp.GetType();

                bool isListener = false;
                var baseType = type;
                while (baseType != null && baseType != typeof(MonoBehaviour)) {
                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(GenericPayloadAdapter<>)
                        || baseType == typeof(VoidPayloadAdapter)) {
                        isListener = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }

                bool referencesSO = false;

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields) {
                    if (field.FieldType.IsAssignableFrom(so.GetType())) {
                        var value = field.GetValue(comp) as ScriptableObject;
                        if (value == so && !isListener) {
                            referencesSO = true;
                            break;
                        }
                    }
                }

                if (!referencesSO) {
                    SerializedObject serializedComp = new SerializedObject(comp);
                    SerializedProperty iterator = serializedComp.GetIterator();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren)) {
                        if (iterator.propertyType == SerializedPropertyType.Generic && iterator.type.Contains("Event")) {
                            SerializedProperty persistentCalls = iterator.FindPropertyRelative("m_PersistentCalls.m_Calls");
                            if (persistentCalls is { isArray: true }) {
                                for (int i = 0; i < persistentCalls.arraySize; i++) {
                                    SerializedProperty call = persistentCalls.GetArrayElementAtIndex(i);
                                    SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                                    SerializedProperty methodNameProp = call.FindPropertyRelative("m_MethodName");

                                    if (targetProp != null && targetProp.objectReferenceValue && methodNameProp != null) {
                                        var target = targetProp.objectReferenceValue;

                                        if (target == so) {
                                            referencesSO = true;
                                            break;
                                        }

                                        var targetMono = target as MonoBehaviour;
                                        if (targetMono) {
                                            var targetType = targetMono.GetType();
                                            var targetFields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                            foreach (var targetField in targetFields) {
                                                if (targetField.FieldType.IsAssignableFrom(so.GetType())) {
                                                    var value = targetField.GetValue(targetMono) as ScriptableObject;
                                                    if (value == so) {
                                                        referencesSO = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if (referencesSO) break;
                                    }
                                }
                            }
                        }
                        enterChildren = false;
                    }
                }

                if (referencesSO) {
                    callers.Add(comp);
                }
            }
            return callers;
        }
    }

    public class EventChannelTreeView : TreeView {
        public delegate void ChannelSelectedDelegate(ScriptableObject channel);
        public event ChannelSelectedDelegate OnChannelSelected;

        public EventChannelTreeView(TreeViewState state) : base(state) {
            Reload();
        }

        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();

            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            int id = 1;

            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset && IsGenericEventChannel(asset)) {
                    allItems.Add(new EventChannelTreeViewItem {
                        id = id++,
                        depth = 0,
                        displayName = asset.name,
                        Channel = asset
                    });
                }
            }

            if (allItems.Count == 0) {
                Debug.LogWarning("EventChannelInspector: No EventChannelSO assets found in the project.");
            } else {
                Debug.Log($"EventChannelInspector: Found {allItems.Count} EventChannelSO assets.");
            }

            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        private bool IsGenericEventChannel(ScriptableObject asset) {
            var type = asset.GetType();
            while (type != null && type != typeof(ScriptableObject)) {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GenericEventChannelSO<>)
                    || type == typeof(VoidEventChannelSO)) {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds) {
            if (selectedIds.Count > 0) {
                if (FindItem(selectedIds[0], rootItem) is EventChannelTreeViewItem selectedItem && selectedItem.Channel) {
                    OnChannelSelected?.Invoke(selectedItem.Channel);
                }
            }
        }
    }

    public class EventChannelTreeViewItem : TreeViewItem {
        public ScriptableObject Channel;
    }
}