// Cishan/Lit - semi-realistic surface for stone, clay, wood, plaster, powder, iron.
// Albedo RGB + roughness in A. One wetness scalar drives darkening, gloss and
// normal softening: wet never reads as bright. Dust settles on up-facing faces.
Shader "Cishan/Lit"
{
    Properties
    {
        _MainTex ("Albedo (RGB) Roughness (A)", 2D) = "white" {}
        [NoScaleOffset] _BumpMap ("Normal", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Roughness ("Roughness Scale", Range(0,1)) = 1.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BumpScale ("Normal Strength", Range(0,2)) = 1.0
        _Wetness ("Wetness", Range(0,1)) = 0.0
        _WetDarken ("Wet Darken", Range(0,0.4)) = 0.14
        _WetSmoothness ("Wet Smoothness", Range(0,1)) = 0.5
        _DustColor ("Dust Color", Color) = (0.72,0.69,0.62,1)
        _DustAmount ("Dust On Up-Facing", Range(0,1)) = 0.0
        _Occlusion ("Occlusion", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _Roughness;
        half _Metallic;
        half _BumpScale;
        half _Wetness;
        half _WetDarken;
        half _WetSmoothness;
        fixed4 _DustColor;
        half _DustAmount;
        half _Occlusion;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            half rough = saturate(tex.a * _Roughness);
            half3 albedo = tex.rgb * _Color.rgb;
            half wet = saturate(_Wetness);

            // Wet darkens the whole surface instead of brightening it.
            albedo *= lerp(1.0, 1.0 - _WetDarken, wet);

            // Dust only on upward faces, broken up by the albedo detail.
            half up = saturate(IN.worldNormal.y);
            half dust = _DustAmount * smoothstep(0.25, 0.9, up);
            dust *= 0.55 + 0.45 * tex.r;
            albedo = lerp(albedo, _DustColor.rgb, dust * 0.65);

            half gloss = lerp(1.0 - rough, _WetSmoothness, wet);
            gloss = lerp(gloss, gloss * 0.7, dust);

            o.Albedo = albedo;
            o.Metallic = _Metallic * (1.0 - dust * 0.8);
            o.Smoothness = saturate(gloss);
            o.Occlusion = _Occlusion;

            half3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            n.xy *= _BumpScale * lerp(1.0, 0.55, wet);
            o.Normal = normalize(n);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
