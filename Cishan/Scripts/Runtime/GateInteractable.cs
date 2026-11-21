using UnityEngine;

namespace Cishan
{
    public class GateInteractable : Interactable
    {
        public SluiceGate gate;

        void Awake()
        {
            range = 2.3f;
            holdSeconds = 1.05f;
            stateTag = "闸板 · 木板与铁箍";
        }

        public override string Verb
        {
            get { return gate != null && gate.IsOpen ? "放下闸板" : "拉起闸板"; }
        }

        public override string Hint
        {
            get
            {
                return gate != null && gate.IsOpen
                    ? "水轮会慢慢停下"
                    : "轮轴启动后，退到护栏外";
            }
        }

        public override void OnUsed()
        {
            if (gate == null) return;
            gate.Toggle();
            if (CishanGame.I != null && CishanGame.I.audio != null) CishanGame.I.audio.PlayGate();
        }
    }
}
