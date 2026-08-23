using System;
using VContainer;
using VContainer.Unity;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Registers the domain-neutral persistence infrastructure and world lifecycle services.
    /// Games extend the system by registering their own <see cref="ISaveable"/> implementations
    /// and optional integration installers after this installer runs.
    /// </summary>
    public static class SaveLoadInstaller
    {
        #region Public Methods

        public static void Install(
            IContainerBuilder builder,
            ISaveMigrationPlan migrationPlan,
            ISaveDataStoreProfile worldStorageProfile,
            WorldSaveConfiguration worldSaveConfiguration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (migrationPlan == null)
            {
                throw new ArgumentNullException(nameof(migrationPlan));
            }

            if (worldStorageProfile == null)
            {
                throw new ArgumentNullException(nameof(worldStorageProfile));
            }

            if (worldSaveConfiguration == null)
            {
                throw new ArgumentNullException(nameof(worldSaveConfiguration));
            }

            var saveEnvironment = new UnitySaveEnvironment();
            builder.RegisterInstance<ISaveEnvironment>(saveEnvironment);
            builder.RegisterInstance<ISaveDataStoreFactory>(new Es3SaveDataStoreFactory(saveEnvironment));
            builder.RegisterInstance<IAtomicFileTransaction>(new AtomicFileTransaction(saveEnvironment));
            builder.RegisterInstance<ISaveTimeProvider>(new SystemSaveTimeProvider());
            builder.RegisterInstance(migrationPlan);
            builder.RegisterInstance(worldStorageProfile);
            builder.RegisterInstance(worldSaveConfiguration);
            builder.Register<SaveMetadataValidator>(Lifetime.Singleton);
            builder.Register<AsyncSaveParticipantDependencyOrderResolver>(Lifetime.Singleton);
            builder.Register<WorldSaveCatalog>(Lifetime.Singleton);
            builder.Register<WorldSnapshotRetentionPolicy>(Lifetime.Singleton);
            builder.Register<SaveFileService>(Lifetime.Singleton);
            builder.Register<WorldSaveRepository>(Lifetime.Singleton);
            builder.Register<GlobalSaveManager>(Lifetime.Singleton)
                .AsSelf()
                .As<IGlobalSaveManager>()
                .As<IGlobalSaveInitializer>()
                .As<IGlobalDocumentSaveRequester>();
            builder.Register<WorldSaveManager>(Lifetime.Singleton)
                .AsSelf()
                .As<IWorldSaveManager>()
                .As<IWorldAutoSaveRequester>()
                .As<IWorldManualSaveRequester>()
                .As<IWorldSaveQueries>()
                .As<IWorldStartupContext>()
                .As<IWorldLifecycleCommands>()
                .As<IAsyncSaveParticipantRegistry>();
            builder.RegisterComponentInHierarchy<PersistenceStartupSubscriber>();
            builder.RegisterComponentInHierarchy<WorldIntervalAutoSaveSubscriber>();
            builder.RegisterComponentInHierarchy<WorldLifecycleAutoSaveSubscriber>();
        }

        #endregion
    }
}
