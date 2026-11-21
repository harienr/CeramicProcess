using System;
using UnityEngine;

namespace Cishan
{
    /// <summary>The 水闸: a sliding board and a lever. Opening is latched by a full pull.</summary>
    public class SluiceGate : MonoBehaviour
    {
        public Transform board;
        public Transform lever;
        public float travel = 0.62f;
        public float speed = 0.8f;
        public float openAmount;
        public bool targetOpen;

        public event Action<bool> StateChanged;

        Vector3 boardClosed;
        Quaternion leverRest;
        bool wasOpen;

        public bool IsOpen { get { return openAmount > 0.94f; } }
        public bool IsClosed { get { return openAmount < 0.05f; } }
        public float OpenAmount { get { return openAmount; } }

        void Awake()
        {
            if (board != null) boardClosed = board.localPosition;
            if (lever != null) leverRest = lever.localRotation;
        }

        public void Toggle()
        {
            SetOpen(!targetOpen);
        }

        public void SetOpen(bool open)
        {
            targetOpen = open;
        }

        public void Snap(float amount)
        {
            openAmount = Mathf.Clamp01(amount);
            targetOpen = openAmount > 0.5f;
            Apply();
        }

        void Update()
        {
            float target = targetOpen ? 1f : 0f;
            if (Mathf.Abs(openAmount - target) > 0.0001f)
            {
                openAmount = Mathf.MoveTowards(openAmount, target, speed * Time.deltaTime);
                Apply();
            }

            bool nowOpen = IsOpen;
            if (nowOpen != wasOpen)
            {
                wasOpen = nowOpen;
                if (StateChanged != null) StateChanged(nowOpen);
            }
        }

        void Apply()
        {
            if (board != null)
            {
                Vector3 p = boardClosed;
                p.y += openAmount * travel;
                board.localPosition = p;
            }
            if (lever != null)
            {
                lever.localRotation = leverRest * Quaternion.Euler(0f, 0f, -62f * openAmount);
            }
        }
    }
}
