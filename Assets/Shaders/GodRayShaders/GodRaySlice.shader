Shader "GodRays/VolumetricSlice"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 0.1)
        _MainLightIntensity ("Light Intensity", Range(0, 10)) = 1.0
        // Controls how soft the intersection with scene geometry is
        _Softness ("Soft Particle Factor", Range(0.01, 5.0)) = 1.0 
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Pass
        {
            Name "VolumetricSlice"
            Tags { "LightMode" = "UniversalForward" } // Important for Main Light Shadows
            
            Blend One One // Additive blending (Light adds up)
            ZWrite Off
            ZTest LEqual
            Cull Off 

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            // --------------------------------------------------
            // KEYWORDS FOR URP SHADOWS
            // --------------------------------------------------
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            // --------------------------------------------------

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD0; // For depth comparison
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _MainLightIntensity;
                float _Softness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 1. DEPTH FADE (Soft Particles)
                // Prevents hard lines where the planes intersect scene geometry
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float partDepth = LinearEyeDepth(input.screenPos.z / input.screenPos.w, _ZBufferParams);
                
                float fade = saturate((sceneDepth - partDepth) * _Softness);

                // 2. SHADOW LOOKUP
                // Convert World Pos to Shadow Map Coords
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                // Sample Shadow Map
                // returns 1.0 (Lit) or 0.0 (Shadow)
                // Note: MainLightRealtimeShadow automatically handles cascades and soft shadows
                half shadowAtten = MainLightRealtimeShadow(shadowCoord);
                
                // 3. COLOR CALCULATION
                // Color * Light Presence * Fade * Intensity
                half3 finalColor = _Color.rgb * shadowAtten * fade * _MainLightIntensity;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}