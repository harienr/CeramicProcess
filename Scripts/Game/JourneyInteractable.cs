using UnityEngine;

namespace CiCongNiSheng.Journey
{
    public enum JourneyAction
    {
        RawStones, StoneSieve, Debris, CleanStones, LoadMortars, Gate, MortarA, MortarB,
        PowderVat, Water, StirVat, Coarse, ReturnCoarse, RoughSlurry, SieveInput, Sieve, Pool
    }

    public sealed class JourneyInteractable : Interactable
    {
        public JourneyWorkshop workshop;
        public JourneyAction action;
        public string label;
        public float holdDuration = 0.85f;
        public float HoldProgress { get; private set; }
        bool heldThisFrame;
        public override string Prompt(PlayerInteractor player) => workshop != null ? workshop.PromptFor(this) : label;
        public override bool CanPress(PlayerInteractor player) => workshop != null && workshop.CanUse(action);
        public override void OnPrimaryHold(PlayerInteractor player, float dt) => Hold(dt, player.MouseMotion.magnitude);

        public void Hold(float dt, float motion)
        {
            if (workshop == null || !workshop.CanUse(action)) { HoldProgress = 0f; return; }
            heldThisFrame = true;
            if (action == JourneyAction.StoneSieve) { workshop.Rinse(dt, motion); return; }
            if (action == JourneyAction.StirVat) { workshop.Stir(dt, motion); return; }
            if (action == JourneyAction.Sieve)
            {
                workshop.Sieve.st.Interact(dt, false, true);
                return;
            }
            HoldProgress = Mathf.Clamp01(HoldProgress + dt / Mathf.Max(0.1f, holdDuration));
            if (HoldProgress < 1f) return;
            HoldProgress = 0f;
            workshop.Use(action);
        }

        void LateUpdate()
        {
            if (!heldThisFrame) HoldProgress = 0f;
            heldThisFrame = false;
        }
    }
}
