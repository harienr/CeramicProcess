using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 料缸：装待筛的灰白泥浆。
    /// 按住 E 是“一瓢一瓢地舀”——每舀一下：木瓢探进缸里、液面泛涟漪、溅起水花、液面下降一点。
    /// 浆被取走后要回石槽重新取浆，材料流向可读（规范 §2、§4）。
    /// </summary>
    public class SlurryTub : MonoBehaviour, IInteractable
    {
        public float Fill01 = 1f;
        public float refillSeconds = 1.9f;
        public Transform level;
        public RippleRing ripple;
        public ParticleSystem splash;
        public SievingStation station;     // 让木瓢跟着做舀的动作

        float held;
        float dipTimer;
        bool dipping;

        const float DipInterval = 0.62f;

        public bool Empty { get { return Fill01 <= 0.02f; } }
        public bool Busy { get { return false; } }
        public Transform Point { get { return transform; } }
        public float Radius { get { return 1.1f; } }

        public string Prompt
        {
            get
            {
                if (Fill01 > 0.98f) return null;
                return "按住 E｜从石槽取浆";
            }
        }

        public void Consume(float amount)
        {
            Fill01 = Mathf.Clamp01(Fill01 - amount);
        }

        public void Interact(float dt, bool pressed, bool heldNow)
        {
            held = (heldNow || pressed) ? 1f : 0f;
        }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            if (held > 0.5f && Fill01 < 1f)
            {
                Fill01 = Mathf.Min(1f, Fill01 + dt / Mathf.Max(0.1f, refillSeconds));

                // 一瓢一瓢：每 0.62 秒舀一下
                dipTimer -= dt;
                if (dipTimer <= 0f)
                {
                    dipTimer = DipInterval;
                    Scoop();
                }
            }
            else
            {
                dipTimer = 0.02f;
            }

            // 液面高度（取浆时下去，取满了慢慢回来）
            if (level != null)
            {
                float y = Mathf.Lerp(0.055f, 0.335f, Fill01);
                Vector3 p = level.localPosition;
                level.localPosition = new Vector3(p.x, y, p.z);
            }

            held = 0f;
        }

        void Scoop()
        {
            if (ripple != null) ripple.Play(0.85f);
            if (splash != null) splash.Emit(9);
            if (station != null) station.PlayDip();
        }
    }
}
