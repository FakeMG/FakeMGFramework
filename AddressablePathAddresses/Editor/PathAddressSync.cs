using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FakeMG.AddressablePathAddresses.Editor
{
    /// <summary>Keeps each address that is its asset's path equal to that path when the asset, or a folder above it, moves.</summary>
    /// <remarks>
    /// Addressables leaves addresses alone on a move, so path addresses went stale silently. Custom addresses are never
    /// touched.
    /// </remarks>
    internal sealed class PathAddressSync : AssetPostprocessor
    {
        #region Private Methods

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (movedFromAssetPaths.Length == 0 || !settings)
            {
                return;
            }

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (!group)
                {
                    continue;
                }

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    // The entry's own AssetPath is cached and can still hold the old path while assets are being moved.
                    if (PathAddressRules.WasMovedFrom(entry.address, movedFromAssetPaths))
                    {
                        entry.SetAddress(AssetDatabase.GUIDToAssetPath(entry.guid));
                    }
                }
            }
        }

        #endregion
    }
}
