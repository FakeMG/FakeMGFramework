using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Executes pending migrations in order and advances metadata after each successful step. A
    /// failure leaves metadata at the last completed version so the next load can resume safely.
    /// </summary>
    public sealed class VersionMigrator
    {
        private readonly ISaveMigrationPlan _migrationPlan;
        private readonly ISaveDataStore _saveDataStore;

        public VersionMigrator(
            ISaveMigrationPlan migrationPlan,
            ISaveDataStore saveDataStore)
        {
            _migrationPlan = migrationPlan;
            _saveDataStore = saveDataStore;
        }

        #region Public Methods

        public bool MigrateSaveFile(string saveFilePath, string savedVersion)
        {
            IReadOnlyList<ISaveMigrationStep> pendingMigrations =
                _migrationPlan.GetPendingMigrations(savedVersion);
            if (pendingMigrations.Count == 0)
            {
                Echo.Log($"No pending migrations for version {savedVersion}.");
                return true;
            }

            Echo.Log($"Running {pendingMigrations.Count} migration(s) from version {savedVersion}.");
            foreach (ISaveMigrationStep migrationStep in pendingMigrations)
            {
                try
                {
                    migrationStep.Migrate(_saveDataStore, saveFilePath);
                    UpdateSaveVersion(saveFilePath, migrationStep.TargetVersion);
                    Echo.Log($"Migration to {migrationStep.TargetVersion} succeeded.");
                }
                catch (Exception exception)
                {
                    Echo.Error(
                        $"Migration to {migrationStep.TargetVersion} failed: {exception.Message}. " +
                        "Save file remains at the last successful version.");
                    return false;
                }
            }

            Echo.Log($"All migrations complete. Current application version is {Application.version}.");
            return true;
        }

        #endregion

        #region Private Methods

        private void UpdateSaveVersion(string saveFilePath, string newVersion)
        {
            SaveMetadata metadata = _saveDataStore.LoadMetadata(saveFilePath);
            metadata.GameVersion = newVersion;
            _saveDataStore.SaveMetadata(saveFilePath, metadata);
        }

        #endregion
    }
}
