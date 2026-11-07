using VContainer;
using VContainer.Unity;

namespace FakeMG.InputContexts
{
    /// <summary>Registers input contexts in the persistent scope, so contexts from every scene share one stack.</summary>
    public static class InputContextsInstaller
    {
        #region Public Methods

        public static void Install(IContainerBuilder builder)
        {
            InstallContexts(builder);
            InstallRouting(builder);
        }

        #endregion

        #region Private Methods

        private static void InstallContexts(IContainerBuilder builder)
        {
            builder.Register<InputContextStack>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputContextSubscriber>();
        }

        private static void InstallRouting(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InputContextRouter>();
        }

        #endregion
    }
}
