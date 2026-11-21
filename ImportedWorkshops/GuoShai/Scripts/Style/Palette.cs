using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 《瓷从泥生》美术规范色板（见 美术风格AI提示词.md 第 3 节）。
    /// 所有材质颜色都从这里取，改风格只改这一处。
    /// </summary>
    public static class Palette
    {
        public static Color Hex(string hex)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString(hex, out c)) return c;
            return Color.magenta;
        }

        // —— 规范五色 ——
        public static readonly Color MiBai    = Hex("#E8E1D4"); // 米白：墙面 / 界面底 / 说明牌
        public static readonly Color NiHui    = Hex("#97958A"); // 泥灰：原料 / 泥料 / 环境中间调
        public static readonly Color JiuMuHe  = Hex("#655043"); // 旧木褐：梁 / 架 / 台 / 工具柄
        public static readonly Color QingHua  = Hex("#345675"); // 青花蓝：工序编号 / 指引（小面积）
        public static readonly Color ShenHui  = Hex("#343633"); // 深灰：正文 / 轮廓 / 局部深色

        // —— 由五色派生的材质基色（保持同一色域，不引入高饱和） ——
        public static readonly Color BeamWood   = Color.Lerp(JiuMuHe, ShenHui, 0.35f);   // 老梁，更暗
        public static readonly Color BeamWood2  = Color.Lerp(JiuMuHe, ShenHui, 0.18f);
        public static readonly Color ToolWood   = Color.Lerp(JiuMuHe, MiBai, 0.10f);     // 手常摸的工具，略亮
        public static readonly Color WornWood   = Color.Lerp(JiuMuHe, MiBai, 0.22f);     // 磨损露出的浅木
        public static readonly Color WetWood    = Color.Lerp(JiuMuHe, ShenHui, 0.45f);   // 湿木
        public static readonly Color StoneFloor = Color.Lerp(NiHui, ShenHui, 0.12f);
        public static readonly Color StoneWall  = Color.Lerp(NiHui, MiBai, 0.18f);
        public static readonly Color StoneWet   = Color.Lerp(NiHui, ShenHui, 0.35f);
        public static readonly Color RockStone  = Color.Lerp(NiHui, MiBai, 0.42f);       // 瓷石：偏白、粗粝
        public static readonly Color Plaster    = Color.Lerp(MiBai, NiHui, 0.10f);
        public static readonly Color Slurry     = Color.Lerp(MiBai, NiHui, 0.24f);       // 泥浆：灰白
        public static readonly Color SlurryWet  = Color.Lerp(MiBai, NiHui, 0.14f);
        public static readonly Color ClayRaw    = Color.Lerp(NiHui, MiBai, 0.30f);       // 未烧泥料
        public static readonly Color Water      = Color.Lerp(NiHui, ShenHui, 0.28f);
        public static readonly Color Bamboo     = Color.Lerp(JiuMuHe, NiHui, 0.30f);
        public static readonly Color Rope       = Color.Lerp(JiuMuHe, MiBai, 0.18f);
        public static readonly Color Iron       = Color.Lerp(ShenHui, NiHui, 0.10f);
        public static readonly Color SieveCloth = Color.Lerp(MiBai, NiHui, 0.16f);
        public static readonly Color Residue    = Color.Lerp(NiHui, JiuMuHe, 0.22f);    // 筛上粗渣
    }
}
