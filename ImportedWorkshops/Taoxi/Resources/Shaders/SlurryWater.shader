Shader "PorcelainWorkshop/SlurryWater"
{
    Properties
    {
        _ShallowColor ("Water Tint", Color) = (0.34,0.37,0.35,1)
        _TurbidColor ("Turbid Slurry", Color) = (0.64,0.62,0.56,1)
        _Turbidity ("Turbidity", Range(0,1)) = 0.0
        _BaseAlpha ("Base Alpha", Range(0,1)) = 0.3
        _NormalMap ("Ripple Normal", 2D) = "bump" {}
        _NormalScale ("Ripple Strength", Range(0,2)) = 0.55
        _Smoothness ("Smoothness", Range(0,1)) = 0.16
        _FlowSpeed ("Flow Speed", Float) = 0.035
        _Swirl ("Swirl", Range(0,1)) = 0
        _Center ("Swirl Center (world xz)", Vector) = (0,0,0,0)
        _RippleAmount ("Ripple", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        CGPROGRAM
        #pragma surface surf Standard alpha:fade noshadow
        #pragma target 3.0

        sampler2D _NormalMap;
        fixed4 _ShallowColor;
        fixed4 _TurbidColor;
        half _Turbidity;
        half _BaseAlpha;
        half _NormalScale;
        half _Smoothness;
        half _FlowSpeed;
        half _Swirl;
        float4 _Center;
        half _RippleAmount;

        struct Input
        {
            float2 uv_NormalMap;
            float3 worldPos;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 d = IN.worldPos.xz - _Center.xz;
            float radius = length(d);
            float swirlAngle = _Swirl * saturate(1.0 - radius / 0.7) * _Time.y * 2.2;
            float cs = cos(swirlAngle);
            float sn = sin(swirlAngle);
            float2 centered = IN.uv_NormalMap - 0.5;
            float2 swirlUV = mul(float2x2(cs, -sn, sn, cs), centered) + 0.5;

            float2 flow = _FlowSpeed * _Time.y * float2(0.42, 0.18);
            float3 n1 = UnpackNormal(tex2D(_NormalMap, swirlUV + flow));
            float3 n2 = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap * 0.63 - flow * 0.55 + 0.21));
            float3 n = normalize(n1 + n2 * 0.7);
            n.xy *= _NormalScale * (0.7 + _RippleAmount);

            half fresnel = pow(1.0h - saturate(dot(normalize(IN.viewDir), half3(0,1,0))), 4.0h);
            fixed3 tint = lerp(_ShallowColor.rgb, _TurbidColor.rgb, _Turbidity);
            half ripple = saturate((n1.x + n1.y) * 0.55h + 0.5h);
            tint *= 0.82h + ripple * 0.3h;
            half alpha = saturate(_BaseAlpha + _Turbidity * 0.5h + fresnel * 0.1h);

            o.Albedo = tint;
            o.Normal = n;
            o.Smoothness = lerp(0.13h, _Smoothness, 1.0h - _Turbidity * 0.55h);
            o.Metallic = 0;
            o.Alpha = alpha;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
