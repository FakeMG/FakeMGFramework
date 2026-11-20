using System.Collections.Generic;
using FakeMG.AddressablePathAddresses.Editor;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FakeMG.AddressablePathAddresses.Tests
{
    /// <summary>Checks the game's own Addressables settings, so an address left behind by a move fails here.</summary>
    public sealed class ProjectPathAddressTests
    {
        [Test]
        public void EveryPathAddress_NamesItsAssetsCurrentPath()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            var staleAddresses = new List<string>();
            foreach (AddressableAssetGroup group in settings.groups)
            {
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (PathAddressRules.IsStalePathAddress(entry.address, entry.AssetPath))
                    {
                        staleAddresses.Add($"{group.Name}: '{entry.address}' is at '{entry.AssetPath}'");
                    }
                }
            }

            Assert.That(staleAddresses, Is.Empty);
        }
    }
}
