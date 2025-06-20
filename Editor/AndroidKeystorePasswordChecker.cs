#if UNITY_ANDROID
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace FakeMG.FakeMGFramework.Editor {
    public class AndroidKeystorePasswordChecker
    {
        static AndroidKeystorePasswordChecker()
        {
            // Register our custom build handler
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
        }

        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            // Check if we are building for Android
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                (options.options & BuildOptions.Development) == 0)
            {
                string keystorePassword = PlayerSettings.Android.keystorePass;
                string keyaliasPassword = PlayerSettings.Android.keyaliasPass;

                // If either password is empty or null, block the build
                if (string.IsNullOrEmpty(keystorePassword) || string.IsNullOrEmpty(keyaliasPassword))
                {
                    EditorUtility.DisplayDialog("Keystore Password Missing",
                        "Please set both the Keystore and Key Alias passwords in Project Settings > Player > Publishing Settings before building.",
                        "OK");
                    return; // Cancel build
                }
            }

            // Proceed with build
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
    }
}
#endif