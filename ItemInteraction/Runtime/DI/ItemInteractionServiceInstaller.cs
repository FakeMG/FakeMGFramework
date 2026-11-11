using VContainer;
using VContainer.Unity;

namespace FakeMG.ItemInteraction
{
    public static class ItemInteractionServiceInstaller
    {
        #region Public Methods

        public static void RegisterShared(IContainerBuilder builder)
        {
            builder.Register<AddressablesHeldItemPrefabProvider>(Lifetime.Singleton).As<IHeldItemPrefabProvider>();
            builder.RegisterEntryPoint<HeldItemPrefabPool>(Lifetime.Singleton).AsSelf();
        }

        /// <summary>
        /// Register controllers, item sources, and presentations in the character's scope.
        /// Use a common parent for shared services and sibling scopes for independently controlled characters.
        /// </summary>
        public static void RegisterCharacter(IContainerBuilder builder)
        {
            builder.Register<InteractionControllerResolver>(Lifetime.Scoped);
            builder.Register<InteractionInputState>(Lifetime.Scoped);
        }

        public static void RegisterConsumables(IContainerBuilder builder)
        {
            builder.Register<ConsumableEffectSignal>(Lifetime.Scoped).AsSelf().As<IConsumableEffect>();
            builder.Register<ConsumableInteractionController>(Lifetime.Scoped).As<IInteractionController>();
        }

        #endregion
    }
}
