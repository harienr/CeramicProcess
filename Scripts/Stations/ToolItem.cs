using System.Collections;
using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 可取放的工具。工具轮廓必须能让玩家一眼看出"抓握在哪、作用在哪"，
    /// 因此保持简单明确的功能造型，磨损集中在手握位置。
    /// </summary>
    public class ToolItem : Interactable
    {
        public ToolKind kind = ToolKind.Ladle;
        public string displayName = "木瓢";

        [Header("持握姿态（相对手部挂点）")]
        public Vector3 heldLocalPosition = new Vector3(0f, 0f, 0.06f);
        public Vector3 heldLocalEuler = new Vector3(8f, -12f, 0f);

        Transform _restParent;
        Transform _floorParent;
        Vector3 _restPos;
        Quaternion _restRot;
        Collider _col;
        Coroutine _tween;

        public bool IsHeld { get; private set; }
        public string DisplayName => displayName;

        protected override void Awake()
        {
            base.Awake();
            _col = GetComponent<Collider>();
        }

        public void SetRest(Transform parent)
        {
            _restParent = parent;
            _floorParent = parent != null ? parent.parent : null;
            _restPos = transform.localPosition;
            _restRot = transform.localRotation;
        }

        public override string Prompt(PlayerInteractor player)
        {
            if (player.HeldItem == this) return "已握在手中 · 右键放下";
            if (player.HeldItem != null) return "换用" + displayName;
            return "拾起" + displayName;
        }

        public override bool CanPress(PlayerInteractor player)
        {
            // 手里已有工具时允许直接换用，不必先放下
            return true;
        }

        public override void OnPrimaryDown(PlayerInteractor player)
        {
            if (player.HeldItem == this) player.DropTool();
            else player.PickUpTool(this);
        }

        public void OnEquip(PlayerInteractor player)
        {
            IsHeld = true;
            EnsureCollider();
            if (_col != null) _col.enabled = false;
            if (_tween != null) StopCoroutine(_tween);

            var socket = player.hands != null ? player.hands.socket : null;
            if (socket == null) return;

            transform.SetParent(socket, false);
            transform.localPosition = heldLocalPosition + new Vector3(0f, -0.28f, -0.05f);
            transform.localRotation = Quaternion.Euler(heldLocalEuler) * Quaternion.Euler(0f, 0f, -40f);

            _tween = StartCoroutine(TweenTo(heldLocalPosition, Quaternion.Euler(heldLocalEuler), 0.20f, null));
        }

        public void OnUnequip(PlayerInteractor player)
        {
            IsHeld = false;
            if (_tween != null) StopCoroutine(_tween);

            // 靠近原位就放回原处；走远了则就近放在脚边地面，
            // 避免出现工具"飞回工作台"的怪表现。
            Vector3 homeWorld = _restParent != null ? _restParent.TransformPoint(_restPos) : _restPos;
            bool nearHome = player == null || Vector3.Distance(homeWorld, player.transform.position) < 1.5f;

            if (!nearHome)
            {
                var parent = _floorParent != null ? _floorParent : _restParent;
                if (parent != null)
                {
                    Vector3 world = player.transform.position + player.transform.forward * 0.45f + Vector3.up * 0.035f;
                    _restParent = parent;
                    _restPos = parent.InverseTransformPoint(world);
                    _restRot = Quaternion.Euler(0f, player.transform.eulerAngles.y + 12f, 0f);
                    transform.SetParent(parent, false);
                    transform.localPosition = _restPos;
                    transform.localRotation = _restRot;
                    EnsureCollider();
                    if (_col != null) _col.enabled = true;
                    return;
                }
            }

            if (_restParent != null)
            {
                transform.SetParent(_restParent, false);
            }
            _tween = StartCoroutine(TweenTo(_restPos, _restRot, 0.24f, () =>
            {
                EnsureCollider();
                if (_col != null) _col.enabled = true;
            }));
        }

        // 碰撞体可能晚于本组件创建，取用前再确认一次
        void EnsureCollider()
        {
            if (_col == null) _col = GetComponent<Collider>();
        }

        IEnumerator TweenTo(Vector3 pos, Quaternion rot, float duration, System.Action done)
        {
            Vector3 p0 = transform.localPosition;
            Quaternion r0 = transform.localRotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, duration);
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                transform.localPosition = Vector3.Lerp(p0, pos, e);
                transform.localRotation = Quaternion.Slerp(r0, rot, e);
                yield return null;
            }
            transform.localPosition = pos;
            transform.localRotation = rot;
            _tween = null;
            done?.Invoke();
        }
    }
}
