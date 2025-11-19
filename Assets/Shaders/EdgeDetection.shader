Shader "Hidden/Edge Detection"
{
    Properties
    {
        _OutlineThickness ("Outline Thickness", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass 
        {
            Name "EDGE DETECTION OUTLINE"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // needed to sample scene depth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl" // needed to sample scene normals
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl" // needed to sample scene color/luminance

            float _OutlineThickness;
            float4 _OutlineColor;

            #pragma vertex Vert // vertex shader is provided by the Blit.hlsl include
            #pragma fragment frag

            // Edge detection kernel that works by taking the sum of the squares of the differences between diagonally adjacent pixels (Roberts Cross).
            float RobertsCross(float3 samples[4])
            {
                const float3 difference_1 = samples[1] - samples[2];
                const float3 difference_2 = samples[0] - samples[3];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            // The same kernel logic as above, but for a single-value instead of a vector3.
            float RobertsCross(float samples[4])
            {
                const float difference_1 = samples[1] - samples[2];
                const float difference_2 = samples[0] - samples[3];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }
            
            // Helper function to sample scene normals remapped from [-1, 1] range to [0, 1].
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return SampleSceneNormals(uv) * 0.5 + 0.5;
            }

            // Helper function to sample scene luminance.
            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                float2 uv = IN.texcoord;
                float2 texel_size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                // Generate 4 diagonal samples
                const float half_width_f = floor(_OutlineThickness * 0.5);
                const float half_width_c = ceil(_OutlineThickness * 0.5);

                float2 uvs[4];
                uvs[0] = uv + texel_size * float2(half_width_f, half_width_c) * float2(-1, 1);  
                uvs[1] = uv + texel_size * float2(half_width_c, half_width_c) * float2(1, 1);   
                uvs[2] = uv + texel_size * float2(half_width_f, half_width_f) * float2(-1, -1);
                uvs[3] = uv + texel_size * float2(half_width_c, half_width_f) * float2(1, -1);  

                float3 normal_samples[4];
                float depth_samples[4], luminance_samples[4];

                for (int i = 0; i < 4; i++) {
                    depth_samples[i] = SampleSceneDepth(uvs[i]);
                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
                    luminance_samples[i] = SampleSceneLuminance(uvs[i]);
                }

                // Edge detection
                float edge_depth = RobertsCross(depth_samples);
                float edge_normal = RobertsCross(normal_samples);
                float edge_luminance = RobertsCross(luminance_samples);

                // Thresholds
                edge_depth = edge_depth > (1.0 / 200.0) ? 1 : 0;
                edge_normal = edge_normal > (1.0 / 4.0) ? 1 : 0;
                edge_luminance = edge_luminance > (1.0 / 0.5) ? 1 : 0;

                float edge = max(edge_depth, max(edge_normal, edge_luminance));

                // ----------------------------
                // OCCLUSION TEST (important)
                // ----------------------------

                float depthCenter = SampleSceneDepth(uv);

                float depthMin = min(min(depth_samples[0], depth_samples[1]),
                                    min(depth_samples[2], depth_samples[3]));

                // If the closest sampled edge pixel is *behind* the center pixel,
                // the outline is occluded.
                bool occluded = depthMin > depthCenter + 0.0005;

                if (occluded || edge == 0)
                    return float4(0,0,0,0);

                // ----------------------------
                // LIGHTING-AWARE OUTLINE
                // ----------------------------

                float3 sceneColor = SampleSceneColor(uv);
                float sceneLuminance = dot(sceneColor, float3(0.299, 0.587, 0.114));

                float lightingFactor = saturate(sceneLuminance * 2.0);

                // Final shaded outline color
                return _OutlineColor * edge * lightingFactor;
            }

            ENDHLSL
        }
    }
}