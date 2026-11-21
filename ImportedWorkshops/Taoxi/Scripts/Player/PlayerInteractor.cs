using UnityEngine;

namespace PorcelainFromClay
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public Camera Cam;
        public WorkshopUI UI;
        public TaoxiSequence Sequence;
        public float Reach = 2.35f;
        public float SphereRadius = 0.07f;

        Interactable current;
        string currentPrompt;
        bool currentAvailable;

        void Update()
        {
            if (Sequence == null || UI == null || Cam == null) return;

            if (!Sequence.CanLookForInteractions)
            {
                SetHover(null, null);
                UI.SetPrompt(null, false);
                return;
            }

            Interactable found = null;
            string prompt = null;
            bool available = false;

            Ray ray = new Ray(Cam.transform.position, Cam.transform.forward);
            RaycastHit hit;
            if (Physics.SphereCast(ray, SphereRadius, out hit, Reach, ~(1 << 8), QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponentInParent<Interactable>();
                if (found != null)
                {
                    prompt = Sequence.GetPrompt(found);
                    available = !string.IsNullOrEmpty(prompt);
                    if (!available) found = null;
                }
            }

            SetHover(found, prompt);
            UI.SetPrompt(prompt, available);

            if (available && found != null)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
                {
                    Sequence.Activate(found);
                }
                else if (Input.GetMouseButtonDown(0) && Sequence.AllowPrimaryClick)
                {
                    Sequence.Activate(found);
                }
            }
        }

        void SetHover(Interactable target, string prompt)
        {
            if (target != current)
            {
                current = target;
                if (current != null)
                {
                    UI.SetHoverLabel(prompt);
                }
            }
            currentPrompt = prompt;
            currentAvailable = target != null;
            UI.SetHoverActive(currentAvailable);
        }
    }
}
