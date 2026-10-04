using Unity.Cinemachine;
using UnityEngine;
using VContainer;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Applies camera rig state to Unity and projects screen positions onto the ground plane.
    /// </summary>
    public class CameraRigView : MonoBehaviour
    {
        [SerializeField] private Transform _focusRoot;
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CameraProfileSO _cameraProfileSO;
        [SerializeField] private CameraBoundsProvider _boundsProvider;

        private readonly Plane _groundPlane = new(Vector3.up, Vector3.zero);
        private CameraVisibleAreaCalculator _visibleAreaCalculator;

        public CameraProfileSO CameraProfileSO => _cameraProfileSO;
        public CameraBoundsProvider BoundsProvider => _boundsProvider;
        public float Aspect => _gameplayCamera.aspect;
        public Vector3 FocusPositionMeters => _focusRoot.position;

        #region Unity Lifecycle

        private void Awake()
        {
            ApplyProjectionSettings(_cameraProfileSO.DefaultOrthographicSizeMeters);
        }

        #endregion

        #region Public Methods

        public CameraRigState CreateInitialState()
        {
            return new CameraRigState(
                _focusRoot.position,
                _focusRoot.eulerAngles.y,
                _cameraProfileSO.DefaultOrthographicSizeMeters,
                _cameraProfileSO.DefaultCameraDistanceMeters);
        }

        public void ApplyState(CameraRigState cameraRigState)
        {
            _focusRoot.SetPositionAndRotation(
                cameraRigState.FocusPositionMeters,
                Quaternion.Euler(0f, cameraRigState.YawDegrees, 0f));
            _visibleAreaCalculator.GetCameraPose(
                cameraRigState.FocusPositionMeters,
                cameraRigState.YawDegrees,
                _cameraProfileSO.PitchDegrees,
                _cameraProfileSO.GetCameraDistanceMeters(cameraRigState),
                out Vector3 cameraPositionMeters,
                out Quaternion cameraRotation);

            _cinemachineCamera.transform.SetPositionAndRotation(cameraPositionMeters, cameraRotation);
            ApplyProjectionSettings(cameraRigState.OrthographicSizeMeters);
        }

        public Vector2 GetScreenCenterPixels()
        {
            // Converting through the camera respects viewport rectangles and render-target placement.
            Vector3 screenCenterPixels = _gameplayCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
            return screenCenterPixels;
        }

        public bool TryProjectScreenPointToGround(Vector2 screenPositionPixels, out Vector3 groundPositionMeters)
        {
            Ray ray = _gameplayCamera.ScreenPointToRay(screenPositionPixels);
            return TryProjectRayToGround(ray, out groundPositionMeters);
        }

        public bool TryProjectScreenPointToGround(
            Vector2 screenPositionPixels,
            Vector3 focusPositionMeters,
            float yawDegrees,
            float zoomMeters,
            out Vector3 groundPositionMeters)
        {
            if (_cameraProfileSO.ProjectionType == CameraProjectionType.Perspective)
            {
                return TryProjectPerspectiveScreenPointToGround(
                    screenPositionPixels,
                    focusPositionMeters,
                    yawDegrees,
                    zoomMeters,
                    out groundPositionMeters);
            }

            _visibleAreaCalculator.GetCameraPose(
                focusPositionMeters,
                yawDegrees,
                _cameraProfileSO.PitchDegrees,
                _cameraProfileSO.CameraDistanceMeters,
                out Vector3 cameraPositionMeters,
                out Quaternion cameraRotation);

            Vector2 viewportPosition = _gameplayCamera.ScreenToViewportPoint(screenPositionPixels);
            Vector2 viewportOffset = viewportPosition - Vector2.one * 0.5f;
            Vector3 right = cameraRotation * Vector3.right;
            Vector3 up = cameraRotation * Vector3.up;
            Vector3 forward = cameraRotation * Vector3.forward;
            // Orthographic rays are parallel; their origins move across the near-plane rectangle.
            Vector3 rayOriginMeters = cameraPositionMeters
                                      + right * (viewportOffset.x * zoomMeters * Aspect * 2f)
                                      + up * (viewportOffset.y * zoomMeters * 2f);

            return TryProjectRayToGround(new Ray(rayOriginMeters, forward), out groundPositionMeters);
        }

        #endregion

        #region Private Methods

        [Inject]
        private void Construct(CameraVisibleAreaCalculator visibleAreaCalculator)
        {
            _visibleAreaCalculator = visibleAreaCalculator;
        }

        private void ApplyProjectionSettings(float orthographicSizeMeters)
        {
            LensSettings lensSettings = _cinemachineCamera.Lens;
            lensSettings.NearClipPlane = _cameraProfileSO.NearClipPlaneMeters;
            lensSettings.FarClipPlane = _cameraProfileSO.FarClipPlaneMeters;

            if (_cameraProfileSO.ProjectionType == CameraProjectionType.Orthographic)
            {
                lensSettings.ModeOverride = LensSettings.OverrideModes.Orthographic;
                lensSettings.OrthographicSize = orthographicSizeMeters;
                _gameplayCamera.orthographic = true;
                _gameplayCamera.orthographicSize = orthographicSizeMeters;
            }
            else
            {
                lensSettings.ModeOverride = LensSettings.OverrideModes.Perspective;
                lensSettings.FieldOfView = _cameraProfileSO.FieldOfViewDegrees;
                _gameplayCamera.orthographic = false;
                _gameplayCamera.fieldOfView = _cameraProfileSO.FieldOfViewDegrees;
            }

            _cinemachineCamera.Lens = lensSettings;

            _gameplayCamera.nearClipPlane = _cameraProfileSO.NearClipPlaneMeters;
            _gameplayCamera.farClipPlane = _cameraProfileSO.FarClipPlaneMeters;
        }

        private bool TryProjectPerspectiveScreenPointToGround(
            Vector2 screenPositionPixels,
            Vector3 focusPositionMeters,
            float yawDegrees,
            float cameraDistanceMeters,
            out Vector3 groundPositionMeters)
        {
            _visibleAreaCalculator.GetCameraPose(
                focusPositionMeters,
                yawDegrees,
                _cameraProfileSO.PitchDegrees,
                cameraDistanceMeters,
                out Vector3 cameraPositionMeters,
                out Quaternion cameraRotation);

            Vector2 viewportPosition = _gameplayCamera.ScreenToViewportPoint(screenPositionPixels);
            Ray ray = _visibleAreaCalculator.CreatePerspectiveViewportRay(
                cameraPositionMeters,
                cameraRotation,
                viewportPosition,
                _cameraProfileSO.FieldOfViewDegrees,
                Aspect);

            return TryProjectRayToGround(ray, out groundPositionMeters);
        }

        private bool TryProjectRayToGround(Ray ray, out Vector3 groundPositionMeters)
        {
            if (_groundPlane.Raycast(ray, out float enter))
            {
                groundPositionMeters = ray.GetPoint(enter);
                return true;
            }

            groundPositionMeters = Vector3.zero;
            return false;
        }

        #endregion
    }
}
