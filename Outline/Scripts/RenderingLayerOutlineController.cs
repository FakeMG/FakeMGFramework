using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Outline
{
    /// <summary>
    /// Enables or disables an outline by changing the rendering layers of child renderers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RenderingLayerOutlineController : MonoBehaviour
    {
        [SerializeField] private RenderingLayerMask _outlineMask;
        [SerializeField] private RenderingLayerMask _selectedOutlineMask;

        [SerializeField] private bool _keepOtherRenderingLayers = true;

        private Renderer[] _targetRenderers = Array.Empty<Renderer>();
        private uint[] _originalMasks = Array.Empty<uint>();
        private bool _isSelected;

        private void Awake()
        {
            _targetRenderers = GetComponentsInChildren<Renderer>(true);
            _originalMasks = new uint[_targetRenderers.Length];

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                _originalMasks[i] = _targetRenderers[i].renderingLayerMask;
            }
        }

        private void OnDisable()
        {
            Restore();
        }

        private void OnDestroy()
        {
            Restore();
        }

        [Button]
        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;

            uint outlineMaskValue = _outlineMask.value;
            uint selectedOutlineMaskValue = _selectedOutlineMask.value;

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                Renderer targetRenderer = _targetRenderers[i];

                if (targetRenderer == null) continue;

                uint original = _originalMasks[i];

                if (selected)
                {
                    uint result = _keepOtherRenderingLayers ? original : 0u;

                    result &= ~outlineMaskValue;
                    result |= selectedOutlineMaskValue;

                    targetRenderer.renderingLayerMask = result;
                }
                else
                {
                    targetRenderer.renderingLayerMask = original;
                }
            }
        }

        private void Restore()
        {
            if (!_isSelected) return;

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                if (_targetRenderers[i] != null)
                {
                    _targetRenderers[i].renderingLayerMask = _originalMasks[i];
                }
            }

            _isSelected = false;
        }
    }
}
