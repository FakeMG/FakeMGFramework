using System;
using FakeMG.SaveLoad;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Settings.SaveLoad
{
    /// <summary>
    /// Composes settings persistence without coupling the Settings or SaveLoad assemblies to each other.
    /// </summary>
    public static class SettingsSaveLoadInstaller
    {
        #region Public Methods

        public static void Install(IContainerBuilder builder, ISaveDataStoreProfile storageProfile)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (storageProfile == null)
            {
                throw new ArgumentNullException(nameof(storageProfile));
            }

            builder.Register<SettingsStateRepository>(Lifetime.Singleton);
            builder.Register(
                    resolver => new SettingsGlobalSaveDocument(resolver.Resolve<SettingsStateRepository>(), storageProfile),
                    Lifetime.Singleton).As<IGlobalSaveDocument>();
            builder.Register<SettingsPersistenceRequester>(Lifetime.Singleton).As<ISettingsPersistenceRequester>();
            builder.RegisterComponentInHierarchy<SettingsSaveSubscriber>();
        }

        #endregion
    }
}
