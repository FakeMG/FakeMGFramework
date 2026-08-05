using FakeMG.SaveLoad;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    /// <summary>
    /// Verifies the centralized companion-file naming contract used by atomic transactions.
    /// </summary>
    public sealed class AtomicFileTransactionPathsTests
    {
        [Test]
        public void CompanionPaths_CanonicalPath_ReturnExpectedSuffixes()
        {
            const string CANONICAL_FILE_PATH = "Worlds/id/world.es3";

            Assert.That(
                AtomicFileTransactionPaths.GetBackupPath(CANONICAL_FILE_PATH),
                Is.EqualTo("Worlds/id/world.es3.bak"));
            Assert.That(
                AtomicFileTransactionPaths.GetTemporaryPath(CANONICAL_FILE_PATH),
                Is.EqualTo("Worlds/id/world.es3.tmp"));
            Assert.That(
                AtomicFileTransactionPaths.GetRecoveryTemporaryPath(CANONICAL_FILE_PATH),
                Is.EqualTo("Worlds/id/world.es3.recovery.tmp"));
        }
    }
}
