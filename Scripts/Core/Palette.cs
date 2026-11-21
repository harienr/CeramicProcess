using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 《瓷从泥生》美术规范色彩与派生色。
    /// 对应《美术风格 AI 提示词》第 3 节：米白、泥灰、旧木褐为主体，青花蓝仅小面积使用。
    /// </summary>
    public static class Palette
    {
        // ---- 规范五色 ----
        public static readonly Color RiceWhite = Hex(0xE8, 0xE1, 0xD4); // 米白：墙面 / 界面底
        public static readonly Color ClayGrey = Hex(0x97, 0x95, 0x8A);  // 泥灰：原料 / 中间调
        public static readonly Color OldWood = Hex(0x65, 0x50, 0x43);   // 旧木褐：梁 / 台 / 柄
        public static readonly Color Cobalt = Hex(0x34, 0x56, 0x75);    // 青花蓝：编号 / 指引
        public static readonly Color Charcoal = Hex(0x34, 0x36, 0x33);  // 深灰：正文 / 轮廓

        // ---- 派生：材料状态 ----
        public static readonly Color SlurryWet = Hex(0xC4, 0xC0, 0xB4);   // 湿泥浆
        public static readonly Color SlurryDry = Hex(0xAD, 0xA8, 0x9B);   // 干泥料
        public static readonly Color CoarseGrit = Hex(0x8A, 0x82, 0x74);  // 粗渣
        public static readonly Color WaterClear = Hex(0x9E, 0xA6, 0xA2);  // 清水
        public static readonly Color StoneFloor = Hex(0x8E, 0x8A, 0x80);  // 石地
        public static readonly Color ClothRaw = Hex(0x9A, 0x91, 0x82);    // 粗布
        public static readonly Color Skin = Hex(0xBE, 0xA5, 0x8E);        // 手部占位色
        public static readonly Color Bamboo = Hex(0xA8, 0x96, 0x6F);      // 竹材

        // ---- 派生：界面 ----
        public static readonly Color PanelFill = new Color(0.910f, 0.882f, 0.831f, 0.90f); // 米白 90%
        public static readonly Color PanelFillSoft = new Color(0.910f, 0.882f, 0.831f, 0.55f);
        public static readonly Color InkText = new Color(0.204f, 0.212f, 0.200f, 1f);
        public static readonly Color InkMuted = new Color(0.204f, 0.212f, 0.200f, 0.62f);
        public static readonly Color CobaltLine = new Color(0.204f, 0.337f, 0.459f, 0.85f);

        public static Color Hex(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        /// <summary>按 0-1 比例把颜色加深，用于湿痕、阴影侧。</summary>
        public static Color Darken(Color c, float t)
        {
            return new Color(c.r * (1f - t), c.g * (1f - t), c.b * (1f - t), c.a);
        }

        public static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
