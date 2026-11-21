using UnityEngine;

namespace Cishan
{
    /// <summary>
    /// Deliberate, slow first-person body. No sprint, no jump, no head-bob loop;
    /// only a small breathing offset and a 0.3 m / 45° lean while the right
    /// mouse button is held.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public Transform head;
        public float eyeHeight = WorkshopLayout.EyeHeight;
        public float walkSpeed = 1.35f;
        public float acceleration = 9f;
        public float mouseSensitivity = 2.2f;
        public float leanDistance = 0.3f;
        public float fovNormal = 58f;
        public float fovLean = 45f;
        public float leanSpeed = 3.2f;

        [Header("Audio")]
        public CishanAudio audioSourceHost;

        CharacterController controller;
        float yaw;
        float pitch;
        float smoothSpeed;
        Vector3 velocity;
        float lean01;
        float leanZ;
        float breathPhase;
        float impulse;
        float stepDistance;
        bool cursorLocked = true;
        Vector3 headLocalRest;

        public bool CursorLocked { get { return cursorLocked; } }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (head == null) head = transform.Find("Head");
            if (head != null) headLocalRest = head.localPosition;
            yaw = transform.eulerAngles.y;
            Application.targetFrameRate = 60;
        }

        void OnEnable()
        {
            LockCursor(true);
        }

        void Start()
        {
            if (head == null) return;
            head.localPosition = new Vector3(0f, eyeHeight, 0f);
            headLocalRest = head.localPosition;
        }

        public void LockCursor(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(false);
            }
            else if (!cursorLocked && Input.GetMouseButtonDown(0))
            {
                LockCursor(true);
            }

            if (cursorLocked)
            {
                float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
                float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
                yaw += mx;
                pitch = Mathf.Clamp(pitch - my, -72f, 72f);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Vector3 wish = Vector3.zero;
            if (cursorLocked)
            {
                float h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
                float v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
                wish = transform.right * h + transform.forward * v;
                if (wish.sqrMagnitude > 1f) wish.Normalize();
            }

            smoothSpeed = Mathf.Lerp(smoothSpeed, wish.magnitude, Time.deltaTime * acceleration);
            Vector3 move = wish * (walkSpeed * Mathf.Clamp01(smoothSpeed));

            if (controller.isGrounded && velocity.y < 0f) velocity.y = -1.5f;
            velocity.y += -9.81f * Time.deltaTime;

            Vector3 before = transform.position;
            controller.Move((move + velocity) * Time.deltaTime);

            if (audioSourceHost != null && wish.sqrMagnitude > 0.01f && controller.isGrounded)
            {
                stepDistance += Vector3.Distance(before, transform.position);
                if (stepDistance > 0.72f)
                {
                    stepDistance = 0f;
                    audioSourceHost.PlayFootstep(0.55f);
                }
            }

            // lean + breathing
            bool wantLean = cursorLocked && Input.GetMouseButton(1);
            lean01 = Mathf.MoveTowards(lean01, wantLean ? 1f : 0f, Time.deltaTime * leanSpeed);
            breathPhase += Time.deltaTime * 1.1f;
            float breath = Mathf.Sin(breathPhase) * 0.004f;
            impulse = Mathf.MoveTowards(impulse, 0f, Time.deltaTime * 0.05f);

            float wantLeanZ = 0f;
            if (head != null)
            {
                float allowed = leanDistance * lean01;
                RaycastHit hit;
                if (Physics.SphereCast(head.position, 0.12f, head.forward, out hit,
                        allowed + 0.12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    allowed = Mathf.Max(0f, hit.distance - 0.16f);
                }
                wantLeanZ = allowed;
                leanZ = Mathf.Lerp(leanZ, wantLeanZ, Time.deltaTime * 6f);
                head.localPosition = new Vector3(0f, eyeHeight + breath + impulse, leanZ);
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Camera cam = head != null ? head.GetComponent<Camera>() : GetComponentInChildren<Camera>();
            if (cam != null)
            {
                float wantFov = Mathf.Lerp(fovNormal, fovLean, lean01);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, wantFov, Time.deltaTime * 4f);
            }
        }

        /// <summary>Very small camera impulse for hammer impacts; never a screen shake.</summary>
        public void Nudge(float amount)
        {
            impulse -= Mathf.Abs(amount);
        }
    }
}
