using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad.Advanced
{
    /// <summary>
    /// Holds an ordered list of migration steps.
    /// Drag and drop MigrationStepSO assets here via the Inspector.
    /// Steps must be ordered from oldest to newest target version.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/SaveLoad/Migration Registry")]
    public class MigrationRegistrySO : ScriptableObject
    {
        [Tooltip("Ordered list of migration steps, from oldest to newest target version.")]
        [SerializeField] private List<MigrationStepSO> _migrationSteps = new();

        public IReadOnlyList<MigrationStepSO> MigrationSteps => _migrationSteps;

        public List<MigrationStepSO> GetPendingMigrations(string savedVersion)
        {
            Version saved = Version.Parse(savedVersion);
            List<MigrationStepSO> pending = new();

            foreach (var step in _migrationSteps)
            {
                if (step.ParsedTargetVersion > saved)
                {
                    pending.Add(step);
                }
            }

            return pending;
        }

        private void OnValidate()
        {
            ValidateNoNullEntries();
            ValidateNoDuplicateVersions();
            ValidateAscendingOrder();
        }

        private void ValidateNoNullEntries()
        {
            for (int i = 0; i < _migrationSteps.Count; i++)
            {
                if (!_migrationSteps[i])
                {
                    Debug.LogWarning($"[{name}] Migration step at index {i} is null.", this);
                }
            }
        }

        private void ValidateNoDuplicateVersions()
        {
            HashSet<string> seen = new();

            foreach (var step in _migrationSteps)
            {
                if (!step) continue;

                if (!seen.Add(step.TargetVersion))
                {
                    Debug.LogError($"[{name}] Duplicate TargetVersion '{step.TargetVersion}' found.", this);
                }
            }
        }

        private void ValidateAscendingOrder()
        {
            Version previous = null;

            foreach (var step in _migrationSteps)
            {
                if (!step) continue;
                if (!Version.TryParse(step.TargetVersion, out Version current)) continue;

                if (previous != null && current <= previous)
                {
                    Debug.LogWarning(
                        $"[{name}] Migration steps are not in ascending order. " +
                        $"'{step.TargetVersion}' should come after a version greater than '{previous}'.",
                        this);
                }

                previous = current;
            }
        }
    }
}