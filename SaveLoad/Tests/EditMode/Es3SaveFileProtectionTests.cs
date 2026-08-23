using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class Es3SaveFileProtectionTests
    {
        private readonly List<string> _temporaryFilePaths = new();

        [TearDown]
        public void TearDown()
        {
            foreach (string temporaryFilePath in _temporaryFilePaths)
            {
                if (File.Exists(temporaryFilePath))
                {
                    File.Delete(temporaryFilePath);
                }
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void WriteSaveFile_AllProtectionCombinations_RoundTripWithMatchingProfile(
            bool isEncryptionEnabled,
            bool isCompressionEnabled)
        {
            SaveFileProtectionSettings protectionSettings = CreateProtectionSettings(
                isEncryptionEnabled,
                isCompressionEnabled);
            string temporaryFilePath = CreateTemporaryFilePath();
            var saveDataStore = new Es3SaveDataStore(protectionSettings, Path.GetTempPath());

            saveDataStore.WriteSaveFile(
                temporaryFilePath,
                CreateMetadata(),
                new Dictionary<string, object> { ["Score"] = 42 });

            Assert.That(saveDataStore.LoadState("Score", temporaryFilePath), Is.EqualTo(42));
            string fileText = File.ReadAllText(temporaryFilePath);
            Assert.That(
                fileText.Contains("Score"),
                Is.EqualTo(!isEncryptionEnabled && !isCompressionEnabled));
        }

        [Test]
        public void LoadMetadata_WrongPassword_RejectsProtectedFile()
        {
            string temporaryFilePath = CreateTemporaryFilePath();
            var writer = new Es3SaveDataStore(
                new SaveFileProtectionSettings(true, true, "correct-password"),
                Path.GetTempPath());
            var wrongReader = new Es3SaveDataStore(
                new SaveFileProtectionSettings(true, true, "wrong-password"),
                Path.GetTempPath());
            writer.WriteSaveFile(temporaryFilePath, CreateMetadata(), new Dictionary<string, object>());

            Assert.That(
                () => wrongReader.LoadMetadata(temporaryFilePath),
                Throws.InstanceOf<Exception>());
        }

        [Test]
        public void Constructor_EncryptionWithoutPassword_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SaveFileProtectionSettings(true, false, string.Empty));
        }

        private string CreateTemporaryFilePath()
        {
            string temporaryFilePath = Path.Combine(
                Path.GetTempPath(),
                $"FakeMGEs3ProtectionTests_{Guid.NewGuid():N}.es3");
            _temporaryFilePaths.Add(temporaryFilePath);
            return temporaryFilePath;
        }

        private static SaveFileProtectionSettings CreateProtectionSettings(
            bool isEncryptionEnabled,
            bool isCompressionEnabled)
        {
            return new SaveFileProtectionSettings(
                isEncryptionEnabled,
                isCompressionEnabled,
                isEncryptionEnabled ? "test-password" : string.Empty);
        }

        private static SaveMetadata CreateMetadata()
        {
            return new SaveMetadata
            {
                TimestampUtc = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc),
                ApplicationVersion = "1.0.0",
                SaveKind = SaveFileKind.GlobalDocument,
                OwnerId = "settings",
            };
        }
    }
}
