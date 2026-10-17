using System;
using FakeMG.Framework;
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
            RenderingLayerOutlineController[] controllers = UnityEngine.Object.FindObjectsByType<RenderingLayerOutlineController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
            {
                controllers[controllerIndex].RefreshSelectionRegistration();
            }
        }

        #endregion

        #region Public Methods

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
            uint selectedOutlineMaskValue = _selectedOutlineMask.value;

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
                RenderingLayerOutlineSelectionRegistry.Shared.RegisterSelection(this, _selectedOutlineMask.value);
                return;
            }

            RenderingLayerOutlineSelectionRegistry.Shared.UnregisterSelection(this);
        }

        #endregion
    }
}
