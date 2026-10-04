using VContainer;
using VContainer.Unity;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Registers the tutorial system across two scopes. Global registers the
    /// cross-session progress store and its saveable bridge; Gameplay registers the
    /// scene-bound runtime (target registry, gate, loader, runner, service, debug,
    /// raycaster filter, visual root). The camera controller is optional and is registered
    /// by the game when it has one; the gameplay scope resolves the progress store from its
    /// parent global scope.
    /// </summary>
    public static class TutorialDependencyInstaller
    {
        #region Public Methods

        public static void RegisterGlobal(IContainerBuilder builder)
        {
            builder.Register<TutorialProgressStore>(Lifetime.Singleton);
            builder.Register<TutorialInteractionGate>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<TutorialProgressSaveable>();
            builder.RegisterComponentInHierarchy<TutorialRaycasterFilter>();
        }

        public static void RegisterGameplay(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<TutorialTargetRegistry>().AsSelf();
            builder.Register<TutorialAddressableLoader>(Lifetime.Singleton);
            builder.Register<TutorialForceCompleteSignal>(Lifetime.Singleton);
            builder.Register<TutorialSelectableVisibilityController>(Lifetime.Singleton);
            builder.Register<TutorialRunner>(Lifetime.Singleton);
            builder.Register<TutorialService>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<TutorialDebugController>();
            builder.RegisterComponentInHierarchy<TutorialRaycasterFilter>();
            builder.RegisterComponentInHierarchy<TutorialVisualRoot>();
            builder.RegisterComponentInHierarchy<TutorialHiddenUIRoot>();
        }

        #endregion
    }
}
