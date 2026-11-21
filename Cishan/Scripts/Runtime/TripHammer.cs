using System;
using UnityEngine;

namespace Cishan
{
    /// <summary>
    /// One beam on one cam. The cam presses the tail down; the head rises, then
    /// falls by its own weight. A strike is emitted at the release.
    /// </summary>
    public class TripHammer : MonoBehaviour
    {
        public WaterWheelDrive drive;
        public float camOffsetDeg;
        public Transform beamPivot;
        public float restAngle = WorkshopLayout.BeamRestAngle;
        public float liftAngle = WorkshopLayout.BeamLiftAngle;
        public MortarStation mortar;
        public PowderPuff dust;

        public float CurrentLift { get; private set; }
        public event Action<TripHammer> OnStrike;

        float prevLift;

        void Update()
        {
            if (drive == null || beamPivot == null) return;

            // 停水后碓头自然落回静止位，避免任意停轮相位使取粉永久受阻。
            if (drive.IsSettled && drive.gate != null && drive.gate.IsClosed)
            {
                CurrentLift = 0f;
                prevLift = 0f;
                beamPivot.localRotation = Quaternion.Euler(0f, 0f, restAngle);
                return;
            }

            float camDeg = Mathf.Repeat(drive.AngleDeg - 90f + camOffsetDeg, 360f);
            CurrentLift = ProcMesh.CamLift01(camDeg);

            beamPivot.localRotation = Quaternion.Euler(0f, 0f, restAngle - liftAngle * CurrentLift);

            if (prevLift >= 0.10f && CurrentLift < 0.10f)
            {
                Strike();
            }
            prevLift = CurrentLift;
        }

        void Strike()
        {
            if (mortar != null) mortar.OnStrike();
            if (dust != null) dust.Puff(5);

            if (CishanGame.I != null && CishanGame.I.audio != null)
            {
                CishanGame.I.audio.PlayHammer(transform.position);
            }
            if (CishanGame.I != null && CishanGame.I.player != null)
            {
                CishanGame.I.player.Nudge(0.0022f);
            }
            if (OnStrike != null) OnStrike(this);
        }

        public bool AtRest { get { return CurrentLift < 0.04f; } }
    }
}
