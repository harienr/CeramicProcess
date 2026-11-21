using UnityEngine;

namespace PorcelainFromClay
{
    public sealed class FirstPersonController : MonoBehaviour
    {
        public Transform CameraPivot;
        public float EyeHeight = 1.62f;
        public float MouseSensitivity = 2.1f;
        public float WalkSpeed = 1.25f;
        public float BobAmount = 0.012f;
        public float BobFrequency = 7.0f;

        public bool InputEnabled = false;
        public bool LookEnabled = true;

        CharacterController controller;
        float yaw;
        float pitch;
        float bobPhase;
        float smoothSpeed;
        Vector3 baseCameraLocal;
        float shake;
        float shakeTime;

        public float Yaw
        {
            get { return yaw; }
            set { yaw = value; }
        }

        public float Pitch
        {
            get { return pitch; }
            set { pitch = Mathf.Clamp(value, -72f, 72f); }
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (CameraPivot != null) baseCameraLocal = CameraPivot.localPosition;
        }

        public void SetCameraPivot(Transform pivot)
        {
            CameraPivot = pivot;
            baseCameraLocal = pivot != null ? pivot.localPosition : new Vector3(0f, EyeHeight, 0f);
        }

        void Start()
        {
            if (baseCameraLocal == Vector3.zero && CameraPivot != null)
            {
                baseCameraLocal = CameraPivot.localPosition;
            }
        }

        public void Teleport(Vector3 position, float newYaw, float newPitch)
        {
            if (controller != null)
            {
                controller.enabled = false;
                transform.position = position;
                controller.enabled = true;
            }
            else
            {
                transform.position = position;
            }
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, -72f, 72f);
            ApplyRotation();
        }

        public void AddCameraShake(float amount)
        {
            shake = Mathf.Max(shake, amount);
            shakeTime = 0f;
        }

        void Update()
        {
            if (CameraPivot == null) return;

            if (InputEnabled && LookEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                float mx = Input.GetAxisRaw("Mouse X") * MouseSensitivity;
                float my = Input.GetAxisRaw("Mouse Y") * MouseSensitivity;
                yaw += mx;
                pitch = Mathf.Clamp(pitch - my, -72f, 72f);
            }
            ApplyRotation();

            Vector3 move = Vector3.zero;
            if (InputEnabled)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector3 fwd = transform.forward;
                Vector3 right = transform.right;
                fwd.y = 0f;
                right.y = 0f;
                fwd.Normalize();
                right.Normalize();
                move = (fwd * v + right * h);
                if (move.sqrMagnitude > 1f) move.Normalize();
            }

            float targetSpeed = move.magnitude * WalkSpeed;
            smoothSpeed = Mathf.Lerp(smoothSpeed, targetSpeed, Time.deltaTime * 9f);
            if (controller != null && controller.enabled)
            {
                Vector3 motion = move * WalkSpeed;
                motion.y = -2.2f;
                controller.Move(motion * Time.deltaTime);
            }

            float bobTarget = 0f;
            if (smoothSpeed > 0.05f && controller != null && controller.isGrounded)
            {
                bobPhase += Time.deltaTime * BobFrequency * Mathf.Clamp(smoothSpeed / WalkSpeed, 0.4f, 1.2f);
                bobTarget = Mathf.Sin(bobPhase) * BobAmount;
            }
            else
            {
                bobPhase = Mathf.Lerp(bobPhase, Mathf.Round(bobPhase / Mathf.PI) * Mathf.PI, Time.deltaTime * 6f);
            }

            if (shake > 0.0001f)
            {
                shakeTime += Time.deltaTime;
                shake = Mathf.Lerp(shake, 0f, Time.deltaTime * 7f);
            }
            float shakeOffset = Mathf.Sin(shakeTime * 41f) * shake;

            Vector3 targetLocal = baseCameraLocal + new Vector3(0f, bobTarget + shakeOffset, 0f);
            CameraPivot.localPosition = Vector3.Lerp(CameraPivot.localPosition, targetLocal, Time.deltaTime * 16f);
        }

        void ApplyRotation()
        {
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void SetMouseLook(bool enabled)
        {
            LookEnabled = enabled;
        }

        public bool IsMoving
        {
            get { return smoothSpeed > 0.08f; }
        }

        public bool IsGrounded
        {
            get { return controller != null && controller.isGrounded; }
        }
    }
}
