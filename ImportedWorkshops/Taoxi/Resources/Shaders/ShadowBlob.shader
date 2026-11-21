Shader "PorcelainWorkshop/ShadowBlob"
{
    Properties
    {
        _Color ("Color", Color) = (0.10,0.085,0.07,1)
        _Strength ("Strength", Range(0,1)) = 0.35
        _Softness ("Softness", Range(0.01,1)) = 0.55
        _Noise ("Noise", Range(0,1)) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Strength;
            half _Softness;
            half _Noise;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = i.uv * 2.0 - 1.0;
                float r = length(d);
                float edge = 1.0 - smoothstep(1.0 - _Softness, 1.0, r);
                edge *= edge;
                float n = hash21(floor(i.uv * 38.0));
                edge *= lerp(1.0, 0.82 + n * 0.3, _Noise);
                return fixed4(_Color.rgb, edge * _Strength);
            }
            ENDCG
        }
    }
    FallBack Off
}
