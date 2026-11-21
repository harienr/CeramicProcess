Shader "PorcelainWorkshop/PostGrade"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Exposure ("Exposure", Range(0.4,2.0)) = 1.05
        _Saturation ("Saturation", Range(0,1.3)) = 0.9
        _Contrast ("Contrast", Range(0.7,1.4)) = 1.06
        _Warmth ("Warmth", Range(0,1)) = 0.35
        _Lift ("Lift", Range(0,0.08)) = 0.018
        _Vignette ("Vignette", Range(0,0.5)) = 0.14
        _Grain ("Grain", Range(0,0.08)) = 0.014
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _Exposure;
            half _Saturation;
            half _Contrast;
            half _Warmth;
            half _Lift;
            half _Vignette;
            half _Grain;

            float3 aces(float3 x)
            {
                return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float3 col = tex2D(_MainTex, uv).rgb * _Exposure;
                col = aces(col);

                float luma = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(luma.xxx, col, _Saturation);
                col = max(0.0, (col - 0.18) * _Contrast + 0.18);
                col += _Lift * float3(1.0, 0.96, 0.88);

                // very gentle split tone: cool shadows, warm light
                col *= lerp(float3(0.98, 0.99, 1.02), float3(1.035, 1.0, 0.955), _Warmth);

                float2 d = uv * 2.0 - 1.0;
                float vig = saturate(1.0 - dot(d, d) * _Vignette);
                col *= vig;

                float g = hash21(uv * _ScreenParams.xy + _Time.y * 13.0) - 0.5;
                col += g * _Grain;
                col = saturate(col);

                // subtle dither to keep the plaster gradients from banding
                float dither = (hash21(uv * _ScreenParams.xy + 7.1) - 0.5) / 255.0;
                return fixed4(col + dither, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
