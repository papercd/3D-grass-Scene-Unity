Shader "Custom/ToonLeavesSimple"
{
    Properties
    {
        _LeafTex ("Leaf Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _RampSteps("Light Steps", Range(1,8)) = 3
        _LeafSize ("Leaf Size", Float) = 0.5
        
        [Header(Wind)]
        _WindSpeed ("Wind Speed", Float) = 2.0
        _WindStrength ("Wind Strength", Float) = 0.5
        _WindScale ("Wind Scale", Float) = 1.0
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags { 
            "RenderType"="TransparentCutout" 
            "Queue"="AlphaTest" 
            "RenderPipeline"="UniversalPipeline" 
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_LeafTex);
            SAMPLER(sampler_LeafTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _RampSteps;
                float _LeafSize;
                float _WindSpeed;
                float _WindStrength;
                float _WindScale;
                float4 _WindDirection;
            CBUFFER_END

            StructuredBuffer<float4> _BasePos;
            StructuredBuffer<float4> _BaseNormal;
            StructuredBuffer<float4> _RandomValues;

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

                return lit;
            }

            float3 ApplyWind(float3 worldPos, float3 basePos, float verticalGradient, uint instanceID)
            {
                float4 randomVals = _RandomValues[instanceID];
                
                float time = _Time.y * _WindSpeed;
                float phaseOffset = randomVals.x * 6.28318;
                float speedVar = 0.8 + randomVals.y * 0.4;
                float strengthVar = 0.7 + randomVals.z * 0.6;
                
                float wind1 = sin(time * speedVar + worldPos.x * _WindScale + worldPos.z * _WindScale * 0.5 + phaseOffset);
                float wind2 = sin(time * 1.3 * speedVar + worldPos.x * _WindScale * 0.7 + worldPos.z * _WindScale + phaseOffset * 0.7);
                float wind3 = cos(time * 0.7 * speedVar + worldPos.x * _WindScale * 1.5 + phaseOffset * 1.3);
                
                float windEffect = (wind1 + wind2 * 0.5 + wind3 * 0.3) / 1.8;
                
                float3 windDir = normalize(_WindDirection.xyz + float3(0.001, 0, 0));
                float3 windOffset = windDir * windEffect * _WindStrength * strengthVar;
                windOffset *= verticalGradient;
                
                return windOffset;
            }

            Varyings vert(Attributes v, uint instanceID : SV_InstanceID)
            {
                Varyings o;
                
                float3 basePos = _BasePos[instanceID].xyz;
                float3 baseNormal = normalize(_BaseNormal[instanceID].xyz);

                float3 camPos = GetCameraPositionWS();
                float3 viewDir = normalize(basePos - camPos);
                viewDir.y = 0.0;
                viewDir = normalize(viewDir);

                float3 up = baseNormal;
                float3 right = normalize(cross(up, viewDir));
                float3 forward = normalize(cross(right, up));

                float3 localPos = v.positionOS * _LeafSize;
                float3 worldPos = basePos + right * localPos.x + up * localPos.y + forward * localPos.z;

                float verticalGradient = v.uv.y;
                float3 windOffset = ApplyWind(worldPos, basePos, verticalGradient, instanceID);
                worldPos += windOffset;

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.worldPos = worldPos;
                o.normalWS = baseNormal;
                o.uv = v.uv;
                
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Sample leaf texture (ONLY for alpha/shape)
                float4 texSample = SAMPLE_TEXTURE2D(_LeafTex, sampler_LeafTex, i.uv);
                
                // Discard transparent pixels
                clip(texSample.a - 0.5);
                
                // Compute toon lighting (this IS the color)
                float3 toonCol = ComputeToonLighting(i.worldPos, i.normalWS, _Tint, _RampSteps);
                
                // Return ONLY the toon color (texture RGB is ignored)
                return float4(toonCol, 1.0);
            }

            ENDHLSL
        }
        
        // REMOVED ShadowCaster pass entirely
    }
}