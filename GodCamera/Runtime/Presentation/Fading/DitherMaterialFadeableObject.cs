using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Fades renderers by overriding material color alpha through per-renderer property blocks.
    /// </summary>
    public sealed class DitherMaterialFadeableObject : FadeableObject
    {
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        private const float OPAQUE_ALPHA_THRESHOLD = 0.999f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock[][] _originalPropertyBlocksByRenderer;
        private MaterialPropertyBlock[][] _fadePropertyBlocksByRenderer;
        private Color[][] _originalColorsByRenderer;
        private float _currentAlpha01 = 1f;
        private bool _hasInitializedPropertyBlocks;
        private bool _isApplyingFade;

        #region Unity Lifecycle

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            if (_renderers.Length == 0)
            {
                Echo.Warning("Dither material fadeable object has no child renderers.", context: this);
            }
        }

        private void OnDestroy()
        {
            Release();
        }

        #endregion

        #region Public Methods

        public override bool UpdateFade(float targetAlpha01, float fadeDurationSeconds, float deltaTimeSeconds)
        {
            InitializePropertyBlocks();

            // Dividing elapsed time by duration converts this frame into normalized fade progress.
            float maximumAlphaDelta01 = fadeDurationSeconds <= 0f ? 1f : deltaTimeSeconds / fadeDurationSeconds;
            _currentAlpha01 = Mathf.MoveTowards(_currentAlpha01, targetAlpha01, maximumAlphaDelta01);

            if (_currentAlpha01 < OPAQUE_ALPHA_THRESHOLD)
            {
                ApplyAlpha();
                _isApplyingFade = true;
                return true;
            }

            RestoreOriginalPropertyBlocks();
            return false;
        }

        public override void Release()
        {
            if (!_hasInitializedPropertyBlocks)
            {
                return;
            }

            RestoreOriginalPropertyBlocks();

            _originalPropertyBlocksByRenderer = null;
            _fadePropertyBlocksByRenderer = null;
            _originalColorsByRenderer = null;
            _currentAlpha01 = 1f;
            _hasInitializedPropertyBlocks = false;
        }

        #endregion

        #region Private Methods

        private void InitializePropertyBlocks()
        {
            if (_hasInitializedPropertyBlocks)
            {
                return;
            }

            _originalPropertyBlocksByRenderer = new MaterialPropertyBlock[_renderers.Length][];
            _fadePropertyBlocksByRenderer = new MaterialPropertyBlock[_renderers.Length][];
            _originalColorsByRenderer = new Color[_renderers.Length][];

            for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
            {
                InitializeRendererPropertyBlocks(rendererIndex);
            }

            _hasInitializedPropertyBlocks = true;
        }

        private void InitializeRendererPropertyBlocks(int rendererIndex)
        {
            Renderer targetRenderer = _renderers[rendererIndex];
            Material[] materials = targetRenderer.sharedMaterials;

            _originalPropertyBlocksByRenderer[rendererIndex] = new MaterialPropertyBlock[materials.Length];
            _fadePropertyBlocksByRenderer[rendererIndex] = new MaterialPropertyBlock[materials.Length];
            _originalColorsByRenderer[rendererIndex] = new Color[materials.Length];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                MaterialPropertyBlock originalPropertyBlock = new();
                targetRenderer.GetPropertyBlock(originalPropertyBlock, materialIndex);

                MaterialPropertyBlock fadePropertyBlock = new();
                targetRenderer.GetPropertyBlock(fadePropertyBlock, materialIndex);

                _originalPropertyBlocksByRenderer[rendererIndex][materialIndex] = originalPropertyBlock;
                _fadePropertyBlocksByRenderer[rendererIndex][materialIndex] = fadePropertyBlock;
                _originalColorsByRenderer[rendererIndex][materialIndex] =
                    ResolveOriginalColor(materials[materialIndex], originalPropertyBlock);
            }
        }

        private Color ResolveOriginalColor(Material material, MaterialPropertyBlock propertyBlock)
        {
            if (propertyBlock.HasColor(COLOR_ID))
            {
                return propertyBlock.GetColor(COLOR_ID);
            }

            if (material.HasProperty(COLOR_ID))
            {
                return material.GetColor(COLOR_ID);
            }

            Echo.Warning("Dither fade material does not expose a _Color property.", context: this);
            return Color.white;
        }

        private void ApplyAlpha()
        {
            for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
            {
                ApplyRendererAlpha(rendererIndex);
            }
        }

        private void ApplyRendererAlpha(int rendererIndex)
        {
            Renderer targetRenderer = _renderers[rendererIndex];
            if (!targetRenderer)
            {
                return;
            }

            for (int materialIndex = 0;
                 materialIndex < _fadePropertyBlocksByRenderer[rendererIndex].Length;
                 materialIndex++)
            {
                Color fadeColor = _originalColorsByRenderer[rendererIndex][materialIndex];
                fadeColor.a *= _currentAlpha01;

                MaterialPropertyBlock fadePropertyBlock =
                    _fadePropertyBlocksByRenderer[rendererIndex][materialIndex];
                fadePropertyBlock.SetColor(COLOR_ID, fadeColor);
                targetRenderer.SetPropertyBlock(fadePropertyBlock, materialIndex);
            }
        }

        private void RestoreOriginalPropertyBlocks()
        {
            if (!_isApplyingFade)
            {
                return;
            }

            for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
            {
                RestoreRendererPropertyBlocks(rendererIndex);
            }

            _isApplyingFade = false;
        }

        private void RestoreRendererPropertyBlocks(int rendererIndex)
        {
            Renderer targetRenderer = _renderers[rendererIndex];
            if (!targetRenderer)
            {
                return;
            }

            for (int materialIndex = 0;
                 materialIndex < _originalPropertyBlocksByRenderer[rendererIndex].Length;
                 materialIndex++)
            {
                targetRenderer.SetPropertyBlock(
                    _originalPropertyBlocksByRenderer[rendererIndex][materialIndex],
                    materialIndex);
            }
        }

        #endregion
    }
}
