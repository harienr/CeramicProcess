Shader "Porcelain/Beam"
{
    // 窗口射入的光柱：加色混合，靠边缘柔化 + 远端衰减，避免变成一块白塑料。
    Properties
    {
        _Color ("Color", Color) = (1,0.96,0.88,1)
        _Alpha ("Alpha", Range(0,1)) = 0.06
        _Edge ("Edge Softness", Range(0.5,6)) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off
        Fog { Mode Off }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Alpha;
            float _Edge;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float viewDist : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.viewDist = length(UnityWorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(i.viewDir);

                // 侧向看得多、正对着看得少 → 体积感
                float rim = pow(saturate(1.0 - abs(dot(n, v))), _Edge);

                // uv.y：0 = 窗口，1 = 落地
                float along = saturate(i.uv.y * 0.15 + 0.85) * (1.0 - i.uv.y * 0.65);

                // 贴近镜头时淡出，避免糊住画面
                float depth = i.viewDist;
                float nearFade = saturate((depth - 0.25) / 0.9);

                float a = _Alpha * rim * along * nearFade;
                return fixed4(_Color.rgb, a);
            }
            ENDCG
        }
    }
}
