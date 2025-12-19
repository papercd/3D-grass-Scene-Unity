Shader "Custom/CloudPlaneCutout"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Range(0.1, 50)) = 5
        _NoiseSpeed ("Noise Animation Speed", Vector) = (0.1, 0.1, 0, 0)
        _CloudThreshold ("Cloud Threshold", Range(0, 1)) = 0.5
        _CloudSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.05
        _Color ("Cloud Color", Color) = (1, 1, 1, 1)
        
        [Header(Secondary Noise)]
        _SecondaryNoiseScale ("Secondary Noise Scale", Range(0.1, 50)) = 10
        _SecondaryNoiseSpeed ("Secondary Noise Speed", Vector) = (0.05, 0.05, 0, 0)
        _SecondaryNoiseStrength ("Secondary Noise Strength", Range(0, 1)) = 0.3
        
        [Header(Dithering)]
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
        }
        
        LOD 100
        Cull Off
        AlphaToMask On // Enables MSAA-based transparency
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _NoiseScale;
            float4 _NoiseSpeed;
            float _CloudThreshold;
            float _CloudSoftness;
            float4 _Color;
            float _SecondaryNoiseScale;
            float4 _SecondaryNoiseSpeed;
            float _SecondaryNoiseStrength;
            float _DitherStrength;
            
            // Noise functions
            float2 random2(float2 st)
            {
                st = float2(dot(st, float2(127.1, 311.7)),
                           dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }
            
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(dot(random2(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                dot(random2(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                           lerp(dot(random2(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                dot(random2(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }
            
            float fbm(float2 st, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(st * frequency);
                    st *= 2.0;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            // Screen-space dithering pattern
            float dither(float2 screenPos)
            {
                float2 ditherCoord = screenPos * 0.25;
                float ditherPattern = frac(dot(float2(171.0, 231.0), ditherCoord));
                return ditherPattern;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // Calculate noise
                float2 animOffset1 = _Time.y * _NoiseSpeed.xy;
                float2 animOffset2 = _Time.y * _SecondaryNoiseSpeed.xy;
                
                float2 noisePos = (i.worldPos.xz * _NoiseScale * 0.01) + animOffset1;
                float2 secondaryNoisePos = (i.worldPos.xz * _SecondaryNoiseScale * 0.01) + animOffset2;
                
                float cloudNoise = fbm(noisePos, 4) * 0.5 + 0.5;
                float secondaryNoise = fbm(secondaryNoisePos, 3) * 0.5 + 0.5;
                
                // Combine noise layers
                cloudNoise = lerp(cloudNoise, secondaryNoise, _SecondaryNoiseStrength);
                
                // Apply dithering for softer edges
                float2 screenPos = i.screenPos.xy / i.screenPos.w;
                float ditherValue = dither(screenPos * _ScreenParams.xy);
                
                // Adjust threshold with dithering
                float threshold = _CloudThreshold + (ditherValue - 0.5) * _CloudSoftness * _DitherStrength;
                
                // Alpha test
                clip(cloudNoise - threshold);
                
                // Sample base texture
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Final color
                fixed4 col = texColor * _Color;
                
                return col;
            }
            ENDCG
        }
        
        // Shadow caster pass - this is what makes it cast shadows!
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            float _NoiseScale;
            float4 _NoiseSpeed;
            float _CloudThreshold;
            float _SecondaryNoiseScale;
            float4 _SecondaryNoiseSpeed;
            float _SecondaryNoiseStrength;
            
            // Same noise functions (simplified for shadow pass)
            float2 random2(float2 st)
            {
                st = float2(dot(st, float2(127.1, 311.7)),
                           dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }
            
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(dot(random2(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                dot(random2(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                           lerp(dot(random2(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                dot(random2(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }
            
            float fbm(float2 st, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(st * frequency);
                    st *= 2.0;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // Calculate noise (same as main pass)
                float2 animOffset1 = _Time.y * _NoiseSpeed.xy;
                float2 animOffset2 = _Time.y * _SecondaryNoiseSpeed.xy;
                
                float2 noisePos = (i.worldPos.xz * _NoiseScale * 0.01) + animOffset1;
                float2 secondaryNoisePos = (i.worldPos.xz * _SecondaryNoiseScale * 0.01) + animOffset2;
                
                float cloudNoise = fbm(noisePos, 4) * 0.5 + 0.5;
                float secondaryNoise = fbm(secondaryNoisePos, 3) * 0.5 + 0.5;
                
                cloudNoise = lerp(cloudNoise, secondaryNoise, _SecondaryNoiseStrength);
                
                // Alpha test - this determines shadow shape
                clip(cloudNoise - _CloudThreshold);
                
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Cutout/Diffuse"
}