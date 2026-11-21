Shader "Porcelain/PostFX"
{
    // 半写实调色：ACES 曲线 → 低饱和 → 轻 S 曲线 → 暗角 → 极轻颗粒。
    // 规范要求：低饱和、保留暗部细节、无强烈泛光。
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BloomTex;
            float _Bloom;
            float4 _MainTex_TexelSize;

            float _Exposure;
            float _Saturation;
            float _Contrast;
            float _Lift;
            float _Vignette;
            float _Grain;
            float _Sharpen;
            float _Aspect;
            float _FrameTime;

            // Narkowicz 的 ACES 近似
            half3 ACESFilm(half3 x)
            {
                const float a = 2.51;
                const float b = 0.03;
                const float c = 2.43;
                const float d = 0.59;
                const float e = 0.14;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                half3 c = tex2D(_MainTex, uv).rgb;

                // 轻微锐化：程序化贴图 + 低模在近景容易发糊，这一下能明显“收紧”边缘
                if (_Sharpen > 0.001)
                {
                    float2 tx = _MainTex_TexelSize.xy;
                    half3 blur = tex2D(_MainTex, uv + float2(tx.x, 0)).rgb
                               + tex2D(_MainTex, uv - float2(tx.x, 0)).rgb
                               + tex2D(_MainTex, uv + float2(0, tx.y)).rgb
                               + tex2D(_MainTex, uv - float2(0, tx.y)).rgb;
                    c += (c * 4.0 - blur) * (_Sharpen * 0.25);
                }

                // 曝光 + 高光压缩
                c *= _Exposure;

                // 泛光：只加在窗口/天光那种高光上，强度很低
                if (_Bloom > 0.0001)
                {
                    half3 b = tex2D(_BloomTex, uv).rgb;
                    c += b * _Bloom;
                }

                c = ACESFilm(c);

                // 低饱和（保留一点色彩倾向）
                half l = dot(c, half3(0.2126, 0.7152, 0.0722));
                c = lerp(half3(l, l, l), c, _Saturation);

                // 轻 S 曲线
                c = saturate((c - 0.5) * _Contrast + 0.5);

                // 抬一点黑位，让暗部还能看清工具（规范 §2、§6）
                c = c + _Lift * (1.0 - c) * (1.0 - c);

                // 暗角
                float2 p = uv * 2.0 - 1.0;
                p.x *= _Aspect;
                float vig = 1.0 - _Vignette * dot(p, p) * 0.42;
                c *= saturate(vig);

                // 极轻颗粒，压一压程序化贴图的“干净感”
                float n = hash21(uv * _ScreenParams.xy + _FrameTime) - 0.5;
                c += n * _Grain;

                return fixed4(c, 1.0);
            }
            ENDCG
        }
    }
}
