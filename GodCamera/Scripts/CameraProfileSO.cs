using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Defines projection, movement, zoom, rotation, and clipping settings for a camera rig.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.CAMERA + "/Camera Profile SO")]
    public class CameraProfileSO : ScriptableObject
    {
        [TitleGroup("Projection", "Selects the camera projection and controls its shared viewing angle.")]
        [SerializeField]
        [PropertyTooltip("Determines whether zoom changes orthographic size or perspective camera distance.")]
        private CameraProjectionType _projectionType = CameraProjectionType.Orthographic;

        [TitleGroup("Projection")]
        [SerializeField]
        [PropertyRange(1f, 89f)]
        [SuffixLabel("degrees", overlay: true)]
        [PropertyTooltip("Downward angle from the horizontal plane. Higher values look more directly at the ground.")]
        private float _pitchDegrees = 40f;

        [FoldoutGroup("Projection/Orthographic", expanded: true)]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Distance between the camera and focus point in orthographic mode. This changes perspective depth without changing visible size.")]
        private float _cameraDistanceMeters = 12f;

        [FoldoutGroup("Projection/Orthographic")]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Closest orthographic zoom. Orthographic size is half of the visible vertical span.")]
        private float _minimumOrthographicSizeMeters = 3f;

        [FoldoutGroup("Projection/Orthographic")]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Farthest orthographic zoom. Orthographic size is half of the visible vertical span.")]
        private float _maximumOrthographicSizeMeters = 10f;

        [FoldoutGroup("Projection/Orthographic")]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Orthographic size applied when the camera rig initializes.")]
        private float _defaultOrthographicSizeMeters = 6f;

        [FoldoutGroup("Projection/Orthographic")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Per-edge X/Z allowance beyond the camera bounds in orthographic mode.")]
        private Vector2 _orthographicBoundsOffsetMeters;

        [FoldoutGroup("Projection/Perspective", expanded: true)]
        [SerializeField]
        [PropertyRange(1f, 179f)]
        [SuffixLabel("degrees", overlay: true)]
        [PropertyTooltip("Vertical perspective field of view.")]
        private float _fieldOfViewDegrees = 35f;

        [FoldoutGroup("Projection/Perspective")]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Closest perspective zoom, measured from the camera to its focus point.")]
        private float _minimumCameraDistanceMeters = 6f;

        [FoldoutGroup("Projection/Perspective")]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Farthest perspective zoom, measured from the camera to its focus point.")]
        private float _maximumCameraDistanceMeters = 20f;

        [FoldoutGroup("Projection/Perspective")]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Camera-to-focus distance applied when the camera rig initializes.")]
        private float _defaultCameraDistanceMeters = 12f;

        [FoldoutGroup("Projection/Perspective")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Per-edge X/Z allowance beyond the camera bounds in perspective mode.")]
        private Vector2 _perspectiveBoundsOffsetMeters;

        [FoldoutGroup("Projection/Clipping")]
        [SerializeField]
        [MinValue(0.001f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Closest distance rendered by the gameplay and Cinemachine cameras.")]
        private float _nearClipPlaneMeters = 0.3f;

        [FoldoutGroup("Projection/Clipping")]
        [SerializeField]
        [MinValue(0.01f)]
        [SuffixLabel("m", overlay: true)]
        [PropertyTooltip("Farthest distance rendered by the gameplay and Cinemachine cameras.")]
        private float _farClipPlaneMeters = 100f;

        [TitleGroup("Pan", "Controls keyboard, stick, pointer-drag, and gamepad-drag movement.")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("m/s", overlay: true)]
        [PropertyTooltip("Maximum focus-point movement speed for directional and gamepad drag input.")]
        private float _panSpeedMetersPerSecond = 12f;

        [TitleGroup("Pan")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("seconds", overlay: true)]
        [PropertyTooltip("Time used to smooth directional pan velocity.")]
        private float _panSmoothingSeconds = 0.12f;

        [TitleGroup("Pan")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("seconds", overlay: true)]
        [PropertyTooltip("Time used to smooth pointer and gamepad drag movement.")]
        private float _dragPanSmoothingSeconds = 0.04f;

        [TitleGroup("Zoom", "Controls zoom response and the world point retained while zooming.")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("m/s", overlay: true)]
        [PropertyTooltip("Continuous zoom speed used by controller input.")]
        private float _zoomSpeedMetersPerSecond = 8f;

        [TitleGroup("Zoom")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("m/step", overlay: true)]
        [PropertyTooltip("Zoom change produced by one normalized mouse-wheel step.")]
        private float _pointerZoomSizeStepMeters = 1f;

        [TitleGroup("Zoom")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("seconds", overlay: true)]
        [PropertyTooltip("Time used to smooth zoom and its focus-position correction.")]
        private float _zoomSmoothingSeconds = 0.1f;

        [FoldoutGroup("Zoom/Anchoring", expanded: true)]
        [SerializeField]
        [PropertyTooltip("World-space point kept stationary while zooming with the mouse wheel.")]
        private CameraZoomAnchorMode _mouseZoomAnchorMode = CameraZoomAnchorMode.PointerGroundPoint;

        [FoldoutGroup("Zoom/Anchoring")]
        [SerializeField]
        [PropertyTooltip("World-space point kept stationary while zooming with a controller.")]
        private CameraZoomAnchorMode _controllerZoomAnchorMode = CameraZoomAnchorMode.ScreenCenter;

        [TitleGroup("Rotation", "Controls optional stepped rotation around the focus point.")]
        [SerializeField]
        [PropertyTooltip("Determines whether rotation input is ignored or applied in fixed steps.")]
        private CameraRotationMode _rotationMode = CameraRotationMode.Step;

        [TitleGroup("Rotation")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("degrees", overlay: true)]
        [PropertyTooltip("Yaw added for each left or right rotation command.")]
        private float _rotationStepDegrees = 90f;

        [TitleGroup("Rotation")]
        [SerializeField]
        [MinValue(0f)]
        [SuffixLabel("seconds", overlay: true)]
        [PropertyTooltip("Time used to smooth yaw toward the requested rotation step.")]
        private float _rotationSmoothingSeconds = 0.18f;

        public CameraProjectionType ProjectionType => _projectionType;
        public float PitchDegrees => _pitchDegrees;
        public float CameraDistanceMeters => _cameraDistanceMeters;
        public float FieldOfViewDegrees => _fieldOfViewDegrees;
        public float NearClipPlaneMeters => _nearClipPlaneMeters;
        public float FarClipPlaneMeters => _farClipPlaneMeters;
        public float PanSpeedMetersPerSecond => _panSpeedMetersPerSecond;
        public float PanSmoothingSeconds => _panSmoothingSeconds;
        public float DragPanSmoothingSeconds => _dragPanSmoothingSeconds;
        public float MinimumOrthographicSizeMeters => _minimumOrthographicSizeMeters;
        public float MaximumOrthographicSizeMeters => _maximumOrthographicSizeMeters;
        public float DefaultOrthographicSizeMeters => _defaultOrthographicSizeMeters;
        public float MinimumCameraDistanceMeters => _minimumCameraDistanceMeters;
        public float MaximumCameraDistanceMeters => _maximumCameraDistanceMeters;
        public float DefaultCameraDistanceMeters => _defaultCameraDistanceMeters;
        public Vector2 BoundsOffsetMeters => _projectionType == CameraProjectionType.Perspective
            ? _perspectiveBoundsOffsetMeters
            : _orthographicBoundsOffsetMeters;
        public float MinimumZoomMeters => _projectionType == CameraProjectionType.Perspective
            ? _minimumCameraDistanceMeters
            : _minimumOrthographicSizeMeters;
        public float MaximumZoomMeters => _projectionType == CameraProjectionType.Perspective
            ? _maximumCameraDistanceMeters
            : _maximumOrthographicSizeMeters;
        public float DefaultZoomMeters => _projectionType == CameraProjectionType.Perspective
            ? _defaultCameraDistanceMeters
            : _defaultOrthographicSizeMeters;
        public float ZoomSpeedMetersPerSecond => _zoomSpeedMetersPerSecond;
        public float PointerZoomSizeStepMeters => _pointerZoomSizeStepMeters;
        public float ZoomSmoothingSeconds => _zoomSmoothingSeconds;
        public CameraZoomAnchorMode MouseZoomAnchorMode => _mouseZoomAnchorMode;
        public CameraZoomAnchorMode ControllerZoomAnchorMode => _controllerZoomAnchorMode;
        public CameraRotationMode RotationMode => _rotationMode;
        public float RotationStepDegrees => _rotationStepDegrees;
        public float RotationSmoothingSeconds => _rotationSmoothingSeconds;

        #region Public Methods

        public float GetZoomMeters(CameraRigState cameraRigState)
        {
            return _projectionType == CameraProjectionType.Perspective
                ? cameraRigState.CameraDistanceMeters
                : cameraRigState.OrthographicSizeMeters;
        }

        public void SetZoomMeters(ref CameraRigState cameraRigState, float zoomMeters)
        {
            if (_projectionType == CameraProjectionType.Perspective)
            {
                cameraRigState.CameraDistanceMeters = zoomMeters;
                return;
            }

            cameraRigState.OrthographicSizeMeters = zoomMeters;
        }

        public float GetCameraDistanceMeters(CameraRigState cameraRigState)
        {
            return _projectionType == CameraProjectionType.Perspective
                ? cameraRigState.CameraDistanceMeters
                : _cameraDistanceMeters;
        }

        #endregion
    }
}
