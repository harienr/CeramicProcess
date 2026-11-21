using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 第一人称：站立视角、慢速步行、轻微头部起伏；按住右键可以凑近观察（规范 §6 镜头）。
    /// 不做冲刺、不做跳跃——这是工坊，不是跑酷。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public Camera cam;

        [Header("移动")]
        public float walkSpeed = 1.45f;
        public float accel = 12f;
        public float gravity = -18f;

        [Header("视角")]
        public float sensitivity = 2.1f;
        public float eyeHeight = 1.62f;
        public float pitchMin = -78f;
        public float pitchMax = 78f;
        public float startPitch = 14f;

        [Header("近观（按住右键）")]
        public float nearFov = 36f;
        public float wideFov = 58f;
        public float nearLean = 0.16f;

        CharacterController cc;
        float yaw, pitch;
        Vector3 velocity;
        float bobPhase;
        float bobAmount;
        float fovTarget;
        bool zoom;

        public bool Locked { get; set; }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cam == null) cam = GetComponentInChildren<Camera>();
            yaw = transform.eulerAngles.y;
            pitch = Mathf.Clamp(startPitch, pitchMin, pitchMax);
            if (cam != null)
            {
                cam.fieldOfView = wideFov;
                fovTarget = wideFov;
            }
        }

        void Start()
        {
            Lock(true);
        }

        void Lock(bool on)
        {
            Locked = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        void Update()
        {
            // 防御：Awake 未跑（例如编辑器里手动驱动）时不至于空引用
            if (cc == null)
            {
                cc = GetComponent<CharacterController>();
                if (cc == null) return;
            }
            if (cam == null) cam = GetComponentInChildren<Camera>();

            // 鼠标
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Lock(!Locked);
            }
            if (!Locked && Input.GetMouseButtonDown(0))
            {
                Lock(true);
            }

            if (Locked)
            {
                float mx = Input.GetAxisRaw("Mouse X") * sensitivity;
                float my = Input.GetAxisRaw("Mouse Y") * sensitivity;
                yaw += mx;
                pitch = Mathf.Clamp(pitch - my, pitchMin, pitchMax);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // 近观
            zoom = Input.GetMouseButton(1);
            fovTarget = zoom ? nearFov : wideFov;
            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovTarget, Time.deltaTime * 6f);
            }

            // 移动（WASD，1 单位 = 1 米）
            Vector3 input = Vector3.zero;
            if (Locked)
            {
                if (Input.GetKey(KeyCode.W)) input.z += 1f;
                if (Input.GetKey(KeyCode.S)) input.z -= 1f;
                if (Input.GetKey(KeyCode.A)) input.x -= 1f;
                if (Input.GetKey(KeyCode.D)) input.x += 1f;
            }
            if (input.sqrMagnitude > 1f) input.Normalize();

            float speed = walkSpeed * (zoom ? 0.55f : 1f) * (Input.GetKey(KeyCode.LeftShift) ? 0.55f : 1f);
            Vector3 world = transform.TransformDirection(input) * speed;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, world, accel * Time.deltaTime);
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            cc.Move(velocity * Time.deltaTime);

            // 头部起伏：只在走动时出现，幅度极小
            float planar = new Vector2(velocity.x, velocity.z).magnitude;
            bobPhase += Time.deltaTime * (4.2f + planar * 2f);
            float targetBob = planar > 0.15f ? Mathf.Sin(bobPhase * 2f) * 0.010f : 0f;
            bobAmount = Mathf.Lerp(bobAmount, targetBob, Time.deltaTime * 5f);

            if (cam != null)
            {
                Vector3 localEye = new Vector3(0f, eyeHeight + bobAmount, 0f);
                if (zoom) localEye += new Vector3(0f, -0.06f, nearLean);
                cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, localEye, Time.deltaTime * 8f);
                cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        /// <summary>把玩家放到指定位置与朝向（构建器与截图用）</summary>
        public void Teleport(Vector3 pos, float yawDeg, float pitchDeg)
        {
            if (cc != null) cc.enabled = false;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            if (cc != null) cc.enabled = true;
            yaw = yawDeg;
            pitch = Mathf.Clamp(pitchDeg, pitchMin, pitchMax);
            if (cam != null)
            {
                cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                cam.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            }
        }
    }
}
