using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 第一人称移动：真实步速（约 1.45 m/s），轻微步伐起伏，
    /// 不使用冲刺和大幅镜头晃动，保持"安静、专注"的观感。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public Transform cameraPivot;
        public float walkSpeed = 1.45f;
        public float slowSpeed = 0.78f;
        public float acceleration = 11f;
        public float mouseSensitivity = 2.1f;
        public float pitchLimit = 82f;
        public float bobAmount = 0.011f;
        public float bobFrequency = 1.55f;
        public float eyeHeight = 1.62f;

        CharacterController _cc;
        float _yaw;
        float _pitch;
        float _verticalVelocity;
        Vector3 _planarVelocity;
        Vector3 _pivotBase;
        bool _pivotBaseCaptured;
        float _bobPhase;
        float _focusBlend;
        float _lean;

        public float Speed01 { get; private set; }
        public bool InputEnabled = true;
        public bool StabilizeForInteraction;

        public void SetViewAngles(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
            _verticalVelocity = 0f;
            _planarVelocity = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
        /// <summary>操作近景时的轻微呼吸感：0 站立，1 操作。</summary>
        public float FocusBlend
        {
            get => _focusBlend;
            set => _focusBlend = Mathf.Clamp01(value);
        }

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cc.height = 1.72f;
            _cc.radius = 0.28f;
            _cc.center = new Vector3(0f, 0.86f, 0f);
            _cc.slopeLimit = 50f;
            _cc.stepOffset = 0.25f;
            _yaw = transform.eulerAngles.y;
        }

        void Start()
        {
            // cameraPivot 由装配器在 AddComponent 之后赋值，因此基准位置在 Start 采样
            if (cameraPivot != null && !_pivotBaseCaptured)
            {
                _pivotBase = cameraPivot.localPosition;
                _pivotBaseCaptured = true;
            }
            if (!InputEnabled) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (!InputEnabled) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                bool overUI = es != null && es.IsPointerOverGameObject();
                if (!overUI)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (Cursor.lockState == CursorLockMode.Locked && !StabilizeForInteraction)
            {
                float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
                float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
                _yaw += mx;
                _pitch = Mathf.Clamp(_pitch - my, -pitchLimit, pitchLimit);
                transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            }

            // 操作近景时略微前倾并稳定视线，避免大幅位移带来的晕眩
            _lean = Mathf.Lerp(_lean, _focusBlend * 0.032f, 1f - Mathf.Exp(-6f * dt));
            _bobPhase += dt * bobFrequency * Mathf.PI * 2f * Mathf.Max(0.25f, Speed01);
            float bob = Mathf.Sin(_bobPhase) * bobAmount * Speed01;

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                cameraPivot.localPosition = _pivotBase + new Vector3(0f, bob, _lean);
            }

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);
            bool slow = Input.GetKey(KeyCode.LeftShift);
            float speed = slow ? slowSpeed : walkSpeed;
            if (Cursor.lockState != CursorLockMode.Locked) input = Vector2.zero;

            Vector3 desired = (transform.right * input.x + transform.forward * input.y) * speed;
            _planarVelocity = Vector3.Lerp(_planarVelocity, desired, 1f - Mathf.Exp(-acceleration * dt));

            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -1f;
            _verticalVelocity -= 9.81f * dt;

            Vector3 motion = _planarVelocity + Vector3.up * _verticalVelocity;
            _cc.Move(motion * dt);

            Speed01 = Mathf.Clamp01(new Vector2(_cc.velocity.x, _cc.velocity.z).magnitude / Mathf.Max(0.01f, walkSpeed));
        }
    }
}
