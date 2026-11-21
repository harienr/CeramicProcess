using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 沉淀缸：承接筛下的细浆，静置后分层，再用撇水勺撇去上层清水。
    /// 静置过程用明确标注的时间跳转表达，不做假装实时的等待。
    /// </summary>
    public class SettlingVat : Interactable
    {
        public Transform fineSurface;
        public Transform waterSurface;
        public float fineLowY = 0.12f;
        public float fineHighY = 0.48f;
        public float fineFullAmount = 0.72f;
        public float waterLayerScale = 0.30f;

        SieveWashProcess Proc => SieveWashProcess.I;

        protected override void Awake()
        {
            base.Awake();
            SetMaterialsHighlightable(GetComponentsInChildren<Renderer>());
        }

        void Update()
        {
            var p = Proc;
            if (p == null) return;

            float fineY = Mathf.Lerp(fineLowY, fineHighY, Mathf.Clamp01(p.vatFine / Mathf.Max(0.01f, fineFullAmount)));

            if (fineSurface != null)
            {
                var lp = fineSurface.localPosition;
                fineSurface.localPosition = new Vector3(lp.x, fineY, lp.z);
                fineSurface.gameObject.SetActive(p.vatFine > 0.008f);
            }

            if (waterSurface != null)
            {
                bool show = p.settled && p.vatWater > 0.004f;
                waterSurface.gameObject.SetActive(show);
                if (show)
                {
                    var lp = waterSurface.localPosition;
                    waterSurface.localPosition = new Vector3(lp.x, fineY + p.vatWater * waterLayerScale + 0.004f, lp.z);
                }
            }
        }

        public override string Prompt(PlayerInteractor player)
        {
            var p = Proc;
            if (p == null) return string.Empty;

            if (p.completed) return "泥料已备好";
            if (p.settling) return "静置中……";
            if (p.settled)
            {
                if (player.HeldTool != ToolKind.DecantLadle) return "需要撇水勺";
                return "按住左键左右撇动，撇去上层清水";
            }
            if (p.vatFine < 0.02f) return "沉淀缸 · 空";
            if (!p.CanSettleSafely) return "沉淀缸 · 筛面还有浆料未处理";
            return "沉淀缸 · 按 T 静置沉淀";
        }

        public override bool CanPress(PlayerInteractor player)
        {
            var p = Proc;
            if (p == null) return false;
            return p.settled && !p.completed && p.vatWater > 0.02f && player.HeldTool == ToolKind.DecantLadle;
        }

        public override void OnPrimaryHold(PlayerInteractor player, float dt)
        {
            var p = Proc;
            if (p == null || !p.settled || p.completed) return;
            if (player.HeldTool != ToolKind.DecantLadle) return;

            float input = Mathf.Clamp01(Mathf.InverseLerp(10f, 80f, player.MouseSpeed));
            if (input < 0.05f) return;
            p.Decant(dt, input);
        }
    }
}
