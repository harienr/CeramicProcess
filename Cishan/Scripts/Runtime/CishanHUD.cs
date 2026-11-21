using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cishan
{
    /// <summary>
    /// Paper-and-ink interface. Built in code so the scene carries no prefab
    /// dependencies; the editor builder can call Build() at edit time, and the
    /// same hierarchy is saved into the scene.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CishanHUD : MonoBehaviour
    {
        public Font bodyFont;
        public Font titleFont;

        public CanvasGroup chapterGroup;
        public CanvasGroup objectiveGroup;
        public CanvasGroup tagGroup;
        public CanvasGroup promptGroup;
        public CanvasGroup subtitleGroup;
        public CanvasGroup safetyGroup;
        public CanvasGroup notebookGroup;
        public CanvasGroup completeGroup;

        public Text chapterNo;
        public Text chapterTitle;
        public Text chapterSub;
        public Text objectiveLabel;
        public Text objectiveText;
        public Text tagText;
        public Text pendingText;
        public Text promptVerb;
        public Text promptHint;
        public Text subtitleText;
        public Text safetyText;
        public Text notebookTitle;
        public Text notebookBody;
        public Text completeQuote;
        public Text completeSub;

        public Image reticle;
        public RectTransform holdLine;
        public Image fade;
        public Image tagPanel;
        public Image promptPanel;
        public RectTransform notebookRect;

        Coroutine subtitleRoutine;
        Coroutine chapterRoutine;
        Coroutine fadeRoutine;
        Coroutine notebookRoutine;
        bool notebookOpen;

        public bool NotebookOpen { get { return notebookOpen; } }

        // ------------------------------------------------------------------ build
        public static CishanHUD Build(Font body, Font title)
        {
            CishanHUD existing = Object.FindObjectOfType<CishanHUD>();
            if (existing != null) return existing;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(null);
            }

            GameObject root = new GameObject("UI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CishanHUD hud = root.AddComponent<CishanHUD>();
            hud.bodyFont = body;
            hud.titleFont = title;
            hud.BuildHierarchy();
            return hud;
        }

        void BuildHierarchy()
        {
            Transform t = transform;

            // ---- chapter card (top left)
            chapterGroup = Group(t, "Chapter", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(56f, -44f), new Vector2(620f, 180f));
            chapterNo = MakeText(chapterGroup.transform, "No", "06", 58, Palette.Blue,
                TextAnchor.UpperLeft, true);
            SetRect(chapterNo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(140f, 76f));
            Image chapterRule = Rule(chapterGroup.transform, "Rule", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(64f, 2f), Palette.Blue);
            chapterRule.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            chapterTitle = MakeText(chapterGroup.transform, "Title", "舂石", 34, Palette.Ink,
                TextAnchor.UpperLeft, true);
            SetRect(chapterTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -86f), new Vector2(520f, 48f));
            chapterSub = MakeText(chapterGroup.transform, "Sub", "水碓棚 · 净石 → 石粉", 17,
                Palette.Muted, TextAnchor.UpperLeft, false);
            SetRect(chapterSub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -132f), new Vector2(520f, 30f));
            chapterGroup.alpha = 0f;

            // ---- objective
            objectiveGroup = Group(t, "Objective", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(56f, -248f), new Vector2(440f, 108f));
            objectiveLabel = MakeText(objectiveGroup.transform, "Label", "当前", 14, Palette.Muted,
                TextAnchor.UpperLeft, false);
            SetRect(objectiveLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(200f, 20f));
            Image objRule = Rule(objectiveGroup.transform, "Rule", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, 2f), Palette.Line);
            objRule.rectTransform.anchoredPosition = new Vector2(0f, -26f);
            objectiveText = MakeText(objectiveGroup.transform, "Text", "", 20, Palette.Ink,
                TextAnchor.UpperLeft, false);
            SetRect(objectiveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -38f), new Vector2(440f, 66f));
            objectiveGroup.alpha = 0f;

            // ---- material observation tag (top right)
            tagGroup = Group(t, "MaterialTag", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-56f, -48f), new Vector2(272f, 66f));
            tagPanel = Panel(tagGroup.transform, "Panel", Palette.Alpha(Palette.Paper, 0.93f));
            Stretch(tagPanel.rectTransform);
            Image tagRule = Rule(tagGroup.transform, "Rule", new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(2f, 66f), Palette.Blue);
            tagRule.rectTransform.anchorMin = new Vector2(0f, 0f);
            tagRule.rectTransform.anchorMax = new Vector2(0f, 1f);
            tagRule.rectTransform.sizeDelta = new Vector2(2f, 0f);
            tagRule.rectTransform.anchoredPosition = Vector2.zero;
            tagText = MakeText(tagGroup.transform, "Text", "", 16, Palette.Ink,
                TextAnchor.MiddleLeft, false);
            SetRect(tagText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, 8f), new Vector2(240f, 24f));
            pendingText = MakeText(tagGroup.transform, "Pending", "待核实", 12, Palette.Muted,
                TextAnchor.MiddleLeft, false);
            SetRect(pendingText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, -16f), new Vector2(240f, 18f));
            tagGroup.alpha = 0f;

            // ---- prompt (bottom, max two lines)
            promptGroup = Group(t, "Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 56f), new Vector2(440f, 96f));
            promptPanel = Panel(promptGroup.transform, "Panel", Palette.Alpha(Palette.Paper, 0.94f));
            Stretch(promptPanel.rectTransform);
            promptVerb = MakeText(promptGroup.transform, "Verb", "", 22, Palette.Ink,
                TextAnchor.UpperLeft, false);
            SetRect(promptVerb.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -16f), new Vector2(396f, 30f));
            promptHint = MakeText(promptGroup.transform, "Hint", "", 16, Palette.Muted,
                TextAnchor.UpperLeft, false);
            SetRect(promptHint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -46f), new Vector2(396f, 42f));
            holdLine = Rule(promptGroup.transform, "HoldLine", new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(440f, 2f), Palette.Blue).rectTransform;
            holdLine.anchorMin = new Vector2(0f, 0f);
            holdLine.anchorMax = new Vector2(0f, 0f);
            holdLine.pivot = new Vector2(0f, 0f);
            holdLine.anchoredPosition = Vector2.zero;
            holdLine.sizeDelta = new Vector2(440f, 2f);
            holdLine.localScale = new Vector3(0f, 1f, 1f);
            promptGroup.alpha = 0f;

            // ---- reticle
            GameObject dot = new GameObject("Reticle", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(t, false);
            reticle = dot.GetComponent<Image>();
            reticle.color = Palette.Alpha(Palette.Ink, 0.55f);
            reticle.raycastTarget = false;
            RectTransform dr = reticle.rectTransform;
            dr.anchorMin = new Vector2(0.5f, 0.5f);
            dr.anchorMax = new Vector2(0.5f, 0.5f);
            dr.pivot = new Vector2(0.5f, 0.5f);
            dr.anchoredPosition = Vector2.zero;
            dr.sizeDelta = new Vector2(3f, 3f);

            // ---- subtitle
            subtitleGroup = Group(t, "Subtitle", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(56f, 160f), new Vector2(660f, 76f));
            subtitleText = MakeText(subtitleGroup.transform, "Text", "", 18, Palette.InkSoft,
                TextAnchor.LowerLeft, false);
            Stretch(subtitleText.rectTransform);
            Shadow sh = subtitleText.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.1f, 0.09f, 0.08f, 0.35f);
            sh.effectDistance = new Vector2(0f, 1f);
            subtitleGroup.alpha = 0f;

            // ---- safety line
            safetyGroup = Group(t, "Safety", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 250f), new Vector2(600f, 40f));
            safetyText = MakeText(safetyGroup.transform, "Text", "", 20, Palette.Wood,
                TextAnchor.MiddleCenter, false);
            Stretch(safetyText.rectTransform);
            safetyGroup.alpha = 0f;

            // ---- notebook drawer
            notebookRect = RectNode(t, "Notebook", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(360f, 0f));
            notebookGroup = notebookRect.gameObject.AddComponent<CanvasGroup>();
            Image nbPanel = Panel(notebookRect, "Panel", Palette.Alpha(Palette.Paper, 0.985f));
            Stretch(nbPanel.rectTransform);
            Image nbEdge = Rule(notebookRect, "Edge", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(1f, 0f), Palette.Line);
            nbEdge.rectTransform.anchorMin = new Vector2(0f, 0f);
            nbEdge.rectTransform.anchorMax = new Vector2(0f, 1f);
            nbEdge.rectTransform.sizeDelta = new Vector2(1f, 0f);
            nbEdge.rectTransform.anchoredPosition = Vector2.zero;

            notebookTitle = MakeText(notebookRect, "Title", "学徒手册", 24, Palette.Ink,
                TextAnchor.UpperLeft, true);
            SetRect(notebookTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -34f), new Vector2(300f, 36f));
            Image nbRule = Rule(notebookRect, "Rule", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, 2f), Palette.Blue);
            nbRule.rectTransform.anchoredPosition = new Vector2(28f, -78f);

            notebookBody = MakeText(notebookRect, "Body", NotebookText, 16, Palette.InkSoft,
                TextAnchor.UpperLeft, false);
            notebookBody.lineSpacing = 1.45f;
            SetRect(notebookBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -100f), new Vector2(304f, 820f));

            Text nbHint = MakeText(notebookRect, "Hint", "Tab 关闭", 13, Palette.Muted,
                TextAnchor.LowerLeft, false);
            SetRect(nbHint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(28f, 32f), new Vector2(200f, 24f));
            notebookGroup.alpha = 0f;
            notebookRect.anchoredPosition = new Vector2(360f, 0f);

            // ---- completion card
            completeGroup = Group(t, "Complete", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(760f, 340f));
            Image compPanel = Panel(completeGroup.transform, "Panel", Palette.Alpha(Palette.Paper, 0.96f));
            Stretch(compPanel.rectTransform);
            completeQuote = MakeText(completeGroup.transform, "Quote",
                "水轮不停，碓头起落；\n人只负责在正确的时候关闸。", 26, Palette.Ink,
                TextAnchor.MiddleLeft, true);
            completeQuote.lineSpacing = 1.4f;
            SetRect(completeQuote.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(56f, -48f), new Vector2(648f, 120f));
            Image compRule = Rule(completeGroup.transform, "Rule", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(64f, 2f), Palette.Blue);
            compRule.rectTransform.anchoredPosition = new Vector2(56f, -186f);
            completeSub = MakeText(completeGroup.transform, "Sub",
                "06 舂石 · 完成　下一道工序：07 淘洗", 16, Palette.Muted,
                TextAnchor.UpperLeft, false);
            SetRect(completeSub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(56f, -206f), new Vector2(648f, 60f));
            completeGroup.alpha = 0f;

            // ---- fade, created last so it covers everything
            Image fadeImg = Panel(t, "Fade", Palette.Alpha(Palette.Paper, 0f));
            fade = fadeImg;
            Stretch(fade.rectTransform);
            fade.raycastTarget = false;
            fade.transform.SetAsLastSibling();

            ApplyFonts();
        }

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (chapterNo == null) Resolve();
            ApplyFonts();
            HidePrompt();
            if (safetyGroup != null) safetyGroup.alpha = 0f;
            if (tagGroup != null) tagGroup.alpha = 0f;
            if (notebookGroup != null)
            {
                notebookGroup.alpha = 0f;
                if (notebookRect != null) notebookRect.anchoredPosition = new Vector2(360f, 0f);
            }
            if (completeGroup != null) completeGroup.alpha = 0f;
        }

        void Start()
        {
            if (fade != null)
            {
                fade.color = Palette.Paper;
                fadeRoutine = StartCoroutine(FadeRoutine());
            }
        }

        void Resolve()
        {
            chapterGroup = FindGroup(transform, "Chapter");
            objectiveGroup = FindGroup(transform, "Objective");
            tagGroup = FindGroup(transform, "MaterialTag");
            promptGroup = FindGroup(transform, "Prompt");
            subtitleGroup = FindGroup(transform, "Subtitle");
            safetyGroup = FindGroup(transform, "Safety");
            notebookRect = transform.Find("Notebook") as RectTransform;
            notebookGroup = notebookRect != null ? notebookRect.GetComponent<CanvasGroup>() : null;
            completeGroup = FindGroup(transform, "Complete");
            if (chapterGroup != null)
            {
                chapterNo = FindText(chapterGroup.transform, "No");
                chapterTitle = FindText(chapterGroup.transform, "Title");
                chapterSub = FindText(chapterGroup.transform, "Sub");
            }
            if (objectiveGroup != null)
            {
                objectiveLabel = FindText(objectiveGroup.transform, "Label");
                objectiveText = FindText(objectiveGroup.transform, "Text");
            }
            if (tagGroup != null)
            {
                tagText = FindText(tagGroup.transform, "Text");
                pendingText = FindText(tagGroup.transform, "Pending");
            }
            if (promptGroup != null)
            {
                promptVerb = FindText(promptGroup.transform, "Verb");
                promptHint = FindText(promptGroup.transform, "Hint");
                Transform hl = promptGroup.transform.Find("HoldLine");
                holdLine = hl as RectTransform;
            }
            if (subtitleGroup != null) subtitleText = FindText(subtitleGroup.transform, "Text");
            if (safetyGroup != null) safetyText = FindText(safetyGroup.transform, "Text");
            if (notebookRect != null)
            {
                notebookTitle = FindText(notebookRect, "Title");
                notebookBody = FindText(notebookRect, "Body");
            }
            if (completeGroup != null)
            {
                completeQuote = FindText(completeGroup.transform, "Quote");
                completeSub = FindText(completeGroup.transform, "Sub");
            }
            Transform rt = transform.Find("Reticle");
            reticle = rt != null ? rt.GetComponent<Image>() : null;
            Transform fd = transform.Find("Fade");
            fade = fd != null ? fd.GetComponent<Image>() : null;
        }

        static Text FindText(Transform parent, string name)
        {
            Transform c = parent.Find(name);
            return c != null ? c.GetComponent<Text>() : null;
        }

        static CanvasGroup FindGroup(Transform parent, string name)
        {
            Transform c = parent.Find(name);
            return c != null ? c.GetComponent<CanvasGroup>() : null;
        }

        void ApplyFonts()
        {
            Font body = bodyFont;
            if (body == null)
            {
                body = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Noto Sans SC", "PingFang SC", "SimHei" }, 22);
                bodyFont = body;
            }
            Font title = titleFont != null ? titleFont : body;
            foreach (Text txt in GetComponentsInChildren<Text>(true))
            {
                bool serif = txt == chapterNo || txt == chapterTitle || txt == notebookTitle ||
                             txt == completeQuote;
                txt.font = serif ? title : body;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleNotebook();
        }

        // ------------------------------------------------------------------ API
        public void ShowChapter(string no, string title, string sub, float holdSeconds = 4f)
        {
            if (chapterGroup == null) return;
            if (chapterNo != null) chapterNo.text = no;
            if (chapterTitle != null) chapterTitle.text = title;
            if (chapterSub != null) chapterSub.text = sub;
            if (chapterRoutine != null) StopCoroutine(chapterRoutine);
            chapterRoutine = StartCoroutine(ChapterRoutine(holdSeconds));
        }

        IEnumerator ChapterRoutine(float holdSeconds)
        {
            yield return FadeGroup(chapterGroup, 1f, 0.18f);
            yield return new WaitForSeconds(holdSeconds);
            yield return FadeGroup(chapterGroup, 0f, 0.6f);
        }

        public void SetObjective(string text)
        {
            if (objectiveText == null) return;
            objectiveText.text = text;
            StopCoroutineSafe(ref objectiveRoutine);
            objectiveRoutine = StartCoroutine(FadeGroup(objectiveGroup, 1f, 0.18f));
        }

        Coroutine objectiveRoutine;

        public void SetTag(string text, bool pending = false)
        {
            if (tagGroup == null) return;
            if (string.IsNullOrEmpty(text))
            {
                tagGroup.alpha = 0f;
                return;
            }
            if (tagText != null) tagText.text = text;
            if (pendingText != null) pendingText.enabled = pending;
            tagGroup.alpha = 1f;
        }

        public void ShowPrompt(string verb, string hint, bool showHold)
        {
            if (promptGroup == null) return;
            if (promptVerb != null) promptVerb.text = verb;
            if (promptHint != null) promptHint.text = hint;
            if (holdLine != null)
            {
                holdLine.gameObject.SetActive(showHold);
            }
            promptGroup.alpha = 1f;
        }

        public void SetHold(float t)
        {
            if (holdLine != null) holdLine.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
        }

        public void HidePrompt()
        {
            if (promptGroup != null) promptGroup.alpha = 0f;
            if (holdLine != null)
            {
                holdLine.localScale = new Vector3(0f, 1f, 1f);
            }
        }

        public void ShowSubtitle(string text, float seconds = 3f)
        {
            if (subtitleText == null) return;
            subtitleText.text = text;
            if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
            subtitleRoutine = StartCoroutine(SubtitleRoutine(seconds));
        }

        IEnumerator SubtitleRoutine(float seconds)
        {
            yield return FadeGroup(subtitleGroup, 1f, 0.15f);
            yield return new WaitForSeconds(seconds);
            yield return FadeGroup(subtitleGroup, 0f, 0.5f);
        }

        public void SetSafety(string text)
        {
            if (safetyGroup == null) return;
            if (string.IsNullOrEmpty(text))
            {
                safetyGroup.alpha = 0f;
                if (safetyText != null) safetyText.text = "";
                return;
            }
            if (safetyText != null) safetyText.text = text;
            safetyGroup.alpha = 1f;
        }

        public void ToggleNotebook()
        {
            SetNotebook(!notebookOpen);
        }

        public void SetNotebook(bool open)
        {
            if (notebookGroup == null || notebookRect == null) return;
            notebookOpen = open;
            if (notebookRoutine != null) StopCoroutine(notebookRoutine);
            notebookRoutine = StartCoroutine(NotebookRoutine(open));
        }

        IEnumerator NotebookRoutine(bool open)
        {
            float from = notebookRect.anchoredPosition.x;
            float to = open ? 0f : 360f;
            float a0 = notebookGroup.alpha;
            float a1 = open ? 1f : 0f;
            float dur = 0.18f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                notebookRect.anchoredPosition = new Vector2(Mathf.Lerp(from, to, k), 0f);
                notebookGroup.alpha = Mathf.Lerp(a0, a1, k);
                yield return null;
            }
            notebookRect.anchoredPosition = new Vector2(to, 0f);
            notebookGroup.alpha = a1;
        }

        public void ShowComplete(string quote, string sub)
        {
            if (completeQuote != null) completeQuote.text = quote;
            if (completeSub != null) completeSub.text = sub;
            if (completeGroup != null) StartCoroutine(CompleteRoutine());
        }

        IEnumerator CompleteRoutine()
        {
            yield return FadeGroup(completeGroup, 1f, 0.5f);
            yield return new WaitForSeconds(8f);
            yield return FadeGroup(completeGroup, 0f, 1.2f);
        }

        public void SetReticleFocus(bool focused)
        {
            if (reticle == null) return;
            reticle.rectTransform.localScale = Vector3.one * (focused ? 1.7f : 1f);
            reticle.color = Palette.Alpha(Palette.Ink, focused ? 0.85f : 0.55f);
        }

        IEnumerator FadeRoutine()
        {
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                fade.color = Palette.Alpha(Palette.Paper, 1f - Mathf.SmoothStep(0f, 1f, t / 1.2f));
                yield return null;
            }
            fade.color = Palette.Alpha(Palette.Paper, 0f);
            fade.raycastTarget = false;
        }

        IEnumerator FadeGroup(CanvasGroup g, float target, float dur)
        {
            if (g == null) yield break;
            float from = g.alpha;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                g.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / dur));
                yield return null;
            }
            g.alpha = target;
        }

        void StopCoroutineSafe(ref Coroutine c)
        {
            if (c != null) StopCoroutine(c);
        }

        // ------------------------------------------------------------------ helpers
        static CanvasGroup Group(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            RectTransform rt = RectNode(parent, name, anchorMin, anchorMax, anchorMin, pos, size);
            CanvasGroup g = rt.gameObject.AddComponent<CanvasGroup>();
            g.interactable = false;
            g.blocksRaycasts = false;
            return g;
        }

        static RectTransform RectNode(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Image Panel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Image Rule(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 size, Color color)
        {
            Image img = Panel(parent, name, color);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        static Text MakeText(Transform parent, string name, string content, int size,
            Color color, TextAnchor align, bool bold)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text txt = go.GetComponent<Text>();
            txt.text = content;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = align;
            txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            txt.lineSpacing = 1.25f;
            return txt;
        }

        public const string NotebookText =
            "06 舂石\n" +
            "净石放入碓臼，开闸引水；水轮带动轮轴与凸轮，凸轮压下碓杆尾端，碓头抬起，脱开后自重落下，反复砸碎瓷石。\n\n" +
            "手要做的只有两件事：在正确的时候开闸，在正确的时候关闸。碓未停稳，手不入臼。\n\n" +
            "材料\n" +
            "净石 → 粗粉 → 细粉（灰白、极哑、不结块）。\n" +
            "舂完后舀入木桶，送去 07 淘洗。\n\n" +
            "待核实\n" +
            "· 轮径 3.0–3.5 m（现取 3.1 m）\n" +
            "· 凸轮角度与升程\n" +
            "· 单轮双碓为建议，碓数待定\n" +
            "· 碓头重量与碓臼形制\n\n" +
            "操作\n" +
            "E 按住 · 开 / 关闸\n" +
            "E · 舀粉 / 查看\n" +
            "右键按住 · 前倾观察\n" +
            "Tab · 手册　Esc · 释放鼠标";
    }
}
