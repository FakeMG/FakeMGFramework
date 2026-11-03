#ifndef FAKE_MG_CEL_SHADING_INPUT_INCLUDED
#define FAKE_MG_CEL_SHADING_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// Every cel pass owns this same layout, independent of URP Lit's material layout.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _ShadowColor;
    half _StepThreshold;
    half _StepSmoothing;
    half _Cutoff;
    half _Surface;
CBUFFER_END

#endif
