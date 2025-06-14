using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace FakeMG.FakeMGFramework.Editor {
    public class KeyStorePasswordCheck : IPreprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
#if UNITY_ANDROID
        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
        {
            Debug.LogError("Build canceled: Android KeyStore password is not set.");
            throw new BuildFailedException("Android KeyStore password is not set.");
        }

        if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
        {
            Debug.LogError("Build canceled: Key Alias password is not set.");
            throw new BuildFailedException("Key Alias password is not set.");
        }
#endif
        }
    }
}