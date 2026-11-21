// 清水层：沉淀后缸面的一层薄水。半透明、低饱和、菲涅尔边缘略亮，
// 不使用镜面反射，避免"镜面泥浆 / 果冻弹性"的观感。
Shader "Porcelain/WaterSurface"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.60, 0.64, 0.62, 0.42)
        _NormalMap ("Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.30
        _Scroll ("Scroll XY", Vector) = (0.012, 0.018, 0, 0)
        _Gloss ("Gloss", Range(0, 1)) = 0.45
        _Clarity ("Clarity", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            half _NormalStrength;
            float4 _Scroll;
            half _Gloss;
            half _Clarity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NormalMap);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 运行时生成的法线为 RGBA32（非 DXT5nm），直接 xyz*2-1 解码
                half3 n1 = tex2D(_NormalMap, i.uv + _Scroll.xy * _Time.y).xyz * 2.0 - 1.0;
                half3 n2 = tex2D(_NormalMap, i.uv * 1.73 - _Scroll.xy * _Time.y * 0.65).xyz * 2.0 - 1.0;
                half3 n = normalize(half3((n1.xy + n2.xy) * _NormalStrength, 1.0));
                half3 N = normalize(half3(n.x, 1.0, n.y));

                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                half3 L = normalize(_WorldSpaceLightPos0.xyz);
                half diff = saturate(dot(N, L));
                half spec = pow(saturate(dot(reflect(-L, N), V)), lerp(12.0, 120.0, _Gloss)) * _Gloss;
                half fres = pow(1.0 - saturate(dot(N, V)), 3.0);

                fixed3 col = _Color.rgb * (0.78 + diff * 0.32);
                col += _LightColor0.rgb * (spec * 0.35 + fres * 0.20);
                // 越接近垂直越透明（能看见缸底泥层），边缘更反光
                fixed alpha = saturate(_Color.a * (1.0 - _Clarity * 0.35) + fres * 0.35);

                fixed4 outCol = fixed4(col, alpha);
                UNITY_APPLY_FOG(i.fogCoord, outCol);
                return outCol;
            }
            ENDCG
        }
    }
    FallBack Off
}
