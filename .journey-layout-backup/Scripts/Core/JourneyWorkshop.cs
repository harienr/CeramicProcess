using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using T = PorcelainFromClay;
using G = Porcelain;

namespace CiCongNiSheng.Journey
{
    [DefaultExecutionOrder(-500)]
    public sealed partial class JourneyWorkshop : MonoBehaviour
    {
        public static JourneyWorkshop Instance { get; private set; }
        public JourneyFlow Flow { get; private set; } = new JourneyFlow();
        public PlayerInteractor Player { get; private set; }
        public FirstPersonController Movement { get; private set; }
        public Camera Camera { get; private set; }
        public StoneWashStation StoneWash { get; private set; }
        public T.WorkshopRefs Taoxi { get; private set; }
        public T.TaoxiVat FinalPool { get; private set; }
        public sealed class SieveRefs { public G.SievingStation st; public G.SlurryTub tub; public Transform root; }
        public SieveRefs Sieve { get; private set; }
        public Cishan.SluiceGate Gate { get; private set; }
        public Cishan.WaterWheelDrive Wheel { get; private set; }
        public Cishan.MortarStation[] Mortars { get; private set; }
        public JourneyHUD HUD { get; private set; }
        public JourneyInteractable[] Targets { get; private set; }
        public bool Modal { get; private set; }
        public bool Busy { get; private set; }
        public bool ValidationMode;
        public bool IsReady { get; private set; }
        public float Elapsed { get; private set; }
        public float FinalResidue { get; private set; }
        StyleMaterials style;
        int revision = -1;
        float stirAngle;
        Transform rake;
        GameObject cargo;
        GameObject cargoStones;
        Transform cargoSurface;
        Transform fineWaste;
        Vector3 spawn = new Vector3(4.65f, 0.06f, 3.6f);

        void Awake()
        {
            Instance = this;
            Flow.Reset();
            DisableLegacySystems();
            Gate = FindObjectOfType<Cishan.SluiceGate>();
            Wheel = FindObjectOfType<Cishan.WaterWheelDrive>();
            Mortars = FindObjectsOfType<Cishan.MortarStation>();
            System.Array.Sort(Mortars, (a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
            if (Gate == null || Wheel == null || Mortars.Length != 2)
            {
                Debug.LogError("整合场景缺少原始水碓，请从整合工坊菜单修复场景。");
                enabled = false;
                return;
            }
            Gate.SetOpen(false);
            foreach (var mortar in Mortars) mortar.SetMaterialLoaded(false);
            style = new StyleMaterials();
            style.Init();
            BuildStations();
            BuildPlayer();
            BuildTargets();
            BuildSigns();
            HUD = gameObject.AddComponent<JourneyHUD>();
            HUD.workshop = this;
            RenderSettings.fog = false;
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.51f, 0.47f);
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            IsReady = true;
            RefreshStep();
        }

        void Start()
        {
            if (IsReady) HUD.ShowBook(true);
        }

        void DisableLegacySystems()
        {
            foreach (var p in FindObjectsOfType<Cishan.FirstPersonController>()) p.gameObject.SetActive(false);
            foreach (var ui in FindObjectsOfType<Cishan.CishanHUD>()) ui.gameObject.SetActive(false);
            foreach (var seq in FindObjectsOfType<Cishan.ChongShiSequence>()) seq.enabled = false;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("10_") || t.name.StartsWith("11_") || t.name.StartsWith("12_") ||
                    t.name.StartsWith("13_") || t.name.StartsWith("14_") || t.name.StartsWith("15_")) t.gameObject.SetActive(false);
            foreach (var label in FindObjectsOfType<TextMesh>()) label.gameObject.SetActive(false);
            foreach (var target in FindObjectsOfType<Cishan.Interactable>()) target.enabled = false;
            var services = FindObjectOfType<Cishan.CishanGame>();
            if (services != null) { services.player = null; services.hud = null; services.interactor = null; }
        }

        void Update()
        {
            if (!IsReady) return;
            if (!ValidationMode)
            {
                if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.J)) HUD.ToggleBook();
                if (Input.GetKeyDown(KeyCode.Escape)) HUD.ShowPause();
            }
            if (Modal) return;
            if (!Flow.Completed) Elapsed += Time.deltaTime;
            var hovered = Player.Hovered as JourneyInteractable;
            bool keyboard = !ValidationMode && Input.GetKey(KeyCode.E);
            Movement.StabilizeForInteraction = (Input.GetMouseButton(0) || keyboard) && hovered != null;
            var step = Flow.Step;
            if (step == JourneyStep.WatchHammers && Mortars[0].IsDone && Mortars[1].IsDone && BehindRail()) Flow.Advance(step);
            if (step == JourneyStep.WatchRegrind && Mortars[0].IsDone && BehindRail()) Flow.Advance(step);
            if ((step == JourneyStep.CloseGate || step == JourneyStep.CloseRegrindGate) && Stopped()) Flow.Advance(step);
            if (step == JourneyStep.LoadSieve && Sieve.st.Current == G.SievingStation.Stage.ReadyToSieve) Flow.Advance(step);
            if (step == JourneyStep.ShakeSieve && Sieve.st.Current == G.SievingStation.Stage.Done) Flow.Advance(step);
            if (step == JourneyStep.ScrapeSieve)
            {
                FinalResidue = Mathf.Max(FinalResidue, 1f - Sieve.st.Residue01);
                fineWaste.gameObject.SetActive(true);
                fineWaste.localScale = new Vector3(0.16f, Mathf.Max(0.005f, FinalResidue * 0.04f), 0.12f);
                if (Sieve.st.Current == G.SievingStation.Stage.Empty) Flow.Advance(step);
            }
            FinalPool.SetFill(Sieve.st.BasinFill * 0.85f);
            if (hovered != null && keyboard && !Input.GetMouseButton(0)) hovered.Hold(Time.deltaTime, 1f);
            if (!ValidationMode && hovered != null && hovered.action == JourneyAction.Sieve && step == JourneyStep.ScrapeSieve && Input.GetKeyDown(KeyCode.R)) ScrapeFineResidue();
            if (Flow.Revision != revision) RefreshStep();
            if (Player.transform.position.y < -3f) Respawn();
        }

        void LateUpdate()
        {
            if (!IsReady) return;
            // Stone powder in water is an opaque slurry, not clear water.
            if (Taoxi.UpperVat.CurrentFill > 0.01f && Taoxi.UpperVat.WaterMaterial != null)
            {
                Taoxi.UpperVat.WaterMaterial.SetFloat("_BaseAlpha", 0.98f);
                Taoxi.UpperVat.WaterMaterial.SetColor("_TurbidColor", Palette.SlurryWet);
            }
            if (FinalPool.CurrentFill > 0.01f && FinalPool.WaterMaterial != null)
            {
                FinalPool.WaterMaterial.SetFloat("_BaseAlpha", 0.98f);
                FinalPool.WaterMaterial.SetColor("_TurbidColor", Palette.SlurryWet);
            }
        }
        bool BehindRail() => Player.transform.position.x < Cishan.WorkshopLayout.RailX;
        public bool Stopped() => Gate.IsClosed && Wheel.IsSettled && System.Array.TrueForAll(Mortars, m => m.hammer == null || m.hammer.AtRest);

        void RefreshStep()
        {
            revision = Flow.Revision;
            cargo.SetActive(Flow.HasCargo);
            bool stones = Flow.Material == CargoKind.CleanStones || Flow.Material == CargoKind.CoarseResidue;
            cargoStones.SetActive(stones);
            cargoSurface.gameObject.SetActive(!stones && Flow.HasCargo);
            cargoSurface.GetComponent<Renderer>().sharedMaterial = Flow.Material == CargoKind.RoughSlurry ? style.Slurry : style.SlurryDry;
            foreach (var target in Targets) target.gameObject.SetActive(IsRelevant(target.action));
            StoneWash.StopMotion();
            if (HUD != null) HUD.Toast(Flow.Completed ? Flow.Objective : Flow.ArticleNumber + " · " + JourneyFlow.Names[Flow.Chapter] + " / " + Flow.Objective);
        }

        public bool IsRelevant(JourneyAction action)
        {
            var step = Flow.Step;
            switch (action)
            {
                case JourneyAction.RawStones: return step == JourneyStep.LoadRawStones;
                case JourneyAction.StoneSieve: return step == JourneyStep.RinseStones;
                case JourneyAction.Debris: return step == JourneyStep.RemoveDebris;
                case JourneyAction.CleanStones: return step == JourneyStep.TakeCleanStones;
                case JourneyAction.LoadMortars: return step == JourneyStep.LoadMortars;
                case JourneyAction.Gate: return step == JourneyStep.OpenGate || step == JourneyStep.CloseGate || step == JourneyStep.ReopenGate || step == JourneyStep.CloseRegrindGate;
                case JourneyAction.MortarA: return step == JourneyStep.TakeRegroundPowder || (step == JourneyStep.ScoopPowder && !Mortars[0].Scooped);
                case JourneyAction.MortarB: return step == JourneyStep.ScoopPowder && !Mortars[1].Scooped;
                case JourneyAction.PowderVat: return step == JourneyStep.CarryPowder || step == JourneyStep.ReturnRegroundPowder;
                case JourneyAction.Water: return step == JourneyStep.AddWater;
                case JourneyAction.StirVat: return step == JourneyStep.Stir || step == JourneyStep.StirAgain;
                case JourneyAction.Coarse: return step == JourneyStep.RemoveCoarse;
                case JourneyAction.ReturnCoarse: return step == JourneyStep.ReturnCoarse;
                case JourneyAction.RoughSlurry: return step == JourneyStep.TakeRoughSlurry;
                case JourneyAction.SieveInput: return step == JourneyStep.CarrySlurry;
                case JourneyAction.Sieve: return step == JourneyStep.LoadSieve || step == JourneyStep.ShakeSieve || step == JourneyStep.ScrapeSieve;
                case JourneyAction.Pool: return step == JourneyStep.InspectPool;
                default: return false;
            }
        }

        public bool CanUse(JourneyAction action)
        {
            if (Modal || Busy || !IsRelevant(action)) return false;
            if (action == JourneyAction.Gate)
                return Flow.Step == JourneyStep.OpenGate || Flow.Step == JourneyStep.ReopenGate ? !Gate.targetOpen : Gate.targetOpen;
            if (action == JourneyAction.LoadMortars || action == JourneyAction.ReturnCoarse) return Stopped();
            if (action == JourneyAction.MortarA || action == JourneyAction.MortarB)
            {
                var mortar = Mortars[action == JourneyAction.MortarA ? 0 : 1];
                return Stopped() && mortar.IsDone && !mortar.Scooped && !mortar.Busy;
            }
            return action != JourneyAction.Sieve || Flow.Step != JourneyStep.ScrapeSieve;
        }

        public string PromptFor(JourneyInteractable target)
        {
            if (Busy) return "正在操作，请稍候";
            if (target.action == JourneyAction.Gate)
                return Gate.targetOpen ? "按住 E / 左键 · 关闸，等待轮碓停稳" : "按住 E / 左键 · 开闸舂石";
            if ((target.action == JourneyAction.LoadMortars || target.action == JourneyAction.ReturnCoarse || target.action == JourneyAction.MortarA || target.action == JourneyAction.MortarB) && !Stopped())
                return "先关闸，等碓头完全落稳后再操作";
            if (target.action == JourneyAction.StoneSieve) return "按住 E 摇洗瓷石 / 按住左键左右拖动";
            if (target.action == JourneyAction.StirVat) return "按住 E 用泥耙淘洗 / 按住左键移动鼠标";
            if (target.action == JourneyAction.Sieve && Flow.Step == JourneyStep.ScrapeSieve) return "R · 将筛上细渣刮入弃渣盘";
            return CanUse(target.action) ? "按住 E / 左键 · " + target.label : target.label;
        }

        public void Rinse(float dt, float motion)
        {
            if (Flow.Step != JourneyStep.RinseStones || Modal || motion < 0.01f || dt <= 0f) return;
            bool done = Flow.AddWork(JourneyStep.RinseStones, dt * Mathf.Clamp(motion, 0f, 1.3f) / 6f);
            StoneWash.ApplyWash(Flow.Work);
            if (done) Flow.Advance(JourneyStep.RinseStones);
        }

        public void Stir(float dt, float motion)
        {
            var step = Flow.Step;
            if ((step != JourneyStep.Stir && step != JourneyStep.StirAgain) || Modal || motion < 0.01f || dt <= 0f) return;
            float effort = Mathf.Clamp(motion, 0f, 1.3f);
            bool done = Flow.AddWork(step, dt * effort / (step == JourneyStep.Stir ? 7f : 4f));
            stirAngle += dt * effort * 3.8f;
            rake.localPosition = new Vector3(Mathf.Cos(stirAngle) * 0.21f, -0.1f, Mathf.Sin(stirAngle) * 0.17f);
            rake.localRotation = Quaternion.Euler(12f, Mathf.Sin(stirAngle) * 15f, 28f);
            Taoxi.UpperVat.SetSwirl(effort * 0.5f);
            Taoxi.UpperVat.SetTurbidity(Mathf.Lerp(0.55f, 0.96f, Flow.Work));
            if (!done) return;
            Taoxi.UpperVat.SetSwirl(0f);
            if (step == JourneyStep.Stir) Taoxi.UpperVat.SetCoarse(true);
            Flow.Advance(step);
        }

        public void ScrapeFineResidue()
        {
            if (!Modal && !Busy && Flow.Step == JourneyStep.ScrapeSieve) Sieve.st.RequestScrape();
        }

        public void Use(JourneyAction action)
        {
            if (!CanUse(action)) return;
            var step = Flow.Step;
            switch (action)
            {
                case JourneyAction.RawStones: StoneWash.Load(); Flow.Advance(step); break;
                case JourneyAction.Debris: StoneWash.RemoveDebris(); Flow.Advance(step); break;
                case JourneyAction.CleanStones: StoneWash.Take(); Flow.Advance(step); break;
                case JourneyAction.LoadMortars:
                    foreach (var mortar in Mortars) mortar.SetMaterialLoaded(true, 6);
                    Flow.Advance(step);
                    break;
                case JourneyAction.Gate:
                    bool open = step == JourneyStep.OpenGate || step == JourneyStep.ReopenGate;
                    Gate.SetOpen(open);
                    if (Cishan.CishanGame.I != null && Cishan.CishanGame.I.audio != null) Cishan.CishanGame.I.audio.PlayGate();
                    if (open) Flow.Advance(step);
                    break;
                case JourneyAction.MortarA: StartCoroutine(Scoop(0, step)); break;
                case JourneyAction.MortarB: StartCoroutine(Scoop(1, step)); break;
                case JourneyAction.PowderVat:
                    if (step == JourneyStep.CarryPowder) Taoxi.UpperVat.SetPowder(1f);
                    else { Taoxi.UpperVat.SetTurbidity(0.86f); Taoxi.TailingsPile.gameObject.SetActive(false); }
                    Flow.Advance(step);
                    break;
                case JourneyAction.Water: StartCoroutine(AddWater()); break;
                case JourneyAction.Coarse:
                    Taoxi.UpperVat.SetCoarse(false);
                    Taoxi.TailingsPile.gameObject.SetActive(true);
                    Flow.Advance(step);
                    break;
                case JourneyAction.ReturnCoarse:
                    Mortars[0].SetMaterialLoaded(true, 3);
                    Mortars[1].SetMaterialLoaded(false);
                    Taoxi.TailingsPile.gameObject.SetActive(false);
                    Flow.Advance(step);
                    break;
                case JourneyAction.SieveInput:
                    Sieve.tub.Fill01 = 0.25f;
                    Sieve.st.enabled = true;
                    Flow.Advance(step);
                    break;
                case JourneyAction.RoughSlurry: CollectRoughSlurry(); break;
                case JourneyAction.Sieve: Sieve.st.Interact(Time.deltaTime, true, true); break;
                case JourneyAction.Pool:
                    if (FinalPool.CurrentFill > 0.1f && FinalResidue > 0.9f) Flow.Advance(step);
                    break;
            }
        }

        public void CollectRoughSlurry()
        {
            if (Modal || Busy || Flow.Step != JourneyStep.TakeRoughSlurry) return;
            Taoxi.UpperVat.SetFill(0.16f);
            Flow.Advance(JourneyStep.TakeRoughSlurry);
        }

        IEnumerator Scoop(int index, JourneyStep expected)
        {
            Busy = true;
            yield return Mortars[index].ScoopRoutine();
            if (Mortars[index].Scooped && Flow.Step == expected)
            {
                if (expected == JourneyStep.ScoopPowder) Flow.AddScoop(index);
                else Flow.Advance(JourneyStep.TakeRegroundPowder);
            }
            Busy = false;
        }

        IEnumerator AddWater()
        {
            Busy = true;
            float t = 0f;
            while (t < 1.8f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 1.8f);
                Taoxi.UpperVat.SetFill(k * 0.86f);
                Taoxi.UpperVat.SetPowder(1f - k);
                Taoxi.UpperVat.SetTurbidity(0.55f);
                yield return null;
            }
            Busy = false;
            Flow.Advance(JourneyStep.AddWater);
        }

        public Vector3 NavigationPosition
        {
            get
            {
                if (Flow.Step == JourneyStep.WatchHammers || Flow.Step == JourneyStep.WatchRegrind) return new Vector3(5.55f, 0.3f, 3.45f);
                if (Flow.Step == JourneyStep.TakeRoughSlurry) return Taoxi.StirAnchor.position + Vector3.up * 0.15f;
                foreach (var target in Targets) if (IsRelevant(target.action)) return target.transform.position;
                return Player.transform.position;
            }
        }

        public void SetModal(bool modal)
        {
            Modal = modal;
            if (Movement != null) { Movement.InputEnabled = !modal; Movement.StabilizeForInteraction = false; }
            if (Player != null) Player.enabled = !modal;
            Cursor.lockState = modal ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = modal;
            Time.timeScale = modal ? 0f : 1f;
            AudioListener.pause = modal;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && IsReady && !ValidationMode && !Modal && HUD != null) HUD.ShowPause();
        }

        public void Respawn()
        {
            MovePlayer(spawn, 180f, 12f);
            if (HUD != null) HUD.Toast("已回到工坊中央，工序进度保留。");
        }

        public void MovePlayer(Vector3 position, float yaw, float pitch)
        {
            var cc = Player.GetComponent<CharacterController>();
            cc.enabled = false;
            Player.transform.position = position;
            Movement.SetViewAngles(yaw, pitch);
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        public void Restart()
        {
            SetModal(false);
            SceneManager.LoadScene("PorcelainJourney");
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (Instance == this) Instance = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
