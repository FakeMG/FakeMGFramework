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
        float4 _MaskOriginPixels;
        float4 _OutlineMaskClipScaleOffset;

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
            output.positionCS.xy = (output.positionCS.xy - _OutlineMaskClipScaleOffset.zw * output.positionCS.w) /
                _OutlineMaskClipScaleOffset.xy;
            return output;
        }

        float MaskFragment(OutlineVaryings input) : SV_Target
        {
            return input.positionCS.z;
        }

        OutlineVaryings FullscreenVertex(FullscreenAttributes input)
        {
            OutlineVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }

        OutlineVaryings CompositeVertex(FullscreenAttributes input)
        {
            const float2 cornersNdc[6] = {
                float2(-1, -1), float2(-1, 1), float2(1, 1),
                float2(-1, -1), float2(1, 1), float2(1, -1)
            };
            OutlineVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = float4(cornersNdc[input.vertexID], UNITY_NEAR_CLIP_VALUE, 1.0);
            output.positionCS.xy = output.positionCS.xy * _OutlineMaskClipScaleOffset.xy +
                _OutlineMaskClipScaleOffset.zw * output.positionCS.w;
            return output;
        }

        float MergeMaskDepth(float maskDepth01, float candidateDepth01)
        {
            if (candidateDepth01 < 0.0)
                return maskDepth01;
            if (maskDepth01 < 0.0)
                return candidateDepth01;

            // Keep the nearest selected surface; coverage never depends on a depth or normal edge.
            #if UNITY_REVERSED_Z
                return max(maskDepth01, candidateDepth01);
            #else
                return min(maskDepth01, candidateDepth01);
            #endif
        }

        float DilateMask(int2 pixelPosition, int2 axis, bool isHorizontal)
        {
            float dilatedDepth01 = -1.0;
            int thicknessPixels = clamp((int)round(_OutlineThicknessPixels), 0, 16);
            [loop]
            for (int offsetPixels = -thicknessPixels; offsetPixels <= thicknessPixels; offsetPixels++)
            {
                int2 samplePosition = pixelPosition + axis * offsetPixels;
                if (any(samplePosition < 0) || any(samplePosition >= (int2)_MaskSizePixels.xy))
                    continue;

                float candidateDepth01 = isHorizontal
                    ? LOAD_TEXTURE2D_X(_SelectionMask, samplePosition).r
                    : LOAD_TEXTURE2D_X(_HorizontallyDilatedMask, samplePosition).r;
                dilatedDepth01 = MergeMaskDepth(dilatedDepth01, candidateDepth01);
            }
            return dilatedDepth01;
        }

        float HorizontalDilationFragment(OutlineVaryings input) : SV_Target
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
            int2 pixelPosition = int2(input.positionCS.xy) - (int2)_MaskOriginPixels.xy;
            float originalDepth01 = LOAD_TEXTURE2D_X(_SelectionMask, pixelPosition).r;
            clip(-originalDepth01 - 0.000001);
            float dilatedDepth01 = DilateMask(pixelPosition, int2(0, 1), false);
            clip(dilatedDepth01);

            CompositeOutput output;
            output.color = _OutlineColor;
            // Test the expanded exterior against scene depth without cutting holes into the solid mask.
            output.depth = dilatedDepth01;
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
            #pragma vertex CompositeVertex
            #pragma fragment CompositeFragment
            ENDHLSL
        }
    }
    FallBack Off
}
