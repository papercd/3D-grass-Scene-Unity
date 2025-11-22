Shader "Hidden/GodRays"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        float4 _MainTex_ST;
        float3 _MainLightDir;
        float _PlaneDistance;
        float _StepSize;
        int _MaxSteps;
        float _Intensity;
        float _ScatteringCoefficient;
        float4 _Tint;
        
        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };
        
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 viewRayWS : TEXCOORD1;
        };
        
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            
            // Calculate view ray in world space for this vertex
            // This will be interpolated across the screen
            float3 viewRayVS = mul(unity_CameraInvProjection, float4(input.uv * 2 - 1, 0, -1)).xyz;
            output.viewRayWS = mul(unity_CameraToWorld, float4(viewRayVS, 0)).xyz;
            
            return output;
        }
        
        // Ray-Plane intersection
        // Returns the distance along the ray where it intersects the plane
        float RayPlaneIntersection(float3 rayOrigin, float3 rayDir, float3 planePoint, float3 planeNormal)
        {
            float denom = dot(planeNormal, rayDir);
            
            // Ray is parallel to plane
            if (abs(denom) < 0.0001)
                return -1.0;
            
            float3 diff = planePoint - rayOrigin;
            float t = dot(diff, planeNormal) / denom;
            
            return t;
        }
        
        // Simple atmospheric scattering approximation
        float GetScattering(float3 worldPos, float3 lightDir)
        {
            // Calculate distance from the light ray
            float3 toLight = -lightDir;
            
            // Simple exponential falloff based on distance from camera
            float dist = length(worldPos - _WorldSpaceCameraPos);
            float falloff = exp(-dist * 0.1);
            
            // Add some variation - you can replace this with texture sampling, noise, etc.
            float variation = 1.0;
            
            return falloff * variation * _ScatteringCoefficient;
        }
        
        half4 Frag(Varyings input) : SV_Target
        {
            // Sample the original color
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            
            // Sample depth
            float depth = SampleSceneDepth(input.uv);
            
            // Reconstruct world position from depth
            float3 viewRayWS = normalize(input.viewRayWS);
            
            #if UNITY_REVERSED_Z
                depth = 1.0 - depth;
            #endif
            
            // Convert depth to linear
            depth = LinearEyeDepth(depth, _ZBufferParams);
            
            // Calculate world position
            float3 worldPos = _WorldSpaceCameraPos + viewRayWS * depth;
            
            // Define the origin plane
            // The plane is perpendicular to the light direction at a distance from camera
            float3 planePoint = _WorldSpaceCameraPos + _MainLightDir * _PlaneDistance;
            float3 planeNormal = normalize(_MainLightDir);
            
            // Find intersection of view ray with origin plane
            float intersectionDist = RayPlaneIntersection(_WorldSpaceCameraPos, viewRayWS, planePoint, planeNormal);
            
            // If no intersection or behind camera, skip god rays
            if (intersectionDist < 0.0)
                return color;
            
            // The raymarch starts at the intersection point and goes back to camera
            float3 rayStart = _WorldSpaceCameraPos + viewRayWS * intersectionDist;
            float3 rayEnd = worldPos;
            
            // If the scene geometry is in front of the origin plane, use that as the end point
            if (depth < intersectionDist)
            {
                rayEnd = worldPos;
            }
            else
            {
                rayEnd = rayStart;
            }
            
            // Calculate ray direction and total distance
            float3 rayDir = rayEnd - rayStart;
            float rayLength = length(rayDir);
            rayDir = normalize(rayDir);
            
            // Raymarch
            float stepSize = _StepSize;
            int numSteps = min(_MaxSteps, (int)(rayLength / stepSize));
            
            float3 accumulated = float3(0, 0, 0);
            float transmittance = 1.0;
            
            for (int i = 0; i < numSteps; i++)
            {
                // Current position along the ray
                float t = (float)i / (float)numSteps;
                float3 currentPos = rayStart + rayDir * rayLength * t;
                
                // Sample scattering at this position
                float scattering = GetScattering(currentPos, _MainLightDir);
                
                // Accumulate light
                accumulated += scattering * transmittance * stepSize;
                
                // Update transmittance (simple exponential decay)
                transmittance *= exp(-scattering * stepSize * 0.1);
            }
            
            // Apply intensity and tint
            float3 godRays = accumulated * _Intensity * _Tint.rgb;
            
            // Blend with original color
            color.rgb += godRays;
            
            return color;
        }
        ENDHLSL
        
        Pass
        {
            Name "God Rays"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
