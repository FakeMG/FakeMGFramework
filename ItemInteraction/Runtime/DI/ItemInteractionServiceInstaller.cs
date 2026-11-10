using VContainer;
using VContainer.Unity;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Composes generic interaction routing and input delivery.
    /// </summary>
    public static class ItemInteractionServiceInstaller
    {
        #region Public Methods

        public static void Register(IContainerBuilder builder)
        {
            RegisterServices(builder);
            RegisterPresentation(builder);
        }

        public static void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<InteractionControllerResolver>(Lifetime.Singleton);
            builder.Register<InteractionInputState>(Lifetime.Singleton);
            builder.Register<ConsumableEffectSignal>(Lifetime.Singleton).AsSelf().As<IConsumableEffect>();
            builder.Register<ConsumableInteractionController>(Lifetime.Singleton).As<IInteractionController>();
            builder.Register<AddressablesHeldItemPrefabProvider>(Lifetime.Singleton).As<IHeldItemPrefabProvider>();
            builder.RegisterEntryPoint<HeldItemPrefabPool>(Lifetime.Singleton).AsSelf();
        }

        public static void RegisterPresentation(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<HeldItemSelectionView>().As<IItemSelectionPresentation>();
            builder.RegisterComponentInHierarchy<UpperBodyItemAnimationView>()
                .AsSelf()
                .As<IItemSelectionPresentation>()
                .As<IItemUsePresentation>()
                .As<ISelectedItemUseAnimation>()
                .As<ICharacterWaterStateListener>();
            builder.Register<InteractionHudSelectionView>(Lifetime.Singleton).As<IItemSelectionPresentation>();

            builder.RegisterComponentInHierarchy<InteractionHudView>()
                .AsSelf()
                .As<ICharacterWaterStateListener>();
            builder.RegisterComponentInHierarchy<MobileUseInput>().AsSelf().As<IUsableItemInteractionPointerInputSource>();
            builder.RegisterComponentInHierarchy<InteractionInputSubscriber>();
            builder.RegisterComponentInHierarchy<SelectionLifecycleSubscriber>();
        }

        #endregion
    }
}
