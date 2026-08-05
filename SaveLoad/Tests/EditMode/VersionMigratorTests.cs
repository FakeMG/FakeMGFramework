using System.Collections.Generic;
using FakeMG.SaveLoad;
using NSubstitute;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    /// <summary>
    /// Verifies migration ordering and per-step metadata advancement through storage abstractions.
    /// </summary>
    public sealed class VersionMigratorTests
    {
        [Test]
        public void MigrateSaveFile_TwoPendingSteps_RunsInOrderAndAdvancesMetadata()
        {
            ISaveDataStore saveDataStore = Substitute.For<ISaveDataStore>();
            ISaveMigrationPlan migrationPlan = Substitute.For<ISaveMigrationPlan>();
            ISaveMigrationStep firstStep = Substitute.For<ISaveMigrationStep>();
            ISaveMigrationStep secondStep = Substitute.For<ISaveMigrationStep>();
            firstStep.TargetVersion.Returns("1.1.0");
            secondStep.TargetVersion.Returns("1.2.0");
            migrationPlan.GetPendingMigrations("1.0.0").Returns(
                new List<ISaveMigrationStep> { firstStep, secondStep });
            var metadata = new SaveMetadata { GameVersion = "1.0.0" };
            saveDataStore.LoadMetadata("save.es3").Returns(metadata);
            var versionMigrator = new VersionMigrator(migrationPlan, saveDataStore);

            bool didMigrationSucceed = versionMigrator.MigrateSaveFile("save.es3", "1.0.0");

            Assert.That(didMigrationSucceed, Is.True);
            Received.InOrder(() =>
            {
                firstStep.Migrate(saveDataStore, "save.es3");
                secondStep.Migrate(saveDataStore, "save.es3");
            });
            Assert.That(metadata.GameVersion, Is.EqualTo("1.2.0"));
            saveDataStore.Received(2).SaveMetadata("save.es3", metadata);
        }
    }
}
