Shader "FakeMGFramework/Outline/URPSilhouetteOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 0.8, 0.05, 1)
        [IntRange] _OutlineThicknessPixels("Outline Thickness (Pixels)", Range(0, 16)) = 3
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D_X_FLOAT(_SelectionMask);
        TEXTURE2D_X_FLOAT(_HorizontallyDilatedMask);
        float4 _MaskSizePixels;

        CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor;
            float _OutlineThicknessPixels;
        CBUFFER_END

        struct GeometryAttributes
        {
            float3 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct FullscreenAttributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct OutlineVaryings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        OutlineVaryings MaskVertex(GeometryAttributes input)
        {
            OutlineVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS);
            return output;
        }

        float2 MaskFragment(OutlineVaryings input) : SV_Target
        {
            return float2(1.0, input.positionCS.z);
        }

        OutlineVaryings FullscreenVertex(FullscreenAttributes input)
        {
            OutlineVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }

        float2 MergeMaskDepth(float2 mask, float2 candidate)
        {
            if (candidate.x == 0.0)
                return mask;
            if (mask.x == 0.0)
                return candidate;

            // Keep the nearest selected surface; coverage never depends on a depth or normal edge.
            #if UNITY_REVERSED_Z
                mask.y = max(mask.y, candidate.y);
            #else
                mask.y = min(mask.y, candidate.y);
            #endif
            return mask;
        }

        float2 DilateMask(int2 pixelPosition, int2 axis, bool isHorizontal)
        {
            float2 dilatedMask = 0.0;
            int thicknessPixels = (int)round(_OutlineThicknessPixels);
            [loop]
            for (int offsetPixels = -thicknessPixels; offsetPixels <= thicknessPixels; offsetPixels++)
            {
                int2 samplePosition = pixelPosition + axis * offsetPixels;
                if (any(samplePosition < 0) || any(samplePosition >= (int2)_MaskSizePixels.xy))
                    continue;

                float2 candidate = isHorizontal
                    ? LOAD_TEXTURE2D_X(_SelectionMask, samplePosition).rg
                    : LOAD_TEXTURE2D_X(_HorizontallyDilatedMask, samplePosition).rg;
                dilatedMask = MergeMaskDepth(dilatedMask, candidate);
            }
            return dilatedMask;
        }

        float2 HorizontalDilationFragment(OutlineVaryings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return DilateMask(int2(input.positionCS.xy), int2(1, 0), true);
        }

        struct CompositeOutput
        {
            half4 color : SV_Target;
            float depth : SV_Depth;
        };

        CompositeOutput CompositeFragment(OutlineVaryings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 pixelPosition = int2(input.positionCS.xy);
            float originalMask = LOAD_TEXTURE2D_X(_SelectionMask, pixelPosition).r;
            float2 dilatedMask = DilateMask(pixelPosition, int2(0, 1), false);
            clip(dilatedMask.x - originalMask - 0.5);

            CompositeOutput output;
            output.color = _OutlineColor;
            // Test the expanded exterior against scene depth without cutting holes into the solid mask.
            output.depth = dilatedMask.y;
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "SolidSelectionMask"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "HorizontalMaskDilation"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FullscreenVertex
            #pragma fragment HorizontalDilationFragment
            ENDHLSL
        }

        Pass
        {
            Name "CompositeExteriorOutline"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FullscreenVertex
            #pragma fragment CompositeFragment
            ENDHLSL
        }
    }
    FallBack Off
}
