Shader "FakeMGFramework/CommonShaders/URPCelShading"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Cel Shading Settings)]
        _ShadowColor("Shadow Tint", Color) = (0.3, 0.3, 0.4, 1)
        _StepThreshold("Shadow Threshold", Range(0.0, 1.0)) = 0.5
        _StepSmoothing("Shadow Softness", Range(0.0, 0.1)) = 0.01

        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _Surface("__surface", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Includes/CelShadingInput.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half3 normalWS     : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                half fogFactor     : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                output.positionWS = positionInputs.positionWS;

                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                half4 baseColor = baseSample * _BaseColor;

                half3 normalWS = normalize(input.normalWS);

                // Select the cascade per fragment; triangles can span more than one cascade.
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half normalDotLight01 = saturate(dot(normalWS, mainLight.direction));
                half lightAttenuation01 = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half lightingValue01 = normalDotLight01 * lightAttenuation01;
                half smoothing01 = max(_StepSmoothing, 0.0001h);
                half celBand01 = smoothstep(_StepThreshold, _StepThreshold + smoothing01, lightingValue01);
                half3 celLighting = lerp(_ShadowColor.rgb, half3(1.0h, 1.0h, 1.0h), celBand01);

                half3 finalColor = baseColor.rgb * celLighting * mainLight.color;

                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, baseColor.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
