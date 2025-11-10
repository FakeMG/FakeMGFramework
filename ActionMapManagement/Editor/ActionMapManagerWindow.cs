#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.Framework.ActionMapManagement.Editor
{
    public class ActionMapManagerWindow : EditorWindow
    {
        private ActionMapManager _actionMapManager;
        private InputActionAsset _inputActionAsset;
        private ActionMapConflictsPairsSO _conflictPairsSO;
        private Vector2 _scrollPosition;
        private double _lastUpdateTime;
        private const double UPDATE_INTERVAL = 0.5; // Update every 500ms

        [MenuItem(FakeMGEditorMenus.ROOT + "/Action Map Manager")]
        public static void ShowWindow()
        {
            GetWindow<ActionMapManagerWindow>("Action Map Manager");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Action Map Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawConfigurationSection();
            EditorGUILayout.Space();

            if (_actionMapManager && _inputActionAsset && _conflictPairsSO)
            {
                DrawActionMapsStatus();
            }
            else
            {
                EditorGUILayout.HelpBox("Please configure the Action Map Manager, Input Action Asset, and Conflict Pairs SO.", MessageType.Info);
            }
        }

        private void DrawConfigurationSection()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            _actionMapManager = EditorGUILayout.ObjectField(
                "Action Map Manager",
                _actionMapManager,
                typeof(ActionMapManager),
                true
            ) as ActionMapManager;

            _inputActionAsset = EditorGUILayout.ObjectField(
                "Input Action Asset",
                _inputActionAsset,
                typeof(InputActionAsset),
                false
            ) as InputActionAsset;

            _conflictPairsSO = EditorGUILayout.ObjectField(
                "Conflict Pairs SO",
                _conflictPairsSO,
                typeof(ActionMapConflictsPairsSO),
                false
            ) as ActionMapConflictsPairsSO;

            EditorGUILayout.Space();

            if (GUILayout.Button("Auto-Find in Scene", GUILayout.Height(30)))
            {
                AutoFindComponents();
            }
        }

        private void AutoFindComponents()
        {
            if (!_actionMapManager)
            {
                _actionMapManager = FindAnyObjectByType<ActionMapManager>();
            }

            if (!_inputActionAsset && _actionMapManager)
            {
                SerializedObject so = new(_actionMapManager);
                SerializedProperty prop = so.FindProperty("_inputActions");
                if (prop != null)
                {
                    _inputActionAsset = prop.objectReferenceValue as InputActionAsset;
                }
                else
                {
                    Echo.Warning("Could not find InputActionAsset property via reflection.", true);
                }
            }

            if (!_conflictPairsSO && _actionMapManager)
            {
                SerializedObject so = new(_actionMapManager);
                SerializedProperty prop = so.FindProperty("_conflictPairsSO");
                if (prop != null)
                {
                    _conflictPairsSO = prop.objectReferenceValue as ActionMapConflictsPairsSO;
                }
                else
                {
                    Echo.Warning("Could not find ActionMapConflictsPairsSO property via reflection.", true);
                }
            }
        }

        private void DrawActionMapsStatus()
        {
            EditorGUILayout.LabelField("Action Maps Status", EditorStyles.boldLabel);

            // Get reflection info to access private fields
            Type managerType = _actionMapManager.GetType();
            FieldInfo activeMapsField = managerType.GetField("_activeMaps", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo suppressedByField = managerType.GetField("_suppressedBy", BindingFlags.NonPublic | BindingFlags.Instance);

            if (activeMapsField == null || suppressedByField == null)
            {
                EditorGUILayout.HelpBox("Could not access ActionMapManager internal state.", MessageType.Error);
                return;
            }

            var activeMaps = activeMapsField.GetValue(_actionMapManager) as HashSet<string>;
            var suppressedBy = suppressedByField.GetValue(_actionMapManager) as Dictionary<string, HashSet<string>>;

            if (activeMaps == null || suppressedBy == null)
            {
                EditorGUILayout.HelpBox("Could not retrieve action map states.", MessageType.Error);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Display all action maps from the InputActionAsset
            List<InputActionMap> actionMaps = new(_inputActionAsset.actionMaps);

            if (actionMaps.Count == 0)
            {
                EditorGUILayout.HelpBox("No action maps found in the Input Action Asset.", MessageType.Info);
            }
            else
            {
                foreach (InputActionMap actionMap in actionMaps)
                {
                    DrawActionMapStatus(actionMap.name, activeMaps, suppressedBy);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawActionMapStatus(string mapName, HashSet<string> activeMaps, Dictionary<string, HashSet<string>> suppressedBy)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Determine status
            bool isActive = activeMaps.Contains(mapName);
            bool isSuppressed = suppressedBy.ContainsKey(mapName);

            // Header with map name and status badge
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(mapName, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // Status badge
            if (isSuppressed)
            {
                GUI.backgroundColor = Color.yellow;
                EditorGUILayout.LabelField("SUPPRESSED", EditorStyles.miniButton, GUILayout.Width(100));
            }
            else if (isActive)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.LabelField("ENABLED", EditorStyles.miniButton, GUILayout.Width(100));
            }
            else
            {
                GUI.backgroundColor = Color.red;
                EditorGUILayout.LabelField("DISABLED", EditorStyles.miniButton, GUILayout.Width(100));
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Suppression info
            if (isSuppressed && suppressedBy.TryGetValue(mapName, out HashSet<string> suppressors))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Suppressed by:", GUILayout.Width(100));
                EditorGUILayout.LabelField(string.Join(", ", suppressors), EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Conflicts info
            HashSet<string> conflicts = _conflictPairsSO.GetConflictsFor(mapName);
            if (conflicts.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Conflicts with:", GUILayout.Width(100));
                EditorGUILayout.LabelField(string.Join(", ", conflicts), EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void OnInspectorUpdate()
        {
            // Request repaint periodically to update status
            double timeSinceLastUpdate = EditorApplication.timeSinceStartup - _lastUpdateTime;
            if (timeSinceLastUpdate >= UPDATE_INTERVAL)
            {
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
    }
}
#endif