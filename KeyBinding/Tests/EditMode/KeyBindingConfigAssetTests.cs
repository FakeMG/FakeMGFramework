using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace FakeMG.KeyBinding.Tests
{
    /// <summary>Checks the game's own key binding configs, so an authoring gap fails here instead of only warning at runtime.</summary>
    public sealed class KeyBindingConfigAssetTests
    {
        private static IEnumerable<string> ConfigAssetPaths()
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(KeyBindingConfigSO)}"))
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        /// <remarks>
        /// A secondary slot exists only where the input actions author an empty binding for it. Without one, the slot shows
        /// but can never be rebound.
        /// </remarks>
        [TestCaseSource(nameof(ConfigAssetPaths))]
        public void ConfigAsset_EveryListedActionHasAllItsSlots(string configAssetPath)
        {
            var configSO = AssetDatabase.LoadAssetAtPath<KeyBindingConfigSO>(configAssetPath);

            var slotIndex = new KeyBindingSlotIndex(configSO);

            Assert.That(slotIndex.MissingSlotReports, Is.Empty);
        }
    }
}
