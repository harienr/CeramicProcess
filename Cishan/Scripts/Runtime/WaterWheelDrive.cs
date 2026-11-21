using UnityEngine;

namespace Cishan
{
    /// <summary>Wheel + axle + cams: one rotation drives both hammers.</summary>
    public class WaterWheelDrive : MonoBehaviour
    {
        public SluiceGate gate;
        public Transform axleRoot;
        public float maxRpm = 18f;
        public float acceleration = 5f;
        public float deceleration = 4f;

        public float Rpm { get; private set; }
        public float AngleDeg { get; private set; }

        public float Speed01 { get { return Mathf.Clamp01(Rpm / Mathf.Max(0.01f, maxRpm)); } }
        public bool IsSettled { get { return Rpm < 0.25f; } }

        void Update()
        {
            float open = gate != null ? gate.OpenAmount : 1f;
            float target = Mathf.SmoothStep(0f, 1f, open) * maxRpm;
            float rate = target > Rpm ? acceleration : deceleration;
            Rpm = Mathf.MoveTowards(Rpm, target, rate * Time.deltaTime);
            AngleDeg = Mathf.Repeat(AngleDeg + Rpm * 6f * Time.deltaTime, 360f);

            if (axleRoot != null)
            {
                axleRoot.localRotation = Quaternion.Euler(0f, 0f, -AngleDeg);
            }
        }
    }
}
