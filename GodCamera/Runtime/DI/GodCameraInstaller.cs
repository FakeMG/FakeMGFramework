using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.GodCamera
{
    public static class GodCameraInstaller
    {
        #region Public Methods

        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<Camera>();
            builder.RegisterComponentInHierarchy<CameraInputSubscriber>();
            builder.RegisterComponentInHierarchy<CameraRigView>();
            builder.Register<CameraVisibleAreaCalculator>(Lifetime.Scoped);
            builder.Register<CameraPanCalculator>(Lifetime.Scoped);
            builder.Register<CameraZoomCalculator>(Lifetime.Scoped);
            builder.Register<CameraRotationStepper>(Lifetime.Scoped);
            builder.Register<CameraBoundsClamp>(Lifetime.Scoped);
            builder.Register<CameraPanMotion>(Lifetime.Scoped);
            builder.Register<CameraZoomMotion>(Lifetime.Scoped);
            builder.Register<CameraRotationMotion>(Lifetime.Scoped);
            builder.Register<CameraMotionBounds>(Lifetime.Scoped);
            builder.Register<GodCameraPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        }

        #endregion
    }
}

