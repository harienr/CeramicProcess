using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 水面涟漪：一圈环面片从中心扩散开、同时淡出。
    /// 挂在液面物体下面，跟着液面高度走；用独立材质实例控制透明度。
    /// </summary>
    public class RippleRing : MonoBehaviour
    {
        public float duration = 1.15f;
        public float startScale = 0.22f;
        public float endScale = 2.4f;
        public float startAlpha = 0.55f;

        Renderer rend;
        Material mat;
        float t = -1f;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null)
            {
                // 每个涟漪一份材质实例，才能各自淡出
                mat = new Material(rend.sharedMaterial);
                mat.name = "ripple_instance";
                rend.sharedMaterial = mat;
            }
            gameObject.SetActive(false);
        }

        /// <summary>开始一次扩散</summary>
        public void Play(float scaleMul = 1f)
        {
            if (rend == null) Awake();
            if (mat == null) return;
            if (scaleMul <= 0f) scaleMul = 1f;
            endScaleCurrent = endScale * scaleMul;
            t = 0f;
            gameObject.SetActive(true);
            transform.localScale = Vector3.one * startScale;
        }

        float endScaleCurrent = 2.4f;

        void Update()
        {
            if (t < 0f || mat == null) return;
            t += Time.deltaTime;
            float k = t / Mathf.Max(0.05f, duration);
            if (k >= 1f)
            {
                t = -1f;
                gameObject.SetActive(false);
                return;
            }
            // 先快后慢地散开，像水面
            float e = Mathf.Sqrt(k);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScaleCurrent, e);
            Color c = mat.color;
            c.a = Mathf.Lerp(startAlpha, 0f, k * k);
            mat.color = c;
        }
    }
}
