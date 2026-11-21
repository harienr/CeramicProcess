using UnityEngine;
using UnityEngine.UI;

namespace CiCongNiSheng
{
    /// <summary>
    /// 界面构建工具。界面遵循：米白底、深灰正文、青花蓝细线与工序编号；
    /// 面板不长期遮挡材料与双手。
    /// </summary>
    public static class UIFactory
    {
        static Font _font;
        static Sprite _ringSprite;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = LoadFont();
                return _font;
            }
        }

        public static Sprite RingSprite
        {
            get
            {
                if (_ringSprite == null)
                {
                    var tex = ProcTex.Ring();
                    _ringSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                }
                return _ringSprite;
            }
        }

        static Font LoadFont()
        {
            var bundled = Resources.Load<Font>("Fonts/SourceHanSansCN-Regular");
            if (bundled != null) return bundled;
            // 优先系统中文字体，保证正文可读；失败时回退内置字体
            string[] preferred =
            {
                "Microsoft YaHei", "微软雅黑", "Microsoft YaHei UI", "SimHei", "黑体",
                "SimSun", "Noto Sans CJK SC", "Source Han Sans SC", "Arial Unicode MS"
            };
            var f = Font.CreateDynamicFontFromOSFont(preferred, 36);
            if (f != null) return f;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image AddImage(RectTransform rt, Color color, Sprite sprite = null)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
            }
            return img;
        }

        public static Text AddText(RectTransform rt, string text, int fontSize, Color color,
                                   TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>创建一行"标签 + 数值 + 细进度条"，返回用于更新的引用。</summary>
        public static HudRow CreateRow(Transform parent, string label, float y, float width)
        {
            var row = new HudRow();
            var root = Place(Node(parent, "row_" + label), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, 26f));

            row.Label = AddText(Place(Node(root, "label"), Vector2.zero, Vector2.one,
                new Vector2(0f, 1f), new Vector2(0f, -6f), new Vector2(width * 0.55f, 16f)),
                label, 14, Palette.InkMuted);

            row.Value = AddText(Place(Node(root, "value"), Vector2.zero, Vector2.one,
                new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(width * 0.45f, 16f)),
                "", 14, Palette.InkText, TextAnchor.MiddleRight);

            Place(Node(root, "barBg"), Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 3f));

            var bg = root.Find("barBg").gameObject.AddComponent<Image>();
            bg.color = new Color(0.204f, 0.212f, 0.200f, 0.14f);
            bg.raycastTarget = false;

            var fillRt = Place(Node(root, "barFill"), Vector2.zero, new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 3f));
            row.Fill = fillRt.gameObject.AddComponent<Image>();
            row.Fill.color = Palette.CobaltLine;
            row.Fill.raycastTarget = false;
            row.BarWidth = width;

            return row;
        }
    }

    public class HudRow
    {
        public Text Label;
        public Text Value;
        public Image Fill;
        public float BarWidth;

        public void Set(string value, float t)
        {
            if (Value != null) Value.text = value;
            if (Fill != null) Fill.rectTransform.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(t), 3f);
        }
    }
}
