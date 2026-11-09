using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Outline
{
    /// <summary>
    /// Enables or disables an outline by changing the rendering layers of child renderers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RenderingLayerSelectionOutline : MonoBehaviour, IOutlineSelectionBounds
    {
        [SerializeField] private RenderingLayerMask _outlineMask;
        [SerializeField] private RenderingLayerMask _selectedOutlineMask;

        [Tooltip("Replaces the selected outline while the selection is hovered. Leave empty to keep the selected outline.")]
        [SerializeField] private RenderingLayerMask _hoveredOutlineMask;

        [Tooltip("Replaces the selected and hovered outlines while the selection is marked unavailable. Leave empty to keep them.")]
        [SerializeField] private RenderingLayerMask _unavailableOutlineMask;

        [SerializeField] private bool _keepOtherRenderingLayers = true;

        private Renderer[] _targetRenderers = Array.Empty<Renderer>();
        private uint[] _originalMasks = Array.Empty<uint>();
        private bool _isSelected;
        private bool _isHovered;
        private bool _isUnavailable;

        private uint ActiveOutlineMask
        {
            get
            {
                if (_isUnavailable && _unavailableOutlineMask.value != 0)
                {
                    return _unavailableOutlineMask.value;
                }

                return _isHovered && _hoveredOutlineMask.value != 0 ? _hoveredOutlineMask.value : _selectedOutlineMask.value;
            }
        }

        #region Unity Lifecycle

        private void Awake()
        {
            _targetRenderers = GetComponentsInChildren<Renderer>(true);
            _originalMasks = new uint[_targetRenderers.Length];

            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                _originalMasks[rendererIndex] = _targetRenderers[rendererIndex].renderingLayerMask;
            }
        }

        private void OnEnable()
        {
            RefreshSelectionRegistration();
        }

        private void OnDisable()
        {
            RestoreAndUnregisterSelection();
        }

        private void OnDestroy()
        {
            RestoreAndUnregisterSelection();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearSelectionsForPlayMode()
        {
            RenderingLayerOutlineSelectionRegistry.Shared.ClearSelections();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RestoreSurvivingSelectionRegistrations()
        {
            // With scene and domain reload disabled, already-enabled controllers retain their selection state.
            // Rebuild once at startup; the rendering feature never scans the scene or its renderers.
            RenderingLayerSelectionOutline[] controllers = UnityEngine.Object.FindObjectsByType<RenderingLayerSelectionOutline>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
            {
                controllers[controllerIndex].RefreshSelectionRegistration();
            }
        }

        #endregion

        #region Public Methods

        public bool TryGetWorldBounds(out Bounds boundsMeters)
        {
            boundsMeters = default;
            bool hasBounds = false;
            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                Renderer targetRenderer = _targetRenderers[rendererIndex];
                if (targetRenderer == null || !targetRenderer.enabled || !targetRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (hasBounds)
                {
                    boundsMeters.Encapsulate(targetRenderer.bounds);
                }
                else
                {
                    boundsMeters = targetRenderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        [Button]
        public void SetSelected(bool isSelected)
        {
            if (_isSelected == isSelected)
            {
                RefreshSelectionRegistration();
                return;
            }

            _isSelected = isSelected;
            ApplyRenderingLayers(isSelected);
            RefreshSelectionRegistration();
        }

        /// <summary>Swaps the selected outline for the hovered one; has no visible effect while not selected.</summary>
        public void SetHovered(bool isHovered)
        {
            if (_isHovered == isHovered)
            {
                return;
            }

            _isHovered = isHovered;
            if (_isSelected)
            {
                ApplyRenderingLayers(true);
                RefreshSelectionRegistration();
            }
        }

        /// <summary>Swaps in the unavailable outline, e.g. for a target that cannot take what is held; no effect while not selected.</summary>
        public void SetUnavailable(bool isUnavailable)
        {
            if (_isUnavailable == isUnavailable)
            {
                return;
            }

            _isUnavailable = isUnavailable;
            if (_isSelected)
            {
                ApplyRenderingLayers(true);
                RefreshSelectionRegistration();
            }
        }

        #endregion

        #region Private Methods

        private void RestoreAndUnregisterSelection()
        {
            RenderingLayerOutlineSelectionRegistry.Shared.UnregisterSelection(this);
            if (!_isSelected)
            {
                return;
            }

            _isSelected = false;
            ApplyRenderingLayers(false);
        }

        private void ApplyRenderingLayers(bool isSelected)
        {
            uint outlineMaskValue = _outlineMask.value;
            uint selectedOutlineMaskValue = ActiveOutlineMask;

            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                Renderer targetRenderer = _targetRenderers[rendererIndex];

                if (targetRenderer == null)
                {
                    continue;
                }

                uint originalMask = _originalMasks[rendererIndex];

                if (isSelected)
                {
                    uint selectedMask = _keepOtherRenderingLayers ? originalMask : 0u;

                    selectedMask &= ~outlineMaskValue;
                    selectedMask |= selectedOutlineMaskValue;

                    targetRenderer.renderingLayerMask = selectedMask;
                }
                else
                {
                    targetRenderer.renderingLayerMask = originalMask;
                }
            }
        }

        private void RefreshSelectionRegistration()
        {
            if (_isSelected && isActiveAndEnabled && _targetRenderers.Length > 0)
            {
                RenderingLayerOutlineSelectionRegistry.Shared.RegisterSelection(this, ActiveOutlineMask);
                return;
            }

            RenderingLayerOutlineSelectionRegistry.Shared.UnregisterSelection(this);
        }

        #endregion
    }
}
