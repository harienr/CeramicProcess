using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 调色 + 泛光（Built-in 管线，零依赖）。
    /// 顺序：锐化 → 亮度提取 → 两次高斯 → 合成 → ACES 曝光 → 低饱和 → S 曲线 → 抬黑位 → 暗角 → 颗粒。
    /// 泛光只给窗口与天光这类高光，强度很低（规范：避免强烈泛光）。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class PostFX : MonoBehaviour
    {
        public Shader shader;
        public Shader bloomShader;

        [Header("影调")]
        public float exposure = 0.93f;
        public float saturation = 0.92f;
        public float contrast = 1.07f;
        public float lift = 0.020f;
        public float vignette = 0.32f;
        public float grain = 0.008f;
        [Range(0f, 2f)] public float sharpen = 0.30f;

        [Header("泛光（克制）")]
        [Range(0f, 1f)] public float bloomStrength = 0.16f;
        public float bloomThreshold = 0.82f;
        public float bloomKnee = 0.35f;

        Material mat;
        Material bloomMat;
        const int Downsample = 2;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (shader == null) shader = Shader.Find("Porcelain/PostFX");
            if (shader == null || !shader.isSupported)
            {
                Graphics.Blit(src, dst);
                return;
            }
            if (mat == null)
            {
                mat = new Material(shader);
                mat.hideFlags = HideFlags.HideAndDontSave;
            }

            RenderTexture bright = null;
            if (bloomShader == null) bloomShader = Shader.Find("Porcelain/Bloom");
            if (bloomShader != null && bloomShader.isSupported && bloomStrength > 0.001f)
            {
                if (bloomMat == null)
                {
                    bloomMat = new Material(bloomShader);
                    bloomMat.hideFlags = HideFlags.HideAndDontSave;
                }
                int w = Mathf.Max(2, src.width / Downsample);
                int h = Mathf.Max(2, src.height / Downsample);

                bright = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
                var tmp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);

                // 1) 亮度提取
                bloomMat.SetFloat("_Mode", 0f);
                bloomMat.SetFloat("_Threshold", bloomThreshold);
                bloomMat.SetFloat("_Knee", bloomKnee);
                Graphics.Blit(src, bright, bloomMat);

                // 2) 横 + 竖
                bloomMat.SetFloat("_Mode", 1f);
                bloomMat.SetVector("_Dir", new Vector4(1f, 0f, 0f, 0f));
                Graphics.Blit(bright, tmp, bloomMat);
                bloomMat.SetVector("_Dir", new Vector4(0f, 1f, 0f, 0f));
                Graphics.Blit(tmp, bright, bloomMat);

                RenderTexture.ReleaseTemporary(tmp);
            }

            float aspect = src.height > 0 ? (float)src.width / src.height : 1.7778f;
            mat.SetFloat("_Exposure", exposure);
            mat.SetFloat("_Saturation", saturation);
            mat.SetFloat("_Contrast", contrast);
            mat.SetFloat("_Lift", lift);
            mat.SetFloat("_Vignette", vignette);
            mat.SetFloat("_Grain", grain);
            mat.SetFloat("_Sharpen", sharpen * 0.45f); // 着色器里乘了 0.25 的模糊差权重
            mat.SetFloat("_Aspect", aspect);
            mat.SetFloat("_FrameTime", (Time.realtimeSinceStartup % 100f) * 13.7f);
            mat.SetFloat("_Bloom", bright != null ? bloomStrength : 0f);
            if (bright != null) mat.SetTexture("_BloomTex", bright);

            Graphics.Blit(src, dst, mat);

            if (bright != null) RenderTexture.ReleaseTemporary(bright);
        }

        void OnDisable()
        {
            if (mat != null)
            {
                if (Application.isPlaying) Destroy(mat); else DestroyImmediate(mat);
                mat = null;
            }
            if (bloomMat != null)
            {
                if (Application.isPlaying) Destroy(bloomMat); else DestroyImmediate(bloomMat);
                bloomMat = null;
            }
        }
    }
}
