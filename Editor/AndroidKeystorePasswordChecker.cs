#if UNITY_EDITOR && UNITY_ANDROID
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace FakeMG.Framework.Editor
{
    // Works in Unity 6+ and older Unity versions
    [InitializeOnLoad]
    public class AndroidKeystorePasswordChecker : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static AndroidKeystorePasswordChecker()
        {
#if !UNITY_6000_0_OR_NEWER
            // For Unity 2022–2023 and earlier (Register custom build handler)
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
#endif
        }

        // Unity 6+ build pipeline automatically calls this before a build starts
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android &&
                !EditorUserBuildSettings.development)
            {
                ValidateKeystorePasswords();
            }
        }

#if !UNITY_6000_0_OR_NEWER
        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            // Check for Android release builds
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                (options.options & BuildOptions.Development) == 0)
            {
                if (!ValidateKeystorePasswords())
                    return; // Cancel build
            }

            // Continue build
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
#endif

        private static bool ValidateKeystorePasswords()
        {
            string keystorePassword = PlayerSettings.Android.keystorePass;
            string keyaliasPassword = PlayerSettings.Android.keyaliasPass;

            if (string.IsNullOrEmpty(keystorePassword) || string.IsNullOrEmpty(keyaliasPassword))
            {
                EditorUtility.DisplayDialog(
                    "Keystore Password Missing",
                    "Please set both the Keystore and Key Alias passwords in " +
                    "Project Settings > Player > Publishing Settings before building.",
                    "OK"
                );

                throw new BuildFailedException("Keystore or Key Alias password missing.");
            }

            return true;
        }
    }
}
#endif
