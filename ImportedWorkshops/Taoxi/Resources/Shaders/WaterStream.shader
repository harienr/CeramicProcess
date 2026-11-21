Shader "PorcelainWorkshop/WaterStream"
{
    Properties
    {
        _Color ("Color", Color) = (0.82,0.84,0.80,1)
        _Alpha ("Alpha", Range(0,1)) = 0.55
        _Speed ("Speed", Float) = 1.2
        _NoiseScale ("Noise Scale", Float) = 9
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Alpha;
            half _Speed;
            half _NoiseScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float streak = sin((uv.y * _NoiseScale - _Time.y * _Speed) * 6.2831 + uv.x * 13.0) * 0.5 + 0.5;
                float n = hash21(floor(float2(uv.x * 18, uv.y * 44 - _Time.y * 6)));
                float edge = pow(saturate(1.0 - abs(dot(normalize(i.worldNormal), normalize(i.viewDir)))), 0.35);
                float a = _Alpha * (0.55 + streak * 0.25 + n * 0.2) * lerp(0.55, 1.0, edge);
                float3 c = _Color.rgb * (0.86 + streak * 0.14);
                return fixed4(c, saturate(a));
            }
            ENDCG
        }
    }
    FallBack Off
}
