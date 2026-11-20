using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace FakeMG.KeyBinding.Tests
{
    /// <summary>Checks the game's own key binding configs, so an authoring gap fails here instead of only warning at runtime.</summary>
    public sealed class KeyBindingConfigAssetTests
    {
        /// <remarks>
        /// A secondary slot exists only where the input actions author an empty binding for it. Without one, the slot shows
        /// but can never be rebound. One test loops over the configs because a TestCaseSource with no configs is reported as
        /// a failure, and a game without key binding has none.
        /// </remarks>
        [Test]
        public void EveryConfigAsset_EveryListedActionHasAllItsSlots()
        {
            var missingSlotReports = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(KeyBindingConfigSO)}"))
            {
                string configAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                var slotIndex = new KeyBindingSlotIndex(AssetDatabase.LoadAssetAtPath<KeyBindingConfigSO>(configAssetPath));
                foreach (string report in slotIndex.MissingSlotReports)
                {
                    missingSlotReports.Add($"{configAssetPath}: {report}");
                }
            }

            Assert.That(missingSlotReports, Is.Empty);
        }
    }
}
