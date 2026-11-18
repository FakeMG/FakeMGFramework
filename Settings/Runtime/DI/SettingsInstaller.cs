using FakeMG.Settings.Display;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Settings
{
    public static class SettingsInstaller
    {
        #region Public Methods

        public static void InstallDisplay(IContainerBuilder builder, DisplaySettingsConfigSO configSO)
        {
            builder.RegisterInstance(configSO);
            builder.RegisterEntryPoint<DisplaySettingsApplier>();
        }

        #endregion
    }
}
