using System;
using System.Collections.Generic;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public readonly struct MigrationResult
    {
        public bool Succeeded { get; }
        public string FailureReason { get; }

        private MigrationResult(bool succeeded, string failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason ?? string.Empty;
        }

        public static MigrationResult Success()
        {
            return new MigrationResult(true, string.Empty);
        }

        public static MigrationResult Failure(string failureReason)
        {
            return new MigrationResult(false, failureReason);
        }
    }

    public sealed class VersionMigrator
    {
        private readonly ISaveMigrationPlan _migrationPlan;
        private readonly ISaveDataStore _saveDataStore;
        private readonly IAtomicFileTransaction _atomicFileTransaction;
        private readonly string _targetVersion;

        public VersionMigrator(
            ISaveMigrationPlan migrationPlan,
            ISaveDataStore saveDataStore,
            IAtomicFileTransaction atomicFileTransaction,
            string targetVersion)
        {
            _migrationPlan = migrationPlan;
            _saveDataStore = saveDataStore;
            _atomicFileTransaction = atomicFileTransaction;
            _targetVersion = targetVersion;
        }

        #region Public Methods

        public MigrationResult MigrateSaveFile(string saveFilePath, string savedVersion)
        {
            if (!_migrationPlan.TryGetMigrationPath(
                    savedVersion,
                    _targetVersion,
                    out IReadOnlyList<ISaveMigrationStep> migrationSteps,
                    out string failureReason))
            {
                Echo.Error(failureReason);
                return MigrationResult.Failure(failureReason);
            }

            foreach (ISaveMigrationStep migrationStep in migrationSteps)
            {
                try
                {
                    _atomicFileTransaction.Commit(
                        saveFilePath,
                        temporaryFilePath => MigrateTemporaryFile(saveFilePath, temporaryFilePath, migrationStep));
                }
                catch (Exception exception)
                {
                    string stepFailureReason = $"Migration from '{migrationStep.SourceVersion}' to '{migrationStep.TargetVersion}' failed: {exception}";
                    Echo.Error(stepFailureReason);
                    return MigrationResult.Failure(stepFailureReason);
                }
            }

            return MigrationResult.Success();
        }

        #endregion

        #region Private Methods

        private void MigrateTemporaryFile(
            string sourceFilePath,
            string temporaryFilePath,
            ISaveMigrationStep migrationStep)
        {
            _saveDataStore.CopyFile(sourceFilePath, temporaryFilePath);
            migrationStep.Migrate(_saveDataStore, temporaryFilePath);
            SaveMetadata metadata = _saveDataStore.LoadMetadata(temporaryFilePath);
            metadata.ApplicationVersion = migrationStep.TargetVersion;
            _saveDataStore.SaveMetadata(temporaryFilePath, metadata);
        }

        #endregion
    }
}
