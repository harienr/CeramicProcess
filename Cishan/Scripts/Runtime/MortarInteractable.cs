using UnityEngine;

namespace Cishan
{
    public class MortarInteractable : Interactable
    {
        public MortarStation station;

        void Awake()
        {
            range = 2.15f;
            holdSeconds = 0f;
        }

        void Update()
        {
            if (station == null) return;
            if (station.Scooped) stateTag = "石臼 · 已取粉";
            else if (station.IsRunning) stateTag = "石粉 · 舂击中";
            else if (station.Fill01 > 0.95f) stateTag = "石粉 · 细 · 干燥";
            else if (station.Fill01 > 0.05f) stateTag = "石粉 · 粗 · 干燥";
            else stateTag = "净石 · 待舂";
        }

        public override string Verb
        {
            get { return station != null && station.Scooped ? "已取粉" : "舀粉"; }
        }

        public override string Hint
        {
            get { return "木瓢舀入桶，送去 07 淘洗"; }
        }

        public override bool CanUse(out string reason)
        {
            if (CishanGame.I == null || CishanGame.I.sequence == null)
            {
                reason = "";
                return false;
            }
            return CishanGame.I.sequence.CanScoop(station, out reason);
        }

        public override void OnUsed()
        {
            if (CishanGame.I != null) CishanGame.I.sequence.Scoop(station);
        }
    }
}
