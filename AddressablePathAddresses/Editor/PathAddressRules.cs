using System;
using System.Collections.Generic;

namespace FakeMG.AddressablePathAddresses.Editor
{
    /// <summary>Decides which Addressables addresses are asset paths that a move left behind.</summary>
    public static class PathAddressRules
    {
        public const string ASSET_PATH_PREFIX = "Assets/";

        #region Public Methods

        /// <returns>True when the address is a moved path, or a path inside a moved folder.</returns>
        public static bool WasMovedFrom(string address, IReadOnlyList<string> movedFromAssetPaths)
        {
            foreach (string movedFromAssetPath in movedFromAssetPaths)
            {
                if (address == movedFromAssetPath || address.StartsWith(movedFromAssetPath + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <returns>True when the address is written as an asset path but names another one, so it no longer says where the asset is.</returns>
        public static bool IsStalePathAddress(string address, string assetPath)
        {
            return address.StartsWith(ASSET_PATH_PREFIX, StringComparison.Ordinal) && address != assetPath;
        }

        #endregion
    }
}
