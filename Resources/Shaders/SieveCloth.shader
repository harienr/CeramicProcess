// 筛网：经纬交织的 alphaTest 挖孔；湿态整体加深并提高柔和高光，
// 对应美术规范"湿木和湿石只出现局部柔和反光"。
Shader "Porcelain/SieveCloth"
{
    Properties
    {
        _MainTex ("Weave", 2D) = "white" {}
        _Color ("Base Tint", Color) = (0.78, 0.74, 0.64, 1)
        _Cutoff ("Cutoff", Range(0, 1)) = 0.30
        _Wet ("Wetness", Range(0, 1)) = 0.0
        _WetColor ("Wet Tint", Color) = (0.50, 0.49, 0.45, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.14
        _WetSmoothness ("Wet Smoothness", Range(0, 1)) = 0.52
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow alphatest:_Cutoff
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _WetColor;
        half _Wet;
        half _Smoothness;
        half _WetSmoothness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            half wet = saturate(_Wet);
            float3 albedo = lerp(_Color.rgb * tex.rgb, _WetColor.rgb * tex.rgb, wet);
            o.Albedo = albedo;
            o.Alpha = tex.a;
            o.Metallic = 0.0;
            o.Smoothness = lerp(_Smoothness, _WetSmoothness, wet);
            o.Occlusion = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
