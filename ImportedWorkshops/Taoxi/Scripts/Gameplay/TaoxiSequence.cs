using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PorcelainFromClay
{
    public enum TaoxiStep
    {
        Intro,
        Powder,
        Water,
        Stir,
        Settle,
        Draw,
        Collect,
        Done
    }

    public sealed class TaoxiSequence : MonoBehaviour
    {
        WorkshopRefs refs;
        PlayerRig player;
        WorkshopUI ui;
        WorkshopAudio audio;

        TaoxiStep current = TaoxiStep.Intro;
        bool started;
        bool busy;
        bool stirring;
        bool notesOpen;
        bool scraped;
        float stirProgress;
        float stirAngle;
        float stirIdle;
        float settleTimer;
        float lowerClay;
        int scoops;
        Vector3 gateClosedLocal;

        enum PendingAction { None, Powder, Water, Draw, Collect, Scrape }
        PendingAction pending;
        float pendingTimer;
        float drawUpperStart;
        float drawLowerStart;
        int collectScoopsDone;

        public TaoxiStep Current { get { return current; } }
        public bool IsBusy { get { return busy; } }
        public bool IsStirring { get { return stirring; } }
        public float PendingTimer { get { return pendingTimer; } }
        public bool AllowPrimaryClick { get { return false; } }
        public bool CanLookForInteractions
        {
            get
            {
                return started && !busy && !stirring && !notesOpen && current != TaoxiStep.Done;
            }
        }

        public void Init(WorkshopRefs worldRefs, PlayerRig playerRig, WorkshopUI workshopUI, WorkshopAudio workshopAudio)
        {
            refs = worldRefs;
            player = playerRig;
            ui = workshopUI;
            audio = workshopAudio;
            player.Interactor.Sequence = this;
            audio.AttachPlayer(player.Movement);

            ui.StartRequested += Begin;
            ui.ReplayRequested += Replay;
            ui.NotesClosed += OnNotesClosed;

            gateClosedLocal = refs.GateBoard.localPosition;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.ShowIntro();
        }

        void Update()
        {
            if (!started)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    Begin();
                }
                return;
            }

            if (current == TaoxiStep.Done)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (!busy && !stirring) ToggleNotes();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (notesOpen) ui.CloseNotes();
                else if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    player.Movement.InputEnabled = false;
                    player.Movement.LookEnabled = false;
                }
                else
                {
                    LockCursor();
                }
            }

            if (pending != PendingAction.None) UpdatePending();
            if (stirring) UpdateStir();
            else if (current == TaoxiStep.Settle) UpdateSettle();
        }

        void UpdatePending()
        {
            pendingTimer += Time.deltaTime;
            switch (pending)
            {
                case PendingAction.Powder:
                    if (pendingTimer >= 2.9f)
                    {
                        pending = PendingAction.None;
                        refs.UpperVat.SetPowder(0.9f);
                        busy = false;
                        player.Movement.InputEnabled = true;
                        player.Movement.LookEnabled = true;
                        ui.ShowToast("粉料入池。现在加水。", 2.8f);
                        SetStep(TaoxiStep.Water, true);
                    }
                    break;
                case PendingAction.Water:
                    if (pendingTimer >= 3.7f)
                    {
                        pending = PendingAction.None;
                        refs.UpperVat.SetFill(0.88f);
                        refs.UpperVat.SetTurbidity(0.48f);
                        refs.UpperVat.SetPowder(0f);
                        refs.UpperVat.SetRipple(0.32f);
                        busy = false;
                        player.Movement.InputEnabled = true;
                        player.Movement.LookEnabled = true;
                        ui.ShowToast("水没过粉料。搅动时别太急。", 3.2f);
                        SetStep(TaoxiStep.Stir, true);
                    }
                    break;
                case PendingAction.Scrape:
                    if (pendingTimer >= 2.4f)
                    {
                        pending = PendingAction.None;
                        refs.UpperVat.SetCoarse(false);
                        if (refs.TailingsPile != null) refs.TailingsPile.gameObject.SetActive(true);
                        scraped = true;
                        Activate(refs.ScrapeTarget, false);
                        busy = false;
                        player.Movement.InputEnabled = true;
                        player.Movement.LookEnabled = true;
                        ui.ShowToast("粗砂留在尾砂桶里。", 2.6f);
                    }
                    break;
                case PendingAction.Draw:
                    UpdateDrawOff();
                    break;
                case PendingAction.Collect:
                    UpdateCollect();
                    break;
            }
        }

        void UpdateDrawOff()
        {
            float t = pendingTimer;
            Vector3 open = gateClosedLocal + new Vector3(0f, 0.14f, 0f);
            if (t < 0.9f)
            {
                refs.GateBoard.localPosition = Vector3.Lerp(gateClosedLocal, open, Mathf.SmoothStep(0f, 1f, t / 0.9f));
            }
            else if (t < 6.9f)
            {
                refs.GateBoard.localPosition = open;
                if (!refs.ChannelWater.gameObject.activeSelf) refs.ChannelWater.gameObject.SetActive(true);
                float e = Mathf.SmoothStep(0f, 1f, (t - 0.9f) / 6f);
                refs.UpperVat.SetFill(Mathf.Lerp(drawUpperStart, 0.12f, e));
                refs.LowerVat.SetFill(Mathf.Lerp(drawLowerStart, 0.82f, e));
                refs.LowerVat.SetTurbidity(0.88f);
                refs.UpperVat.SetTurbidity(Mathf.Lerp(0.12f, 0.2f, e));
                if (e > 0.35f)
                {
                    refs.LowerVat.SetSilt(Mathf.Lerp(0f, 0.5f, (e - 0.35f) / 0.65f));
                    refs.LowerVat.SetClay(Mathf.Lerp(0f, 0.58f, (e - 0.35f) / 0.65f));
                }
            }
            else if (t < 7.6f)
            {
                float e = (t - 6.9f) / 0.7f;
                refs.GateBoard.localPosition = Vector3.Lerp(open, gateClosedLocal, e);
                if (t > 7.35f) refs.ChannelWater.gameObject.SetActive(false);
            }
            else if (t < 10f)
            {
                refs.GateBoard.localPosition = gateClosedLocal;
                float e = Mathf.SmoothStep(0f, 1f, (t - 7.6f) / 2.4f);
                refs.LowerVat.SetFill(Mathf.Lerp(0.82f, 0.12f, e));
                refs.LowerVat.SetClay(Mathf.Lerp(0.22f, 0.78f, e));
                refs.LowerVat.SetTurbidity(Mathf.Lerp(0.8f, 0.14f, e));
                if (e > 0.55f) refs.LowerVat.SetSilt(Mathf.Lerp(0f, 0.52f, (e - 0.55f) / 0.45f));
            }
            else
            {
                pending = PendingAction.None;
                refs.ChannelWater.gameObject.SetActive(false);
                refs.GateBoard.localPosition = gateClosedLocal;
                refs.UpperVat.SetCoarse(true);
                refs.UpperVat.SetSilt(0.12f);
                refs.UpperVat.SetTurbidity(0.08f);
                refs.LowerVat.SetClay(0.78f);
                refs.LowerVat.SetSilt(0.52f);
                refs.LowerVat.SetTurbidity(0.14f);
                refs.LowerVat.SetFill(0.12f);
                lowerClay = 0.78f;
                scoops = 0;
                busy = false;
                player.Movement.InputEnabled = true;
                player.Movement.LookEnabled = true;
                ui.ShowToast("细泥沉下，滗去清水。舀出来，沥水。", 3.8f);
                SetStep(TaoxiStep.Collect, true);
            }
        }

        void UpdateCollect()
        {
            int targetScoop = Mathf.Min(3, Mathf.FloorToInt(pendingTimer / 1.8f));
            while (collectScoopsDone < targetScoop)
            {
                collectScoopsDone++;
                scoops++;
                lowerClay = Mathf.Max(0.04f, lowerClay - 0.2f);
                refs.LowerVat.SetClay(lowerClay);
                refs.LowerVat.SetFill(Mathf.Lerp(0.12f, 0.08f, scoops / 3f));
                if (scoops - 1 < refs.TrayLumps.Count && refs.TrayLumps[scoops - 1] != null)
                    refs.TrayLumps[scoops - 1].gameObject.SetActive(true);
                audio.PlayScoop();
                player.Viewmodel.StartCollectClay();
            }
            if (pendingTimer >= 5.6f)
            {
                pending = PendingAction.None;
                FinishSequence();
            }
        }

        public void Begin()
        {
            if (started) return;
            started = true;
            ui.HideIntro();
            current = TaoxiStep.Powder;
            SetStep(TaoxiStep.Powder, true);
            LockCursor();
            player.Movement.InputEnabled = true;
            player.Movement.LookEnabled = true;
            ui.ShowToast("先从左侧料槽取粉。", 3f);
        }

        void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            player.Movement.InputEnabled = true;
            player.Movement.LookEnabled = true;
        }

        void SetStep(TaoxiStep step, bool card)
        {
            current = step;
            int idx = Mathf.Clamp((int)step - 1, 0, 5);
            ui.SetStep(idx, card);

            Activate(refs.PowderTarget, step == TaoxiStep.Powder);
            Activate(refs.WaterTarget, step == TaoxiStep.Water);
            Activate(refs.StirTarget, step == TaoxiStep.Stir);
            Activate(refs.GateTarget, step == TaoxiStep.Draw);
            Activate(refs.ClayTarget, step == TaoxiStep.Collect);
            Activate(refs.ScrapeTarget, step == TaoxiStep.Collect && !scraped);

            if (step == TaoxiStep.Stir) ui.SetPrompt("开始搅洗", true);
            else ui.SetPrompt(null, false);
        }

        void Activate(Interactable target, bool active)
        {
            if (target != null) target.gameObject.SetActive(active);
        }

        public string GetPrompt(Interactable target)
        {
            if (!started || busy || stirring || notesOpen || target == null) return null;
            switch (target.Action)
            {
                case WorkshopAction.AddPowder:
                    return current == TaoxiStep.Powder ? "取一勺瓷石粉" : null;
                case WorkshopAction.AddWater:
                    return current == TaoxiStep.Water ? "舀水注入淘洗池" : null;
                case WorkshopAction.Stir:
                    return current == TaoxiStep.Stir ? "开始搅洗" : null;
                case WorkshopAction.OpenGate:
                    return current == TaoxiStep.Draw ? "提起木闸 · 引细泥入沉淀池" : null;
                case WorkshopAction.CollectClay:
                    return current == TaoxiStep.Collect ? "舀取沉降细泥" : null;
                case WorkshopAction.ScrapeSand:
                    return current == TaoxiStep.Collect && !scraped ? "刮除池底粗砂（可选）" : null;
                default:
                    return null;
            }
        }

        public void Activate(Interactable target)
        {
            if (!CanLookForInteractions || target == null) return;
            switch (target.Action)
            {
                case WorkshopAction.AddPowder:
                    if (current == TaoxiStep.Powder) BeginPending(PendingAction.Powder, 2.9f, VmAction.Powder);
                    break;
                case WorkshopAction.AddWater:
                    if (current == TaoxiStep.Water) BeginPending(PendingAction.Water, 3.7f, VmAction.Water);
                    break;
                case WorkshopAction.Stir:
                    if (current == TaoxiStep.Stir) StartStir();
                    break;
                case WorkshopAction.OpenGate:
                    if (current == TaoxiStep.Draw) BeginDraw();
                    break;
                case WorkshopAction.CollectClay:
                    if (current == TaoxiStep.Collect) BeginCollect();
                    break;
                case WorkshopAction.ScrapeSand:
                    if (current == TaoxiStep.Collect && !scraped) BeginPending(PendingAction.Scrape, 2.4f, VmAction.Scrape);
                    break;
            }
        }

        // ------------------------------------------------------------------ actions
        void BeginPending(PendingAction action, float duration, VmAction viewAction)
        {
            pending = action;
            pendingTimer = 0f;
            busy = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.SetPrompt(null, false);
            player.Viewmodel.StartAction(viewAction, Mathf.Max(0.8f, duration - 0.4f));
        }

        void BeginDraw()
        {
            pending = PendingAction.Draw;
            pendingTimer = 0f;
            busy = true;
            drawUpperStart = refs.UpperVat.CurrentFill;
            drawLowerStart = refs.LowerVat.CurrentFill;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.SetPrompt(null, false);
            audio.PlayGate();
            player.Viewmodel.StartAction(VmAction.Gate, 1.2f);
        }

        void BeginCollect()
        {
            pending = PendingAction.Collect;
            pendingTimer = 0f;
            collectScoopsDone = 0;
            busy = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.SetPrompt(null, false);
            player.Viewmodel.StartAction(VmAction.Clay, 2.2f);
        }

        IEnumerator DoPowder()
        {
            busy = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            audio.PlayScoop();
            player.Viewmodel.StartPourPowder();
            yield return WaitSeconds(2.9f);
            refs.UpperVat.SetPowder(0.9f);
            busy = false;
            player.Movement.InputEnabled = true;
            player.Movement.LookEnabled = true;
            ui.ShowToast("粉料入池。现在加水。", 2.8f);
            SetStep(TaoxiStep.Water, true);
        }

        IEnumerator DoWater()
        {
            busy = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            audio.PlayPour();
            player.Viewmodel.StartPourWater();
            yield return WaitSeconds(3.7f);
            refs.UpperVat.SetFill(0.88f);
            refs.UpperVat.SetTurbidity(0.48f);
            refs.UpperVat.SetPowder(0f);
            refs.UpperVat.SetRipple(0.32f);
            busy = false;
            player.Movement.InputEnabled = true;
            player.Movement.LookEnabled = true;
            ui.ShowToast("水没过粉料。搅动时别太急。", 3.2f);
            SetStep(TaoxiStep.Stir, true);
        }

        void StartStir()
        {
            busy = true;
            stirring = true;
            stirProgress = 0f;
            stirAngle = 0f;
            stirIdle = 0f;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.SetPrompt(null, false);
            ui.SetStirMode(true, 0f);
            player.Viewmodel.BeginStir();
            audio.SetStir(true);
            ui.ShowToast("按住左键，缓慢画圈。", 2.4f);
            refs.UpperVat.SetSwirl(0.65f);
        }

        void UpdateStir()
        {
            if (Input.GetMouseButton(0))
            {
                float dx = Input.GetAxisRaw("Mouse X");
                float dy = Input.GetAxisRaw("Mouse Y");
                float motion = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (motion > 0.001f)
                {
                    stirProgress = Mathf.Clamp01(stirProgress + motion * 0.085f);
                    stirAngle += motion * 0.5f;
                    stirIdle = 0f;
                    player.Viewmodel.SetStirAngle(stirAngle);
                    refs.UpperVat.SetTurbidity(Mathf.Lerp(0.48f, 0.96f, stirProgress));
                    refs.UpperVat.SetSwirl(Mathf.Lerp(0.65f, 0.25f, stirProgress));
                    ui.SetStirProgress(stirProgress);
                    audio.SetStir(true);
                }
                else
                {
                    stirIdle += Time.deltaTime;
                }
            }
            else
            {
                stirIdle += Time.deltaTime;
                audio.SetStir(false);
            }

            if (stirProgress >= 1f)
            {
                FinishStir();
            }
            else if (stirIdle > 2.2f)
            {
                stirIdle = 0f;
                ui.ShowToast("画圈，别只按着。", 1.8f);
            }
        }

        void FinishStir()
        {
            stirring = false;
            busy = false;
            player.Movement.InputEnabled = true;
            player.Movement.LookEnabled = true;
            audio.SetStir(false);
            player.Viewmodel.EndStir();
            ui.SetStirMode(false);
            refs.UpperVat.SetTurbidity(0.96f);
            refs.UpperVat.SetSwirl(0f);
            refs.UpperVat.SetSilt(0.05f);
            settleTimer = 0f;
            SetStep(TaoxiStep.Settle, true);
            ui.ShowToast("停手。看水色慢慢清下来。", 3.2f);
        }

        void UpdateSettle()
        {
            settleTimer += Time.deltaTime;
            float t = Mathf.Clamp01(settleTimer / 8.5f);
            refs.UpperVat.SetTurbidity(Mathf.Lerp(0.96f, 0.12f, Mathf.SmoothStep(0f, 1f, t)));
            refs.UpperVat.SetSilt(Mathf.Lerp(0.05f, 0.5f, t));
            if (t > 0.4f) refs.UpperVat.SetCoarse(true);
            if (t >= 1f)
            {
                settleTimer = 0f;
                refs.UpperVat.SetTurbidity(0.1f);
                refs.UpperVat.SetSilt(0.55f);
                SetStep(TaoxiStep.Draw, true);
                ui.ShowToast("水色转清。提起木闸，把细浆引出去。", 3.8f);
            }
        }

        IEnumerator DoDrawOff()
        {
            busy = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.SetPrompt(null, false);
            audio.PlayGate();
            player.Viewmodel.StartPullGate();
            yield return WaitSeconds(2.1f);

            Vector3 open = gateClosedLocal + new Vector3(0f, 0.14f, 0f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.85f;
                refs.GateBoard.localPosition = Vector3.Lerp(gateClosedLocal, open, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            refs.GateBoard.localPosition = open;
            refs.ChannelWater.gameObject.SetActive(true);

            float lowerStart = refs.LowerVat.CurrentFill;
            float upperStart = refs.UpperVat.CurrentFill;
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 5.8f;
                float e = Mathf.SmoothStep(0f, 1f, t);
                refs.UpperVat.SetFill(Mathf.Lerp(upperStart, 0.12f, e));
                refs.LowerVat.SetFill(Mathf.Lerp(lowerStart, 0.82f, e));
                refs.LowerVat.SetTurbidity(0.88f);
                refs.UpperVat.SetTurbidity(Mathf.Lerp(0.12f, 0.2f, e));
                if (e > 0.35f)
                {
                    refs.LowerVat.SetSilt(Mathf.Lerp(0f, 0.5f, (e - 0.35f) / 0.65f));
                    refs.LowerVat.SetClay(Mathf.Lerp(0f, 0.58f, (e - 0.35f) / 0.65f));
                }
                yield return null;
            }

            refs.ChannelWater.gameObject.SetActive(false);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.7f;
                refs.GateBoard.localPosition = Vector3.Lerp(open, gateClosedLocal, t);
                yield return null;
            }
            refs.GateBoard.localPosition = gateClosedLocal;

            refs.UpperVat.SetCoarse(true);
            refs.UpperVat.SetSilt(0.12f);
            refs.UpperVat.SetTurbidity(0.08f);

            // the fines settle, then most of the clear water is decanted before collection
            float decantStart = refs.LowerVat.CurrentFill;
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 2.4f;
                float e = Mathf.SmoothStep(0f, 1f, t);
                refs.LowerVat.SetFill(Mathf.Lerp(decantStart, 0.12f, e));
                refs.LowerVat.SetClay(Mathf.Lerp(0.22f, 0.78f, e));
                refs.LowerVat.SetTurbidity(Mathf.Lerp(0.8f, 0.14f, e));
                if (e > 0.55f) refs.LowerVat.SetSilt(Mathf.Lerp(0f, 0.52f, (e - 0.55f) / 0.45f));
                yield return null;
            }
            lowerClay = 0.78f;
            scoops = 0;
            busy = false;
            player.Movement.InputEnabled = true;
            player.Movement.LookEnabled = true;
            ui.ShowToast("细泥沉下，滗去清水。舀出来，沥水。", 3.8f);
            SetStep(TaoxiStep.Collect, true);
        }

        IEnumerator DoScrape()
        {
            busy = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            audio.PlayScoop();
            player.Viewmodel.StartScrapeSand();
            yield return WaitSeconds(2.6f);
            refs.UpperVat.SetCoarse(false);
            if (refs.TailingsPile != null) refs.TailingsPile.gameObject.SetActive(true);
            scraped = true;
            Activate(refs.ScrapeTarget, false);
            busy = false;
            player.Movement.InputEnabled = true;
            player.Movement.LookEnabled = true;
            ui.ShowToast("粗砂留在尾砂桶里。", 2.6f);
        }

        IEnumerator DoCollect()
        {
            busy = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.SetPrompt(null, false);

            int take = Mathf.Min(3, 3 - scoops);
            for (int i = 0; i < take; i++)
            {
                audio.PlayScoop();
                player.Viewmodel.StartCollectClay();
                yield return WaitSeconds(2.7f);
                lowerClay = Mathf.Max(0.04f, lowerClay - 0.2f);
                refs.LowerVat.SetClay(lowerClay);
                refs.LowerVat.SetFill(Mathf.Lerp(0.12f, 0.08f, (scoops + 1) / 3f));
                if (scoops < refs.TrayLumps.Count && refs.TrayLumps[scoops] != null)
                {
                    refs.TrayLumps[scoops].gameObject.SetActive(true);
                }
                scoops++;
                busy = true;
                yield return new WaitForSeconds(0.3f);
            }

            busy = false;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.ShowToast("细泥已取出。", 2f);
            yield return new WaitForSeconds(1.1f);
            FinishSequence();
        }

        IEnumerator WaitSeconds(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        void FinishSequence()
        {
            current = TaoxiStep.Done;
            Activate(refs.ClayTarget, false);
            Activate(refs.ScrapeTarget, false);
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            player.Viewmodel.Hide(true);
            ui.ShowEnd();
            audio.SetStir(false);
            audio.SetRoomMood(0.16f);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ------------------------------------------------------------------- notes
        void ToggleNotes()
        {
            if (notesOpen)
            {
                ui.CloseNotes();
                return;
            }
            notesOpen = true;
            player.Movement.InputEnabled = false;
            player.Movement.LookEnabled = false;
            ui.SetPrompt(null, false);
            ui.OpenNotes();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnNotesClosed()
        {
            notesOpen = false;
            if (started && current != TaoxiStep.Done)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                player.Movement.InputEnabled = true;
                player.Movement.LookEnabled = true;
            }
        }

        void Replay()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        public void TestFinishStir()
        {
            stirProgress = 1f;
        }

        // --------------------------------------------------------------- preview API
        public void PreviewStep(TaoxiStep step, float amount = 0.5f)
        {
            started = true;
            busy = false;
            stirring = false;
            notesOpen = false;
            current = step;
            ui.HideIntroImmediate();
            player.Movement.InputEnabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            int idx = Mathf.Clamp((int)step - 1, 0, 5);
            ui.SetStep(idx, true);
            RefreshPreviewVisuals(step, amount);
        }

        void RefreshPreviewVisuals(TaoxiStep step, float amount)
        {
            amount = Mathf.Clamp01(amount);
            refs.UpperVat.SetPowder(step == TaoxiStep.Powder ? 1f : 0f);
            switch (step)
            {
                case TaoxiStep.Powder:
                    refs.UpperVat.SetFill(0f);
                    refs.UpperVat.SetTurbidity(0f);
                    refs.LowerVat.SetFill(0f);
                    break;
                case TaoxiStep.Water:
                    refs.UpperVat.SetFill(0.88f);
                    refs.UpperVat.SetTurbidity(0.5f);
                    break;
                case TaoxiStep.Stir:
                    refs.UpperVat.SetFill(0.88f);
                    refs.UpperVat.SetTurbidity(Mathf.Lerp(0.5f, 0.96f, amount));
                    refs.UpperVat.SetSwirl(0.6f);
                    player.Viewmodel.SetPreview(ViewTool.Paddle, amount * 6f);
                    break;
                case TaoxiStep.Settle:
                    refs.UpperVat.SetFill(0.88f);
                    refs.UpperVat.SetTurbidity(Mathf.Lerp(0.9f, 0.12f, amount));
                    refs.UpperVat.SetSwirl(0f);
                    refs.UpperVat.SetSilt(Mathf.Lerp(0.05f, 0.55f, amount));
                    refs.UpperVat.SetCoarse(amount > 0.4f);
                    break;
                case TaoxiStep.Draw:
                    refs.UpperVat.SetFill(0.45f);
                    refs.UpperVat.SetTurbidity(0.2f);
                    refs.UpperVat.SetCoarse(true);
                    refs.LowerVat.SetFill(0.4f);
                    refs.LowerVat.SetTurbidity(0.84f);
                    refs.LowerVat.SetClay(0.25f);
                    refs.ChannelWater.gameObject.SetActive(true);
                    refs.GateBoard.localPosition = gateClosedLocal + new Vector3(0f, 0.14f, 0f);
                    break;
                case TaoxiStep.Collect:
                    refs.UpperVat.SetFill(0.12f);
                    refs.UpperVat.SetCoarse(true);
                    refs.LowerVat.SetFill(0.12f);
                    refs.LowerVat.SetTurbidity(0.14f);
                    refs.LowerVat.SetClay(0.78f);
                    refs.LowerVat.SetSilt(0.52f);
                    for (int i = 0; i < refs.TrayLumps.Count; i++) refs.TrayLumps[i].gameObject.SetActive(i < 2);
                    player.Viewmodel.SetPreview(ViewTool.ClayScoop, 0.55f);
                    break;
                case TaoxiStep.Done:
                    refs.UpperVat.SetFill(0.12f);
                    refs.UpperVat.SetCoarse(true);
                    refs.LowerVat.SetFill(0.08f);
                    refs.LowerVat.SetClay(0.08f);
                    for (int i = 0; i < refs.TrayLumps.Count; i++) refs.TrayLumps[i].gameObject.SetActive(true);
                    ui.ShowEndImmediate();
                    break;
            }
        }
    }
}

