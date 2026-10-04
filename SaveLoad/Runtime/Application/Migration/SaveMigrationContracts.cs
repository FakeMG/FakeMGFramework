using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Defines one serializer-independent save transformation and the version it produces.
    /// </summary>
    public interface ISaveMigrationStep
    {
        string SourceVersion { get; }
        string TargetVersion { get; }
        void Migrate(ISaveDataStore saveDataStore, string saveFilePath);
    }

    /// <summary>
    /// Selects ordered migration steps for a saved version without exposing their storage format.
    /// </summary>
    public interface ISaveMigrationPlan
    {
        bool TryGetMigrationPath(
            string savedVersion,
            string targetVersion,
            out IReadOnlyList<ISaveMigrationStep> migrationSteps,
            out string failureReason);
    }
}
