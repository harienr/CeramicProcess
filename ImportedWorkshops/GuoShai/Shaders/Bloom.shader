Shader "Porcelain/Bloom"
{
    // 亮度提取 + 分离式高斯模糊，两用：
    // _Mode 0 = 提亮（阈值），_Mode 1 = 沿 _Dir 模糊
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Threshold;
            float _Knee;
            float2 _Dir;
            float _Mode;

            half3 Sample(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                if (_Mode < 0.5)
                {
                    // 软阈值：只留下窗、天光这类高光
                    half3 c = Sample(uv);
                    half l = max(c.r, max(c.g, c.b));
                    half soft = saturate((l - _Threshold) / max(_Knee, 1e-4));
                    return fixed4(c * soft * soft, 1.0);
                }

                // 9 抽样高斯
                float2 d = _Dir * _MainTex_TexelSize.xy;
                half3 s = Sample(uv) * 0.2270270;
                s += (Sample(uv + d * 1.3846154) + Sample(uv - d * 1.3846154)) * 0.3162162;
                s += (Sample(uv + d * 3.2307692) + Sample(uv - d * 3.2307692)) * 0.0702703;
                return fixed4(s, 1.0);
            }
            ENDCG
        }
    }
}
