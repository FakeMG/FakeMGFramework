using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    public interface ISaveMigrationStep
    {
        string SourceVersion { get; }
        string TargetVersion { get; }
        void Migrate(ISaveDataStore saveDataStore, string saveFilePath);
    }

    public interface ISaveMigrationPlan
    {
        bool TryGetMigrationPath(
            string savedVersion,
            string targetVersion,
            out IReadOnlyList<ISaveMigrationStep> migrationSteps,
            out string failureReason);
    }
}
