using UnityEngine;

namespace Cishan
{
    /// <summary>Raises the flume's opacity and flow with the gate, without mirror water.</summary>
    public class SluiceWater : MonoBehaviour
    {
        public SluiceGate gate;
        public Renderer surface;
        public float minAlpha = 0.22f;
        public float maxAlpha = 0.92f;

        MaterialPropertyBlock block;

        void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (surface == null) return;
            float open = gate != null ? gate.OpenAmount : 1f;
            surface.GetPropertyBlock(block);
            block.SetFloat("_Alpha", Mathf.Lerp(minAlpha, maxAlpha, open));
            block.SetVector("_Flow", new Vector4(0.015f, 0.05f + 0.22f * open, 0f, 0f));
            surface.SetPropertyBlock(block);
        }
    }
}
