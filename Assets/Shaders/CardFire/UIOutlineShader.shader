Shader "Ayy/UIOutline"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        _DilateKernelSize ("Dilate Kernel Size", Range(0,20)) = 3
        _BlurKernelSize ("Blur Kernel Size", Range(0,20)) = 5

        _MaskLower ("Mask Lower", Range(0,1)) = 0.0
        _MaskInc ("Mask Inc", Range(0,1)) = 0.1

        [Toggle(ENABLE_NOISE_DISTORTION)] _EnableNoiseDistortion ("Enable Noise Distortion", Int) = 1
        _DistortionNoiseScale ("Distortion Noise Scale", Range(-100,100)) = 20
        _DistortionStrengthX ("Distortion Strength X", Range(-1.0,1.0)) = 0.1
        _DistortionStrengthY ("Distortion Strength Y", Range(-1.0,1.0)) = 0.1
        _DistortionSpeedX ("Distortion Speed X", Range(-1,1)) = 1.0
        _DistortionSpeedY ("Distortion Speed Y", Range(-1,1)) = 1.0

        _ColorFrom ("Color From", Color) = (1,0,0,1)
        _ColorTo ("Color To", Color) = (1,1,0,1)
    }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            sampler2D _UIOutlineMask;

            int _DilateKernelSize;
            int _BlurKernelSize;
            float _MaskLower;
            float _MaskInc;
            int _EnableNoiseDistortion;
            float _DistortionNoiseScale;
            float _DistortionStrengthX;
            float _DistortionStrengthY;
            float _DistortionSpeedX;
            float _DistortionSpeedY;
            float4 _ColorFrom;
            float4 _ColorTo;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 0, 1);
                #if UNITY_UV_STARTS_AT_TOP
                    o.vertex.y = -o.vertex.y;
                #endif
                o.uv = v.uv;
                #if UNITY_UV_STARTS_AT_TOP
                    o.uv.y = 1.0 - o.uv.y;
                #endif
                return o;
            }

            float noise_randomValue(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise_interpolate(float a, float b, float t)
            {
                return (1.0 - t) * a + (t * b);
            }

            float noise_valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                float r0 = noise_randomValue(c0);
                float r1 = noise_randomValue(c1);
                float r2 = noise_randomValue(c2);
                float r3 = noise_randomValue(c3);

                float bottomOfGrid = noise_interpolate(r0, r1, f.x);
                float topOfGrid = noise_interpolate(r2, r3, f.x);
                return noise_interpolate(bottomOfGrid, topOfGrid, f.y);
            }

            float simpleNoise(float2 UV, float Scale)
            {
                float t = 0.0;
                float freq = pow(2.0, float(0));
                float amp = pow(0.5, float(3 - 0));
                t += noise_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;
                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3 - 1));
                t += noise_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;
                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3 - 2));
                t += noise_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;
                return t;
            }

            // FBM noise: layered turbulence for flame-like distortion
            float fbm(float2 uv)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                float2 rot = float2(0.866, 0.5); // cos(30), sin(30) — rotates each octave
                for (int i = 0; i < 4; i++)
                {
                    v += a * noise_valueNoise(uv);
                    uv = mul(float2x2(rot.x, -rot.y, rot.y, rot.x), uv) * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }
        ENDHLSL

        // Pass 0: Extract luminance as mask (URP forces alpha=1, so use luminance instead)
        Pass
        {
            Name "ExtractAlpha"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // Use max of RGB channels as mask value (black bg = 0, colored UI > 0)
                float v = max(max(col.r, col.g), col.b);
                return float4(v, v, v, 1.0);
            }
            ENDHLSL
        }

        // Pass 1: Dilate the mask
        Pass
        {
            Name "Dilate"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float v = 0.0;
                int radius = _DilateKernelSize;
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy;
                        float2 curUV = uv + offset;
                        float gray = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, curUV).r;
                        v = max(gray, v);
                    }
                }
                return float4(v, v, v, 1.0);
            }
            ENDHLSL
        }

        // Pass 2: Blur Horizontal
        Pass
        {
            Name "BlurH"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float4 color = float4(0.0, 0.0, 0.0, 0.0);
                float totalWeight = 0.0;
                float2 texelSize = _MainTex_TexelSize.xy;
                for (int x = -_BlurKernelSize; x <= _BlurKernelSize; ++x)
                {
                    float weight = exp(-(x * x) / (2.0 * _BlurKernelSize * _BlurKernelSize));
                    float2 uv = i.uv + float2(x, 0) * texelSize;
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * weight;
                    totalWeight += weight;
                }
                return float4((color / totalWeight).rgb, 1.0);
            }
            ENDHLSL
        }

        // Pass 3: Blur Vertical
        Pass
        {
            Name "BlurV"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float4 color = float4(0.0, 0.0, 0.0, 0.0);
                float totalWeight = 0.0;
                float2 texelSize = _MainTex_TexelSize.xy;
                for (int y = -_BlurKernelSize; y <= _BlurKernelSize; ++y)
                {
                    float weight = exp(-(y * y) / (2.0 * _BlurKernelSize * _BlurKernelSize));
                    float2 uv = i.uv + float2(0, y) * texelSize;
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * weight;
                    totalWeight += weight;
                }
                return float4((color / totalWeight).rgb, 1.0);
            }
            ENDHLSL
        }

        // Pass 4: Composite - blend original color with outline color using mask
        // Output preserves alpha: 1 where UI exists, 0 where transparent
        Pass
        {
            Name "Composite"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 originUV = uv;

                float noiseOffset = 0.0;
                if (_EnableNoiseDistortion)
                {
                    float2 noiseUV = uv;
                    noiseUV.x += _Time.y * _DistortionSpeedX;
                    noiseUV.y += _Time.y * _DistortionSpeedY;
                    noiseOffset = simpleNoise(noiseUV, _DistortionNoiseScale);
                    noiseOffset = noiseOffset * 2.0 - 1.0;
                    // Asymmetric: bias upward — flames rise, don't drip down
                    // In screen UV, up = -Y, so we subtract to move the mask sampling upward
                    uv.x += noiseOffset * _DistortionStrengthX;
                    uv.y += max(0.0, noiseOffset) * _DistortionStrengthY;
                }

                float4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, originUV);
                float4 maskDistorted = tex2D(_UIOutlineMask, uv);

                float lower = clamp(_MaskLower, 0.0, 1.0);
                float higher = clamp(_MaskLower + _MaskInc, 0.0, 1.0);
                float mask = smoothstep(lower, higher, maskDistorted.r);

                float origLum = max(max(mainColor.r, mainColor.g), mainColor.b);
                float isUI = step(0.01, origLum);

                float4 outlineColor = lerp(_ColorFrom, _ColorTo, smoothstep(-1.0, 1.0, noiseOffset));
                
                float4 ret = lerp(outlineColor, mainColor, isUI);
                ret.a = max(mask, isUI);
                return ret;
            }
            ENDHLSL
        }

        // Pass 5: ScreenBlend - alpha blend result onto screen via Blitter
        Pass
        {
            Name "ScreenBlend"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
            }
            ENDHLSL
        }
    }
}
