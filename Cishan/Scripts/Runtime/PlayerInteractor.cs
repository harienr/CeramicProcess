using UnityEngine;

namespace Cishan
{
    /// <summary>Look probe: shows a verb only in range, blocks unsafe actions with a sentence.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public Camera cam;
        public CishanHUD hud;
        public float maxDistance = 3.2f;
        public LayerMask mask = ~0;

        public Interactable Focus { get; private set; }

        Interactable holding;
        float holdT;
        bool holdingActive;

        void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            // Ignore the player's own layer (2) and let triggers be probed.
            mask = Physics.DefaultRaycastLayers;
        }

        void Update()
        {
            if (cam == null) return;

            bool blocked = (CishanGame.I != null && CishanGame.I.sequence != null &&
                            CishanGame.I.sequence.Busy) || (hud != null && hud.NotebookOpen);
            if (blocked)
            {
                SetFocus(null);
                CancelHold();
                return;
            }

            Interactable found = Probe();
            if (found != Focus) SetFocus(found);
            if (Focus == null) return;

            string reason;
            bool can = Focus.CanUse(out reason);
            if (hud != null)
            {
                hud.ShowPrompt(Focus.Verb, can ? Focus.Hint : reason,
                    Focus.holdSeconds > 0.01f && can);
                hud.SetTag(Focus.stateTag, Focus.showPendingTag);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!can)
                {
                    if (hud != null) hud.ShowSubtitle(reason, 2.4f);
                }
                else if (Focus.holdSeconds <= 0.01f)
                {
                    Focus.OnUsed();
                }
                else
                {
                    holding = Focus;
                    holdT = 0f;
                    holdingActive = true;
                    Focus.OnBeginHold();
                    if (hud != null) hud.SetHold(0f);
                }
            }

            if (holdingActive)
            {
                if (holding == null || Focus != holding || !Input.GetKey(KeyCode.E))
                {
                    CancelHold();
                }
                else
                {
                    holdT += Time.deltaTime / Mathf.Max(0.01f, holding.holdSeconds);
                    holding.OnHoldProgress(Mathf.Clamp01(holdT));
                    if (hud != null) hud.SetHold(holdT);
                    if (holdT >= 1f)
                    {
                        Interactable done = holding;
                        holdingActive = false;
                        holding = null;
                        done.OnHoldProgress(1f);
                        done.OnUsed();
                        if (hud != null) hud.SetHold(1f);
                    }
                }
            }
        }

        Interactable Probe()
        {
            RaycastHit hit;
            if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out hit,
                    maxDistance, mask, QueryTriggerInteraction.Collide))
            {
                return null;
            }
            Interactable it = hit.collider.GetComponentInParent<Interactable>();
            if (it == null) return null;
            if (hit.distance > it.range) return null;
            return it;
        }

        void SetFocus(Interactable next)
        {
            if (Focus == next) return;
            if (Focus != null)
            {
                Focus.OnFocus(false);
                Focus = null;
            }
            Focus = next;
            if (Focus != null)
            {
                Focus.OnFocus(true);
                string reason;
                bool can = Focus.CanUse(out reason);
                    if (hud != null)
                {
                    hud.ShowPrompt(Focus.Verb, can ? Focus.Hint : reason,
                        Focus.holdSeconds > 0.01f && can);
                    hud.SetTag(Focus.stateTag, Focus.showPendingTag);
                    hud.SetHold(0f);
                }
            }
            else if (hud != null)
            {
                hud.HidePrompt();
                hud.SetTag("");
            }
            if (hud != null) hud.SetReticleFocus(Focus != null);
        }

        void CancelHold()
        {
            if (holding != null) holding.OnCancelHold();
            holding = null;
            holdingActive = false;
            holdT = 0f;
            if (hud != null) hud.SetHold(0f);
        }
    }
}
