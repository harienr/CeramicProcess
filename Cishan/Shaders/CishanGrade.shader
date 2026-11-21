// Cishan/Grade - one restrained grade pass: low saturation, slight contrast,
// a warm lift in the shadows, mild vignette and fine grain. No bloom.
Shader "Cishan/Grade"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
        _Saturation ("Saturation", Range(0,1.5)) = 0.90
        _Contrast ("Contrast", Range(0.5,1.5)) = 1.035
        _Lift ("Shadow Lift", Color) = (0.018,0.014,0.008,1)
        _Vignette ("Vignette", Range(0,1)) = 0.16
        _Grain ("Grain", Range(0,0.08)) = 0.014
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            sampler2D _MainTex;
            half _Saturation;
            half _Contrast;
            half4 _Lift;
            half _Vignette;
            half _Grain;

            half Hash (float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                half3 c = src.rgb;
                half l = dot(c, half3(0.2126, 0.7152, 0.0722));
                c = lerp(half3(l, l, l), c, _Saturation);
                c = (c - 0.5) * _Contrast + 0.5;
                c += _Lift.rgb;

                float2 d = i.uv - 0.5;
                half vig = 1.0 - dot(d, d) * _Vignette * 1.8;
                c *= saturate(vig);

                half g = Hash(i.uv * _ScreenParams.xy + _Time.xx * 37.0) - 0.5;
                c += g * _Grain;

                return fixed4(c, src.a);
            }
            ENDCG
        }
    }

    FallBack Off
}
