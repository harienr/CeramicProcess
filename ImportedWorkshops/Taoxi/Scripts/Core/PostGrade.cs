using UnityEngine;

namespace PorcelainFromClay
{
    [RequireComponent(typeof(Camera))]
    public sealed class PostGrade : MonoBehaviour
    {
        public Material GradeMaterial;
        public float Exposure = 1.05f;
        public float Saturation = 0.9f;
        public float Contrast = 1.06f;
        public float Warmth = 0.35f;
        public float Vignette = 0.14f;
        public float Grain = 0.014f;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (GradeMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }
            GradeMaterial.SetFloat("_Exposure", Exposure);
            GradeMaterial.SetFloat("_Saturation", Saturation);
            GradeMaterial.SetFloat("_Contrast", Contrast);
            GradeMaterial.SetFloat("_Warmth", Warmth);
            GradeMaterial.SetFloat("_Vignette", Vignette);
            GradeMaterial.SetFloat("_Grain", Grain);
            Graphics.Blit(source, destination, GradeMaterial);
        }
    }
}
