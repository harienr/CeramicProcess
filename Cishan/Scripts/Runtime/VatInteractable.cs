using UnityEngine;

namespace Cishan
{
    public class VatInteractable : Interactable
    {
        public string vatName = "淘洗池";

        void Awake()
        {
            range = 2.4f;
            holdSeconds = 0f;
            stateTag = "粗浆 · 湿润";
            showPendingTag = true;
        }

        public override string Verb
        {
            get { return vatName; }
        }

        public override string Hint
        {
            get { return "倒入石粉，加水，用木耙搅洗"; }
        }

        public override void OnUsed()
        {
            if (CishanGame.I != null && CishanGame.I.sequence != null)
            {
                CishanGame.I.sequence.Deliver();
            }
        }
    }
}
