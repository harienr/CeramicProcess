using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CiCongNiSheng.Journey
{
    public sealed class JourneyHUD : MonoBehaviour
    {
        public JourneyWorkshop workshop;
        Canvas canvas;
        RectTransform book;
        RectTransform pause;
        RectTransform marker;
        RectTransform restart;
        RectTransform settings;
        Text settingsInfo;
        Text objective;
        Text chapter;
        Text prompt;
        Text cargo;
        Text navigation;
        Text markerLabel;
        Text material;
        Text bookTitle;
        Text bookConcept;
        Text bookTasks;
        Text bookFoot;
        Text toast;
        Text[] chapterRows;
        Image progress;
        Image workProgress;
        float toastUntil;
        bool bookOpen;
        bool shownComplete;
        int selectedPage;


        void Start()
        {
            if (canvas == null) Build();
        }

        void OnEnable()
        {
            HideExtraHudInfo();
        }

        void HideExtraHudInfo()
        {
            if (canvas == null) return;
            if (navigation != null) navigation.gameObject.SetActive(false);
            if (material != null) material.gameObject.SetActive(false);
            foreach (Transform child in canvas.transform)
            {
                if (child.name == "瓷从泥生  /  原料工坊" || child.name == "Tab 任务书")
                    child.gameObject.SetActive(false);
            }
        }

        RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            return UIFactory.Place(UIFactory.Node(parent, name), Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.one * 0.5f, new Vector2(x, y), new Vector2(w, h));
        }

        Text Label(Transform parent, string value, float x, float y, float w, float h, int size = 24,
            TextAnchor alignment = TextAnchor.MiddleLeft, Color? color = null)
        {
            return UIFactory.AddText(Rect(parent, value, x, y, w, h), value, size, color ?? Palette.InkText, alignment);
        }

        Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            return UIFactory.AddImage(Rect(parent, name, x, y, w, h), color);
        }

        Button Button(Transform parent, string label, float x, float y, float w, System.Action action)
        {
            var rt = Rect(parent, label, x, y, w, 48f);
            var image = UIFactory.AddImage(rt, Palette.Cobalt);
            image.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            Label(rt, label, 0, 0, w - 12, 48, 21, TextAnchor.MiddleCenter, Palette.RiceWhite);
            return button;
        }

        void Build()
        {
            if (EventSystem.current == null)
                new GameObject("工坊界面事件", typeof(EventSystem), typeof(StandaloneInputModule));
            canvas = UIFactory.CreateCanvas("瓷从泥生 · 任务界面", 80);
            canvas.transform.SetParent(transform, false);
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Panel(canvas.transform, "任务底", -600, 417, 620, 150, Palette.PanelFill);
            chapter = Label(canvas.transform, "05 / 筛洗", -600, 462, 570, 40, 25, TextAnchor.MiddleLeft, Palette.Cobalt);
            objective = Label(canvas.transform, "", -600, 407, 570, 76, 23);
            objective.verticalOverflow = VerticalWrapMode.Truncate;
            progress = Panel(canvas.transform, "总进度", -600, 340, 620, 4, Palette.Cobalt);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            Label(canvas.transform, "瓷从泥生  /  原料工坊", 655, 468, 490, 32, 25, TextAnchor.MiddleRight, Palette.Cobalt);
            Button(canvas.transform, "Tab 任务书", 800, 414, 190, ToggleBook);
            navigation = Label(canvas.transform, "", 0, 455, 520, 40, 22, TextAnchor.MiddleCenter, Palette.Cobalt);
            Label(canvas.transform, "·", 0, 0, 30, 30, 32, TextAnchor.MiddleCenter, Palette.RiceWhite);
            Panel(canvas.transform, "操作提示底", 0, -370, 910, 94, Palette.PanelFill);
            prompt = Label(canvas.transform, "", 0, -365, 870, 70, 23, TextAnchor.MiddleCenter);
            workProgress = Panel(canvas.transform, "操作进度", 0, -420, 440, 5, Palette.Cobalt);
            workProgress.type = Image.Type.Filled;
            workProgress.fillMethod = Image.FillMethod.Horizontal;
            cargo = Label(canvas.transform, "", -660, -470, 520, 35, 23, TextAnchor.MiddleLeft, Palette.RiceWhite);
            material = Label(canvas.transform, "", 660, -450, 510, 80, 21, TextAnchor.MiddleRight, Palette.RiceWhite);
            Label(canvas.transform, "WASD 行走    F 就位并对准    按住 E 操作    R 清筛渣    Tab 任务书    Esc 画面与暂停", 0, -515, 1700, 32, 19, TextAnchor.MiddleCenter, Palette.RiceWhite);
            toast = Label(canvas.transform, "", 0, 305, 1180, 64, 24, TextAnchor.MiddleCenter, Palette.Cobalt);
            marker = Rect(canvas.transform, "目标指引", 0, 130, 300, 64);
            markerLabel = Label(marker, "◇", 0, 0, 300, 64, 23, TextAnchor.MiddleCenter, Palette.Cobalt);
            BuildBook();
            BuildPause();
            BuildRestart();
            BuildSettings();
            HideExtraHudInfo();
        }

        void BuildBook()
        {
            book = Rect(canvas.transform, "学徒任务书", 0, 0, 1350, 820);
            UIFactory.AddImage(book, Palette.RiceWhite).raycastTarget = true;
            Panel(book, "书脊", -337, 0, 2, 745, Palette.CobaltLine);
            Label(book, "学 徒 任 务 书", -490, 326, 260, 65, 31, TextAnchor.MiddleCenter, Palette.Cobalt);
            Label(book, "原料的一次旅程\n净石 → 石粉 → 粗泥", -490, 245, 260, 80, 22, TextAnchor.MiddleCenter);
            chapterRows = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                int page = i;
                Button(book, (i + 5).ToString("00") + "  " + JourneyFlow.Names[i], -490, 120 - i * 76, 245, () => { selectedPage = page; RefreshBook(); });
                chapterRows[i] = Label(book, "", -490, 82 - i * 76, 245, 28, 17, TextAnchor.MiddleCenter, Palette.InkMuted);
            }
            Label(book, "按工序先后完成\n将每一批原料亲手送到下一站", -490, -287, 270, 80, 19, TextAnchor.MiddleCenter, Palette.InkMuted);
            bookTitle = Label(book, "", 152, 317, 850, 60, 33, TextAnchor.MiddleLeft, Palette.Cobalt);
            bookConcept = Label(book, "", 152, 222, 850, 120, 23);
            bookConcept.verticalOverflow = VerticalWrapMode.Truncate;
            Panel(book, "正文线", 152, 145, 850, 2, Palette.CobaltLine);
            bookTasks = Label(book, "", 152, -22, 850, 310, 23, TextAnchor.UpperLeft);
            bookTasks.verticalOverflow = VerticalWrapMode.Truncate;
            bookTasks.lineSpacing = 1.22f;
            bookFoot = Label(book, "", 152, -243, 850, 112, 20, TextAnchor.UpperLeft, Palette.InkMuted);
            bookFoot.verticalOverflow = VerticalWrapMode.Truncate;
            Button(book, "开始 / 返回工坊", 365, -352, 290, CloseBook);
            Button(book, "重新体验", 35, -352, 240, () => { book.gameObject.SetActive(false); ShowRestart(); });
            book.gameObject.SetActive(false);
        }

        void BuildPause()
        {
            pause = Rect(canvas.transform, "暂停", 0, 0, 640, 640);
            UIFactory.AddImage(pause, Palette.RiceWhite).raycastTarget = true;
            Label(pause, "工坊小憩", 0, 245, 540, 60, 34, TextAnchor.MiddleCenter, Palette.Cobalt);
            Button(pause, "画面与操作", 0, 145, 330, ShowSettings);
            Button(pause, "继续体验", 0, 75, 330, Resume);
            Button(pause, "查看任务书", 0, 5, 330, () => { pause.gameObject.SetActive(false); ShowBook(false); });
            Button(pause, "返回工坊中央", 0, -65, 330, () => { workshop.Respawn(); Resume(); });
            Button(pause, "重新体验全部工序", 0, -135, 330, ShowRestart);
            pause.gameObject.SetActive(false);
        }

        void BuildSettings()
        {
            settings = Rect(canvas.transform, "画面与操作设置", 0, 0, 880, 700);
            UIFactory.AddImage(settings, Palette.RiceWhite).raycastTarget = true;
            Label(settings, "画面与操作", 0, 280, 790, 60, 32, TextAnchor.MiddleCenter, Palette.Cobalt);
            settingsInfo = Label(settings, "", 0, 190, 780, 95, 22, TextAnchor.MiddleCenter);
            Button(settings, "清晰画质（推荐）", -200, 95, 340, () => workshop.ApplyClarity(true));
            Button(settings, "流畅画质", 200, 95, 340, () => workshop.ApplyClarity(false));
            Button(settings, "使用屏幕原生分辨率", 0, 20, 500, workshop.UseNativeResolution);
            Button(settings, "较近视角 54°", -200, -55, 340, () => workshop.Camera.fieldOfView = 54f);
            Button(settings, "标准视角 60°", 200, -55, 340, () => workshop.Camera.fieldOfView = 60f);
            Button(settings, "鼠标灵敏度：慢", -200, -130, 340, () => workshop.Movement.mouseSensitivity = 1.2f);
            Button(settings, "鼠标灵敏度：标准", 200, -130, 340, () => workshop.Movement.mouseSensitivity = 2.1f);
            Button(settings, "返回工坊", 0, -225, 360, Resume);
            Label(settings, "F：靠近后就位并对准器具。按住 E 可完成摇洗、耙洗、上料。", 0, -295, 810, 45, 19, TextAnchor.MiddleCenter, Palette.InkMuted);
            settings.gameObject.SetActive(false);
        }

        public void ShowSettings()
        {
            bookOpen = false;
            book.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            restart.gameObject.SetActive(false);
            settings.gameObject.SetActive(true);
            workshop.SetModal(true);
        }
        void BuildRestart()
        {
            restart = Rect(canvas.transform, "重新体验确认", 0, 0, 640, 450);
            UIFactory.AddImage(restart, Palette.RiceWhite).raycastTarget = true;
            Label(restart, "从第 5 步筛洗重新开始？", 0, 130, 590, 70, 30, TextAnchor.MiddleCenter, Palette.Cobalt);
            Label(restart, "本次操作进度会重新计数。", 0, 35, 560, 70, 22, TextAnchor.MiddleCenter);
            Button(restart, "确认重新开始", 0, -60, 330, workshop.Restart);
            Button(restart, "保留进度，返回工坊", 0, -135, 330, Resume);
            restart.gameObject.SetActive(false);
        }

        void ShowRestart()
        {
            bookOpen = false;
            book.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            restart.gameObject.SetActive(true);
            workshop.SetModal(true);
        }
        public void ShowBook(bool intro)
        {
            if (canvas == null) Build();
            bookOpen = true;
            selectedPage = workshop.Flow.Chapter;
            pause.gameObject.SetActive(false);
            restart.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            book.gameObject.SetActive(true);
            workshop.SetModal(true);
            RefreshBook();
        }

        public void ToggleBook()
        {
            if (bookOpen) CloseBook(); else ShowBook(false);
        }

        public void CloseBook()
        {
            bookOpen = false;
            pause.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            book.gameObject.SetActive(false);
            workshop.SetModal(false);
        }

        public void ShowPause()
        {
            if (canvas == null) Build();
            if (pause.gameObject.activeSelf) { Resume(); return; }
            bookOpen = false;
            book.gameObject.SetActive(false);
            restart.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            pause.gameObject.SetActive(true);
            workshop.SetModal(true);
        }

        void Resume()
        {
            bookOpen = false;
            book.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            restart.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            workshop.SetModal(false);
        }

        void RefreshBook()
        {
            var flow = workshop.Flow;
            for (int i = 0; i < 4; i++)
                chapterRows[i].text = flow.Completed || i < flow.Chapter ? "已完成" : i == flow.Chapter ? "当前工序" : "等待上一工序";
            bookTitle.text = flow.Completed ? "第 5—8 步完成 · 过筛浆已入池" : (selectedPage + 5).ToString("00") + " / " + JourneyFlow.Names[selectedPage];
            bookConcept.text = JourneyFlow.Concepts[selectedPage];
            string[] instructions =
            {
                "① 将待洗瓷石放进竹筛。\n\n② 浅水中摇洗，让附着泥沙离开石块。\n\n③ 挑出夹杂物，放入废杂物盘。\n\n④ 取走净石，送往碓臼。",
                "① 把净石分装进两只碓臼。\n\n② 开水闸，退到护栏外守碓。\n\n③ 观察碓头起落，石块逐渐变成石粉。\n\n④ 关闸并等轮碓停稳，再从两臼各取一瓢粉。\n\n⑤ 将石粉送入淘洗池。",
                "① 把舂后石粉浸入水中。\n\n② 用泥耙反复淘洗，分出粗渣。\n\n③ 粗渣返碓臼：开闸 → 守碓 → 关闸 → 取粉。\n\n④ 将返舂石粉送回淘洗池，再用泥耙淘洗。\n\n⑤ 收取淘洗后的粗泥，送去过筛。",
                "① 把淘洗粗泥倒入待筛料缸。\n\n② 舀取粗泥上筛，轻摇筛面。\n\n③ 细料随浆水流入筛下沉淀池，细渣留在筛上。\n\n④ 按 R 把细渣刮入弃渣盘，再检查沉淀池。"
            };
            bookTasks.text = instructions[selectedPage];
            bookFoot.text = flow.Completed
                ? "用时 " + Mathf.FloorToInt(workshop.Elapsed / 60f) + " 分 " + Mathf.FloorToInt(workshop.Elapsed % 60f) + " 秒 · 已完成粗渣返舂\n本段结束于过筛入沉淀池；第 9 步制浆不在本次四工序中。"
                : "当前任务：" + flow.Objective + "\n沿通道按 05 → 08 操作 · 靠近后按 F 就位 · 按住 E 操作 · R 清筛渣\n依据你提供的工序截图，第 5—8 步；操作时长为教学压缩。";
        }
        void Update()
        {
            if (workshop == null || !workshop.IsReady || canvas == null) return;
            var flow = workshop.Flow;
            RefreshDisplayInfo();
            chapter.text = flow.ArticleNumber.ToString("00") + " / " + JourneyFlow.Names[flow.Chapter] + "    ·    " + (flow.Completed ? "完成" : "学徒任务");
            objective.text = flow.Objective;
            progress.fillAmount = flow.Progress;
            cargo.text = "携带 / " + flow.Cargo;
            prompt.text = workshop.Busy ? "正在操作，请稍候…" : workshop.Player.Hovered != null ? workshop.Player.PromptText :
                flow.Step == JourneyStep.WatchHammers || flow.Step == JourneyStep.WatchRegrind ? "在工位前方标记处守碓，观察石料变细" : "沿通道靠近目标，按 F 就位并对准";
            var target = workshop.Player.Hovered as JourneyInteractable;
            workProgress.fillAmount = target != null ? target.HoldProgress : 0f;
            if (flow.Step == JourneyStep.RinseStones || flow.Step == JourneyStep.Stir || flow.Step == JourneyStep.StirAgain) workProgress.fillAmount = flow.Work;
            if (flow.Step == JourneyStep.WatchHammers) workProgress.fillAmount = (workshop.Mortars[0].Fill01 + workshop.Mortars[1].Fill01) * 0.5f;
            if (flow.Step == JourneyStep.WatchRegrind) workProgress.fillAmount = workshop.Mortars[0].Fill01;
            if (flow.Step == JourneyStep.ShakeSieve) workProgress.fillAmount = workshop.Sieve.st.Purity01;
            if (flow.Chapter == 0) material.text = "瓷石 → 净石\n清洁度 " + Mathf.RoundToInt(workshop.StoneWash.Cleanliness * 100f) + "% · " + (workshop.StoneWash.DebrisRemoved ? "杂物已剔除" : "待除杂物");
            else if (flow.Chapter == 1) material.text = "净石 → 石粉\n已取粉 " + flow.Scoops + " / 2 臼";
            else if (flow.Chapter == 2) material.text = "石粉 → 粗泥\n" + (flow.Reground ? "返舂石粉已回池" : "粗渣须返回碓臼继续舂打");
            else material.text = "粗泥 → 过筛浆\n沉淀池蓄浆 " + Mathf.RoundToInt(workshop.FinalPool.CurrentFill * 100f) + "% · 细渣另置";
            progress.rectTransform.localScale = new Vector3(progress.fillAmount, 1f, 1f);
            workProgress.rectTransform.localScale = new Vector3(workProgress.fillAmount, 1f, 1f);
            toast.gameObject.SetActive(Time.unscaledTime < toastUntil && !workshop.Modal);
            marker.gameObject.SetActive(!workshop.Modal && !flow.Completed);
            UpdateNavigation();
            if (flow.Completed && !shownComplete) { shownComplete = true; ShowBook(false); }
            if (bookOpen && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))) CloseBook();
        }
        public void RefreshDisplayInfo()
        {
            if (settingsInfo == null || workshop.Camera == null) return;
            settingsInfo.text = (workshop.ClearRendering ? "清晰" : "流畅") + " · " + workshop.Camera.pixelWidth + " × " + workshop.Camera.pixelHeight +
                " · 100% 渲染\n视角 " + Mathf.RoundToInt(workshop.Camera.fieldOfView) + "° · 镜头晃动已减弱";
        }
        void UpdateNavigation()
        {
            if (workshop.Flow.Completed) { navigation.text = "原料工坊 · 第 5—8 步已完成"; return; }
            Vector3 goal = workshop.NavigationPosition;
            Vector3 direction = goal - workshop.Player.transform.position;
            direction.y = 0f;
            float angle = Vector3.SignedAngle(workshop.Player.transform.forward, direction, Vector3.up);
            string arrow = Mathf.Abs(angle) < 18f ? "↑" : Mathf.Abs(angle) > 155f ? "↓" : angle < 0f ? "←" : "→";
            navigation.text = arrow + "  当前目标  " + direction.magnitude.ToString("0.0") + " m";
            Vector3 point = workshop.Camera.WorldToViewportPoint(goal + Vector3.up * 0.25f);
            marker.gameObject.SetActive(!workshop.Modal && point.z > 0 && !workshop.Flow.Completed);
            marker.anchoredPosition = new Vector2(Mathf.Clamp((point.x - 0.5f) * 1920f, -790f, 790f), Mathf.Clamp((point.y - 0.5f) * 1080f, -265f, 280f));
            markerLabel.text = direction.magnitude < 2.5f ? "◇ 操作位置" : "◇ " + direction.magnitude.ToString("0.0") + " m";
        }

        public void Toast(string message)
        {
            if (toast == null) return;
            toast.text = message;
            toastUntil = Time.unscaledTime + 3.8f;
        }
    }
}
