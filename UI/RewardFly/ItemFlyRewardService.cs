using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.RewardFly
{
    public sealed class ItemFlyRewardService
    {
        private readonly Canvas _screenSpaceCanvas;
        private readonly Camera _conversionCamera;

        public ItemFlyRewardService(
            Camera conversionCamera,
            Canvas screenSpaceCanvas)
        {
            _conversionCamera = conversionCamera;
            _screenSpaceCanvas = screenSpaceCanvas;
        }

        public RectTransform ScreenSpaceCanvasRectTransform => (RectTransform)_screenSpaceCanvas.transform;
        public Camera MovementCamera => _conversionCamera;

        #region Public Methods

        public UniTask PlayUiTransformToUiAsync(
            RectTransform flyingRectTransform,
            RectTransform targetUIElement,
            float durationSeconds,
            AnimationCurve speedCurve,
            float arcHeightPixels,
            Vector2 arcDirection,
            CancellationToken cancellationToken)
        {
            if (!flyingRectTransform)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly a null UI transform to UI.");
                return UniTask.CompletedTask;
            }

            if (!targetUIElement)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly '{flyingRectTransform.name}' because the target UI element is missing.");
                return UniTask.CompletedTask;
            }

            Vector3 startLocalPositionPixels = flyingRectTransform.localPosition;
            Vector3 targetLocalPositionPixels = ConvertTransformToCanvasLocalPosition(targetUIElement);
            Vector3 localArcDirection = NormalizeDirection(arcDirection);

            return PlayTransformFlightAsync(
                flyingRectTransform,
                position => flyingRectTransform.localPosition = position,
                startLocalPositionPixels,
                targetLocalPositionPixels,
                durationSeconds,
                speedCurve,
                arcHeightPixels,
                localArcDirection,
                cancellationToken);
        }

        public UniTask PlayWorldTransformToWorldAsync(
            Transform flyingTransform,
            Transform worldTarget,
            float durationSeconds,
            AnimationCurve speedCurve,
            float arcHeightMeters,
            Vector3 arcDirection,
            CancellationToken cancellationToken)
        {
            if (!flyingTransform)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly a null world transform to a world object.");
                return UniTask.CompletedTask;
            }

            if (!worldTarget)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly '{flyingTransform.name}' because the world target is missing.");
                return UniTask.CompletedTask;
            }

            return PlayTransformFlightAsync(
                flyingTransform,
                position => flyingTransform.position = position,
                flyingTransform.position,
                worldTarget.position,
                durationSeconds,
                speedCurve,
                arcHeightMeters,
                NormalizeDirection(arcDirection),
                cancellationToken);
        }

        public UniTask PlayWorldTransformToUiAsync(
            Transform flyingTransform,
            RectTransform targetUIElement,
            float durationSeconds,
            float targetDepthFromCameraMeters,
            AnimationCurve speedCurve,
            float arcHeightMeters,
            Vector3 arcDirection,
            CancellationToken cancellationToken)
        {
            if (!flyingTransform)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly a null world transform to UI.");
                return UniTask.CompletedTask;
            }

            if (!targetUIElement)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly '{flyingTransform.name}' because the target UI element is missing.");
                return UniTask.CompletedTask;
            }

            if (!_conversionCamera)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly '{flyingTransform.name}' to UI because no conversion camera is available.");
                return UniTask.CompletedTask;
            }

            Vector3 startWorldPosition = flyingTransform.position;
            Vector3 worldArcDirection = _conversionCamera.transform.TransformDirection(NormalizeDirection(arcDirection));

            return PlayFlightAsync(
                flyingTransform,
                progress01 =>
                {
                    Vector3 targetScreenPosition = targetUIElement.position;
                    Vector3 targetWorldPosition = _conversionCamera.ScreenToWorldPoint(new Vector3(
                        targetScreenPosition.x,
                        targetScreenPosition.y,
                        targetDepthFromCameraMeters));

                    return EvaluateArcPosition(
                        startWorldPosition,
                        targetWorldPosition,
                        progress01,
                        speedCurve,
                        arcHeightMeters,
                        worldArcDirection);
                },
                position => flyingTransform.position = position,
                durationSeconds,
                cancellationToken);
        }

        public UniTask PlayUiTransformToWorldAsync(
            RectTransform flyingRectTransform,
            Transform worldTarget,
            float durationSeconds,
            AnimationCurve speedCurve,
            float arcHeightPixels,
            Vector2 arcDirection,
            CancellationToken cancellationToken)
        {
            if (!flyingRectTransform)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly a null UI transform to a world object.");
                return UniTask.CompletedTask;
            }

            if (!worldTarget)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly '{flyingRectTransform.name}' because the world target is missing.");
                return UniTask.CompletedTask;
            }

            if (!_conversionCamera)
            {
                Debug.LogError($"{nameof(ItemFlyRewardService)} cannot fly '{flyingRectTransform.name}' to a world object because no conversion camera is available.");
                return UniTask.CompletedTask;
            }

            Vector3 startScreenPosition = flyingRectTransform.position;
            Vector3 screenArcDirection = NormalizeDirection(arcDirection);

            return PlayFlightAsync(
                flyingRectTransform,
                progress01 =>
                {
                    Vector3 targetScreenPosition = _conversionCamera.WorldToScreenPoint(worldTarget.position);
                    targetScreenPosition.z = startScreenPosition.z;

                    return EvaluateArcPosition(
                        startScreenPosition,
                        targetScreenPosition,
                        progress01,
                        speedCurve,
                        arcHeightPixels,
                        screenArcDirection);
                },
                position => flyingRectTransform.position = position,
                durationSeconds,
                cancellationToken);
        }

        public Vector3 ConvertTransformToCanvasLocalPosition(Transform sourceTransform)
        {
            Vector3 sourceWorldPosition = GetTransformCenterWorldPosition(sourceTransform);
            return ConvertWorldPositionToCanvasLocalPosition(sourceWorldPosition);
        }

        public Vector3 ConvertWorldPositionToCanvasLocalPosition(Vector3 sourceWorldPosition)
        {
            Camera conversionCamera = GetCanvasConversionCamera();
            Vector2 sourceScreenPoint = RectTransformUtility.WorldToScreenPoint(conversionCamera, sourceWorldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ScreenSpaceCanvasRectTransform,
                sourceScreenPoint,
                conversionCamera,
                out Vector2 sourceLocalPoint);

            return new Vector3(sourceLocalPoint.x, sourceLocalPoint.y, 0f);
        }

        #endregion

        #region Private Methods

        private UniTask PlayTransformFlightAsync(
            Transform flyingTransform,
            Action<Vector3> applyPosition,
            Vector3 startPosition,
            Vector3 targetPosition,
            float durationSeconds,
            AnimationCurve speedCurve,
            float arcHeight,
            Vector3 arcDirection,
            CancellationToken cancellationToken)
        {
            return PlayFlightAsync(
                flyingTransform,
                progress01 => EvaluateArcPosition(
                    startPosition,
                    targetPosition,
                    progress01,
                    speedCurve,
                    arcHeight,
                    arcDirection),
                applyPosition,
                durationSeconds,
                cancellationToken);
        }

        private static UniTask PlayFlightAsync(
            Component flyingComponent,
            Func<float, Vector3> evaluatePosition,
            Action<Vector3> applyPosition,
            float durationSeconds,
            CancellationToken cancellationToken)
        {
            Tween flightTween = DOTween.To(
                    () => 0f,
                    progress01 => applyPosition(evaluatePosition(progress01)),
                    1f,
                    durationSeconds)
                .SetEase(Ease.InQuad)
                .SetLink(flyingComponent.gameObject);

            return flightTween.ToUniTask(cancellationToken: cancellationToken);
        }

        private static Vector3 EvaluateArcPosition(
            Vector3 startPosition,
            Vector3 targetPosition,
            float progress01,
            AnimationCurve speedCurve,
            float arcHeight,
            Vector3 arcDirection)
        {
            AnimationCurve curve = speedCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            float easedProgress01 = curve.Evaluate(progress01);
            Vector3 basePosition = Vector3.Lerp(startPosition, targetPosition, easedProgress01);
            float arcOffset = Mathf.Sin(progress01 * Mathf.PI) * arcHeight;
            return basePosition + arcDirection * arcOffset;
        }

        private static Vector3 NormalizeDirection(Vector2 direction)
        {
            return direction.sqrMagnitude <= Mathf.Epsilon
                ? Vector3.up
                : ((Vector3)direction).normalized;
        }

        private static Vector3 NormalizeDirection(Vector3 direction)
        {
            return direction.sqrMagnitude <= Mathf.Epsilon
                ? Vector3.up
                : direction.normalized;
        }

        private static Vector3 GetTransformCenterWorldPosition(Transform sourceTransform)
        {
            if (sourceTransform is RectTransform rectTransform)
            {
                return rectTransform.TransformPoint(rectTransform.rect.center);
            }

            return sourceTransform.position;
        }

        private Camera GetCanvasConversionCamera()
        {
            if (_screenSpaceCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _screenSpaceCanvas.worldCamera ? _screenSpaceCanvas.worldCamera : _conversionCamera;
        }

        #endregion
    }
}
