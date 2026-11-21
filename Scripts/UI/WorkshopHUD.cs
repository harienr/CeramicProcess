using UnityEngine;
using UnityEngine.UI;

namespace CiCongNiSheng
{
    /// <summary>
    /// 工序 HUD：工序编号、当前阶段、材料状态、上下文提示、静置与结算面板。
    /// 所有面板都靠边放置，中央区域始终留给材料、工具与操作接触点。
    /// </summary>
    public class WorkshopHUD : MonoBehaviour
    {
        public PlayerInteractor player;
        public SieveWashProcess process;

        Canvas _canvas;
        Text _phaseText;
        Image _progressFill;
        Text _promptText;
        Text _controlsText;
        Image _crosshair;

        HudRow _rawSlurryRow;
        HudRow _ladleRow;
        HudRow _sieveSlurryRow;
        HudRow _sieveResidueRow;
        HudRow _vatRow;
        HudRow _trayRow;

        CanvasGroup _stateGroup;
        CanvasGroup _settleGroup;
        RectTransform _resultPanel;
        Text _resultBody;
        Text _resultGrade;

        Text _toastText;
        float _toastTimer;
        float _idleTimer;
        bool _resultShown;
        float _promptPulse;

        void Awake()
        {
            _canvas = UIFactory.CreateCanvas("工序界面", 10);
            _canvas.transform.SetParent(transform, false);
            BuildHeader();
            BuildStatePanel();
            BuildPrompt();
            BuildControls();
            BuildCrosshair();
            BuildToast();
            BuildSettleOverlay();
            BuildResultPanel();
        }

        // ---------------------------------------------------------------- 构建

        void BuildHeader()
        {
            var root = UIFactory.Place(UIFactory.Node(_canvas.transform, "header"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -40f), new Vector2(520f, 150f));

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(root, "num"),
                Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(60f, 56f)),
                "05", 44, Palette.Cobalt, TextAnchor.UpperLeft);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(root, "title"),
                Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(68f, -2f), new Vector2(220f, 56f)),
                "筛洗", 40, Palette.InkText, TextAnchor.UpperLeft);

            var rule = UIFactory.Place(UIFactory.Node(root, "rule"),
                Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, -60f), new Vector2(52f, 2f));
            UIFactory.AddImage(rule, Palette.CobaltLine);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(root, "subtitle"),
                Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, -68f), new Vector2(500f, 22f)),
                "景德镇传统制瓷 · 第五步", 15, Palette.InkMuted);

            var barBg = UIFactory.Place(UIFactory.Node(root, "progressBg"),
                Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, -98f), new Vector2(300f, 3f));
            UIFactory.AddImage(barBg, new Color(0.204f, 0.212f, 0.200f, 0.16f));

            var barFill = UIFactory.Place(UIFactory.Node(root, "progressFill"),
                Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 3f));
            _progressFill = UIFactory.AddImage(barFill, Palette.CobaltLine);

            _phaseText = UIFactory.AddText(UIFactory.Place(UIFactory.Node(root, "phase"),
                Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, -108f), new Vector2(500f, 22f)),
                "", 15, Palette.InkMuted);
        }

        void BuildStatePanel()
        {
            var panel = UIFactory.Place(UIFactory.Node(_canvas.transform, "statePanel"),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-40f, 0f), new Vector2(264f, 286f));

            UIFactory.AddImage(panel, Palette.PanelFillSoft);
            _stateGroup = panel.gameObject.AddComponent<CanvasGroup>();

            var line = UIFactory.Place(UIFactory.Node(panel, "topLine"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -16f), new Vector2(40f, 2f));
            UIFactory.AddImage(line, Palette.CobaltLine);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(panel, "caption"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -26f), new Vector2(220f, 20f)),
                "材料状态", 14, Palette.InkMuted);

            float w = 228f;
            float y = -56f;
            _rawSlurryRow = UIFactory.CreateRow(panel, "粗浆", y, w);
            _ladleRow = UIFactory.CreateRow(panel, "木瓢", y - 34f, w);
            _sieveSlurryRow = UIFactory.CreateRow(panel, "筛面浆", y - 68f, w);
            _sieveResidueRow = UIFactory.CreateRow(panel, "筛面渣", y - 102f, w);
            _vatRow = UIFactory.CreateRow(panel, "沉淀缸", y - 136f, w);
            _trayRow = UIFactory.CreateRow(panel, "渣盘", y - 170f, w);

            foreach (var t in panel.GetComponentsInChildren<Text>()) t.raycastTarget = false;
        }

        void BuildPrompt()
        {
            _promptText = UIFactory.AddText(UIFactory.Place(UIFactory.Node(_canvas.transform, "prompt"),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 92f), new Vector2(1000f, 36f)),
                "", 24, Palette.InkText, TextAnchor.LowerCenter);
            _promptText.supportRichText = true;

            var cap = UIFactory.Place(UIFactory.Node(_canvas.transform, "promptRule"),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 78f), new Vector2(36f, 2f));
            UIFactory.AddImage(cap, new Color(0.204f, 0.337f, 0.459f, 0.35f));
        }

        void BuildControls()
        {
            _controlsText = UIFactory.AddText(UIFactory.Place(UIFactory.Node(_canvas.transform, "controls"),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(48f, 38f), new Vector2(420f, 110f)),
                "WASD  移动        Shift  放慢\n左键  操作 / 拾取    右键  放下工具\nT  静置        Ctrl + R  重新开始", 14,
                Palette.InkMuted, TextAnchor.LowerLeft);
            _controlsText.lineSpacing = 1.45f;
        }

        void BuildCrosshair()
        {
            var rt = UIFactory.Place(UIFactory.Node(_canvas.transform, "crosshair"),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(9f, 9f));
            _crosshair = UIFactory.AddImage(rt, Palette.WithAlpha(Palette.Cobalt, 0.42f), UIFactory.RingSprite);
        }

        void BuildToast()
        {
            _toastText = UIFactory.AddText(UIFactory.Place(UIFactory.Node(_canvas.transform, "toast"),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 150f), new Vector2(900f, 28f)),
                "", 19, Palette.Cobalt, TextAnchor.LowerCenter);
            _toastText.color = Palette.WithAlpha(Palette.Cobalt, 0f);
        }

        void BuildSettleOverlay()
        {
            var rt = UIFactory.Place(UIFactory.Node(_canvas.transform, "settleOverlay"),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.AddImage(rt, new Color(0.910f, 0.882f, 0.831f, 0.94f));
            _settleGroup = rt.gameObject.AddComponent<CanvasGroup>();
            _settleGroup.alpha = 0f;
            _settleGroup.blocksRaycasts = false;

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(rt, "t1"),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 26f), new Vector2(700f, 60f)),
                "静置  一时辰", 38, Palette.InkText, TextAnchor.MiddleCenter);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(rt, "t2"),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -22f), new Vector2(700f, 30f)),
                "细浆正在沉淀，粗粒下沉", 18, Palette.InkMuted, TextAnchor.MiddleCenter);

            var rule = UIFactory.Place(UIFactory.Node(rt, "rule"),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -2f), new Vector2(60f, 2f));
            UIFactory.AddImage(rule, Palette.CobaltLine);
        }

        void BuildResultPanel()
        {
            _resultPanel = UIFactory.Place(UIFactory.Node(_canvas.transform, "result"),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(620f, 420f));
            UIFactory.AddImage(_resultPanel, new Color(0.910f, 0.882f, 0.831f, 0.97f));

            var accent = UIFactory.Place(UIFactory.Node(_resultPanel, "accent"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(4f, 420f));
            UIFactory.AddImage(accent, Palette.CobaltLine);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(_resultPanel, "t1"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -38f), new Vector2(520f, 44f)),
                "筛洗完成", 34, Palette.InkText, TextAnchor.UpperLeft);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(_resultPanel, "t2"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -84f), new Vector2(520f, 24f)),
                "第五步 · 筛洗　细浆已入沉淀缸", 15, Palette.InkMuted, TextAnchor.UpperLeft);

            _resultBody = UIFactory.AddText(UIFactory.Place(UIFactory.Node(_resultPanel, "body"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -126f), new Vector2(360f, 210f)),
                "", 18, Palette.InkText, TextAnchor.UpperLeft);
            _resultBody.lineSpacing = 1.55f;

            _resultGrade = UIFactory.AddText(UIFactory.Place(UIFactory.Node(_resultPanel, "grade"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(420f, -126f), new Vector2(160f, 120f)),
                "", 64, Palette.Cobalt, TextAnchor.UpperCenter);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(_resultPanel, "note"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -336f), new Vector2(520f, 24f)),
                "设备结构与工序编号依据待核实", 13, Palette.WithAlpha(Palette.InkMuted, 0.75f),
                TextAnchor.UpperLeft);

            UIFactory.AddText(UIFactory.Place(UIFactory.Node(_resultPanel, "hint"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -366f), new Vector2(520f, 24f)),
                "按 Ctrl + R 重新开始", 14, Palette.Cobalt, TextAnchor.UpperLeft);

            _resultPanel.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- 刷新

        void Update()
        {
            if (process == null) return;
            float dt = Time.deltaTime;

            _phaseText.text = "当前阶段　" + process.PhaseTitle;
            if (_progressFill != null)
            {
                _progressFill.rectTransform.sizeDelta = new Vector2(300f * Mathf.Clamp01(process.Completion01), 3f);
            }

            UpdatePrompt(dt);
            UpdateState(dt);
            UpdateSettle(dt);
            UpdateToast(dt);
            UpdateResult();
        }

        void UpdatePrompt(float dt)
        {
            string text;
            bool activeHint = false;

            if (player != null && player.Hovered != null)
            {
                text = player.PromptText;
                activeHint = player.PressAvailable;
            }
            else
            {
                text = process.PhaseHint;
            }

            if (string.IsNullOrEmpty(text))
            {
                _promptText.text = string.Empty;
                return;
            }

            _promptPulse = Mathf.Lerp(_promptPulse, activeHint ? 1f : 0f, 1f - Mathf.Exp(-10f * dt));
            string prefix = activeHint ? "<color=#345675>左键</color>　" : string.Empty;
            _promptText.text = prefix + text;
            _promptText.color = activeHint ? Palette.InkText : Palette.InkMuted;

            if (_crosshair != null)
            {
                _crosshair.color = Palette.WithAlpha(Palette.Cobalt, activeHint ? 0.85f : 0.35f);
            }
        }

        void UpdateState(float dt)
        {
            var p = process;
            _rawSlurryRow.Set(Mathf.RoundToInt(p.rawSlurry * 100f) + "%", p.rawSlurry);

            string ladleText;
            if (p.ladleContent == LadleContent.None) ladleText = "空";
            else if (p.ladleContent == LadleContent.Slurry) ladleText = "粗浆 " + Mathf.RoundToInt(p.ladleFill * 100f) + "%";
            else ladleText = "清水 " + Mathf.RoundToInt(p.ladleFill * 100f) + "%";
            _ladleRow.Set(ladleText, p.ladleFill);

            _sieveSlurryRow.Set(Mathf.RoundToInt(p.sieveSlurry / Mathf.Max(0.01f, p.sieveCapacity) * 100f) + "%", p.sieveSlurry / Mathf.Max(0.01f, p.sieveCapacity));
            _sieveResidueRow.Set(Mathf.RoundToInt(p.sieveResidue / Mathf.Max(0.01f, p.clogThreshold) * 100f) + "%", p.sieveResidue / Mathf.Max(0.01f, p.clogThreshold));

            string vatText;
            if (p.completed) vatText = "完成";
            else if (p.settled) vatText = "清水 " + Mathf.RoundToInt(p.vatWater / Mathf.Max(0.01f, p.settledWaterLayer) * 100f) + "%";
            else vatText = Mathf.RoundToInt(p.vatFine * 100f) + "%";
            _vatRow.Set(vatText, p.settled ? 1f : p.vatFine / Mathf.Max(0.01f, p.settleTarget));

            _trayRow.Set(Mathf.RoundToInt(p.trayFill * 100f) + "%", p.trayFill);

            // 状态面板在长时间无变化时淡出，避免遮挡画面
            bool changed = true;
            float signature = p.rawSlurry + p.ladleFill * 3f + p.sieveSlurry * 5f + p.sieveResidue * 7f
                              + p.vatFine * 11f + p.vatWater * 13f + p.trayFill * 17f;
            if (Mathf.Abs(signature - _lastSignature) > 0.0008f)
            {
                _lastSignature = signature;
                _idleTimer = 0f;
                changed = true;
            }
            else
            {
                _idleTimer += dt;
                changed = false;
            }

            float target = (changed || _idleTimer < 3.5f) ? 1f : 0.30f;
            if (_stateGroup != null)
            {
                _stateGroup.alpha = Mathf.Lerp(_stateGroup.alpha, target, 1f - Mathf.Exp(-3f * dt));
            }
        }

        float _lastSignature = -999f;

        void UpdateSettle(float dt)
        {
            if (_settleGroup == null) return;
            float target = process.settling ? 1f : 0f;
            _settleGroup.alpha = Mathf.Lerp(_settleGroup.alpha, target, 1f - Mathf.Exp(-6f * dt));
        }

        void UpdateToast(float dt)
        {
            if (_toastText == null) return;

            if (player != null && player.Hovered is SieveStation sieve && sieve.Splashing)
            {
                Toast("搅动过快，浆料溅出筛沿");
            }

            if (_toastTimer > 0f)
            {
                _toastTimer -= dt;
                _toastText.color = Palette.WithAlpha(Palette.Cobalt, Mathf.Clamp01(_toastTimer));
            }
            else
            {
                _toastText.color = Palette.WithAlpha(Palette.Cobalt, 0f);
            }
        }

        public void Toast(string message)
        {
            if (_toastText == null) return;
            _toastText.text = message;
            _toastTimer = 1.6f;
        }

        void UpdateResult()
        {
            bool done = process.completed;
            if (done && !_resultShown)
            {
                _resultShown = true;
                _resultPanel.gameObject.SetActive(true);
                int fine = Mathf.RoundToInt(process.FinenessScore * 100f);
                int recovery = Mathf.RoundToInt(process.RecoveryScore * 100f);
                int waste = Mathf.RoundToInt(process.WasteScore * 100f);
                int seconds = Mathf.RoundToInt(process.elapsed);
                _resultBody.text =
                    "细腻度　　" + fine + "\n" +
                    "回收率　　" + recovery + "\n" +
                    "损耗　　　" + waste + "\n" +
                    "上筛次数　" + process.loadCount + "\n" +
                    "清渣次数　" + process.scrapeCount + "\n" +
                    "用时　　　" + (seconds / 60) + " 分 " + (seconds % 60).ToString("00") + " 秒";
                _resultGrade.text = process.Grade;
            }
            else if (!done && _resultShown)
            {
                _resultShown = false;
                _resultPanel.gameObject.SetActive(false);
            }
        }
    }
}
