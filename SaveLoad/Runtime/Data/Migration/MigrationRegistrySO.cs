using System;
using System.Collections.Generic;
using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/SaveLoad/Migration Registry")]
    public sealed class MigrationRegistrySO : ScriptableObject, ISaveMigrationPlan
    {
        [ValidateInput(nameof(AreMigrationStepEntriesAssigned), "Every migration step must be assigned.")]
        [SerializeField] private List<MigrationStepSO> _migrationStepSOs = new();

        public IReadOnlyList<MigrationStepSO> MigrationStepSOs => _migrationStepSOs;

        #region Unity Lifecycle

        private void OnValidate()
        {
            foreach (string failureReason in GetRegistryValidationFailures())
            {
                Echo.Error($"[{name}] {failureReason}");
            }
        }

        #endregion

        #region Public Methods

        public bool TryGetMigrationPath(
            string savedVersion,
            string targetVersion,
            out IReadOnlyList<ISaveMigrationStep> migrationSteps,
            out string failureReason)
        {
            migrationSteps = Array.Empty<ISaveMigrationStep>();
            if (!Version.TryParse(savedVersion, out Version parsedSavedVersion))
            {
                failureReason = $"Saved version '{savedVersion}' is invalid.";
                return false;
            }

            if (!Version.TryParse(targetVersion, out Version parsedTargetVersion))
            {
                failureReason = $"Runtime version '{targetVersion}' is invalid.";
                return false;
            }

            if (parsedSavedVersion > parsedTargetVersion)
            {
                failureReason = $"Save version '{savedVersion}' is newer than runtime version '{targetVersion}'.";
                return false;
            }

            if (parsedSavedVersion == parsedTargetVersion)
            {
                failureReason = string.Empty;
                return true;
            }

            if (!TryValidateRegistry(out failureReason))
            {
                return false;
            }

            Dictionary<Version, MigrationStepSO> stepsBySourceVersion = new();
            foreach (MigrationStepSO migrationStepSO in _migrationStepSOs)
            {
                stepsBySourceVersion.Add(Version.Parse(migrationStepSO.SourceVersion), migrationStepSO);
            }

            List<ISaveMigrationStep> resolvedSteps = new();
            Version currentVersion = parsedSavedVersion;
            while (currentVersion < parsedTargetVersion)
            {
                if (!stepsBySourceVersion.TryGetValue(currentVersion, out MigrationStepSO migrationStepSO))
                {
                    failureReason = $"Migration chain has a gap after version '{currentVersion}'.";
                    return false;
                }

                Version nextVersion = Version.Parse(migrationStepSO.TargetVersion);
                if (nextVersion > parsedTargetVersion)
                {
                    failureReason = $"Migration to '{nextVersion}' exceeds runtime target '{parsedTargetVersion}'.";
                    return false;
                }

                resolvedSteps.Add(migrationStepSO);
                currentVersion = nextVersion;
            }

            if (currentVersion != parsedTargetVersion)
            {
                failureReason = $"Migration chain ended at '{currentVersion}' instead of '{parsedTargetVersion}'.";
                return false;
            }

            migrationSteps = resolvedSteps;
            failureReason = string.Empty;
            return true;
        }

        #endregion

        #region Private Methods

        private bool TryValidateRegistry(out string failureReason)
        {
            IReadOnlyList<string> validationFailures = GetRegistryValidationFailures();
            if (validationFailures.Count > 0)
            {
                failureReason = string.Join(Environment.NewLine, validationFailures);
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private IReadOnlyList<string> GetRegistryValidationFailures()
        {
            List<string> validationFailures = new();
            HashSet<string> sourceVersions = new(StringComparer.Ordinal);
            HashSet<string> targetVersions = new(StringComparer.Ordinal);
            for (int migrationIndex = 0; migrationIndex < _migrationStepSOs.Count; migrationIndex++)
            {
                MigrationStepSO migrationStepSO = _migrationStepSOs[migrationIndex];
                if (!migrationStepSO)
                {
                    validationFailures.Add($"Migration step at index {migrationIndex} is missing.");
                    continue;
                }

                if (!Version.TryParse(migrationStepSO.SourceVersion, out Version sourceVersion) ||
                    !Version.TryParse(migrationStepSO.TargetVersion, out Version targetVersion))
                {
                    validationFailures.Add($"Migration '{migrationStepSO.name}' has an invalid or empty version.");
                    continue;
                }

                if (targetVersion <= sourceVersion)
                {
                    validationFailures.Add($"Migration '{migrationStepSO.name}' does not advance the version.");
                }

                if (!sourceVersions.Add(migrationStepSO.SourceVersion))
                {
                    validationFailures.Add($"Duplicate migration source '{migrationStepSO.SourceVersion}'.");
                }

                if (!targetVersions.Add(migrationStepSO.TargetVersion))
                {
                    validationFailures.Add($"Duplicate migration target '{migrationStepSO.TargetVersion}'.");
                }
            }

            return validationFailures;
        }

        private bool AreMigrationStepEntriesAssigned(List<MigrationStepSO> migrationStepSOs)
        {
            return migrationStepSOs != null && migrationStepSOs.TrueForAll(migrationStepSO => migrationStepSO);
        }

        #endregion
    }
}
