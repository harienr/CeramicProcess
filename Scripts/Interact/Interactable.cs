using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 可交互物件的基类。提示文字保持"短而具体"，例如"将容器放到承接位置"。
    /// 悬停反馈采用低强度自发光 + 世界空间细环，不做霓虹描边。
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Header("交互")]
        public float maxDistance = 2.3f;
        public Transform focusPoint;

        [Tooltip("留空则在 Awake 时自动收集子物体渲染器")]
        public Renderer[] highlightRenderers;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock _mpb;
        float _lastHighlight = -1f;

        public virtual Transform Focus => focusPoint != null ? focusPoint : transform;

        protected virtual void Awake()
        {
            CollectRenderers();
        }

        protected void CollectRenderers()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        /// <summary>当前应显示的提示。返回空串表示不提示。</summary>
        public abstract string Prompt(PlayerInteractor player);

        /// <summary>按左键是否真的能触发（工具不对时返回 false，但仍显示提示）。</summary>
        public virtual bool CanPress(PlayerInteractor player) => true;

        public virtual void OnHoverEnter(PlayerInteractor player) { }
        public virtual void OnHoverExit(PlayerInteractor player) { }
        public virtual void OnPrimaryDown(PlayerInteractor player) { }
        public virtual void OnPrimaryHold(PlayerInteractor player, float dt) { }
        public virtual void OnPrimaryUp(PlayerInteractor player) { }
        public virtual void OnSecondaryDown(PlayerInteractor player) { }

        /// <summary>由交互器每帧驱动，t 为 0-1 的悬停强度。</summary>
        public void ApplyHighlight(float t)
        {
            if (highlightRenderers == null) return;
            t = Mathf.Clamp01(t);
            if (Mathf.Abs(t - _lastHighlight) < 0.01f) return;
            _lastHighlight = t;

            Color c = Color.Lerp(Color.black, Palette.Cobalt * 0.16f, t);
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                var r = highlightRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionId, c);
                r.SetPropertyBlock(_mpb);
            }
        }

        protected static void SetMaterialsHighlightable(params Renderer[] renderers)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (!mats[i].IsKeywordEnabled("_EMISSION"))
                    {
                        mats[i] = StyleMaterials.MakeHighlightable(mats[i], r.name);
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }
    }
}
