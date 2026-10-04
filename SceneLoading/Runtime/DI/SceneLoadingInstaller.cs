using VContainer;
using VContainer.Unity;

namespace FakeMG.SceneLoading
{
    public static class SceneLoadingInstaller
    {
        #region Public Methods

        public static void InstallGameplay(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<SceneDataApplicationScope>();
        }

        public static void InstallPersistent(IContainerBuilder builder, float sceneDataApplicationTimeoutSeconds)
        {
            builder.RegisterInstance(new SceneDataApplicationConfiguration(sceneDataApplicationTimeoutSeconds));
            builder.Register<SceneDataApplicationExecutionPlanBuilder>(Lifetime.Singleton)
                .As<ISceneDataApplicationExecutionPlanBuilder>();
            builder.Register<SceneDataApplicationCoordinator>(Lifetime.Singleton)
                .As<ISceneDataApplicationCoordinator>()
                .As<ISceneDataApplicationSceneLifecycle>();
            builder.RegisterEntryPoint<SceneDataApplicationSceneUnloadSubscriber>();
            builder.Register<AddressableSceneGateway>(Lifetime.Singleton).As<ISceneGateway>();
            builder.RegisterComponentInHierarchy<SceneLoader>().AsSelf().As<ISceneLoader>();
            builder.RegisterComponentInHierarchy<SceneLoadTrigger>();
        }

        #endregion
    }
}
