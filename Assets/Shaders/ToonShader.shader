Shader "Custom/SimpleToonURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _RampSteps("Light Steps", Range(1, 8)) = 3
    }

    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Force cascade shadows instead of screen space
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _RampSteps;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionHCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 normal = normalize(i.normalWS);
                
                // Always use world space shadow coordinates for render texture compatibility
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                
                // Get main light with shadow attenuation
                Light mainLight = GetMainLight(shadowCoord);

                // Basic lighting
                float NdotL = saturate(dot(normal, mainLight.direction));

                // Toon quantization
                float stepSize = 1.0 / _RampSteps;
                NdotL = floor(NdotL / stepSize) * stepSize;

                // Apply lighting with shadows
                float3 litColor = _BaseColor.rgb * NdotL * mainLight.color * mainLight.shadowAttenuation;

                // Add ambient
                float3 ambient = SampleSH(normal);
                litColor += _BaseColor.rgb * ambient * 0.5;

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            ENDHLSL
        }
    }

    FallBack Off
}