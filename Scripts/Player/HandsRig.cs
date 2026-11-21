using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 手部挂点：负责持物时的轻微惯性摆动与步伐起伏。
    /// 工具挂在 socket 上，视觉手臂随 root 一起动。
    /// </summary>
    public class HandsRig : MonoBehaviour
    {
        public Transform root;
        public Transform socket;
        public float swayPerUnit = 0.0022f;
        public float swayLimit = 0.028f;
        public float bobDistance = 0.012f;
        public float smoothing = 8.5f;

        Vector3 _baseLocal;
        Quaternion _baseRot;
        Vector3 _sway;
        float _bobPhase;
        bool _baseCaptured;

        void Awake()
        {
            CaptureBase();
        }

        void CaptureBase()
        {
            // root 由装配器在 AddComponent 之后赋值，所以首次使用时再采样基准姿态
            if (_baseCaptured || root == null) return;
            _baseLocal = root.localPosition;
            _baseRot = root.localRotation;
            _baseCaptured = true;
        }

        public void Tick(Vector2 mouseDelta, float speed01, float dt)
        {
            if (root == null || dt <= 0f) return;
            CaptureBase();

            _bobPhase += dt * 6.8f * Mathf.Max(0.15f, speed01);

            Vector3 target = new Vector3(-mouseDelta.x, -mouseDelta.y, 0f) * swayPerUnit;
            target.x = Mathf.Clamp(target.x, -swayLimit, swayLimit);
            target.y = Mathf.Clamp(target.y, -swayLimit, swayLimit);
            _sway = Vector3.Lerp(_sway, target, 1f - Mathf.Exp(-smoothing * dt));

            Vector3 bob = new Vector3(
                Mathf.Sin(_bobPhase * 0.5f) * 0.0035f,
                Mathf.Abs(Mathf.Cos(_bobPhase)) * bobDistance * speed01,
                0f);

            root.localPosition = Vector3.Lerp(root.localPosition, _baseLocal + _sway + bob, 1f - Mathf.Exp(-smoothing * dt));

            Quaternion swayRot = Quaternion.Euler(_sway.y * 120f, -_sway.x * 120f, 0f);
            root.localRotation = Quaternion.Slerp(root.localRotation, _baseRot * swayRot, 1f - Mathf.Exp(-smoothing * dt));
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.gameObject.SetActive(visible);
        }
    }
}
