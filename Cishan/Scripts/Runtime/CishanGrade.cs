using UnityEngine;

namespace Cishan
{
    [RequireComponent(typeof(Camera))]
    public class CishanGrade : MonoBehaviour
    {
        public Shader gradeShader;
        Material material;

        void Awake()
        {
            if (gradeShader == null) gradeShader = Shader.Find("Cishan/Grade");
            if (gradeShader != null && gradeShader.isSupported)
            {
                material = new Material(gradeShader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material != null) Graphics.Blit(source, destination, material);
            else Graphics.Blit(source, destination);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
