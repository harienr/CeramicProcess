Shader "PorcelainWorkshop/WindowSky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.72,0.75,0.75,1)
        _BottomColor ("Bottom", Color) = (1.0,0.93,0.80,1)
        _Intensity ("Intensity", Range(0,4)) = 1.15
        _Cloud ("Cloud", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _BottomColor;
            half _Intensity;
            half _Cloud;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float grad = saturate(i.uv.y);
                float3 c = lerp(_BottomColor.rgb, _TopColor.rgb, grad);
                float cloud = noise(i.uv * float2(3.0, 5.0)) * 0.6 + noise(i.uv * float2(7.0, 11.0)) * 0.4;
                c += (cloud - 0.5) * _Cloud * 0.08;

                // A hazy courtyard wall and the underside of an eave give the opening depth.
                float wall = 1.0 - smoothstep(0.2, 0.46, grad);
                c = lerp(c, float3(0.48, 0.47, 0.43), wall * 0.55);
                float eave = smoothstep(0.82, 0.9, grad);
                c = lerp(c, float3(0.16, 0.14, 0.11), eave * 0.8);
                return fixed4(c * _Intensity, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
