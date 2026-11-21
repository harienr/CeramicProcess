using UnityEngine;

namespace Cishan
{
    /// <summary>
    /// Base class for anything the player can look at and operate. Verbs are short
    /// statements ("拉起闸板"), never game prompts like "按 E 继续".
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Prompt")]
        public string verb = "操作";
        [TextArea] public string hint = "";
        [Tooltip("How far the prompt is readable, in metres.")]
        public float range = 2.4f;
        [Tooltip("0 = single press, >0 = hold for this many seconds.")]
        public float holdSeconds = 0f;

        [Tooltip("Observation tag, e.g. 石粉 · 干燥. Shown at the screen edge only.")]
        public string stateTag = "";
        public bool showPendingTag = false;

        public virtual bool CanUse(out string reason)
        {
            reason = null;
            return true;
        }

        public virtual string Verb { get { return verb; } }
        public virtual string Hint { get { return hint; } }

        public virtual void OnFocus(bool focused) { }
        public virtual void OnBeginHold() { }
        public virtual void OnHoldProgress(float t) { }
        public virtual void OnCancelHold() { }
        public virtual void OnUsed() { }
    }
}
