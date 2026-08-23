using System;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class WorldSnapshotRetentionPolicyTests
    {
        [Test]
        public void SelectExpiredAutoSaves_SixAutoSavesAndMaximumFive_ReturnsOnlyOldestAutoSave()
        {
            const string WORLD_ID = "world_aaaaaaaabbbbccccddddeeeeeeeeeeee";
            var snapshots = new[]
            {
                CreateSnapshot(WORLD_ID, "autosave_1.sav", SaveFileKind.Auto, 1),
                CreateSnapshot(WORLD_ID, "autosave_2.sav", SaveFileKind.Auto, 2),
                CreateSnapshot(WORLD_ID, "autosave_3.sav", SaveFileKind.Auto, 3),
                CreateSnapshot(WORLD_ID, "autosave_4.sav", SaveFileKind.Auto, 4),
                CreateSnapshot(WORLD_ID, "autosave_5.sav", SaveFileKind.Auto, 5),
                CreateSnapshot(WORLD_ID, "autosave_6.sav", SaveFileKind.Auto, 6),
                CreateSnapshot(WORLD_ID, "manual_0.sav", SaveFileKind.Manual, 0),
            };
            var retentionPolicy = new WorldSnapshotRetentionPolicy();

            var expiredSnapshots = retentionPolicy.SelectExpiredAutoSaves(snapshots, 5);

            Assert.That(expiredSnapshots.Count, Is.EqualTo(1));
            Assert.That(expiredSnapshots[0].FileName, Is.EqualTo("autosave_1.sav"));
        }

        private static WorldSnapshotSummary CreateSnapshot(
            string worldId,
            string fileName,
            SaveFileKind saveKind,
            long ticks)
        {
            var metadata = new SaveMetadata
            {
                OwnerId = worldId,
                SaveKind = saveKind,
                TimestampUtc = new DateTime(ticks, DateTimeKind.Utc),
                ApplicationVersion = "1.0.0",
            };
            return new WorldSnapshotSummary(
                new ValidatedSaveFileInfo($"Saves/{worldId}/{fileName}", metadata, SaveFileProtectionSettings.Plain));
        }
    }
}
