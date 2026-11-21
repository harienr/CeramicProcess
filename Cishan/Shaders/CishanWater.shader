// Cishan/Water - thin water in the flume and the weak sheen on slurry.
// No mirror water: low smoothness, gentle normal drift, opacity from 0.6 to 0.9.
Shader "Cishan/Water"
{
    Properties
    {
        _MainTex ("Slurry / Water Albedo", 2D) = "white" {}
        [NoScaleOffset] _NormalTex ("Ripple Normal", 2D) = "bump" {}
        _Color ("Tint", Color) = (0.78,0.79,0.76,1)
        _Alpha ("Opacity", Range(0,1)) = 0.82
        _Smoothness ("Smoothness", Range(0,1)) = 0.62
        _Flow ("Flow (XY)", Vector) = (0.02, 0.06, 0, 0)
        _RippleStrength ("Ripple Strength", Range(0,1.5)) = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalTex;
        fixed4 _Color;
        half _Alpha;
        half _Smoothness;
        float4 _Flow;
        half _RippleStrength;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 flow = _Flow.xy * _Time.y;

            half3 n1 = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex + flow));
            half3 n2 = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex * 1.31 - flow * 0.55));
            half3 n = normalize(half3(n1.xy + n2.xy, 1.0));
            n.xy *= _RippleStrength;

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex + flow * 0.25) * _Color;
            half fres = pow(1.0 - saturate(dot(normalize(IN.viewDir), normalize(n))), 3.0);

            o.Albedo = c.rgb;
            o.Normal = n;
            o.Smoothness = saturate(_Smoothness + fres * 0.15);
            o.Metallic = 0.0;
            o.Alpha = saturate(_Alpha + fres * 0.12);
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
