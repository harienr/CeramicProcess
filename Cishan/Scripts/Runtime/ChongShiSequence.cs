using System.Collections;
using UnityEngine;

namespace Cishan
{
    /// <summary>
    /// The 06 step as a quiet procedure: open, retreat, watch, close, wait, scoop,
    /// deliver. No score, no failure screen; a coarse powder is simply too coarse.
    /// </summary>
    public class ChongShiSequence : MonoBehaviour
    {
        public Transform player;
        public SluiceGate gate;
        public WaterWheelDrive wheel;
        public TripHammer[] hammers;
        public MortarStation[] mortars;
        public Transform vatPowder;
        public CishanHUD hud;

        public enum Stage { Intro, ToGate, Retreat, Observe, CloseGate, Scoop, Deliver, Done }

        public Stage Current { get; private set; }
        public bool Busy { get; private set; }

        int scoops;
        float timer;
        bool ready;
        bool halfwaySaid;
        string lastObjective = "";

        void Start()
        {
            Begin();
        }

        public void Begin()
        {
            if (ready) return;
            ready = true;
            if (hud == null && CishanGame.I != null) hud = CishanGame.I.hud;
            if (player == null && CishanGame.I != null && CishanGame.I.player != null)
            {
                player = CishanGame.I.player.transform;
            }

            Current = Stage.Done;
            SetStage(Stage.Intro);

            if (hud != null)
            {
                hud.ShowChapter("06", "舂石", "水碓棚 · 净石 → 石粉", 4.2f);
                hud.ShowSubtitle("水声很轻。石粉落在木板上。", 4.5f);
            }
        }

        void Update()
        {
            if (!ready) return;
            timer += Time.deltaTime;
            UpdateSafety();

            switch (Current)
            {
                case Stage.Intro:
                    if (timer > 2.4f) SetStage(Stage.ToGate);
                    break;

                case Stage.ToGate:
                    if (gate != null && gate.targetOpen) SetStage(Stage.Retreat);
                    break;

                case Stage.Retreat:
                    if (gate != null && !gate.targetOpen)
                    {
                        SetStage(Stage.ToGate);
                        break;
                    }
                    if (player != null && WorkshopLayout.IsBehindRail(player.position))
                    {
                        SetStage(Stage.Observe);
                    }
                    break;

                case Stage.Observe:
                    if (AllDone()) { SetStage(Stage.CloseGate); break; }
                    if (!halfwaySaid && TotalStrikes() >= 6)
                    {
                        halfwaySaid = true;
                        if (hud != null) hud.ShowSubtitle("石粉开始变细。", 2.6f);
                    }
                    if (gate != null && !gate.targetOpen) Objective("石粉仍粗，再拉起闸板");
                    else Objective("守碓：等石粉由粗变细");
                    break;

                case Stage.CloseGate:
                    if (gate != null && gate.targetOpen) { SetStage(Stage.Observe); break; }
                    if (IsSettled())
                    {
                        SetStage(Stage.Scoop);
                        if (hud != null) hud.ShowSubtitle("碓停稳了，再伸手。", 3f);
                    }
                    break;

                case Stage.Scoop:
                    if (scoops >= 2) SetStage(Stage.Deliver);
                    break;
            }
        }

        void SetStage(Stage next)
        {
            if (Current == next) return;
            Current = next;
            timer = 0f;

            switch (next)
            {
                case Stage.Intro:
                    Objective("走到护栏前，看清水碓");
                    break;
                case Stage.ToGate:
                    Objective("到水轮上游，拉起闸板");
                    if (hud != null) hud.ShowSubtitle("闸板在棚口外，水轮西侧。", 4f);
                    break;
                case Stage.Retreat:
                    Objective("退到护栏外，守碓");
                    break;
                case Stage.Observe:
                    Objective("守碓：等石粉由粗变细");
                    break;
                case Stage.CloseGate:
                    Objective("关闸：等碓停稳");
                    if (hud != null) hud.ShowSubtitle("石粉细了。先关闸。", 3.2f);
                    break;
                case Stage.Scoop:
                    Objective("取粉：两臼各舀一瓢");
                    break;
                case Stage.Deliver:
                    Objective("把石粉送到北侧淘洗池（07）");
                    if (hud != null) hud.ShowSubtitle("石粉入桶。送去淘洗。", 3.2f);
                    break;
                case Stage.Done:
                    Objective("07 淘洗 · 待续");
                    break;
            }
        }

        void Objective(string text)
        {
            if (text == lastObjective) return;
            lastObjective = text;
            if (hud != null) hud.SetObjective(text);
        }

        void UpdateSafety()
        {
            if (hud == null) return;
            bool unsafeNow = gate != null && gate.OpenAmount > 0.2f && wheel != null &&
                             wheel.Rpm > 1f && player != null &&
                             WorkshopLayout.InHammerBay(player.position);
            hud.SetSafety(unsafeNow ? "碓未停稳，手不入臼" : null);
        }

        bool AllDone()
        {
            if (mortars == null || mortars.Length == 0) return false;
            for (int i = 0; i < mortars.Length; i++)
            {
                if (mortars[i] == null || !mortars[i].IsDone) return false;
            }
            return true;
        }

        int TotalStrikes()
        {
            int total = 0;
            if (mortars == null) return 0;
            for (int i = 0; i < mortars.Length; i++)
            {
                if (mortars[i] != null) total += mortars[i].Strikes;
            }
            return total;
        }

        bool IsSettled()
        {
            if (gate == null || !gate.IsClosed) return false;
            if (wheel != null && !wheel.IsSettled) return false;
            if (hammers != null)
            {
                for (int i = 0; i < hammers.Length; i++)
                {
                    if (hammers[i] != null && !hammers[i].AtRest) return false;
                }
            }
            return true;
        }

        public bool CanScoop(MortarStation mortar, out string reason)
        {
            reason = null;
            if (mortar == null)
            {
                reason = "这里没有碓臼";
                return false;
            }
            if (mortar.Scooped)
            {
                reason = "这一臼已经取过了";
                return false;
            }
            if (gate != null && !gate.IsClosed)
            {
                reason = "先关闸，等碓停稳";
                return false;
            }
            if (wheel != null && !wheel.IsSettled)
            {
                reason = "碓未停稳，手不入臼";
                return false;
            }
            if (Current == Stage.Intro || Current == Stage.ToGate || Current == Stage.Retreat)
            {
                reason = "还没有舂石，先开闸";
                return false;
            }
            if (Current == Stage.Deliver || Current == Stage.Done)
            {
                reason = "石粉已经入桶了";
                return false;
            }
            if (!mortar.IsDone)
            {
                reason = "石粉还粗，再舂一轮";
                return false;
            }
            return true;
        }

        public void Scoop(MortarStation mortar)
        {
            if (Busy) return;
            string reason;
            if (!CanScoop(mortar, out reason))
            {
                if (hud != null) hud.ShowSubtitle(reason, 2.4f);
                return;
            }
            StartCoroutine(ScoopFlow(mortar));
        }

        IEnumerator ScoopFlow(MortarStation mortar)
        {
            Busy = true;
            yield return mortar.ScoopRoutine();
            scoops++;
            Busy = false;
            if (hud != null && scoops < 2) hud.ShowSubtitle("石粉入桶。还有一臼。", 2.2f);
            if (scoops >= 2) SetStage(Stage.Deliver);
        }

        public bool CanDeliver(out string reason)
        {
            if (Current != Stage.Deliver)
            {
                reason = "桶里还没有两瓢石粉";
                return false;
            }
            reason = null;
            return true;
        }

        public void Deliver()
        {
            string reason;
            if (!CanDeliver(out reason))
            {
                if (hud != null) hud.ShowSubtitle(reason, 2.4f);
                return;
            }
            if (vatPowder != null) vatPowder.gameObject.SetActive(true);
            SetStage(Stage.Done);
            if (hud != null)
            {
                hud.SetTag("");
                hud.SetObjective("07 淘洗 · 待续");
                hud.ShowComplete("水轮不停，碓头起落；\n人只负责在正确的时候关闸。",
                    "06 舂石 · 完成　下一道工序：07 淘洗");
            }
        }
    }
}
