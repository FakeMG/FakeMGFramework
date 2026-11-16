using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FakeMG.TestRunAddressablesLoadDelay.Editor
{
    /// <summary>
    /// Shortens the simulated load delay of "Use Asset Database" Addressables loads while tests run, and restores the
    /// project's authored delay afterwards. Tests load their own assets in every setup, so the authored delay (0.1s by
    /// default, kept for editor play sessions to surface slow-load spots) dominated play mode test time.
    /// The override lives only in memory and is never saved to the settings asset.
    /// </summary>
    public sealed class TestRunAddressablesLoadDelay
    {
        // Must stay at or above 0.01s: below it Addressables completes loads synchronously inside LoadAssetAsync,
        // unlike player builds, which would let tests pass on code that only works with synchronous loads.
        private const float TEST_RUN_LOAD_DELAY_SECONDS = 0.02f;
        private const float NO_OVERRIDE_MARKER = -1f;

        // Survives domain reloads, so the authored delay is still known if a run reloads the domain mid-way.
        private const string AUTHORED_DELAY_SESSION_KEY = "FakeMG.TestRunAddressablesLoadDelay.AuthoredDelaySeconds";

        #region Public Methods

        public void OverrideAuthoredDelayForTestRun()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings || HasActiveOverride())
            {
                return;
            }

            SessionState.SetFloat(AUTHORED_DELAY_SESSION_KEY, settings.SimulatedLoadDelay);
            settings.SimulatedLoadDelay = TEST_RUN_LOAD_DELAY_SECONDS;
        }

        public void RestoreAuthoredDelay()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings || !HasActiveOverride())
            {
                return;
            }

            settings.SimulatedLoadDelay = SessionState.GetFloat(AUTHORED_DELAY_SESSION_KEY, NO_OVERRIDE_MARKER);
            SessionState.EraseFloat(AUTHORED_DELAY_SESSION_KEY);
        }

        #endregion

        #region Private Methods

        private static bool HasActiveOverride()
        {
            return SessionState.GetFloat(AUTHORED_DELAY_SESSION_KEY, NO_OVERRIDE_MARKER) >= 0f;
        }

        #endregion
    }
}
