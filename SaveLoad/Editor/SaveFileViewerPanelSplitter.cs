using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    internal enum SaveFileViewerSplitterAxis
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Draggable divider that resizes the panel before it. Drag tracking uses hotControl and screen-space deltas,
    /// so the drag keeps working when the cursor moves over other controls or outside the window.
    /// </summary>
    internal sealed class SaveFileViewerPanelSplitter
    {
        private const float LINE_THICKNESS_PIXELS = 1f;

        private static readonly Color DARK_SKIN_LINE_COLOR = new(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color LIGHT_SKIN_LINE_COLOR = new(0.6f, 0.6f, 0.6f, 1f);

        private readonly SaveFileViewerSplitterAxis _axis;
        private float _dragStartMousePositionPixels;
        private float _dragStartPanelSizePixels;

        public SaveFileViewerPanelSplitter(SaveFileViewerSplitterAxis axis)
        {
            _axis = axis;
        }

        #region Public Methods

        public float DrawAndResize(Rect splitterRect, float panelSizePixels, float minPanelSizePixels, float maxPanelSizePixels)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event currentEvent = Event.current;

            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown when currentEvent.button == 0 && splitterRect.Contains(currentEvent.mousePosition):
                    GUIUtility.hotControl = controlId;
                    _dragStartMousePositionPixels = GetAxisPosition(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition));
                    _dragStartPanelSizePixels = panelSizePixels;
                    currentEvent.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                    float dragDeltaPixels =
                        GetAxisPosition(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition)) - _dragStartMousePositionPixels;
                    panelSizePixels = _dragStartPanelSizePixels + dragDeltaPixels;
                    GUI.changed = true;
                    currentEvent.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    currentEvent.Use();
                    break;

                case EventType.Repaint:
                    DrawLine(splitterRect);
                    break;
            }

            EditorGUIUtility.AddCursorRect(splitterRect, GetCursor());
            return ClampPanelSize(panelSizePixels, minPanelSizePixels, maxPanelSizePixels);
        }

        #endregion

        #region Private Methods

        private float GetAxisPosition(Vector2 screenPosition)
        {
            return _axis == SaveFileViewerSplitterAxis.Horizontal ? screenPosition.x : screenPosition.y;
        }

        private void DrawLine(Rect splitterRect)
        {
            Rect lineRect = _axis == SaveFileViewerSplitterAxis.Horizontal
                ? new Rect(splitterRect.center.x - LINE_THICKNESS_PIXELS * 0.5f, splitterRect.y, LINE_THICKNESS_PIXELS, splitterRect.height)
                : new Rect(splitterRect.x, splitterRect.center.y - LINE_THICKNESS_PIXELS * 0.5f, splitterRect.width, LINE_THICKNESS_PIXELS);

            EditorGUI.DrawRect(lineRect, EditorGUIUtility.isProSkin ? DARK_SKIN_LINE_COLOR : LIGHT_SKIN_LINE_COLOR);
        }

        private MouseCursor GetCursor()
        {
            return _axis == SaveFileViewerSplitterAxis.Horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical;
        }

        // Max wins over min so a window smaller than both limits still leaves room for the other panel.
        private static float ClampPanelSize(float panelSizePixels, float minPanelSizePixels, float maxPanelSizePixels)
        {
            return Mathf.Min(Mathf.Max(panelSizePixels, minPanelSizePixels), maxPanelSizePixels);
        }

        #endregion
    }
}
