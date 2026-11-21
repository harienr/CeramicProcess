using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 界面（规范 §7）：米白面板、深灰正文、青花蓝细线与工序编号。
    /// 只用 IMGUI，零依赖；面板全部贴边，长期不遮挡材料与双手。
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public SievingStation station;
        public PlayerInteractor interactor;
        public FirstPersonController controller;

        Font font;
        GUIStyle title, body, small, promptStyle, big;
        Texture2D panelTex, whiteTex, bracketTex;
        float promptAlpha;
        float k = 1f;

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Texture2D RoundedPanel(int size, int radius, Color fill)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    var c = fill;
                    c.a *= a;
                    px[y * size + x] = c;
                }
            }
            t.SetPixels(px);
            t.Apply();
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        void Start()
        {
            string[] names = { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "Noto Sans CJK SC", "Source Han Sans SC", "Arial" };
            font = Font.CreateDynamicFontFromOSFont(names, 18);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            panelTex = RoundedPanel(48, 9, new Color(Palette.MiBai.r, Palette.MiBai.g, Palette.MiBai.b, 0.93f));
            whiteTex = Tex(Color.white);
            bracketTex = RoundedPanel(24, 4, new Color(Palette.QingHua.r, Palette.QingHua.g, Palette.QingHua.b, 0.9f));

            title = new GUIStyle { font = font, alignment = TextAnchor.UpperLeft };
            body = new GUIStyle { font = font, alignment = TextAnchor.UpperLeft, wordWrap = false };
            small = new GUIStyle { font = font, alignment = TextAnchor.UpperLeft, wordWrap = false };
            promptStyle = new GUIStyle { font = font, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            big = new GUIStyle { font = font, alignment = TextAnchor.MiddleLeft };
        }

        Rect R(float x, float y, float w, float h)
        {
            return new Rect(x * k, y * k, w * k, h * k);
        }

        void Text(GUIStyle st, Rect r, string s, Color c, int size, TextAnchor anchor)
        {
            st.fontSize = Mathf.RoundToInt(size * k);
            st.alignment = anchor;
            st.normal.textColor = c;
            GUI.Label(r, s, st);
        }

        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, whiteTex);
            GUI.color = old;
        }

        void Panel(Rect r)
        {
            var st = new GUIStyle();
            st.normal.background = panelTex;
            st.border = new RectOffset(12, 12, 12, 12);
            GUI.Box(r, GUIContent.none, st);
        }

        void OnGUI()
        {
            if (font == null) return;
            k = Mathf.Clamp(Screen.height / 900f, 0.7f, 1.6f);

            DrawStep();
            DrawPrompt();
            DrawFilter();
            DrawCrosshair();
            DrawHints();
        }

        // —— 左上：工序编号 + 一句说明（与截图说明文字一致）——
        void DrawStep()
        {
            float x = 32f, y = 28f;
            Fill(R(x, y + 2f, 3f, 54f), Palette.QingHua);
            Text(title, R(x + 14f, y - 5f, 460f, 32f), "五 · 筛洗", Palette.QingHua, 24, TextAnchor.UpperLeft);
            Text(body, R(x + 14f, y + 28f, 560f, 22f),
                 "人工把瓷石进行淘洗，去除杂物，提高原料的纯洁度。", Palette.ShenHui, 14, TextAnchor.UpperLeft);

            // 工序链：当前一步用青花蓝，其余深灰淡化
            string[] steps = { "粗碎", "淘洗", "筛洗", "沉淀" };
            float sx = 32f, sy = 96f;
            float total = 0f;
            int active = 2;
            for (int i = 0; i < steps.Length; i++)
            {
                Color c = i == active ? Palette.QingHua : new Color(Palette.ShenHui.r, Palette.ShenHui.g, Palette.ShenHui.b, 0.42f);
                Text(small, R(sx + total, sy, 90f, 22f), steps[i], c, 14, TextAnchor.UpperLeft);
                float w = small.CalcSize(new GUIContent(steps[i])).x / k;
                total += w + 8f;
                if (i < steps.Length - 1)
                {
                    Fill(R(sx + total, sy + 10f, 12f, 1f), new Color(Palette.ShenHui.r, Palette.ShenHui.g, Palette.ShenHui.b, 0.25f));
                    total += 20f;
                }
            }
        }

        // —— 下方中央：当前可做的事 + 细进度线 ——
        void DrawPrompt()
        {
            string text = interactor != null ? interactor.Prompt : null;
            bool show = !string.IsNullOrEmpty(text);
            promptAlpha = Mathf.MoveTowards(promptAlpha, show ? 1f : 0f, Time.deltaTime * 5f);
            if (promptAlpha <= 0.01f) return;

            float w = 330f, h = 42f;
            float x = (Screen.width / k) * 0.5f - w * 0.5f;
            float y = (Screen.height / k) - 150f;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, promptAlpha);
            Panel(R(x, y, w, h));
            Fill(R(x + 14f, y + 8f, w - 28f, 1f), new Color(Palette.QingHua.r, Palette.QingHua.g, Palette.QingHua.b, 0.55f));
            Text(promptStyle, R(x, y + 12f, w, 24f), text, Palette.ShenHui, 16, TextAnchor.MiddleCenter);

            // 进度（上料 / 摇筛）
            if (station != null)
            {
                float p = -1f;
                if (station.Current == SievingStation.Stage.Loading) p = station.Load01;
                else if (station.Current == SievingStation.Stage.Sieving) p = station.Purity01;
                if (p >= 0f)
                {
                    float bw = w - 40f;
                    Fill(R(x + 20f, y + h - 12f, bw, 2f), new Color(Palette.ShenHui.r, Palette.ShenHui.g, Palette.ShenHui.b, 0.16f));
                    Fill(R(x + 20f, y + h - 12f, bw * Mathf.Clamp01(p), 2f), Palette.QingHua);
                }
            }
            GUI.color = old;
        }

        // —— 右侧：滤净度 ——
        void DrawFilter()
        {
            if (station == null) return;
            bool relevant = station.Current != SievingStation.Stage.Empty;
            float a = relevant ? 1f : 0.35f;
            float w = 150f, h = 54f;
            float x = (Screen.width / k) - w - 32f;
            float y = (Screen.height / k) * 0.5f - h * 0.5f;

            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            Panel(R(x, y, w, h));
            Text(small, R(x + 14f, y + 8f, w, 20f), "滤净", new Color(Palette.ShenHui.r, Palette.ShenHui.g, Palette.ShenHui.b, 0.7f), 12, TextAnchor.UpperLeft);
            Text(big, R(x + 52f, y + 6f, w, 24f),
                 Mathf.RoundToInt(station.Purity01 * 100f) + "%", Palette.ShenHui, 18, TextAnchor.MiddleLeft);
            Text(small, R(x + 14f, y + 30f, w, 20f), station.StageName,
                 station.Busy ? Palette.QingHua : new Color(Palette.ShenHui.r, Palette.ShenHui.g, Palette.ShenHui.b, 0.55f), 12, TextAnchor.UpperLeft);
            Fill(R(x + 14f, y + h - 12f, w - 28f, 2f), new Color(Palette.ShenHui.r, Palette.ShenHui.g, Palette.ShenHui.b, 0.15f));
            Fill(R(x + 14f, y + h - 12f, (w - 28f) * Mathf.Clamp01(station.Purity01), 2f), Palette.QingHua);
            GUI.color = old;
        }

        // —— 准星：有可操作对象时收成青花蓝方框 ——
        void DrawCrosshair()
        {
            bool has = interactor != null && interactor.HasTarget;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            if (!has)
            {
                Fill(new Rect(cx - 1f, cy - 1f, 2f, 2f), new Color(Palette.MiBai.r, Palette.MiBai.g, Palette.MiBai.b, 0.55f));
            }
            else
            {
                float s = 7f * k, th = 1f * k;
                Color c = new Color(Palette.QingHua.r, Palette.QingHua.g, Palette.QingHua.b, 0.85f);
                Fill(new Rect(cx - s - 4f, cy - th * 0.5f, 4f * k, th), c);
                Fill(new Rect(cx + s, cy - th * 0.5f, 4f * k, th), c);
                Fill(new Rect(cx - th * 0.5f, cy - s - 4f, th, 4f * k), c);
                Fill(new Rect(cx - th * 0.5f, cy + s, th, 4f * k), c);
            }
        }

        void DrawHints()
        {
            string hint = "WASD 移动 · 鼠标 观察 · 按住 E 操作 · R 刮渣 · 按住右键 近观 · Esc 释放鼠标";
            float y = (Screen.height / k) - 30f;
            Text(small, R(32f, y, 700f, 20f), hint,
                 new Color(Palette.ShenHui.r, Palette.ShenHui.g, Palette.ShenHui.b, 0.42f), 12, TextAnchor.UpperLeft);
        }
    }
}
