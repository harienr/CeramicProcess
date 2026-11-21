using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 原料容器（粗浆缸 / 清水缸）。用木瓢舀取，浆面随存量下降。
    /// 同一套逻辑复用于两种液体，只在提示语与内容类型上区分。
    /// </summary>
    public class SourceVat : Interactable
    {
        public LadleContent content = LadleContent.Slurry;
        public Transform liquidSurface;
        public float surfaceLowY = 0.10f;
        public float surfaceHighY = 0.62f;
        public Renderer[] rimWetRenderers;

        SieveWashProcess Proc => SieveWashProcess.I;

        protected override void Awake()
        {
            base.Awake();
            // 容器与其他物件共用材质，悬停提示需要独立材质实例
            SetMaterialsHighlightable(GetComponentsInChildren<Renderer>());
        }

        public float Level
        {
            get
            {
                var p = Proc;
                if (p == null) return 0f;
                return content == LadleContent.Slurry ? p.rawSlurry : p.waterSupply;
            }
        }

        string Name => content == LadleContent.Slurry ? "粗浆缸" : "清水缸";

        void Update()
        {
            if (liquidSurface != null)
            {
                float y = Mathf.Lerp(surfaceLowY, surfaceHighY, Mathf.Clamp01(Level));
                var lp = liquidSurface.localPosition;
                liquidSurface.localPosition = new Vector3(lp.x, y, lp.z);
                liquidSurface.gameObject.SetActive(Level > 0.012f);
            }
        }

        public override string Prompt(PlayerInteractor player)
        {
            var p = Proc;
            if (p == null) return string.Empty;

            bool wrongTool = player.HeldTool != ToolKind.Ladle;
            if (wrongTool) return "需要木瓢";

            if (Level <= 0.01f) return Name + "已空";
            if (p.ladleFill >= 0.99f) return "瓢已满，先倒到筛面";
            if (p.ladleContent != LadleContent.None && p.ladleContent != content)
                return "瓢中已有" + (p.ladleContent == LadleContent.Slurry ? "粗浆" : "清水");
            return "按住左键舀取" + (content == LadleContent.Slurry ? "粗浆" : "清水");
        }

        public override bool CanPress(PlayerInteractor player)
        {
            var p = Proc;
            if (p == null) return false;
            if (player.HeldTool != ToolKind.Ladle) return false;
            if (Level <= 0.01f) return false;
            if (p.ladleFill >= 0.99f) return false;
            return p.ladleContent == LadleContent.None || p.ladleContent == content;
        }

        public override void OnPrimaryHold(PlayerInteractor player, float dt)
        {
            var p = Proc;
            if (p == null || player.HeldTool != ToolKind.Ladle) return;
            p.Scoop(content, p.scoopRate * dt);
        }
    }
}
