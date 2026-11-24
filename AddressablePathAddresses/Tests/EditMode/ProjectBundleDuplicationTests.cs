using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEngine.SceneManagement;

namespace FakeMG.AddressablePathAddresses.Tests
{
    /// <summary>
    /// Runs Unity's own duplicate-dependency rule, because raw asset dependencies also list editor-only references that never
    /// reach a bundle.
    /// </summary>
    public sealed class ProjectBundleDuplicationTests
    {
        private const string NO_ISSUES_RESULT = "No issues found";
        private const string NO_SETTINGS_MESSAGE = "This project has no Addressables settings, so it has no bundles to check.";
        private const string UNSAVED_SCENE_MESSAGE = "Save the open scenes first: the analysis would block on a save dialog.";
        private const string ANALYSIS_FAILED_MESSAGE = "The duplicate analysis did not complete.";
        private const string FIX_GUIDANCE = "These assets are copied into several bundles. Mark each one Addressable in a shared group.";

        [Test]
        public void NoImplicitDependency_IsCopiedIntoMoreThanOneBundle()
        {
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
            {
                Assert.Ignore(NO_SETTINGS_MESSAGE);
            }

            RequireSavedScenes();

            IReadOnlyList<string> resultNames = RunDuplicateAnalysis();

            Assert.That(resultNames.Where(IsFailedAnalysisRow), Is.Empty, ANALYSIS_FAILED_MESSAGE);
            Assert.That(ExtractDuplicatedAssetPaths(resultNames), Is.Empty, FIX_GUIDANCE);
        }

        #region Private Methods

        private static void RequireSavedScenes()
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                if (SceneManager.GetSceneAt(sceneIndex).isDirty)
                {
                    Assert.Inconclusive(UNSAVED_SCENE_MESSAGE);
                }
            }
        }

        private static IReadOnlyList<string> RunDuplicateAnalysis()
        {
            var rule = new CheckBundleDupeDependencies();
            try
            {
                return rule.RefreshAnalysis(AddressableAssetSettingsDefaultObject.Settings)
                    .Select(result => result.resultName)
                    .ToArray();
            }
            finally
            {
                rule.ClearAnalysis();
            }
        }

        /// <remarks>The rule reports an unsaved scene or a failed build as a plain row, which must not read as a clean result.</remarks>
        private static bool IsFailedAnalysisRow(string resultName)
        {
            return !IsDuplicateRow(resultName) && resultName != NO_ISSUES_RESULT;
        }

        private static IEnumerable<string> ExtractDuplicatedAssetPaths(IEnumerable<string> resultNames)
        {
            return resultNames
                .Where(IsDuplicateRow)
                .Select(resultName => resultName.Split(AnalyzeRule.kDelimiter).Last())
                .Distinct()
                .OrderBy(assetPath => assetPath);
        }

        private static bool IsDuplicateRow(string resultName)
        {
            return resultName.Contains(AnalyzeRule.kDelimiter);
        }

        #endregion
    }
}
