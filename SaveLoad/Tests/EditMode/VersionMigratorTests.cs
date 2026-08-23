using System;
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
            IAtomicFileTransaction atomicFileTransaction = Substitute.For<IAtomicFileTransaction>();
            ISaveMigrationStep firstStep = Substitute.For<ISaveMigrationStep>();
            ISaveMigrationStep secondStep = Substitute.For<ISaveMigrationStep>();
            firstStep.SourceVersion.Returns("1.0.0");
            firstStep.TargetVersion.Returns("1.1.0");
            secondStep.SourceVersion.Returns("1.1.0");
            secondStep.TargetVersion.Returns("1.2.0");
            IReadOnlyList<ISaveMigrationStep> migrationSteps =
                new List<ISaveMigrationStep> { firstStep, secondStep };
            migrationPlan.TryGetMigrationPath(
                    "1.0.0",
                    "1.2.0",
                    out Arg.Any<IReadOnlyList<ISaveMigrationStep>>(),
                    out Arg.Any<string>())
                .Returns(callInfo =>
                {
                    callInfo[2] = migrationSteps;
                    callInfo[3] = string.Empty;
                    return true;
                });
            var metadata = new SaveMetadata { ApplicationVersion = "1.0.0" };
            saveDataStore.LoadMetadata("save.tmp").Returns(metadata);
            atomicFileTransaction
                .When(transaction => transaction.Commit("save.sav", Arg.Any<Action<string>>()))
                .Do(call => call.Arg<Action<string>>()("save.tmp"));
            var versionMigrator = new VersionMigrator(
                migrationPlan,
                saveDataStore,
                atomicFileTransaction,
                "1.2.0");

            MigrationResult result = versionMigrator.MigrateSaveFile("save.sav", "1.0.0");

            Assert.That(result.Succeeded, Is.True);
            Received.InOrder(() =>
            {
                firstStep.Migrate(saveDataStore, "save.tmp");
                secondStep.Migrate(saveDataStore, "save.tmp");
            });
            Assert.That(metadata.ApplicationVersion, Is.EqualTo("1.2.0"));
            saveDataStore.Received(2).CopyFile("save.sav", "save.tmp");
            saveDataStore.Received(2).SaveMetadata("save.tmp", metadata);
        }
    }
}
