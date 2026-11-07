using VContainer;
using VContainer.Unity;

namespace FakeMG.InputHints
{
    /// <summary>
    /// Registers input hints in the persistent scope, after <c>InputContextsInstaller.Install</c>, whose context stack decides
    /// which hints show. Add the presentation the game uses on top, such as <see cref="InstallScreenBar"/>.
    /// </summary>
    public static class InputHintsInstaller
    {
        #region Public Methods

        public static void Install(IContainerBuilder builder, InputGlyphLibrarySO glyphLibrarySO)
        {
            InstallHints(builder);
            InstallGlyphs(builder, glyphLibrarySO);
        }

        /// <remarks>The bar's prefab instance must be in the scope's hierarchy.</remarks>
        public static void InstallScreenBar(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<InputHintBar>();
        }

        #endregion

        #region Private Methods

        private static void InstallHints(IContainerBuilder builder)
        {
            builder.Register<ActiveInputHints>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputHintContextSubscriber>();
        }

        private static void InstallGlyphs(IContainerBuilder builder, InputGlyphLibrarySO glyphLibrarySO)
        {
            builder.RegisterInstance(glyphLibrarySO);
            builder.Register<InputGlyphResolver>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ActiveGlyphSetTracker>().AsSelf();
        }

        #endregion
    }
}
