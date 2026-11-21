using UnityEngine;

namespace Cishan
{
    /// <summary>Non-operable object that offers one sentence of material knowledge.</summary>
    public class Inspectable : Interactable
    {
        [TextArea] public string line = "";

        void Awake()
        {
            if (holdSeconds <= 0f) holdSeconds = 0f;
        }

        public override void OnUsed()
        {
            if (CishanGame.I != null && CishanGame.I.hud != null)
            {
                CishanGame.I.hud.ShowSubtitle(line, 3.8f);
            }
        }
    }
}
