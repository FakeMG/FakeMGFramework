using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad.Advanced
{
    /// <summary>
    /// Executes pending migration steps on a save file.
    /// Updates metadata after each successful step for crash safety.
    /// </summary>
    public class VersionMigrator
    {
        private readonly MigrationRegistrySO _registry;
        private readonly bool _enableDebug;

        public VersionMigrator(MigrationRegistrySO registry, bool enableDebug)
        {
            _registry = registry;
            _enableDebug = enableDebug;
        }

        public bool MigrateSaveFile(string savePath, string savedVersion)
        {
            List<MigrationStepSO> pending = _registry.GetPendingMigrations(savedVersion);

            if (pending.Count == 0)
            {
                Echo.Log($"No pending migrations for version {savedVersion}.", _enableDebug);
                return true;
            }

            Echo.Log($"Running {pending.Count} migration(s) from version {savedVersion}.", _enableDebug);

            foreach (var step in pending)
            {
                try
                {
                    step.Migrate(savePath);
                    UpdateSaveVersion(savePath, step.TargetVersion);
                    Echo.Log($"Migration to {step.TargetVersion} succeeded.", _enableDebug);
                }
                catch (Exception e)
                {
                    Echo.Error(
                        $"Migration to {step.TargetVersion} failed: {e.Message}. " +
                        $"Save file remains at the last successful version.",
                        _enableDebug);
                    return false;
                }
            }

            Echo.Log($"All migrations complete. Save file is now at version {Application.version}.", _enableDebug);
            return true;
        }

        private static void UpdateSaveVersion(string savePath, string newVersion)
        {
            SaveMetadata metadata = ES3.Load(SaveFileCatalog.METADATA_KEY, savePath, new SaveMetadata());
            metadata.GameVersion = newVersion;
            ES3.Save(SaveFileCatalog.METADATA_KEY, metadata, savePath);
        }
    }
}