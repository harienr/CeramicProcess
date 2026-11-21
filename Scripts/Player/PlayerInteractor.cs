using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 第一人称交互器：从视点发射短射线，命中可交互物件即显示提示；
    /// 左键按下 / 按住 / 抬起分发给当前物件，并把鼠标移动速度交给它们做动作强度判定。
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public Camera cam;
        public FirstPersonController controller;
        public HandsRig hands;
        public Transform cursorDecal;
        public float reach = 2.5f;

        public Interactable Hovered { get; private set; }
        public RaycastHit LastHit { get; private set; }
        public bool HasHit { get; private set; }
        public Vector2 MouseMotion { get; private set; }
        /// <summary>平滑后的鼠标速度（输入轴单位 / 秒）。</summary>
        public float MouseSpeed { get; private set; }

        public ToolItem HeldItem { get; private set; }
        public ToolKind HeldTool => HeldItem != null ? HeldItem.kind : ToolKind.None;

        Interactable _pressed;
        float _hoverT;
        float _decalT;
        float _speedSmooth;
        MaterialPropertyBlock _decalBlock;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || cam == null) return;

            MouseMotion = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            float rawSpeed = MouseMotion.magnitude / dt;
            _speedSmooth = Mathf.Lerp(_speedSmooth, rawSpeed, 1f - Mathf.Exp(-14f * dt));
            MouseSpeed = _speedSmooth;

            if (hands != null) hands.Tick(MouseMotion, controller != null ? controller.Speed01 : 0f, dt);

            UpdateAim();
            UpdateHover(dt);
            UpdateDecal(dt);
            HandleInput(dt);

            // 操作近景：悬停时轻微前倾
            if (controller != null)
            {
                controller.FocusBlend = Hovered != null ? Mathf.Clamp01(_hoverT) : 0f;
            }
        }

        void UpdateAim()
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Journey.JourneyWorkshop.Instance != null)
            {
                bool holding = Input.GetKey(KeyCode.E) || Input.GetMouseButton(0);
                if (!holding) _journeyLock = null;
                if (_journeyLock != null && _journeyLock.isActiveAndEnabled && holding &&
                    Vector3.Distance(cam.transform.position, _journeyLock.Focus.position) <= reach + 0.5f)
                {
                    Hovered = _journeyLock;
                    HasHit = true;
                    return;
                }
                _journeyLock = null;
                var hits = Physics.RaycastAll(ray, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                Hovered = null;
                HasHit = false;
                foreach (var candidate in hits)
                {
                    var target = candidate.collider.GetComponentInParent<Interactable>();
                    if (target != null && target.isActiveAndEnabled && candidate.distance <= target.maxDistance)
                    {
                        Hovered = target;
                        if (holding) _journeyLock = target;
                        LastHit = candidate;
                        HasHit = true;
                        return;
                    }
                    if (!candidate.collider.isTrigger) break;
                }
                return;
            }
            HasHit = Physics.Raycast(ray, out var hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            LastHit = hit;

            Interactable found = null;
            if (HasHit)
            {
                found = hit.collider.GetComponentInParent<Interactable>();
                if (found != null)
                {
                    if (!found.isActiveAndEnabled) found = null;
                    else if (hit.distance > found.maxDistance) found = null;
                }
            }
            Hovered = found;
        }

        void UpdateHover(float dt)
        {
            if (Hovered != null && Hovered != _pressed)
            {
                _hoverT = Mathf.Lerp(_hoverT, 1f, 1f - Mathf.Exp(-16f * dt));
            }
            else if (Hovered != null)
            {
                _hoverT = Mathf.Lerp(_hoverT, 1f, 1f - Mathf.Exp(-16f * dt));
            }
            else
            {
                _hoverT = Mathf.Lerp(_hoverT, 0f, 1f - Mathf.Exp(-16f * dt));
            }

            if (Hovered != null) Hovered.ApplyHighlight(_hoverT);
            if (_lastHovered != null && _lastHovered != Hovered)
            {
                _lastHovered.ApplyHighlight(0f);
                _lastHovered.OnHoverExit(this);
            }
            if (Hovered != null && _lastHovered != Hovered)
            {
                Hovered.OnHoverEnter(this);
            }
            _lastHovered = Hovered;
        }

        Interactable _lastHovered;
        Interactable _journeyLock;
        public void ClearInteractionLock() { _journeyLock = null; Hovered = null; _pressed = null; }

        void HandleInput(float dt)
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Input.GetMouseButtonDown(0))
            {
                _pressed = Hovered;
                if (_pressed != null && _pressed.CanPress(this)) _pressed.OnPrimaryDown(this);
            }

            if (Input.GetMouseButton(0) && Hovered != null && Hovered.CanPress(this))
            {
                Hovered.OnPrimaryHold(this, dt);
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_pressed != null) _pressed.OnPrimaryUp(this);
                _pressed = null;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Q))
            {
                if (HeldItem != null) DropTool();
                else if (Hovered != null) Hovered.OnSecondaryDown(this);
            }
        }

        void UpdateDecal(float dt)
        {
            if (cursorDecal == null) return;
            bool show = Hovered != null && HasHit;
            _decalT = Mathf.Lerp(_decalT, show ? 1f : 0f, 1f - Mathf.Exp(-18f * dt));

            if (_decalT < 0.02f)
            {
                if (cursorDecal.gameObject.activeSelf) cursorDecal.gameObject.SetActive(false);
                return;
            }
            if (!cursorDecal.gameObject.activeSelf) cursorDecal.gameObject.SetActive(true);

            Vector3 n = LastHit.normal;
            Vector3 up = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            cursorDecal.position = LastHit.point + n * 0.004f;
            cursorDecal.rotation = Quaternion.LookRotation(n, up);
            cursorDecal.localScale = Vector3.one * (0.085f * Mathf.Lerp(0.75f, 1f, _decalT));

            if (_decalBlock == null) _decalBlock = new MaterialPropertyBlock();
            var mr = cursorDecal.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.GetPropertyBlock(_decalBlock);
                _decalBlock.SetColor("_Color", Palette.WithAlpha(Palette.Cobalt, 0.8f * _decalT));
                mr.SetPropertyBlock(_decalBlock);
            }
        }

        // ---------------------------------------------------------------- 工具

        public void PickUpTool(ToolItem item)
        {
            if (item == null) return;
            if (HeldItem != null && HeldItem != item) DropTool();
            HeldItem = item;
            item.OnEquip(this);
        }

        public void DropTool()
        {
            if (HeldItem == null) return;
            var t = HeldItem;
            HeldItem = null;
            t.OnUnequip(this);
        }

        public string PromptText => Hovered != null ? Hovered.Prompt(this) : string.Empty;

        public bool PressAvailable => Hovered != null && Hovered.CanPress(this);
    }
}
