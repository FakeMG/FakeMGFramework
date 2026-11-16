using System;
using System.Reflection;
using FakeMG.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditorInternal;
using UnityEngine;

namespace FakeMG.TestRunThrottleBypass.Editor
{
    /// <summary>
    /// Keeps the editor loop at full speed while a test run is active and Unity is unfocused.
    /// Unity sleeps its loop down to ~10 Hz in the background, and the test runner advances one step per editor tick
    /// (every async test, [UnityTest] yield and run setup task), so an unfocused run is bound by that sleep rather than
    /// by test work. A pending window repaint makes Unity skip the sleep, so repainting one cheap window every tick
    /// removes the throttle.
    /// </summary>
    public sealed class TestRunBackgroundThrottleBypass
    {
        // Internal Unity API, resolved by name. Unlike RunStarted/RunFinished callbacks, it covers the whole job,
        // including scene saving, test tree building and entering/exiting play mode.
        private const string IS_RUN_ACTIVE_METHOD_NAME = "IsRunActive";

        // Internal Unity type, resolved by name. The main toolbar is always open and cheap to repaint,
        // unlike the Scene and Game views, whose repaint cost caps the loop at ~26 Hz.
        private const string WAKE_WINDOW_TYPE_NAME = "UnityEditor.MainToolbarWindow";

        // Failsafe for jobs that never finish (aborted or orphaned); otherwise the editor would spin indefinitely.
        private const float MAX_BYPASS_SECONDS_PER_RUN = 30f * 60f;
        private const float NO_ACTIVE_RUN_MARKER = -1f;

        // Survives domain reloads, which play mode test runs may trigger mid-run.
        private const string RUN_DETECTED_AT_SESSION_KEY = "FakeMG.TestRunThrottleBypass.RunDetectedAtSeconds";

        private readonly Func<bool> _isTestRunActive;
        private readonly Type _wakeWindowType;
        private EditorWindow _wakeWindow;

        #region Public Methods

        public TestRunBackgroundThrottleBypass()
        {
            _isTestRunActive = ResolveIsTestRunActive();
            _wakeWindowType = typeof(EditorWindow).Assembly.GetType(WAKE_WINDOW_TYPE_NAME);

            if (_isTestRunActive == null || _wakeWindowType == null)
            {
                Echo.Warning($"{nameof(TestRunnerApi)}.{IS_RUN_ACTIVE_METHOD_NAME} or {WAKE_WINDOW_TYPE_NAME} no longer exists " +
                             "in this Unity version; unfocused test runs stay throttled.");
            }
        }

        public bool IsSupported => _isTestRunActive != null && _wakeWindowType != null;

        public void RepaintWakeWindowWhileTestRunIsActive()
        {
            if (!_isTestRunActive())
            {
                SessionState.EraseFloat(RUN_DETECTED_AT_SESSION_KEY);
                return;
            }

            double bypassedSeconds = EditorApplication.timeSinceStartup - GetOrRecordRunDetectionTimeSeconds();
            if (bypassedSeconds > MAX_BYPASS_SECONDS_PER_RUN || InternalEditorUtility.isApplicationActive)
            {
                return;
            }

            if (!_wakeWindow)
            {
                _wakeWindow = FindWakeWindow();
            }

            if (_wakeWindow)
            {
                _wakeWindow.Repaint();
            }
        }

        #endregion

        #region Private Methods

        private static Func<bool> ResolveIsTestRunActive()
        {
            MethodInfo method = typeof(TestRunnerApi).GetMethod(IS_RUN_ACTIVE_METHOD_NAME, BindingFlags.NonPublic | BindingFlags.Static);
            bool hasExpectedSignature = method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0;
            return hasExpectedSignature ? (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method) : null;
        }

        private static float GetOrRecordRunDetectionTimeSeconds()
        {
            float runDetectedAtSeconds = SessionState.GetFloat(RUN_DETECTED_AT_SESSION_KEY, NO_ACTIVE_RUN_MARKER);
            if (runDetectedAtSeconds >= 0f)
            {
                return runDetectedAtSeconds;
            }

            runDetectedAtSeconds = (float)EditorApplication.timeSinceStartup;
            SessionState.SetFloat(RUN_DETECTED_AT_SESSION_KEY, runDetectedAtSeconds);
            return runDetectedAtSeconds;
        }

        private EditorWindow FindWakeWindow()
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(_wakeWindowType);
            return windows.Length > 0 ? (EditorWindow)windows[0] : null;
        }

        #endregion
    }
}
