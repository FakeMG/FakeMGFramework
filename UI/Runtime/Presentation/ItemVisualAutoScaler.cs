using UnityEngine;

namespace FakeMG.UI
{
    /// <summary>
    /// Automatically scales item visuals to fit their parent slot.
    /// </summary>
    [ExecuteAlways]
    public sealed class ItemVisualAutoScaler : MonoBehaviour
    {
        [SerializeField] private RectTransform itemVisualRoot;
        [SerializeField] private Vector2 baseVisualSize = new(100f, 100f);
        [SerializeField] private bool preserveAspectRatio = true;

        private RectTransform _slotRoot;

        private void Awake()
        {
            _slotRoot = (RectTransform)transform;
            ApplyScale();
        }

        private void OnEnable()
        {
            _slotRoot = (RectTransform)transform;
            ApplyScale();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyScale();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            _slotRoot = (RectTransform)transform;
            ApplyScale();
        }
#endif

        private void ApplyScale()
        {
            if (_slotRoot == null || itemVisualRoot == null)
                return;

            Vector2 slotSize = _slotRoot.rect.size;

            if (slotSize.x <= 0f || slotSize.y <= 0f)
                return;

            float scaleX = slotSize.x / baseVisualSize.x;
            float scaleY = slotSize.y / baseVisualSize.y;

            if (preserveAspectRatio)
            {
                float scale = Mathf.Min(scaleX, scaleY);
                itemVisualRoot.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                itemVisualRoot.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }
    }
}