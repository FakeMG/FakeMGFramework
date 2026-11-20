using VContainer;
using VContainer.Unity;

namespace FakeMG.KeyBinding
{
    public static class KeyBindingInstaller
    {
        #region Public Methods

        public static void InstallPersistent(IContainerBuilder builder, KeyBindingConfigSO configSO)
        {
            builder.RegisterInstance(configSO);
            builder.Register<KeyBindingService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<KeyBindingOverridesSubscriber>();
        }

        #endregion
    }
}
