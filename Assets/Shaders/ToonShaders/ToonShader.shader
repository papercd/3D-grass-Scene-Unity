Shader "Custom/SimpleToonURP_ForwardPlus"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _RampSteps ("Light Steps", Range(1,8)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
            "TerrainCompatible"="True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Forward + Forward+ compatibility
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS

            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            // ===== Correct includes for Forward+ =====
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _RampSteps;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            // === Your original toon ramp ===
            float ToonRamp(float ndotl)
            {
                float steps = max(1.0, _RampSteps);
                float stepSize = 1.0 / steps;
                return floor(ndotl / stepSize) * stepSize;
            }

            float3 ApplyLight(float3 normalWS, Light light)
            {
                float ndotl = saturate(dot(normalWS, normalize(light.direction)));
                ndotl = ToonRamp(ndotl);

                return _BaseColor.rgb
                     * ndotl
                     * light.color
                     * light.distanceAttenuation
                     * light.shadowAttenuation;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 color = 0;

                // ===== Required for Forward+ light loops =====
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);

                // ===== Main directional light (RETAINED) =====
                Light mainLight = GetMainLight();
                color += ApplyLight(normalWS, mainLight);

                // ===== Additional lights =====
                #if defined(_ADDITIONAL_LIGHTS)

                // Forward+ non-main directional lights
                #if USE_FORWARD_PLUS
                UNITY_LOOP
                for (uint i = 0; i < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); i++)
                {
                    Light light =
                        GetAdditionalLight(i, inputData.positionWS, half4(1,1,1,1));
                    color += ApplyLight(normalWS, light);
                }
                #endif

                // Punctual lights (point / spot)
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light =
                        GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    color += ApplyLight(normalWS, light);
                LIGHT_LOOP_END

                #endif

                // ===== Ambient (unchanged) =====
                float3 ambient = SampleSH(normalWS);
                color += _BaseColor.rgb * ambient * 0.5;

                return half4(color, 1.0);
            }

            ENDHLSL
        }

        // Shadow / depth passes unchanged
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}
