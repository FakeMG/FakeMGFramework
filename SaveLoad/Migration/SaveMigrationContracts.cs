using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Defines one serializer-independent save transformation and the version it produces.
    /// </summary>
    public interface ISaveMigrationStep
    {
        string TargetVersion { get; }
        void Migrate(ISaveDataStore saveDataStore, string saveFilePath);
    }

    /// <summary>
    /// Selects ordered migration steps for a saved version without exposing their storage format.
    /// </summary>
    public interface ISaveMigrationPlan
    {
        IReadOnlyList<ISaveMigrationStep> GetPendingMigrations(string savedVersion);
    }
}
