using UnityEngine;

namespace Cishan
{
    /// <summary>Powder level inside a wooden bucket; only two fills are ever needed.</summary>
    public class BucketFill : MonoBehaviour
    {
        public Transform powder;
        public float fill;

        void Awake()
        {
            if (powder != null) powder.gameObject.SetActive(false);
        }

        public void AddFill(float amount)
        {
            SetFill(fill + amount);
        }

        public void SetFill(float amount)
        {
            fill = Mathf.Clamp01(amount);
            if (powder == null) return;
            powder.gameObject.SetActive(fill > 0.01f);
            Vector3 p = powder.localPosition;
            p.y = 0.055f + fill * 0.195f;
            powder.localPosition = p;
            powder.localRotation = Quaternion.Euler(0f, 12f * fill, 0f);
        }
    }
}
