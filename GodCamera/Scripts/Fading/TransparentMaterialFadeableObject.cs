using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Fades renderers with temporary transparent material instances and restores their originals afterward.
    /// </summary>
    public sealed class TransparentMaterialFadeableObject : FadeableObject
    {
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private static readonly int SURFACE_ID = Shader.PropertyToID("_Surface");
        private static readonly int BLEND_ID = Shader.PropertyToID("_Blend");
        private static readonly int SRC_BLEND_ID = Shader.PropertyToID("_SrcBlend");
        private static readonly int DST_BLEND_ID = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWRITE_ID = Shader.PropertyToID("_ZWrite");
        private static readonly int ALPHA_CLIP_ID = Shader.PropertyToID("_AlphaClip");
        private static readonly int CUTOFF_ID = Shader.PropertyToID("_Cutoff");

        private const float OPAQUE_ALPHA_THRESHOLD = 0.999f;

        private Renderer[] _renderers;
        private Material[][] _originalMaterialsByRenderer;
        private Material[][] _fadeMaterialsByRenderer;
        private Color[][] _originalColorsByRenderer;
        private float _currentAlpha01 = 1f;
        private bool _hasInitializedFadeMaterials;
        private bool _isUsingFadeMaterial;

        #region Unity Lifecycle

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            if (_renderers.Length == 0)
            {
                Echo.Warning("Transparent material fadeable object has no child renderers.", context: this);
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
            InitializeFadeMaterials();

            // Dividing elapsed time by duration converts this frame into normalized fade progress.
            float maximumAlphaDelta01 = fadeDurationSeconds <= 0f ? 1f : deltaTimeSeconds / fadeDurationSeconds;
            _currentAlpha01 = Mathf.MoveTowards(_currentAlpha01, targetAlpha01, maximumAlphaDelta01);

            if (_currentAlpha01 < OPAQUE_ALPHA_THRESHOLD)
            {
                ApplyFadeMaterials();
                ApplyAlpha();
                return true;
            }

            RestoreOriginalMaterials();
            return false;
        }

        public override void Release()
        {
            if (!_hasInitializedFadeMaterials)
            {
                return;
            }

            RestoreOriginalMaterials();
            DestroyFadeMaterials();

            _originalMaterialsByRenderer = null;
            _fadeMaterialsByRenderer = null;
            _originalColorsByRenderer = null;
            _currentAlpha01 = 1f;
            _hasInitializedFadeMaterials = false;
        }

        #endregion

        #region Private Methods

        private void InitializeFadeMaterials()
        {
            if (_hasInitializedFadeMaterials)
            {
                return;
            }

            _originalMaterialsByRenderer = new Material[_renderers.Length][];
            _fadeMaterialsByRenderer = new Material[_renderers.Length][];
            _originalColorsByRenderer = new Color[_renderers.Length][];

            for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
            {
                _originalMaterialsByRenderer[rendererIndex] = _renderers[rendererIndex].sharedMaterials;
                _fadeMaterialsByRenderer[rendererIndex] =
                    CreateFadeMaterials(_originalMaterialsByRenderer[rendererIndex]);
                _originalColorsByRenderer[rendererIndex] =
                    CacheOriginalColors(_originalMaterialsByRenderer[rendererIndex]);
            }

            _hasInitializedFadeMaterials = true;
        }

        private Material[] CreateFadeMaterials(Material[] originalMaterials)
        {
            Material[] fadeMaterials = new Material[originalMaterials.Length];

            for (int materialIndex = 0; materialIndex < originalMaterials.Length; materialIndex++)
            {
                fadeMaterials[materialIndex] = new Material(originalMaterials[materialIndex])
                {
                    name = $"{originalMaterials[materialIndex].name} Camera Fade"
                };

                ConfigureTransparentMaterial(fadeMaterials[materialIndex]);
            }

            return fadeMaterials;
        }

        private Color[] CacheOriginalColors(Material[] originalMaterials)
        {
            Color[] originalColors = new Color[originalMaterials.Length];

            for (int materialIndex = 0; materialIndex < originalMaterials.Length; materialIndex++)
            {
                originalColors[materialIndex] = GetMaterialColor(originalMaterials[materialIndex]);
            }

            return originalColors;
        }

        private void ConfigureTransparentMaterial(Material material)
        {
            if (material.HasProperty(SURFACE_ID))
            {
                material.SetFloat(SURFACE_ID, 1f);
            }

            if (material.HasProperty(BLEND_ID))
            {
                material.SetFloat(BLEND_ID, 0f);
            }

            if (material.HasProperty(SRC_BLEND_ID))
            {
                material.SetFloat(SRC_BLEND_ID, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty(DST_BLEND_ID))
            {
                material.SetFloat(DST_BLEND_ID, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(ZWRITE_ID))
            {
                material.SetFloat(ZWRITE_ID, 0f);
            }

            if (material.HasProperty(ALPHA_CLIP_ID))
            {
                material.SetFloat(ALPHA_CLIP_ID, 1f);
            }

            if (material.HasProperty(CUTOFF_ID))
            {
                material.SetFloat(CUTOFF_ID, 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHATEST_ON");
        }

        private void ApplyFadeMaterials()
        {
            if (_isUsingFadeMaterial)
            {
                return;
            }

            for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
            {
                if (_renderers[rendererIndex])
                {
                    _renderers[rendererIndex].sharedMaterials = _fadeMaterialsByRenderer[rendererIndex];
                }
            }

            _isUsingFadeMaterial = true;
        }

        private void ApplyAlpha()
        {
            for (int rendererIndex = 0; rendererIndex < _fadeMaterialsByRenderer.Length; rendererIndex++)
            {
                for (int materialIndex = 0;
                     materialIndex < _fadeMaterialsByRenderer[rendererIndex].Length;
                     materialIndex++)
                {
                    Material fadeMaterial = _fadeMaterialsByRenderer[rendererIndex][materialIndex];
                    Color fadeColor = _originalColorsByRenderer[rendererIndex][materialIndex];
                    fadeColor.a *= _currentAlpha01;
                    SetMaterialColor(fadeMaterial, fadeColor);

                    if (fadeMaterial.HasProperty(CUTOFF_ID))
                    {
                        // Raising cutoff as alpha falls removes progressively more dithered pixels.
                        fadeMaterial.SetFloat(CUTOFF_ID, 1f - _currentAlpha01);
                    }
                }
            }
        }

        private void RestoreOriginalMaterials()
        {
            if (!_isUsingFadeMaterial)
            {
                return;
            }

            for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
            {
                if (_renderers[rendererIndex])
                {
                    _renderers[rendererIndex].sharedMaterials = _originalMaterialsByRenderer[rendererIndex];
                }
            }

            _isUsingFadeMaterial = false;
        }

        private void DestroyFadeMaterials()
        {
            for (int rendererIndex = 0; rendererIndex < _fadeMaterialsByRenderer.Length; rendererIndex++)
            {
                for (int materialIndex = 0;
                     materialIndex < _fadeMaterialsByRenderer[rendererIndex].Length;
                     materialIndex++)
                {
                    Destroy(_fadeMaterialsByRenderer[rendererIndex][materialIndex]);
                }
            }
        }

        private Color GetMaterialColor(Material material)
        {
            if (material.HasProperty(BASE_COLOR_ID))
            {
                return material.GetColor(BASE_COLOR_ID);
            }

            if (material.HasProperty(COLOR_ID))
            {
                return material.GetColor(COLOR_ID);
            }

            return Color.white;
        }

        private void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty(BASE_COLOR_ID))
            {
                material.SetColor(BASE_COLOR_ID, color);
            }

            if (material.HasProperty(COLOR_ID))
            {
                material.SetColor(COLOR_ID, color);
            }
        }

        #endregion
    }
}
