Shader "Custom/ToonGrassURP"
{
    Properties
    {
        _GrassTex ("Grass Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _RampSteps("Light Steps", Range(1,8)) = 3
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
                TEXTURE2D(_GrassTex);
                SAMPLER(sampler_GrassTex);
            CBUFFER_END

            // Instanced per-blade data (set from C# as Vector4 arrays
            /*
            UNITY_INSTANCING_BUFFER_START(PerInstanceData)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BasePos)     // xyz: world pos
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseNormal)  // xyz: world normal
                UNITY_DEFINE_INSTANCED_PROP(float4, _TileIndex)    // 0–8 (for 3x3 grid)
            UNITY_INSTANCING_BUFFER_END(PerInstanceData)
            */
            StructuredBuffer<float4> _BasePos;
            StructuredBuffer<float4> _BaseNormal;
            StructuredBuffer<float4> _TileIndex;

            // Reuse the same toon lighting function you use for terrain
            float3 ComputeToonLighting(float3 worldPos, float3 normalWS, float4 baseColor, float rampSteps)
            {
                // shadow coordinates from world pos
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                Light mainLight = GetMainLight(shadowCoord);

                float NdotL = saturate(dot(normalWS, mainLight.direction));

                // Quantize (toon ramp)
                float stepSize = 1.0 / rampSteps;
                NdotL = floor(NdotL / stepSize) * stepSize;

                float3 lit = baseColor.rgb * NdotL * mainLight.color * mainLight.shadowAttenuation;

                // ambient via spherical harmonics (same approach as your terrain)
                float3 ambient = SampleSH(normalWS);
                lit += baseColor.rgb * ambient * 0.5;

                return lit;
            }
            /*
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 basePos = UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _BasePos).xyz;
                float3 baseNormal = normalize(UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _BaseNormal).xyz);

                // world position for this vertex (mesh vertices are relative to blade base pivot)
                float3 worldPos = v.positionOS + basePos;
                

                //o.positionHCS = mul(UNITY_MATRIX_MVP, float4(worldPos, 1.0));
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.worldPos = worldPos;
                o.baseNormalWS = baseNormal;

                return o;
            }*/
            
            Varyings vert(Attributes v,uint instanceID : SV_InstanceID)
            {
                Varyings o;
                //UNITY_SETUP_INSTANCE_ID(v);
                //UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 basePos = _BasePos[instanceID].xyz;
                float3 baseNormal = normalize(_BaseNormal[instanceID].xyz);
                float tileIndex = _TileIndex[instanceID].x;

                //float3 basePos = UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _BasePos).xyz;
                //float3 baseNormal = normalize(UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _BaseNormal).xyz);

                float3 camPos = GetCameraPositionWS();
                float3 viewDir = normalize(basePos - camPos);
                viewDir.y = 0.0;
                viewDir = normalize(viewDir);

                float3 up = baseNormal;
                float3 right = normalize(cross(up, viewDir));
                float3 forward = normalize(cross(right, up));

                float3 localPos = v.positionOS;
                float3 worldPos = basePos + right * localPos.x + up * localPos.y + forward * localPos.z;

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.worldPos = worldPos;
                o.baseNormalWS = baseNormal;

                // === Atlas UV logic ===
                //float tileIndex = UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _TileIndex).x;
                float2 tileCount = float2(3.0, 3.0); // your 3x3 atlas
                float2 tileIndex2D = float2(fmod(tileIndex, tileCount.x), floor(tileIndex / tileCount.x));
                o.uv = (v.uv / tileCount) + (tileIndex2D / tileCount);
                
                return o;
            }


            /*
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 basePos = UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _BasePos).xyz;
                float3 baseNormal = normalize(UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _BaseNormal).xyz);

                // Camera position (from URP)
                float3 camPos = GetCameraPositionWS();

                // Billboard direction: from camera to grass base
                float3 viewDir = normalize(basePos - camPos);

                // Flatten on Y-axis (so it only rotates around vertical)
                viewDir.y = 0.0;
                viewDir = normalize(viewDir);

                // Build billboard basis
                float3 up = baseNormal; // Use terrain normal as "up" direction
                float3 right = normalize(cross(up, viewDir));
                float3 forward = normalize(cross(right, up));

                // Get local vertex position (the grass mesh quad, centered around origin)
                float3 localPos = v.positionOS;

                // Billboarded world position
                float3 worldPos = basePos + right * localPos.x + up * localPos.y + forward * localPos.z;

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.worldPos = worldPos;
                o.baseNormalWS = baseNormal;

                return o;
            }*/
            
            
            /*
            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // sample grass texture (use SAMPLE_TEXTURE2D for URP helper macros)
                float3 texCol = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, i.uv).rgb;

                // compute toon lighting using terrain-equivalent function
                float3 lighting = ComputeToonLighting(i.worldPos, i.baseNormalWS, _Tint, _RampSteps);

                // final color = texture * lighting (tint included in lighting)
                float3 finalCol = texCol * lighting;

                return float4(finalCol, 1.0);
            }*/
            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Sample the grass texture (only for alpha)
                float4 texSample = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, i.uv);

                // Compute toon lighting (this is the full terrain-like color)
                float3 toonCol = ComputeToonLighting(i.worldPos, i.baseNormalWS, _Tint, _RampSteps);

                // Use texture alpha as a mask
                clip(texSample.a - 0.5);  // discard if below threshold

                // Final output = toon color only
                return float4(toonCol, 1.0);
            }


            ENDHLSL
        }

        // Shadow caster + DepthOnly passes can be added similarly if needed
    }

    FallBack Off
}
