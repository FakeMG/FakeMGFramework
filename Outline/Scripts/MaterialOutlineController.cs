using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Outline
{
    /// <summary>
    /// Enables or disables an outline by adding or removing an outline material.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MaterialOutlineController : MonoBehaviour
    {
        [SerializeField] private Material _outlineMaterial;

        [SerializeField] private bool _includeInactiveChildren = true;

        private Renderer[] _renderers = Array.Empty<Renderer>();
        private Material[][] _originalMaterials = Array.Empty<Material[]>();
        private bool _isHighlighted;

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnDisable()
        {
            SetHighlighted(false);
        }

        [Button]
        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted) return;

            if (_outlineMaterial == null)
            {
                Debug.LogError($"{nameof(MaterialOutlineController)} on {name} has no outline material.", this);
                return;
            }

            if (_renderers.Length == 0)
                CacheRenderers();

            _isHighlighted = highlighted;

            if (highlighted)
                AddOutlineMaterial();
            else
                RestoreOriginalMaterials();
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(_includeInactiveChildren);
            _originalMaterials = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMaterials[i] = _renderers[i].sharedMaterials;
            }
        }

        private void AddOutlineMaterial()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer targetRenderer = _renderers[i];

                if (targetRenderer == null)
                    continue;

                Material[] currentMaterials = targetRenderer.sharedMaterials;

                // Reuse an existing outline slot so enabling the controller does not add a duplicate sub-mesh.
                if (ReplaceExistingOutlineMaterials(currentMaterials))
                {
                    targetRenderer.sharedMaterials = currentMaterials;
                    continue;
                }

                Material[] original = _originalMaterials[i];
                Material[] outlined = new Material[original.Length + 1];

                Array.Copy(original, outlined, original.Length);
                outlined[^1] = _outlineMaterial;

                targetRenderer.sharedMaterials = outlined;
            }
        }

        private bool ReplaceExistingOutlineMaterials(Material[] materials)
        {
            bool hasExistingOutlineMaterial = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material != null && material.shader == _outlineMaterial.shader)
                {
                    materials[i] = _outlineMaterial;
                    hasExistingOutlineMaterial = true;
                }
            }

            return hasExistingOutlineMaterial;
        }

        private void RestoreOriginalMaterials()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }
    }
}
