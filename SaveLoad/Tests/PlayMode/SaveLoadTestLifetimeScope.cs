using FakeMG.Settings.SaveLoad;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.SaveLoad.Tests.PlayMode
{
    public sealed class SaveLoadTestLifetimeScope : LifetimeScope
    {
        [Header("Persistence Dependencies")]
        [Tooltip("Migration registry belonging to this Editor-only fixture.")]
        [SerializeField] private MigrationRegistrySO _migrationRegistrySO;
        [Tooltip("Startup policy belonging to this Editor-only fixture.")]
        [SerializeField] private ResumeOrCreateWorldStartupPolicySO _worldStartupPolicySO;

        #region Protected Methods

        protected override void Configure(IContainerBuilder builder)
        {
            InstallPersistence(builder);
        }

        #endregion

        #region Private Methods

        private void InstallPersistence(IContainerBuilder builder)
        {
            var worldStorageProfile = new SaveFileProtectionSettings(false, true, string.Empty);
            var configuration = new WorldSaveConfiguration(
                WorldSaveConfiguration.DEFAULT_MAXIMUM_AUTO_SAVE_COUNT,
                WorldSaveConfiguration.DEFAULT_AUTO_SAVE_INTERVAL_SECONDS,
                WorldSaveConfiguration.DEFAULT_FLUSH_TIMEOUT_SECONDS,
                true,
                "Test World",
                _worldStartupPolicySO);
            SaveLoadInstaller.Install(builder, _migrationRegistrySO, worldStorageProfile, configuration);
            SettingsSaveLoadInstaller.Install(builder, SaveFileProtectionSettings.Plain);
        }

        #endregion
    }
}
