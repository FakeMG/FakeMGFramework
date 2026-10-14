using VContainer;
using VContainer.Unity;

namespace FakeMG.Toast
{
    public static class ToastServiceInstaller
    {
        #region Public Methods

        public static void Register(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<ToastManager>().AsSelf().As<IToastService>();
        }

        #endregion
    }
}
