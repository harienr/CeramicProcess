Shader "PorcelainWorkshop/Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _NormalMap ("Normal", 2D) = "bump" {}
        _NormalScale ("Normal Strength", Range(0, 2)) = 1
        _SmoothnessMap ("Smoothness", 2D) = "gray" {}
        _Smoothness ("Smoothness Scale", Range(0, 1.5)) = 1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 0.55
        _WetMask ("Wet Mask", 2D) = "white" {}
        _Wetness ("Wetness", Range(0, 1)) = 0
        _WetDarken ("Wet Darken", Color) = (0.58,0.57,0.54,1)
        _WetSmoothness ("Wet Smoothness Boost", Range(0, 0.5)) = 0.18
        _DetailTex ("Fine Detail", 2D) = "gray" {}
        _DetailScale ("Detail Scale", Float) = 18
        _DetailStrength ("Detail Strength", Range(0, 0.35)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 320

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _SmoothnessMap;
        sampler2D _OcclusionMap;
        sampler2D _WetMask;
        sampler2D _DetailTex;
        fixed4 _Color;
        fixed4 _WetDarken;
        half _NormalScale;
        half _Smoothness;
        half _Metallic;
        half _OcclusionStrength;
        half _Wetness;
        half _WetSmoothness;
        half _DetailScale;
        half _DetailStrength;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_SmoothnessMap;
            float2 uv_OcclusionMap;
            float2 uv_WetMask;
            float2 uv_DetailTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            float3 albedo = tex.rgb * _Color.rgb;

            half detail = tex2D(_DetailTex, IN.uv_DetailTex * _DetailScale + 0.37).r;
            albedo *= lerp(1.0h, 0.86h + detail * 0.28h, _DetailStrength);

            half mask = tex2D(_WetMask, IN.uv_WetMask).r;
            half wet = saturate(_Wetness * lerp(0.72h, 1.0h, mask));
            albedo = lerp(albedo, albedo * _WetDarken.rgb, wet);

            half smoothness = tex2D(_SmoothnessMap, IN.uv_SmoothnessMap).r * _Smoothness;
            smoothness = lerp(smoothness, saturate(smoothness + _WetSmoothness), wet);

            float3 normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            normal.xy *= _NormalScale;
            o.Normal = normalize(normal);

            half ao = tex2D(_OcclusionMap, IN.uv_OcclusionMap).r;
            o.Albedo = albedo;
            o.Metallic = _Metallic;
            o.Smoothness = smoothness;
            o.Occlusion = lerp(1.0h, ao, _OcclusionStrength);
            o.Alpha = 1.0h;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
