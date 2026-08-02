Shader "KiKs/CardFire"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _EdgeWidth ("Edge Width", Range(0.001, 0.05)) = 0.008

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
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            float _EdgeWidth;
            float _DistortionNoiseScale;
            float _DistortionStrengthX;
            float _DistortionStrengthY;
            float _DistortionSpeedX;
            float _DistortionSpeedY;
            fixed4 _ColorFrom;
            fixed4 _ColorTo;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldPos = v.vertex;
                o.color = v.color * _Color;
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

            // 采样 alpha，带扭曲偏移
            float sampleAlpha(float2 uv, float2 offset)
            {
                return tex2D(_MainTex, uv + offset).a;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;

                // 采样原图（不扭曲）
                fixed4 mainColor = tex2D(_MainTex, uv) * i.color;
                float origAlpha = mainColor.a;

                // 噪声偏移（用于火焰边缘扭曲）
                float2 noiseUV = uv;
                noiseUV.x += _Time.y * _DistortionSpeedX;
                noiseUV.y += _Time.y * _DistortionSpeedY;
                float noiseOffset = simpleNoise(noiseUV, _DistortionNoiseScale);
                noiseOffset = noiseOffset * 2.0 - 1.0;

                // 火焰渐变色
                fixed4 fireColor = lerp(_ColorFrom, _ColorTo, smoothstep(-1.0, 1.0, noiseOffset));

                // 多层边缘检测 — 用固定 _EdgeWidth 代替 _MainTex_TexelSize
                float glow = 0.0;

                // 3层从近到远
                for (int layer = 1; layer <= 3; layer++)
                {
                    float2 offset = float2(_EdgeWidth, _EdgeWidth) * float(layer);

                    // 带噪声扭曲的采样 UV
                    float2 distortedUV = uv;
                    distortedUV.x += noiseOffset * _DistortionStrengthX * float(layer);
                    distortedUV.y += noiseOffset * _DistortionStrengthY * float(layer);

                    // 8方向采样
                    float a = sampleAlpha(distortedUV, float2(0, offset.y));         // up
                    float b = sampleAlpha(distortedUV, float2(0, -offset.y));        // down
                    float c = sampleAlpha(distortedUV, float2(-offset.x, 0));        // left
                    float d = sampleAlpha(distortedUV, float2(offset.x, 0));         // right
                    float e = sampleAlpha(distortedUV, float2(offset.x, offset.y));  // up-right
                    float f = sampleAlpha(distortedUV, float2(-offset.x, offset.y));  // up-left
                    float g = sampleAlpha(distortedUV, float2(offset.x, -offset.y)); // down-right
                    float h = sampleAlpha(distortedUV, float2(-offset.x, -offset.y));// down-left

                    float neighbor = max(max(max(a, b), max(c, d)), max(max(e, f), max(g, h)));
                    float edge = saturate(neighbor - origAlpha);
                    glow = max(glow, edge * (0.9 - float(layer) * 0.2));
                }

                // UV 边框检测 — 补充 alpha 边缘检测，确保边缘可见
                float2 borderUV = uv;
                borderUV.x += noiseOffset * _DistortionStrengthX;
                borderUV.y += noiseOffset * _DistortionStrengthY;
                float edgeDist = min(min(borderUV.x, 1.0 - borderUV.x), min(borderUV.y, 1.0 - borderUV.y));
                float uvBorder = 1.0 - smoothstep(0.0, _EdgeWidth * 2.0, edgeDist);
                glow = max(glow, uvBorder * 0.8);

                // 组合：只显示火焰描边，中间透明
                // 没有 sprite 时 origAlpha=0，UV 边框检测画出外框火焰
                fixed4 result = fixed4(fireColor.rgb, glow * 0.9);

                #ifdef UNITY_UI_CLIP_RECT
                    result.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
