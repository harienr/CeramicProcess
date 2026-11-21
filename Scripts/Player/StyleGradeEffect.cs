using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>极轻的影调统一，避免使用后期泛光，保持"朴素、安静"的画面。</summary>
    [RequireComponent(typeof(Camera))]
    public class StyleGradeEffect : MonoBehaviour
    {
        public Material gradeMaterial;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (gradeMaterial != null) Graphics.Blit(source, destination, gradeMaterial);
            else Graphics.Blit(source, destination);
        }
    }
}
