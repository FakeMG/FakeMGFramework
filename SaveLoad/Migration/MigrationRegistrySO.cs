using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Stores migration steps from oldest to newest and selects every step newer than a saved
    /// version. Editor validation reports null, duplicate, and incorrectly ordered entries.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/SaveLoad/Migration Registry")]
    public sealed class MigrationRegistrySO : ScriptableObject, ISaveMigrationPlan
    {
        [SerializeField] private List<MigrationStepSO> _migrationSteps = new();

        public IReadOnlyList<MigrationStepSO> MigrationSteps => _migrationSteps;

        #region Unity Lifecycle

        private void OnValidate()
        {
            ValidateNoNullEntries();
            ValidateNoDuplicateVersions();
            ValidateAscendingOrder();
        }

        #endregion

        #region Public Methods

        public IReadOnlyList<ISaveMigrationStep> GetPendingMigrations(string savedVersion)
        {
            Version parsedSavedVersion = Version.Parse(savedVersion);
            List<ISaveMigrationStep> pendingMigrations = new();
            foreach (MigrationStepSO migrationStepSO in _migrationSteps)
            {
                if (migrationStepSO.ParsedTargetVersion > parsedSavedVersion)
                {
                    pendingMigrations.Add(migrationStepSO);
                }
            }

            return pendingMigrations;
        }

        #endregion

        #region Private Methods

        private void ValidateNoNullEntries()
        {
            for (int migrationIndex = 0; migrationIndex < _migrationSteps.Count; migrationIndex++)
            {
                if (!_migrationSteps[migrationIndex])
                {
                    Echo.Warning($"[{name}] Migration step at index {migrationIndex} is null.");
                }
            }
        }

        private void ValidateNoDuplicateVersions()
        {
            HashSet<string> seenVersions = new();
            foreach (MigrationStepSO migrationStepSO in _migrationSteps)
            {
                if (migrationStepSO && !seenVersions.Add(migrationStepSO.TargetVersion))
                {
                    Echo.Error(
                        $"[{name}] Duplicate TargetVersion '{migrationStepSO.TargetVersion}' found.");
                }
            }
        }

        private void ValidateAscendingOrder()
        {
            Version previousVersion = null;
            foreach (MigrationStepSO migrationStepSO in _migrationSteps)
            {
                if (!migrationStepSO ||
                    !Version.TryParse(migrationStepSO.TargetVersion, out Version currentVersion))
                {
                    continue;
                }

                if (previousVersion != null && currentVersion <= previousVersion)
                {
                    Echo.Warning(
                        $"[{name}] Migration steps are not in ascending order at " +
                        $"'{migrationStepSO.TargetVersion}'.");
                }

                previousVersion = currentVersion;
            }
        }

        #endregion
    }
}
