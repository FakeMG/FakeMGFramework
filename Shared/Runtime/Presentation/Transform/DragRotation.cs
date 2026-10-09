using UnityEngine;
using UnityEngine.EventSystems;

namespace FakeMG.Framework.UI
{
    public sealed class DragRotation : MonoBehaviour, IDragHandler
    {
        [SerializeField] private Transform _modelPivot;
        [SerializeField] private RectTransform _dragArea;
        [SerializeField] private Transform _viewFrame;

        #region Public Methods

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dragArea, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 previousPositionPixels)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dragArea, eventData.position, eventData.pressEventCamera, out Vector2 currentPositionPixels))
            {
                Echo.Warning("Drag rotation skipped because the pointer could not be projected onto the drag area.");
                return;
            }

            Rect area = _dragArea.rect;
            float radiusPixels = Mathf.Min(area.width, area.height) * 0.5f;
            if (radiusPixels <= 0f)
            {
                Echo.Warning("Drag rotation skipped because the drag area has no size.");
                return;
            }

            Quaternion viewRotation = VirtualTrackball.CalculateRotation(
                (previousPositionPixels - area.center) / radiusPixels, (currentPositionPixels - area.center) / radiusPixels);
            Quaternion worldRotation = _viewFrame.rotation * viewRotation * Quaternion.Inverse(_viewFrame.rotation);
            _modelPivot.rotation = worldRotation * _modelPivot.rotation;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(Transform modelPivot, RectTransform dragArea, Transform viewFrame)
        {
            _modelPivot = modelPivot;
            _dragArea = dragArea;
            _viewFrame = viewFrame;
        }
#endif

        #endregion
    }
}
