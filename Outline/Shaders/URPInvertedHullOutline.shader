Shader "FakeMGFramework/Outline/URPInvertedHullOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidthMeters("Outline Width (Meters)", Range(0.0, 0.05)) = 0.01
        [Toggle(_SMOOTH_OUTLINE_NORMALS)] _UseSmoothOutlineNormals("Use Baked Smooth Normals (UV4)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+10"
        }

        Pass
        {
            Name "OutlinePass"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_vertex _SMOOTH_OUTLINE_NORMALS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 smoothNormalOS : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidthMeters;
                float _UseSmoothOutlineNormals;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                #if defined(_SMOOTH_OUTLINE_NORMALS)
                    float3 normalWS = TransformObjectToWorldNormal(input.smoothNormalOS);
                #else
                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #endif

                positionWS += normalize(normalWS) * _OutlineWidthMeters;
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return _OutlineColor;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
