Shader "Custom/GodRaysFullScreen"
{
    Properties
    {
        _GodRayColor ("God Ray Color", Color) = (1, 0.9, 0.7, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.5
        _Decay ("Decay", Range(0.9, 1.0)) = 0.95
        _Density ("Density", Range(0, 1)) = 0.5
        _DebugMode ("Debug Mode", Range(0, 3)) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "GodRaysPass"
            
            ZWrite Off
            ZTest Always
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _GodRayColor;
                float _Intensity;
                float _Decay;
                float _Density;
                float3 _PlaneOrigin;
                float3 _PlaneNormal;
                float _DebugMode;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Full screen triangle
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                
                // Flip Y for different graphics APIs
                #if UNITY_UV_STARTS_AT_TOP
                    output.texcoord = uv;
                #else
                    output.texcoord = float2(uv.x, 1.0 - uv.y);
                #endif
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // Sample the original color
                float4 originalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                
                // Debug mode 1: Just return red tint to verify the effect is running
                if (_DebugMode > 0.5 && _DebugMode < 1.5)
                {
                    float depth = SampleSceneDepth(uv);
                    float3 wp = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                    // Normalize for viewing
                    float3 c = wp * 0.02; 
                    return float4(abs(c), 1);
                }
                
                // Sample depth
                float depth = SampleSceneDepth(uv);
                
                // Debug mode 2: Show depth
                if (_DebugMode > 1.5 && _DebugMode < 2.5)
                {
                    float depth = SampleSceneDepth(uv);
                    float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                    float3 rayOrigin = _WorldSpaceCameraPos;
                    float3 rayDir = normalize(worldPos - rayOrigin);

                    float denom = dot(_PlaneNormal, rayDir);

                    if (abs(denom) < 0.0001)
                        return float4(0,0,1,1); // blue = ray parallel to plane

                    float t = dot(_PlaneOrigin - rayOrigin, _PlaneNormal) / denom;

                    if (t < 0)
                        return float4(0,0,1,1); // plane behind camera

                    float dist = length(worldPos - rayOrigin);

                    if (t > dist)
                        return float4(1,0,0,1); // red = ray misses

                    return float4(0,1,0,1); // green = ray hits
                }
                
                // Early exit for skybox
                #if UNITY_REVERSED_Z
                    if (depth < 0.0001)
                        return originalColor;
                #else
                    if (depth > 0.9999)
                        return originalColor;
                #endif
                
                // Reconstruct world position
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                
                // Ray from camera to world position
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(worldPos - rayOrigin);
                
                // Initialize god ray contribution
                float godRayIntensity = 0;
                
                // Ray-plane intersection
                float denom = dot(_PlaneNormal, rayDir);
                
                if (_DebugMode > 2.5 && _DebugMode < 3.5)
                {
                    float depth = SampleSceneDepth(uv);
                    float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                    float3 rayOrigin = _WorldSpaceCameraPos;
                    float3 rayDir = normalize(worldPos - rayOrigin);

                    float denom = dot(_PlaneNormal, rayDir);
                    if (abs(denom) < 0.0001) return float4(0,0,0,1);

                    float t = dot(_PlaneOrigin - rayOrigin, _PlaneNormal) / denom;
                    if (t < 0) return float4(0,0,0,1);

                    float3 hitPos = rayOrigin + rayDir * t;
                    float4 shadowCoord = TransformWorldToShadowCoord(hitPos);

                    float shadow = MainLightRealtimeShadow(shadowCoord);

                    return float4(shadow.xxx, 1);
                }

                
                if (abs(denom) > 0.0001)
                {
                    float t = dot(_PlaneOrigin - rayOrigin, _PlaneNormal) / denom;
                    
                    if (t >= 0 && t < length(worldPos - rayOrigin))
                    {
                        // Simple test: just check shadow at intersection point
                        float3 intersectionPoint = rayOrigin + rayDir * t;
                        float4 shadowCoord = TransformWorldToShadowCoord(intersectionPoint);
                        
                        
                        //Light mainLight = GetMainLight(shadowCoord);
                        
                        // Much simpler effect for testing
                        //godRayIntensity = mainLight.shadowAttenuation;
                        float shadow = MainLightRealtimeShadow(shadowCoord);
                        godRayIntensity = (1.0 - shadow) * _Intensity;
                    }
                }
                
                // Apply god rays
                float3 godRayColor = _GodRayColor.rgb * godRayIntensity * _Intensity;
                float3 finalColor = originalColor.rgb + godRayColor;
                
                return float4(finalColor, originalColor.a);
            }
            ENDHLSL
        }
    }
}
