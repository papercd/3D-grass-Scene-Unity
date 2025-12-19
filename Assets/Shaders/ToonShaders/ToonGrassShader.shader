Shader "Custom/ToonGrassURPWithWind"
{
    Properties
    {
        _GrassTex ("Grass Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _RampSteps("Light Steps", Range(1,8)) = 3
        
        [Header(Wind)]
        _WindSpeed ("Wind Speed", Float) = 2.0
        _WindStrength ("Wind Strength", Float) = 0.3
        _WindScale ("Wind Scale", Float) = 1.0
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 baseNormalWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _RampSteps;
                float _WindSpeed;
                float _WindStrength;
                float _WindScale;
                float4 _WindDirection;
            CBUFFER_END

            TEXTURE2D(_GrassTex);
            SAMPLER(sampler_GrassTex);

            StructuredBuffer<float4> _BasePos;
            StructuredBuffer<float4> _BaseNormal;
            StructuredBuffer<float4> _TileIndex;
            StructuredBuffer<float4> _Scale;  // NEW: Per-instance scale buffer

            float3 ComputeToonLighting(float3 worldPos, float3 normalWS, float4 baseColor, float rampSteps)
            {
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                Light mainLight = GetMainLight(shadowCoord);

                float NdotL = saturate(dot(normalWS, mainLight.direction));

                float stepSize = 1.0 / rampSteps;
                NdotL = floor(NdotL / stepSize) * stepSize;

                float3 lit = baseColor.rgb * NdotL * mainLight.color * mainLight.shadowAttenuation;

                float3 ambient = SampleSH(normalWS);
                lit += baseColor.rgb * ambient * 0.5;

                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, worldPos);
                        
                        // Calculate toon lighting for this additional light
                        float additionalNdotL = saturate(dot(normalWS, light.direction));
                        additionalNdotL = floor(additionalNdotL / stepSize) * stepSize;
                        
                        // Apply light attenuation (distance + shadows)
                        float3 additionalLight = baseColor.rgb * additionalNdotL * light.color * light.distanceAttenuation * light.shadowAttenuation;
                        
                        lit += additionalLight;
                    }
                #endif

                return lit;
            }

            float3 ApplyWind(float3 worldPos, float verticalGradient, uint instanceID)
            {
                // Use instance ID as random seed for variation
                float randomSeed = frac(sin(instanceID * 12.9898) * 43758.5453);
                
                float time = _Time.y * _WindSpeed;
                float phaseOffset = randomSeed * 6.28318;
                float speedVar = 0.8 + frac(sin(instanceID * 78.233)) * 0.4;
                float strengthVar = 0.7 + frac(sin(instanceID * 45.543)) * 0.6;
                
                // Multi-layered wind waves
                float wind1 = sin(time * speedVar + worldPos.x * _WindScale + worldPos.z * _WindScale * 0.5 + phaseOffset);
                float wind2 = sin(time * 1.3 * speedVar + worldPos.x * _WindScale * 0.7 + worldPos.z * _WindScale + phaseOffset * 0.7);
                float wind3 = cos(time * 0.7 * speedVar + worldPos.x * _WindScale * 1.5 + phaseOffset * 1.3);
                
                float windEffect = (wind1 + wind2 * 0.5 + wind3 * 0.3) / 1.8;
                
                float3 windDir = normalize(_WindDirection.xyz + float3(0.001, 0, 0));
                float3 windOffset = windDir * windEffect * _WindStrength * strengthVar;
                
                // Apply more wind to the top of the grass blade
                windOffset *= verticalGradient;
                
                return windOffset;
            }
            
            Varyings vert(Attributes v, uint instanceID : SV_InstanceID)
            {
                Varyings o;
                
                float3 basePos = _BasePos[instanceID].xyz;
                float3 baseNormal = normalize(_BaseNormal[instanceID].xyz);
                float tileIndex = _TileIndex[instanceID].x;
                float instanceScale = _Scale[instanceID].x;  // NEW: Get per-instance scale

                // Billboard to camera
                float3 camPos = GetCameraPositionWS();
                float3 viewDir = normalize(basePos - camPos);
                viewDir.y = 0.0;
                viewDir = normalize(viewDir);

                float3 up = baseNormal;
                float3 right = normalize(cross(up, viewDir));
                float3 forward = normalize(cross(right, up));

                float3 localPos = v.positionOS * instanceScale;  // NEW: Apply scale here
                float3 worldPos = basePos + right * localPos.x + up * localPos.y + forward * localPos.z;

                // Apply wind (more at the top of the blade)
                float verticalGradient = v.uv.y; // 0 at bottom, 1 at top
                float3 windOffset = ApplyWind(worldPos, verticalGradient, instanceID);
                worldPos += windOffset;

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.worldPos = worldPos;
                o.baseNormalWS = baseNormal;

                // Atlas UV logic
                float2 tileCount = float2(3.0, 3.0);
                float2 tileIndex2D = float2(fmod(tileIndex, tileCount.x), floor(tileIndex / tileCount.x));
                o.uv = (v.uv / tileCount) + (tileIndex2D / tileCount);
                
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Sample the grass texture (only for alpha)
                float4 texSample = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, i.uv);

                // Compute toon lighting
                float3 toonCol = ComputeToonLighting(i.worldPos, i.baseNormalWS, _Tint, _RampSteps);

                // Use texture alpha as a mask
                clip(texSample.a - 0.5);

                // Final output = toon color only
                return float4(toonCol, 1.0);
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

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _RampSteps;
                float _WindSpeed;
                float _WindStrength;
                float _WindScale;
                float4 _WindDirection;
            CBUFFER_END

            TEXTURE2D(_GrassTex);
            SAMPLER(sampler_GrassTex);

            StructuredBuffer<float4> _BasePos;
            StructuredBuffer<float4> _BaseNormal;
            StructuredBuffer<float4> _TileIndex;
            StructuredBuffer<float4> _Scale;  // NEW: Per-instance scale buffer

            float3 ApplyWind(float3 worldPos, float verticalGradient, uint instanceID)
            {
                float randomSeed = frac(sin(instanceID * 12.9898) * 43758.5453);
                
                float time = _Time.y * _WindSpeed;
                float phaseOffset = randomSeed * 6.28318;
                float speedVar = 0.8 + frac(sin(instanceID * 78.233)) * 0.4;
                float strengthVar = 0.7 + frac(sin(instanceID * 45.543)) * 0.6;
                
                float wind1 = sin(time * speedVar + worldPos.x * _WindScale + worldPos.z * _WindScale * 0.5 + phaseOffset);
                float wind2 = sin(time * 1.3 * speedVar + worldPos.x * _WindScale * 0.7 + worldPos.z * _WindScale + phaseOffset * 0.7);
                float wind3 = cos(time * 0.7 * speedVar + worldPos.x * _WindScale * 1.5 + phaseOffset * 1.3);
                
                float windEffect = (wind1 + wind2 * 0.5 + wind3 * 0.3) / 1.8;
                
                float3 windDir = normalize(_WindDirection.xyz + float3(0.001, 0, 0));
                float3 windOffset = windDir * windEffect * _WindStrength * strengthVar;
                windOffset *= verticalGradient;
                
                return windOffset;
            }

            Varyings ShadowVert(Attributes v, uint instanceID : SV_InstanceID)
            {
                Varyings o;
                
                float3 basePos = _BasePos[instanceID].xyz;
                float3 baseNormal = normalize(_BaseNormal[instanceID].xyz);
                float tileIndex = _TileIndex[instanceID].x;
                float instanceScale = _Scale[instanceID].x;  // NEW: Get per-instance scale

                float3 camPos = GetCameraPositionWS();
                float3 viewDir = normalize(basePos - camPos);
                viewDir.y = 0.0;
                viewDir = normalize(viewDir);

                float3 up = baseNormal;
                float3 right = normalize(cross(up, viewDir));
                float3 forward = normalize(cross(right, up));

                float3 localPos = v.positionOS * instanceScale;  // NEW: Apply scale here
                float3 worldPos = basePos + right * localPos.x + up * localPos.y + forward * localPos.z;

                // Apply wind to shadow
                float verticalGradient = v.uv.y;
                float3 windOffset = ApplyWind(worldPos, verticalGradient, instanceID);
                worldPos += windOffset;

                // Apply shadow bias
                float3 normalWS = baseNormal;
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, normalWS, _MainLightPosition.xyz));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                o.positionCS = positionCS;
                
                // Atlas UV
                float2 tileCount = float2(3.0, 3.0);
                float2 tileIndex2D = float2(fmod(tileIndex, tileCount.x), floor(tileIndex / tileCount.x));
                o.uv = (v.uv / tileCount) + (tileIndex2D / tileCount);
                
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target
            {
                // Sample texture for alpha test
                float4 texSample = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, i.uv);
                clip(texSample.a - 0.5);
                
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
