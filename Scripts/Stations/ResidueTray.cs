using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>粗渣盘：承接从筛面刮下的粗渣，堆积高度随存量增长。</summary>
    public class ResidueTray : Interactable
    {
        public Transform pile;
        public float pileHeightMax = 0.075f;
        public float pileRadius = 0.10f;

        SieveWashProcess Proc => SieveWashProcess.I;

        void Update()
        {
            var p = Proc;
            if (p == null || pile == null) return;
            float t = Mathf.Clamp01(p.trayFill);
            pile.gameObject.SetActive(t > 0.01f);
            float r = Mathf.Lerp(0.45f, 1f, Mathf.Sqrt(t));
            pile.localScale = new Vector3(pileRadius * r, Mathf.Lerp(0.004f, pileHeightMax, t), pileRadius * r);
        }

        public override string Prompt(PlayerInteractor player)
        {
            var p = Proc;
            if (p == null) return string.Empty;
            if (p.trayFill < 0.015f) return "粗渣盘 · 空";
            return "粗渣盘 · 已积 " + Mathf.RoundToInt(p.trayFill * 100f) + "%";
        }

        public override bool CanPress(PlayerInteractor player) => false;
    }
}
