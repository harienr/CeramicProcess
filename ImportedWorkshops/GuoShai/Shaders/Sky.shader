Shader "Porcelain/Sky"
{
    // 棚外天光：亮但不刺眼，带一点暖色，避免整幅泛黄。
    Properties
    {
        _Top ("Top", Color) = (0.86,0.87,0.86,1)
        _Bottom ("Bottom", Color) = (0.62,0.61,0.57,1)
        _SunColor ("Sun Color", Color) = (1.0,0.97,0.90,1)
        _SunDir ("Sun Dir", Vector) = (-0.5,0.6,0.3,0)
        _SunSize ("Sun Size", Range(1,64)) = 8
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Top;
            fixed4 _Bottom;
            fixed4 _SunColor;
            float4 _SunDir;
            float _SunSize;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = saturate(d.y * 1.25 + 0.15);
                half3 c = lerp(_Bottom.rgb, _Top.rgb, h);

                // 太阳方向的一团柔光
                float k = saturate(dot(d, normalize(_SunDir.xyz)));
                c += _SunColor.rgb * pow(k, _SunSize) * 0.45;
                // 地平线略微变亮（棚外天光从低处进来）
                c += _Bottom.rgb * pow(saturate(1.0 - abs(d.y)), 6.0) * 0.12;

                return fixed4(c, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
