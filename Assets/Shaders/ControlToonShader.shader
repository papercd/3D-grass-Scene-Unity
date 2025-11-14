Shader "Custom/ControlToon"
{
    Properties
    {
        _ShadowColor("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)
        _MidColor("Midtone Color", Color) = (0.6, 0.6, 0.6, 1)
        _HighlightColor("Highlight Color", Color) = (1.0, 1.0, 1.0, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.4
        _Smoothness("Smooth Blend", Range(0.0, 1.0)) = 0.2
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
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
                float4 _MidColor;
                float4 _HighlightColor;
                float _ShadowThreshold;
                float _Smoothness;
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

                float3 normalWS = normalize(i.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Light intensity
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float shadow = mainLight.shadowAttenuation;

                // Apply shadows
                float lightIntensity = NdotL * shadow;

                // Smooth toon transition between colors
                float t1 = smoothstep(_ShadowThreshold - _Smoothness, _ShadowThreshold + _Smoothness, lightIntensity);
                float t2 = smoothstep(1.0 - _Smoothness, 1.0, lightIntensity);

                float3 baseColor = lerp(_ShadowColor.rgb, _MidColor.rgb, t1);
                baseColor = lerp(baseColor, _HighlightColor.rgb, t2);

                // Ambient light contribution
                float3 ambient = SampleSH(normalWS);
                baseColor += ambient * 0.2;

                return float4(baseColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
