using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Represents the complete transform and projection state applied to a camera rig.
    /// </summary>
    public struct CameraRigState
    {
        public Vector3 FocusPositionMeters;
        public float YawDegrees;
        public float OrthographicSizeMeters;
        public float CameraDistanceMeters;

        #region Public Methods

        public CameraRigState(
            Vector3 focusPositionMeters,
            float yawDegrees,
            float orthographicSizeMeters,
            float cameraDistanceMeters)
        {
            FocusPositionMeters = focusPositionMeters;
            YawDegrees = yawDegrees;
            OrthographicSizeMeters = orthographicSizeMeters;
            CameraDistanceMeters = cameraDistanceMeters;
        }

        public float GetZoomMeters(CameraProjectionType projectionType)
        {
            return projectionType == CameraProjectionType.Perspective ? CameraDistanceMeters : OrthographicSizeMeters;
        }

        public void SetZoomMeters(CameraProjectionType projectionType, float zoomMeters)
        {
            if (projectionType == CameraProjectionType.Perspective)
            {
                CameraDistanceMeters = zoomMeters;
            }
            else
            {
                OrthographicSizeMeters = zoomMeters;
            }
        }

        #endregion
    }
}
