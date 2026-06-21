using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Projects camera frustum corners onto the ground plane to calculate the visible world area.
    /// </summary>
    public class CameraVisibleAreaCalculator
    {
        private readonly Plane _groundPlane = new(Vector3.up, Vector3.zero);

        #region Public Methods

        public bool TryCalculateOrthographicGroundBoundsMeters(
            Vector3 focusPositionMeters,
            float yawDegrees,
            float pitchDegrees,
            float cameraDistanceMeters,
            float orthographicSizeMeters,
            float aspect,
            out Bounds groundBoundsMeters)
        {
            GetCameraPose(
                focusPositionMeters,
                yawDegrees,
                pitchDegrees,
                cameraDistanceMeters,
                out Vector3 cameraPositionMeters,
                out Quaternion cameraRotation);

            Vector3 right = cameraRotation * Vector3.right;
            Vector3 up = cameraRotation * Vector3.up;
            Vector3 forward = cameraRotation * Vector3.forward;
            float halfHeightMeters = orthographicSizeMeters;
            float halfWidthMeters = orthographicSizeMeters * aspect;

            // Orthographic size is half the vertical span; aspect scales that span horizontally.
            if (!TryProjectOrthographicCorner(
                    cameraPositionMeters,
                    right,
                    up,
                    forward,
                    -halfWidthMeters,
                    -halfHeightMeters,
                    out Vector3 firstGroundPointMeters))
            {
                groundBoundsMeters = default;
                return false;
            }

            groundBoundsMeters = new Bounds(firstGroundPointMeters, Vector3.zero);
            Vector2[] remainingCornerOffsetsMeters =
            {
                new(-halfWidthMeters, halfHeightMeters),
                new(halfWidthMeters, halfHeightMeters),
                new(halfWidthMeters, -halfHeightMeters)
            };

            for (int i = 0; i < remainingCornerOffsetsMeters.Length; i++)
            {
                if (!TryProjectOrthographicCorner(
                        cameraPositionMeters,
                        right,
                        up,
                        forward,
                        remainingCornerOffsetsMeters[i].x,
                        remainingCornerOffsetsMeters[i].y,
                        out Vector3 groundPointMeters))
                {
                    groundBoundsMeters = default;
                    return false;
                }

                groundBoundsMeters.Encapsulate(groundPointMeters);
            }

            return true;
        }

        public bool TryCalculatePerspectiveGroundBoundsMeters(
            Vector3 focusPositionMeters,
            float yawDegrees,
            float pitchDegrees,
            float cameraDistanceMeters,
            float fieldOfViewDegrees,
            float aspect,
            out Bounds groundBoundsMeters)
        {
            GetCameraPose(
                focusPositionMeters,
                yawDegrees,
                pitchDegrees,
                cameraDistanceMeters,
                out Vector3 cameraPositionMeters,
                out Quaternion cameraRotation);

            Vector2[] viewportCorners =
            {
                Vector2.zero,
                Vector2.up,
                Vector2.one,
                Vector2.right
            };

            // Perspective frustum edges diverge, so each viewport corner needs its own ground-intersection ray.
            Ray firstRay = CreatePerspectiveViewportRay(
                cameraPositionMeters,
                cameraRotation,
                viewportCorners[0],
                fieldOfViewDegrees,
                aspect);
            if (!TryProjectRayToGround(firstRay, out Vector3 firstGroundPointMeters))
            {
                groundBoundsMeters = default;
                return false;
            }

            groundBoundsMeters = new Bounds(firstGroundPointMeters, Vector3.zero);
            for (int i = 1; i < viewportCorners.Length; i++)
            {
                Ray ray = CreatePerspectiveViewportRay(
                    cameraPositionMeters,
                    cameraRotation,
                    viewportCorners[i],
                    fieldOfViewDegrees,
                    aspect);
                if (!TryProjectRayToGround(ray, out Vector3 groundPointMeters))
                {
                    groundBoundsMeters = default;
                    return false;
                }

                groundBoundsMeters.Encapsulate(groundPointMeters);
            }

            return true;
        }

        public bool TryCalculateGroundBoundsMeters(
            Vector3 focusPositionMeters,
            float yawDegrees,
            CameraProfileSO profile,
            float zoomMeters,
            float aspect,
            out Bounds groundBoundsMeters)
        {
            if (profile.ProjectionType == CameraProjectionType.Perspective)
            {
                return TryCalculatePerspectiveGroundBoundsMeters(
                    focusPositionMeters,
                    yawDegrees,
                    profile.PitchDegrees,
                    zoomMeters,
                    profile.FieldOfViewDegrees,
                    aspect,
                    out groundBoundsMeters);
            }

            return TryCalculateOrthographicGroundBoundsMeters(
                focusPositionMeters,
                yawDegrees,
                profile.PitchDegrees,
                profile.CameraDistanceMeters,
                zoomMeters,
                aspect,
                out groundBoundsMeters);
        }

        public void GetCameraPose(
            Vector3 focusPositionMeters,
            float yawDegrees,
            float pitchDegrees,
            float cameraDistanceMeters,
            out Vector3 cameraPositionMeters,
            out Quaternion cameraRotation)
        {
            cameraRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            // The focus lies cameraDistance units forward from the camera, so the camera is placed backward.
            cameraPositionMeters = focusPositionMeters - cameraRotation * Vector3.forward * cameraDistanceMeters;
        }

        public Ray CreatePerspectiveViewportRay(
            Vector3 cameraPositionMeters,
            Quaternion cameraRotation,
            Vector2 viewportPosition,
            float fieldOfViewDegrees,
            float aspect)
        {
            // At unit depth, tan(FOV/2) is the frustum's vertical half-extent; aspect produces its horizontal extent.
            float halfFovRadians = fieldOfViewDegrees * Mathf.Deg2Rad * 0.5f;
            float halfHeightAtUnitDistance = Mathf.Tan(halfFovRadians);
            // Viewport coordinates are 0..1, while ray offsets are centered around -0.5..0.5.
            Vector2 viewportOffset = viewportPosition - Vector2.one * 0.5f;
            Vector3 direction = new(
                viewportOffset.x * aspect * halfHeightAtUnitDistance * 2f,
                viewportOffset.y * halfHeightAtUnitDistance * 2f,
                1f);

            return new Ray(cameraPositionMeters, cameraRotation * direction.normalized);
        }

        #endregion

        #region Private Methods

        private bool TryProjectOrthographicCorner(
            Vector3 cameraPositionMeters,
            Vector3 right,
            Vector3 up,
            Vector3 forward,
            float horizontalOffsetMeters,
            float verticalOffsetMeters,
            out Vector3 groundPointMeters)
        {
            Vector3 originMeters = cameraPositionMeters + right * horizontalOffsetMeters + up * verticalOffsetMeters;
            return TryProjectRayToGround(new Ray(originMeters, forward), out groundPointMeters);
        }

        private bool TryProjectRayToGround(Ray ray, out Vector3 groundPointMeters)
        {
            if (_groundPlane.Raycast(ray, out float enter))
            {
                groundPointMeters = ray.GetPoint(enter);
                return true;
            }

            groundPointMeters = default;
            return false;
        }

        #endregion
    }
}
