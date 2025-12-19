Shader "Custom/RelicMaterialShader"
{
    Properties
    {
        [Header(Base Settings)]
        [MainColor] _BaseColor("Base Color", Color) = (0.4, 0.7, 1, 1) // Crystal Blue
        
        [Header(Toon Shading)]
        _RampSteps("Light Steps", Range(1, 40)) = 2
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.3
        
        [Header(Emission Glow)]
        [HDR] _EmissionColor("Emission Color", Color) = (1, 2.5, 4, 1) // Bright cyan-blue glow
        _EmissionIntensity("Emission Intensity", Range(0, 10)) = 3.5
        _PulseSpeed("Pulse Speed", Range(0, 5)) = 1
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.25
        
        [Header(Rim Light)]
        _RimColor("Rim Color", Color) = (0.5, 0.8, 1, 1) // Cyan rim
        _RimPower("Rim Power", Range(0.1, 10)) = 3
        _RimIntensity("Rim Intensity", Range(0, 5)) = 2.5
    }

    SubShader
    {
        Tags 
        { 
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes 
            { 
                float4 positionOS : POSITION; 
                float3 normalOS : NORMAL;
            };
            
            struct Varyings 
            { 
                float4 positionHCS : SV_POSITION; 
                float3 normalWS : TEXCOORD0; 
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _RampSteps;
                float _ShadowStrength;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _PulseSpeed;
                float _PulseAmount;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionHCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(i.normalWS);
                float3 viewDirWS = normalize(i.viewDirWS);
                
                // Get main light
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // === TOON LIGHTING ===
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Apply shadow
                float shadow = mainLight.shadowAttenuation;
                NdotL *= shadow;
                
                // Toon step lighting (posterize)
                float stepSize = 1.0 / _RampSteps;
                float toonLight = floor(NdotL / stepSize) * stepSize;
                
                // Blend between shadow and lit
                toonLight = lerp(_ShadowStrength, 1.0, toonLight);
                
                // Apply base color with toon lighting
                float3 baseColor = _BaseColor.rgb * toonLight * mainLight.color;
                
                // === RIM LIGHTING (Fresnel) ===
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float rimFactor = 1.0 - NdotV;
                rimFactor = pow(rimFactor, _RimPower);
                float3 rimLight = rimFactor * _RimColor.rgb * _RimIntensity;
                
                // === EMISSION (Pulsing Glow) ===
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float3 emission = _EmissionColor.rgb * _EmissionIntensity * pulse;
                
                // === COMBINE ===
                float3 finalColor = baseColor + rimLight + emission;
                
                return float4(finalColor, 1.0);
            }

            ENDHLSL
        }
        
        // Shadow caster pass for proper shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
