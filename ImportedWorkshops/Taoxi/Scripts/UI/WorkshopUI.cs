using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PorcelainFromClay
{
    public sealed class WorkshopUI : MonoBehaviour
    {
        public event Action StartRequested;
        public event Action ReplayRequested;
        public event Action NotesClosed;

        static readonly Color Rice = new Color32(233, 227, 214, 255);
        static readonly Color Paper = new Color32(242, 237, 226, 255);
        static readonly Color Ink = new Color32(47, 48, 46, 255);
        static readonly Color InkSoft = new Color32(102, 102, 95, 255);
        static readonly Color InkFaint = new Color32(148, 145, 135, 255);
        static readonly Color Blue = new Color32(43, 71, 110, 255);
        static readonly Color BlueFaint = new Color32(112, 135, 163, 255);
        static readonly Color Line = new Color32(198, 191, 178, 255);

        static readonly string[] StepLabels = { "投料", "注水", "搅洗", "静置", "引流", "收泥" };
        static readonly string[] StepTitles = { "投料", "注水", "搅洗", "静置", "引流", "收泥" };
        static readonly string[] StepBodies =
        {
            "取一勺粉碎后的瓷石粉，倒入淘洗池。",
            "沿池壁缓缓注水，让粉料完全浸没。",
            "按住左键画圈，让细泥悬浮、粗砂沉底。",
            "停手等待。水色渐清，细泥沉向池底。",
            "提起木闸，把细泥浆引入沉淀池。",
            "舀出沉降的细泥，沥水备用。"
        };

        MaterialLibrary mats;
        Font fontRegular;
        Font fontBold;

        CanvasGroup introGroup;
        CanvasGroup hudGroup;
        CanvasGroup cardGroup;
        CanvasGroup promptGroup;
        CanvasGroup notesGroup;
        CanvasGroup toastGroup;
        CanvasGroup endGroup;

        Text stepNumberText;
        Text stepTitleText;
        Text stepBodyText;
        Text promptText;
        Text keyText;
        Text toastText;
        Text railCounterText;
        Text[] railNumbers;
        Text[] railLabels;
        Image[] railTicks;
        Image reticleRing;
        Image reticleDot;
        Image stirArc;
        RectTransform promptRoot;
        RectTransform cardRoot;
        Coroutine cardFade;
        Coroutine toastFade;
        Coroutine introFade;
        bool stirMode;
        bool notesOpen;
        float stirProgress;

        public bool NotesOpen { get { return notesOpen; } }

        public static WorkshopUI Create(Transform parent, MaterialLibrary mats)
        {
            var go = new GameObject("WorkshopUI");
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<WorkshopUI>();
            ui.mats = mats;
            ui.Build();
            return ui;
        }

        void Build()
        {
            fontRegular = Resources.Load<Font>("Fonts/SourceHanSansCN-Regular");
            fontBold = Resources.Load<Font>("Fonts/SourceHanSansCN-Bold");
            if (fontRegular == null) fontRegular = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC" }, 28);
            if (fontBold == null) fontBold = fontRegular;

            var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            BuildIntro(canvasGO.transform);
            BuildHud(canvasGO.transform);
            BuildEnd(canvasGO.transform);
            BuildNotes(canvasGO.transform);
        }

        // ---------------------------------------------------------------- intro
        void BuildIntro(Transform parent)
        {
            var root = Rect(parent, "Intro", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            introGroup = root.gameObject.AddComponent<CanvasGroup>();
            introGroup.alpha = 1f;

            FullPaper(root, 1f);

            RectTransform left = Rect(root, "Left", new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, -74), new Vector2(700, 860));
            left.pivot = new Vector2(0, 1);

            var eyebrow = Text(left, "Eyebrow", "景德镇 · 传统制瓷", fontRegular, 14, InkSoft, TextAnchor.UpperLeft, 360, 20);
            Place(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(360, 20));
            eyebrow.characterSpacingFix(2f);

            var rule = Img(left, "Rule", new Color32(43, 71, 110, 255), 32, 1);
            SetRect(rule.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -36), new Vector2(32, 1));

            var title = Text(left, "Title", "瓷从泥生", fontBold, 88, Ink, TextAnchor.UpperLeft, 620, 114);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -78), new Vector2(620, 114));

            var chapter = Text(left, "Chapter", "第三章   淘洗", fontRegular, 20, Blue, TextAnchor.UpperLeft, 420, 30);
            Place(chapter.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -206), new Vector2(420, 30));

            var body = Text(left, "Body", "水与石粉在池中分开。\n细的留下，粗的沉底。", fontRegular, 23, InkSoft, TextAnchor.UpperLeft, 560, 96);
            body.lineSpacing = 1.65f;
            Place(body.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -272), new Vector2(560, 96));

            var note = Text(left, "Note", "学徒的第一件差事，是把泥料交给时间。", fontRegular, 15, InkFaint, TextAnchor.UpperLeft, 560, 28);
            Place(note.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -382), new Vector2(560, 28));

            BuildStartButton(left);

            var controls = Text(left, "Controls", "W A S D   移动        鼠标   观察        E   交互        Tab   工艺注记", fontRegular, 13, InkFaint, TextAnchor.UpperLeft, 760, 24);
            Place(controls.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 30), new Vector2(760, 24));
            controls.characterSpacingFix(1.2f);

            BuildIntroRail(root);
            BuildSeal(root);
        }

        void BuildStartButton(RectTransform parent)
        {
            var rect = Rect(parent, "Start", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -450), new Vector2(250, 48));
            rect.pivot = new Vector2(0, 1);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0.002f);
            var btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit;
            var colors = btn.colors;
            colors.normalColor = new Color(1, 1, 1, 0.002f);
            colors.highlightedColor = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.09f);
            colors.pressedColor = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.15f);
            colors.selectedColor = colors.normalColor;
            btn.colors = colors;
            btn.onClick.AddListener(() =>
            {
                if (StartRequested != null) StartRequested();
                HideIntro();
            });

            var label = Text(rect, "Label", "进入工坊   →", fontBold, 17, Blue, TextAnchor.MiddleLeft, 200, 30);
            Place(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(200, 30));

            var line = Img(rect, "Line", Blue, 180, 1);
            SetRect(line.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 4), new Vector2(180, 1));
        }

        void BuildIntroRail(RectTransform parent)
        {
            var rail = Rect(parent, "IntroRail", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-118, 10), new Vector2(210, 430));
            var line = Img(rail, "Line", new Color(198 / 255f, 191 / 255f, 178 / 255f, 0.7f), 1, 330);
            SetRect(line.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -38), new Vector2(1, 330));

            var title = Text(rail, "Title", "本章工序", fontRegular, 12, InkFaint, TextAnchor.UpperLeft, 150, 20);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, 0), new Vector2(150, 20));
            title.characterSpacingFix(1.6f);

            for (int i = 0; i < StepLabels.Length; i++)
            {
                var row = Rect(rail, "Row" + i, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -48 - i * 48), new Vector2(190, 34));
                var dot = Img(row, "Dot", i == 2 ? Blue : InkFaint, 4, 4);
                SetRect(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(2, 0), new Vector2(4, 4));
                var num = Text(row, "Num", "0" + (i + 1), fontBold, 12, i == 2 ? Blue : InkFaint, TextAnchor.MiddleLeft, 30, 22);
                Place(num.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(30, 22));
                var label = Text(row, "Label", StepLabels[i], fontRegular, 15, i == 2 ? Ink : InkSoft, TextAnchor.MiddleLeft, 70, 24);
                Place(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(56, 0), new Vector2(70, 24));
            }
        }

        void BuildSeal(RectTransform parent)
        {
            var seal = Rect(parent, "Seal", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-118, -92), new Vector2(64, 64));
            var border = Img(seal, "Border", Blue, 64, 1);
            SetRect(border.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(64, 1));
            var left = Img(seal, "L", Blue, 1, 64);
            SetRect(left.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1, 64));
            var right = Img(seal, "R", Blue, 1, 64);
            SetRect(right.rectTransform, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(1, 64));
            var bottom = Img(seal, "B", Blue, 64, 1);
            SetRect(bottom.rectTransform, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(64, 1));
            var ch = Text(seal, "Char", "淘", fontBold, 28, Blue, TextAnchor.MiddleCenter, 64, 40);
            Place(ch.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(64, 40));
        }

        // ------------------------------------------------------------------ hud
        void BuildHud(Transform parent)
        {
            var root = Rect(parent, "HUD", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            hudGroup = root.gameObject.AddComponent<CanvasGroup>();
            hudGroup.alpha = 0f;
            hudGroup.blocksRaycasts = false;

            var scrim = Img(root, "HudPaper", new Color(242 / 255f, 237 / 255f, 226 / 255f, 0.88f), 326, 128);
            SetRect(scrim.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -26), new Vector2(326, 128));
            var shadow = scrim.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.06f, 0.05f, 0.04f, 0.2f);
            shadow.effectDistance = new Vector2(3, -3);
            var scrimLine = Img(root, "HudLine", Blue, 326, 2);
            SetRect(scrimLine.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -26), new Vector2(326, 2));

            var chapter = Text(root, "Chapter", "03   /   淘洗", fontRegular, 12, Blue, TextAnchor.UpperLeft, 300, 20);
            Place(chapter.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -38), new Vector2(300, 20));
            chapter.characterSpacingFix(1.8f);

            var title = Text(root, "Title", "淘洗", fontBold, 30, Ink, TextAnchor.UpperLeft, 220, 42);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -60), new Vector2(220, 42));

            var sub = Text(root, "Sub", "去粗  ·  取细  ·  沉泥", fontRegular, 12, InkSoft, TextAnchor.UpperLeft, 260, 20);
            Place(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -102), new Vector2(260, 20));

            var noteScrim = Img(root, "NotePaper", new Color(242 / 255f, 237 / 255f, 226 / 255f, 0.82f), 178, 30);
            SetRect(noteScrim.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-38, -28), new Vector2(178, 30));
            var noteHint = Text(root, "NoteHint", "TAB   工艺注记", fontRegular, 11, InkSoft, TextAnchor.UpperRight, 200, 20);
            Place(noteHint.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-48, -38), new Vector2(200, 20));

            BuildRail(root);
            BuildCard(root);
            BuildPrompt(root);
            BuildReticle(root);
            BuildToast(root);
        }

        void BuildRail(RectTransform parent)
        {
            var rail = Rect(parent, "Rail", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-38, -20), new Vector2(140, 360));
            rail.pivot = new Vector2(1, 0.5f);
            railNumbers = new Text[StepLabels.Length];
            railLabels = new Text[StepLabels.Length];
            railTicks = new Image[StepLabels.Length];

            var line = Img(rail, "Line", new Color(198 / 255f, 191 / 255f, 178 / 255f, 0.6f), 1, 300);
            SetRect(line.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-1, 0), new Vector2(1, 300));

            for (int i = 0; i < StepLabels.Length; i++)
            {
                float y = 120 - i * 48;
                var row = Rect(rail, "Row" + i, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, y), new Vector2(150, 30));
                row.pivot = new Vector2(1, 0.5f);
                var tick = Img(row, "Tick", Line, 12, 1);
                SetRect(tick.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(12, 1));
                railTicks[i] = tick;
                var num = Text(row, "Num", "0" + (i + 1), fontBold, 11, InkSoft, TextAnchor.MiddleRight, 26, 18);
                Place(num.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-34, 0), new Vector2(26, 18));
                railNumbers[i] = num;
                var outline = num.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(233 / 255f, 227 / 255f, 214 / 255f, 0.85f);
                outline.effectDistance = new Vector2(1, -1);
                var label = Text(row, "Label", StepLabels[i], fontRegular, 13, InkSoft, TextAnchor.MiddleRight, 46, 20);
                var lOutline = label.gameObject.AddComponent<Outline>();
                lOutline.effectColor = new Color(233 / 255f, 227 / 255f, 214 / 255f, 0.85f);
                lOutline.effectDistance = new Vector2(1, -1);
                Place(label.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-66, 0), new Vector2(46, 20));
                railLabels[i] = label;
            }

            railCounterText = Text(rail, "Counter", "01 / 06", fontRegular, 10, InkFaint, TextAnchor.MiddleRight, 80, 18);
            Place(railCounterText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -170), new Vector2(80, 18));
        }

        void BuildCard(RectTransform parent)
        {
            var root = Rect(parent, "StepCard", new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 44), new Vector2(430, 108));
            root.pivot = new Vector2(0, 0);
            cardRoot = root;
            cardGroup = root.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 1f;

            var bg = Img(root, "Paper", new Color(242 / 255f, 237 / 255f, 226 / 255f, 0.94f), 430, 108);
            SetRect(bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(430, 108));
            var sh = bg.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.08f, 0.07f, 0.05f, 0.16f);
            sh.effectDistance = new Vector2(3, -3);

            var accent = Img(root, "Accent", Blue, 430, 2);
            SetRect(accent.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(430, 2));

            stepNumberText = Text(root, "Num", "01", fontBold, 22, Blue, TextAnchor.UpperLeft, 42, 28);
            Place(stepNumberText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -16), new Vector2(42, 28));

            stepTitleText = Text(root, "Title", "投料", fontBold, 20, Ink, TextAnchor.UpperLeft, 120, 28);
            Place(stepTitleText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -15), new Vector2(200, 28));

            stepBodyText = Text(root, "Body", StepBodies[0], fontRegular, 13, InkSoft, TextAnchor.UpperLeft, 386, 44);
            stepBodyText.lineSpacing = 1.4f;
            Place(stepBodyText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -54), new Vector2(386, 44));
        }

        void BuildPrompt(RectTransform parent)
        {
            promptRoot = Rect(parent, "Prompt", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(620, 40));
            promptGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;

            var layout = promptRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10;
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = promptRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var paper = Img(promptRoot, "PromptPaper", new Color(242 / 255f, 237 / 255f, 226 / 255f, 0.88f), 0, 0);
            var paperLayout = paper.gameObject.AddComponent<LayoutElement>();
            paperLayout.ignoreLayout = true;
            SetRect(paper.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            paper.rectTransform.offsetMin = new Vector2(-18, -8);
            paper.rectTransform.offsetMax = new Vector2(18, 8);
            var paperRule = Img(promptRoot, "PromptRule", Blue, 0, 1);
            var ruleLayout = paperRule.gameObject.AddComponent<LayoutElement>();
            ruleLayout.ignoreLayout = true;
            SetRect(paperRule.rectTransform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 2));
            paperRule.rectTransform.offsetMin = new Vector2(-18, -2);
            paperRule.rectTransform.offsetMax = new Vector2(18, 0);

            var key = Rect(promptRoot, "Key", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(28, 26));
            var keyBg = key.gameObject.AddComponent<Image>();
            keyBg.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.10f);
            var keyLayout = key.gameObject.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 28;
            keyLayout.preferredHeight = 26;
            keyText = Text(key, "Char", "E", fontBold, 12, Blue, TextAnchor.MiddleCenter, 28, 22);
            Place(keyText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var leftRule = Img(key, "L", Blue, 2, 26);
            SetRect(leftRule.rectTransform, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(2, 26));

            promptText = Text(promptRoot, "Text", "取一勺瓷石粉", fontRegular, 16, Ink, TextAnchor.MiddleLeft, 420, 30);
            var le = promptText.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 260;
            le.preferredHeight = 30;
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        void BuildReticle(RectTransform parent)
        {
            var holder = Rect(parent, "Reticle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
            reticleDot = Img(holder, "Dot", new Color(47 / 255f, 48 / 255f, 46 / 255f, 0.55f), 5, 5);
            SetRect(reticleDot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 5));
            reticleRing = Img(holder, "Ring", new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.0f), 52, 52);
            reticleRing.sprite = mats.RingSprite;
            reticleRing.type = Image.Type.Filled;
            reticleRing.fillMethod = Image.FillMethod.Radial360;
            reticleRing.fillOrigin = 2;
            reticleRing.fillClockwise = true;
            reticleRing.fillAmount = 0f;
            SetRect(reticleRing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52));

            stirArc = Img(holder, "StirArc", new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.9f), 64, 64);
            stirArc.sprite = mats.RingSprite;
            stirArc.type = Image.Type.Filled;
            stirArc.fillMethod = Image.FillMethod.Radial360;
            stirArc.fillOrigin = 2;
            stirArc.fillClockwise = true;
            stirArc.fillAmount = 0f;
            SetRect(stirArc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
            stirArc.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0f);
        }

        void BuildToast(RectTransform parent)
        {
            var root = Rect(parent, "Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -82), new Vector2(500, 32));
            toastGroup = root.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastText = Text(root, "Text", "水已注入，静置片刻", fontRegular, 13, InkSoft, TextAnchor.MiddleCenter, 500, 28);
            Place(toastText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        }

        // ----------------------------------------------------------------- notes
        void BuildNotes(Transform parent)
        {
            var root = Rect(parent, "Notes", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(390, 560));
            root.pivot = new Vector2(1, 0);
            notesGroup = root.gameObject.AddComponent<CanvasGroup>();
            notesGroup.alpha = 0f;
            notesGroup.blocksRaycasts = false;
            notesGroup.interactable = false;

            var bg = Img(root, "Paper", new Color(242 / 255f, 237 / 255f, 226 / 255f, 0.97f), 390, 560);
            SetRect(bg.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var sh = bg.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.05f, 0.04f, 0.03f, 0.22f);
            sh.effectDistance = new Vector2(4, -4);

            var accent = Img(root, "Accent", Blue, 390, 2);
            SetRect(accent.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(390, 2));

            var title = Text(root, "Title", "工艺注记", fontBold, 22, Ink, TextAnchor.UpperLeft, 330, 32);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22), new Vector2(330, 32));

            var sub = Text(root, "Sub", "TAOXI  /  FIELD NOTES", fontRegular, 10, BlueFaint, TextAnchor.UpperLeft, 330, 18);
            Place(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -58), new Vector2(330, 18));
            sub.characterSpacingFix(1.3f);

            string body =
                "本段只表现淘洗与沉淀。\n" +
                "粉碎、制不、陈腐与练泥不在本段范围内。\n\n" +
                "池体尺寸、闸口高度与沉淀时长因作坊、季节和泥料而异，<color=#2B476E>待核实</color>。\n\n" +
                "粗砂与尾浆的去向各地做法不同，<color=#2B476E>待核实</color>。\n\n" +
                "为便于操作，静置时间与舀泥次数经过压缩；工具取木、竹、石与麻布，未加入电动设备。";
            var text = Text(root, "Body", body, fontRegular, 13, InkSoft, TextAnchor.UpperLeft, 334, 400);
            text.lineSpacing = 1.55f;
            Place(text.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -92), new Vector2(334, 400));

            var tag = Text(root, "Tag", "待核实内容以蓝色标记", fontRegular, 10, BlueFaint, TextAnchor.UpperLeft, 300, 18);
            Place(tag.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 54), new Vector2(300, 18));

            var closeRect = Rect(root, "Close", new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 18), new Vector2(200, 26));
            closeRect.pivot = new Vector2(0, 0);
            var closeHit = closeRect.gameObject.AddComponent<Image>();
            closeHit.color = new Color(1, 1, 1, 0.002f);
            var closeBtn = closeRect.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeHit;
            closeBtn.onClick.AddListener(() => CloseNotes());
            var closeLabel = Text(closeRect, "Label", "关闭   [TAB]", fontRegular, 12, InkSoft, TextAnchor.MiddleLeft, 200, 22);
            Place(closeLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        }

        // ------------------------------------------------------------------- end
        void BuildEnd(Transform parent)
        {
            var root = Rect(parent, "End", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            endGroup = root.gameObject.AddComponent<CanvasGroup>();
            endGroup.alpha = 0f;
            endGroup.blocksRaycasts = false;
            endGroup.interactable = false;
            FullPaper(root, 0.985f);

            var left = Rect(root, "Left", new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, -150), new Vector2(760, 700));
            left.pivot = new Vector2(0, 1);

            var eyebrow = Text(left, "Eyebrow", "工序 06  /  淘洗完成", fontRegular, 13, Blue, TextAnchor.UpperLeft, 400, 22);
            eyebrow.characterSpacingFix(1.5f);
            var title = Text(left, "Title", "细泥已得", fontBold, 68, Ink, TextAnchor.UpperLeft, 600, 90);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -44), new Vector2(600, 90));
            var body = Text(left, "Body", "池水清了。粗砂留在池底，细泥沉在另一池中。\n这只是泥料成为器物之前的一小步。", fontRegular, 19, InkSoft, TextAnchor.UpperLeft, 660, 78);
            body.lineSpacing = 1.55f;
            Place(body.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -154), new Vector2(660, 78));

            string summary = "01 投料      02 注水      03 搅洗      04 静置      05 引流      06 收泥";
            var sum = Text(left, "Summary", summary, fontRegular, 13, InkFaint, TextAnchor.UpperLeft, 720, 24);
            Place(sum.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 110), new Vector2(720, 24));
            sum.characterSpacingFix(1.1f);

            var next = Text(left, "Next", "后续   ·   制不   /   陈腐   /   练泥", fontRegular, 13, InkFaint, TextAnchor.UpperLeft, 520, 22);
            Place(next.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 74), new Vector2(520, 22));

            var btnRect = Rect(left, "Replay", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 10), new Vector2(230, 46));
            btnRect.pivot = new Vector2(0, 0);
            var hit = btnRect.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0.002f);
            var btn = btnRect.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit;
            btn.onClick.AddListener(() =>
            {
                if (ReplayRequested != null) ReplayRequested();
            });
            var btnLabel = Text(btnRect, "Label", "再走一遍   →", fontBold, 15, Blue, TextAnchor.MiddleLeft, 200, 26);
            Place(btnLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var btnLine = Img(btnRect, "Line", Blue, 160, 1);
            SetRect(btnLine.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 2), new Vector2(160, 1));

            var rightNote = Text(root, "Quote", "“别急。\n等它自己沉下来。”", fontRegular, 24, InkFaint, TextAnchor.UpperLeft, 360, 90);
            rightNote.lineSpacing = 1.6f;
            Place(rightNote.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-120, 120), new Vector2(360, 90));

            var seal = Rect(root, "Seal", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-118, -90), new Vector2(64, 64));
            var border = Img(seal, "B", Blue, 64, 1);
            SetRect(border.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(64, 1));
            var l = Img(seal, "L", Blue, 1, 64);
            SetRect(l.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1, 64));
            var r = Img(seal, "R", Blue, 1, 64);
            SetRect(r.rectTransform, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(1, 64));
            var b = Img(seal, "B2", Blue, 64, 1);
            SetRect(b.rectTransform, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(64, 1));
            var c = Text(seal, "C", "泥", fontBold, 27, Blue, TextAnchor.MiddleCenter, 64, 40);
            Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 40));
        }

        // ------------------------------------------------------------- public api
        public void ShowIntro()
        {
            introGroup.alpha = 1f;
            introGroup.blocksRaycasts = true;
            introGroup.interactable = true;
            hudGroup.alpha = 0f;
            endGroup.alpha = 0f;
            endGroup.blocksRaycasts = false;
        }

        public void HideIntro()
        {
            if (introFade != null) StopCoroutine(introFade);
            introFade = StartCoroutine(FadeCanvas(introGroup, 0f, 0.55f, false));
            hudGroup.alpha = 1f;
        }

        public void HideIntroImmediate()
        {
            if (introFade != null) StopCoroutine(introFade);
            introGroup.alpha = 0f;
            introGroup.blocksRaycasts = false;
            introGroup.interactable = false;
            hudGroup.alpha = 1f;
        }

        public void SetStep(int zeroBasedIndex, bool showCard = true)
        {
            zeroBasedIndex = Mathf.Clamp(zeroBasedIndex, 0, StepLabels.Length - 1);
            stepNumberText.text = "0" + (zeroBasedIndex + 1);
            stepTitleText.text = StepTitles[zeroBasedIndex];
            stepBodyText.text = StepBodies[zeroBasedIndex];
            railCounterText.text = "0" + (zeroBasedIndex + 1) + " / 06";
            for (int i = 0; i < StepLabels.Length; i++)
            {
                bool current = i == zeroBasedIndex;
                bool done = i < zeroBasedIndex;
                Color c = current ? Blue : (done ? InkSoft : InkFaint);
                railNumbers[i].color = current ? Blue : InkSoft;
                railLabels[i].color = current ? Blue : InkSoft;
                railTicks[i].color = current ? Blue : Line;
                railTicks[i].rectTransform.sizeDelta = new Vector2(current ? 18 : 12, 1);
                railNumbers[i].font = current ? fontBold : fontRegular;
            }
            if (showCard)
            {
                if (cardFade != null) StopCoroutine(cardFade);
                cardFade = StartCoroutine(ShowCardThenFade());
            }
        }

        IEnumerator ShowCardThenFade()
        {
            cardGroup.alpha = 1f;
            yield return new WaitForSeconds(7.5f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.9f;
                cardGroup.alpha = Mathf.Lerp(1f, 0.18f, t);
                yield return null;
            }
            cardGroup.alpha = 0.18f;
        }

        public void SetPrompt(string prompt, bool active)
        {
            if (stirMode)
            {
                promptRoot.gameObject.SetActive(true);
                promptGroup.alpha = 1f;
                keyText.text = "LMB";
                promptText.text = "按住并画圈搅洗";
                SetDims(56, 26);
                return;
            }
            if (active && !string.IsNullOrEmpty(prompt))
            {
                promptRoot.gameObject.SetActive(true);
                promptGroup.alpha = 1f;
                keyText.text = "E";
                promptText.text = prompt;
                SetDims(28, 26);
                reticleRing.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.7f);
                reticleRing.fillAmount = 1f;
            }
            else
            {
                promptGroup.alpha = 0f;
                reticleRing.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0f);
                reticleRing.fillAmount = 0f;
            }
        }

        void SetDims(float w, float h)
        {
            var key = keyText.transform.parent as RectTransform;
            if (key != null)
            {
                var le = key.GetComponent<LayoutElement>();
                if (le == null) le = key.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = w;
                le.preferredHeight = h;
            }
        }

        public void SetHoverLabel(string label)
        {
            if (!stirMode) promptText.text = label;
        }

        public void SetHoverActive(bool active)
        {
            if (stirMode) return;
            if (active)
            {
                reticleRing.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.7f);
                reticleRing.fillAmount = 1f;
            }
            else if (promptGroup.alpha <= 0.01f)
            {
                reticleRing.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0f);
                reticleRing.fillAmount = 0f;
            }
        }

        public void SetStirMode(bool active, float progress = 0f)
        {
            stirMode = active;
            stirProgress = Mathf.Clamp01(progress);
            if (active)
            {
                reticleRing.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0f);
                stirArc.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.9f);
                stirArc.fillAmount = stirProgress;
                SetPrompt(null, true);
            }
            else
            {
                stirArc.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0f);
                stirArc.fillAmount = 0f;
                promptGroup.alpha = 0f;
            }
        }

        public void SetStirProgress(float progress)
        {
            stirProgress = Mathf.Clamp01(progress);
            stirArc.fillAmount = stirProgress;
        }

        public void ShowToast(string text, float duration = 2.4f)
        {
            if (toastFade != null) StopCoroutine(toastFade);
            toastFade = StartCoroutine(ToastRoutine(text, duration));
        }

        IEnumerator ToastRoutine(string text, float duration)
        {
            toastText.text = text;
            toastGroup.alpha = 1f;
            yield return new WaitForSeconds(duration);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.6f;
                toastGroup.alpha = 1f - t;
                yield return null;
            }
            toastGroup.alpha = 0f;
        }

        public void ShowEnd()
        {
            cardGroup.alpha = 0f;
            promptGroup.alpha = 0f;
            hudGroup.alpha = 0f;
            endGroup.alpha = 0f;
            endGroup.blocksRaycasts = true;
            endGroup.interactable = true;
            StartCoroutine(FadeCanvas(endGroup, 1f, 0.8f, true));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ShowEndImmediate()
        {
            hudGroup.alpha = 0f;
            promptGroup.alpha = 0f;
            endGroup.alpha = 1f;
            endGroup.blocksRaycasts = true;
            endGroup.interactable = true;
        }

        public void OpenNotes()
        {
            if (notesOpen) return;
            notesOpen = true;
            notesGroup.blocksRaycasts = true;
            notesGroup.interactable = true;
            StartCoroutine(FadeCanvas(notesGroup, 1f, 0.22f, true));
        }

        public void CloseNotes()
        {
            if (!notesOpen) return;
            notesOpen = false;
            notesGroup.blocksRaycasts = false;
            notesGroup.interactable = false;
            StartCoroutine(FadeCanvas(notesGroup, 0f, 0.18f, false));
            if (NotesClosed != null) NotesClosed();
        }

        public void ToggleNotes()
        {
            if (notesOpen) CloseNotes();
            else OpenNotes();
        }

        public void SetHudVisible(bool visible)
        {
            hudGroup.alpha = visible ? 1f : 0f;
        }

        // ------------------------------------------------------------------ util
        IEnumerator FadeCanvas(CanvasGroup group, float target, float duration, bool interactable)
        {
            float start = group.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, duration);
                group.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = interactable;
            group.interactable = interactable;
            if (target <= 0.001f)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        void FullPaper(RectTransform root, float alpha)
        {
            var bg = Img(root, "PaperBase", new Color(233 / 255f, 227 / 255f, 214 / 255f, alpha), 0, 0);
            SetRect(bg.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var raw = new GameObject("PaperGrain", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            raw.transform.SetParent(root, false);
            var rawImg = raw.GetComponent<RawImage>();
            rawImg.texture = mats.PaperTexture;
            rawImg.color = new Color(1, 1, 1, 0.16f * alpha);
            rawImg.uvRect = new Rect(0, 0, 3.6f, 2.05f);
            rawImg.raycastTarget = false;
            SetRect(rawImg.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        }

        RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            ApplyAnchorPivot(r);
            return r;
        }

        static void Place(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            ApplyAnchorPivot(r);
        }

        static void SetRect(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            ApplyAnchorPivot(r);
        }

        static void ApplyAnchorPivot(RectTransform r)
        {
            float px = Mathf.Approximately(r.anchorMin.x, r.anchorMax.x)
                ? (r.anchorMin.x < 0.25f ? 0f : (r.anchorMin.x > 0.75f ? 1f : 0.5f))
                : 0.5f;
            float py = Mathf.Approximately(r.anchorMin.y, r.anchorMax.y)
                ? (r.anchorMin.y < 0.25f ? 0f : (r.anchorMin.y > 0.75f ? 1f : 0.5f))
                : 0.5f;
            r.pivot = new Vector2(px, py);
        }

        Text Text(Transform parent, string name, string content, Font font, int size, Color color, TextAnchor align, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.text = content;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            var r = (RectTransform)go.transform;
            r.sizeDelta = new Vector2(w, h);
            return t;
        }

        Image Img(Transform parent, string name, Color color, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(w, h);
            return img;
        }
    }

    internal static class UICharSpacing
    {
        // Kept as a tiny helper so the layout code can express letter-spaced editorial labels.
        public static void characterSpacingFix(this Text text, float spacing)
        {
            // Unity's legacy Text has no character spacing. Insert hair spaces only for short Latin labels.
            if (text == null) return;
            if (spacing > 1.0f && text.text.Length <= 24 && !ContainsCjk(text.text))
            {
                var chars = text.text.ToCharArray();
                text.text = string.Join(" ", chars).Replace("  ", " ");
            }
        }

        static bool ContainsCjk(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] >= 0x2E80) return true;
            }
            return false;
        }
    }
}
