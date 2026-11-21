using System.Collections.Generic;
using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 交互：就近 + 朝向优先选取可操作对象，避免射线打偏（工坊里的操作面比较拥挤）。
    /// 按住 E 持续操作，R 刮渣，提示文字来自对象自己。
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public Camera cam;
        public Transform head;
        public List<MonoBehaviour> targets = new List<MonoBehaviour>();

        public IInteractable Target { get; private set; }
        public string Prompt { get { return Target != null ? Target.Prompt : null; } }
        public bool HasTarget { get { return Target != null; } }

        IInteractable ticking;
        // 工坊里的操作大多要低头看手边，视角判据给宽容一点
        float maxAngle = 58f;

        void Update()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (head == null) head = cam != null ? cam.transform : transform;

            IInteractable best = null;
            float bestScore = float.MaxValue;
            Vector3 eye = head.position;
            Vector3 fwd = head.forward;

            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i] as IInteractable;
                if (t == null) continue;
                Vector3 to = t.Point.position - eye;
                float dist = to.magnitude;
                if (dist > t.Radius + 1.1f) continue;
                Vector3 dir = to / Mathf.Max(0.0001f, dist);
                float ang = Vector3.Angle(fwd, dir);
                if (ang > maxAngle && dist > 0.9f) continue;
                float score = dist + ang * 0.02f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            // 正在操作的对象优先，避免手一抖就丢焦点
            if (ticking != null && ticking.Busy)
            {
                best = ticking;
            }

            Target = best;

            bool down = Input.GetKeyDown(KeyCode.E);
            bool held = Input.GetKey(KeyCode.E);

            if (best != null)
            {
                best.Interact(Time.deltaTime, down, held);
                if (down || held) ticking = best;
            }
            else if (ticking != null && !ticking.Busy)
            {
                ticking = null;
            }
            if (!held && ticking != null && !ticking.Busy) ticking = null;

            if (Input.GetKeyDown(KeyCode.R))
            {
                var st = best as SievingStation;
                if (st != null) st.RequestScrape();
            }
        }

        public void Register(MonoBehaviour mb)
        {
            if (mb == null) return;
            if (!targets.Contains(mb)) targets.Add(mb);
        }
    }
}
