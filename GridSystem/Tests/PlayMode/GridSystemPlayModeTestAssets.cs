using NUnit.Framework;
using UnityEngine;

namespace FakeMG.GridSystem.Tests.PlayMode
{
    internal static class GridSystemPlayModeTestAssets
    {
        internal const string CONFIG_RESOURCE_NAME = "GridSystemTestAssetConfig";

        #region Public Methods

        public static GridSystemTestAssetConfigSO LoadConfig()
        {
            GridSystemTestAssetConfigSO testAssetConfig =
                Resources.Load<GridSystemTestAssetConfigSO>(CONFIG_RESOURCE_NAME);
            Assert.IsNotNull(
                testAssetConfig,
                $"Missing Resources/{CONFIG_RESOURCE_NAME} asset.");
            return testAssetConfig;
        }

        #endregion
    }
}
