using UnityEditor;

namespace FakeMG.TestRunThrottleBypass.Editor
{
    /// <summary>
    /// Drives <see cref="TestRunBackgroundThrottleBypass"/> from the editor loop for the lifetime of each domain.
    /// </summary>
    [InitializeOnLoad]
    public static class TestRunBackgroundThrottleBypassSubscriber
    {
        private static readonly TestRunBackgroundThrottleBypass _bypass = new();

        static TestRunBackgroundThrottleBypassSubscriber()
        {
            if (!_bypass.IsSupported)
            {
                return;
            }

            EditorApplication.update += KeepEditorAwakeDuringTestRun;
            AssemblyReloadEvents.beforeAssemblyReload += UnsubscribeBeforeDomainReload;
        }

        #region Private Methods

        private static void KeepEditorAwakeDuringTestRun()
        {
            _bypass.RepaintWakeWindowWhileTestRunIsActive();
        }

        private static void UnsubscribeBeforeDomainReload()
        {
            EditorApplication.update -= KeepEditorAwakeDuringTestRun;
            AssemblyReloadEvents.beforeAssemblyReload -= UnsubscribeBeforeDomainReload;
        }

        #endregion
    }
}
