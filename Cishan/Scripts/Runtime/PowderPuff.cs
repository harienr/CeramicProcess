using UnityEngine;

namespace Cishan
{
    /// <summary>A small, bounded burst of stone dust. Never a continuous emitter.</summary>
    public class PowderPuff : MonoBehaviour
    {
        public ParticleSystem system;

        void Awake()
        {
            if (system == null) system = GetComponent<ParticleSystem>();
        }

        public void Puff(int count)
        {
            if (system == null) return;
            system.Emit(Mathf.Max(0, count));
        }
    }
}
