// 极轻微的影调统一：略降饱和、轻微对比、柔和暗角、极弱暖调。
// 不做泛光、不做强色彩风格化——对应规范"避免强烈泛光""避免整幅画面泛黄"。
Shader "Porcelain/StyleGrade"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _Saturation ("Saturation", Range(0, 2)) = 0.93
        _Contrast ("Contrast", Range(0, 2)) = 1.03
        _Lift ("Lift", Range(-0.2, 0.2)) = 0.004
        _Vignette ("Vignette", Range(0, 1)) = 0.20
        _Warm ("Warmth", Range(-1, 1)) = 0.018
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _Saturation;
            half _Contrast;
            half _Lift;
            half _Vignette;
            half _Warm;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                half luma = dot(c.rgb, half3(0.299, 0.587, 0.114));
                c.rgb = lerp(half3(luma, luma, luma), c.rgb, _Saturation);
                c.rgb = (c.rgb - 0.5) * _Contrast + 0.5 + _Lift;
                c.rgb += half3(_Warm, 0.0, -_Warm);

                float2 d = i.uv - 0.5;
                half vig = 1.0 - dot(d, d) * _Vignette * 2.0;
                c.rgb *= saturate(vig);
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
