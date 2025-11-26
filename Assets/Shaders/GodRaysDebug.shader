Shader "Custom/GodRaysFullScreenDebug"
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
                    return float4(1, 0, 0, 1); // Red screen = effect is running
                }
                
                // Sample depth
                float depth = SampleSceneDepth(uv);
                
                // Debug mode 2: Show depth
                if (_DebugMode > 1.5 && _DebugMode < 2.5)
                {
                    return float4(depth, depth, depth, 1);
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
                
                // Debug mode 3: Show plane intersection
                if (_DebugMode > 2.5)
                {
                    if (abs(denom) > 0.0001)
                    {
                        float t = dot(_PlaneOrigin - rayOrigin, _PlaneNormal) / denom;
                        if (t >= 0 && t < length(worldPos - rayOrigin))
                        {
                            return float4(0, 1, 0, 1); // Green = ray hits plane
                        }
                    }
                    return float4(1, 0, 0, 1); // Red = ray misses plane
                }
                
                if (abs(denom) > 0.0001)
                {
                    float t = dot(_PlaneOrigin - rayOrigin, _PlaneNormal) / denom;
                    
                    if (t >= 0 && t < length(worldPos - rayOrigin))
                    {
                        // Simple test: just check shadow at intersection point
                        float3 intersectionPoint = rayOrigin + rayDir * t;
                        float4 shadowCoord = TransformWorldToShadowCoord(intersectionPoint);
                        Light mainLight = GetMainLight(shadowCoord);
                        
                        // Much simpler effect for testing
                        godRayIntensity = mainLight.shadowAttenuation;
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
/*
```

## 2. Debug Steps:

1. **Set Debug Mode to 1** in the material:
   - If the screen turns red, the render feature is working
   - If not, there's an issue with the render feature setup

2. **Set Debug Mode to 2**:
   - You should see a grayscale depth visualization
   - If it's all white or black, depth texture might not be available

3. **Set Debug Mode to 3**:
   - Green pixels = rays that hit the plane
   - Red pixels = rays that miss the plane
   - This shows if your plane is positioned correctly

## 3. Common Issues to Check:

1. **Shadows enabled?**
```
   - Main Light → Shadows → Enable
   - URP Renderer → Lighting → Main Light → Cast Shadows
   - Quality Settings → Shadows → Shadow Distance > 0*/