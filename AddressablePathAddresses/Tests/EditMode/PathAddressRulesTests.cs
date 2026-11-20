using FakeMG.AddressablePathAddresses.Editor;
using NUnit.Framework;

namespace FakeMG.AddressablePathAddresses.Tests
{
    public sealed class PathAddressRulesTests
    {
        private static readonly string[] MOVED_FROM_PATHS = { "Assets/Old/Icon.png", "Assets/OldFolder" };

        [Test]
        public void WasMovedFrom_TheMovedAssetsOwnPath_IsTrue()
        {
            Assert.That(PathAddressRules.WasMovedFrom("Assets/Old/Icon.png", MOVED_FROM_PATHS), Is.True);
        }

        [Test]
        public void WasMovedFrom_APathInsideAMovedFolder_IsTrue()
        {
            Assert.That(PathAddressRules.WasMovedFrom("Assets/OldFolder/Deep/Icon.png", MOVED_FROM_PATHS), Is.True);
        }

        [Test]
        public void WasMovedFrom_ASiblingSharingTheFolderNamePrefix_IsFalse()
        {
            Assert.That(PathAddressRules.WasMovedFrom("Assets/OldFolderTwo/Icon.png", MOVED_FROM_PATHS), Is.False);
        }

        [Test]
        public void WasMovedFrom_ACustomAddress_IsFalse()
        {
            Assert.That(PathAddressRules.WasMovedFrom("Icon", MOVED_FROM_PATHS), Is.False);
        }

        [Test]
        public void WasMovedFrom_NothingMoved_IsFalse()
        {
            Assert.That(PathAddressRules.WasMovedFrom("Assets/Old/Icon.png", new string[0]), Is.False);
        }

        [Test]
        public void IsStalePathAddress_PathAddressOfAnotherPath_IsTrue()
        {
            Assert.That(PathAddressRules.IsStalePathAddress("Assets/Old/Icon.png", "Assets/New/Icon.png"), Is.True);
        }

        [Test]
        public void IsStalePathAddress_PathAddressOfItsOwnPath_IsFalse()
        {
            Assert.That(PathAddressRules.IsStalePathAddress("Assets/New/Icon.png", "Assets/New/Icon.png"), Is.False);
        }

        [Test]
        public void IsStalePathAddress_CustomAddress_IsFalse()
        {
            Assert.That(PathAddressRules.IsStalePathAddress("Icon", "Assets/New/Icon.png"), Is.False);
        }
    }
}
