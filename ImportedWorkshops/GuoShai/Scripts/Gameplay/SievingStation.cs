using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 过筛工作站：上料 → 摇筛 → 滤净 → 刮渣。
    /// 反馈优先用“材料状态变化”表达（规范 §7）：筛面由湿到残渣堆积、筛下盆里浆面上升、
    /// 浆色由灰转白，文字只做补充。
    /// </summary>
    public class SievingStation : MonoBehaviour, IInteractable
    {
        public enum Stage { Empty, Loading, ReadyToSieve, Sieving, Done }

        [Header("构件引用（由 WorkshopBuilder 赋值）")]
        public Transform sieveRoot;      // 筛框总成（摇筛时整体抖）
        public Transform dipper;         // 木瓢
        public Transform dipperRest;     // 木瓢静置点（料缸里）
        public Transform dipperPour;     // 上料时木瓢的悬停点
        public GameObject pourStream;    // 浆流
        public Transform film;           // 筛面薄浆层
        public Material filmMat;         // 薄浆层的独立材质实例（按湿度改透明度）
        public Transform residue;        // 筛上粗渣
        public Renderer clothRenderer;
        public Material clothDry;
        public Material clothWet;
        public Transform basinFill;      // 接浆盆里的浆体（底部为轴心）
        public Transform basinSurface;   // 接浆盆液面
        public Transform tubLevel;       // 料缸液面
        public ParticleSystem drips;
        public ParticleSystem splashes;
        public SlurryTub tub;
        public Transform scoopLiquid;    // 瓢里的那点浆
        public Transform pourLip;        // 瓢口（浆流挂在这里）
        public RippleRing basinRipple;   // 接浆盆液面涟漪

        [Header("参数")]
        public float loadSeconds = 1.7f;
        public float sieveSeconds = 8.5f;   // 按住多少秒把一批筛净
        public float scrapeSeconds = 0.9f;
        public float dipDuration = 0.62f;   // 一次“舀”的时长

        public Stage Current { get; private set; }
        public float Load01 { get; private set; }
        public float Purity01 { get; private set; }
        public float Residue01 { get; private set; }
        public float BasinFill { get; private set; }
        public int Batches { get; private set; }
        public bool Busy { get { return Current == Stage.Loading || Current == Stage.Sieving; } }
        public Transform Point { get { return transform; } }
        public float Radius { get { return 1.35f; } }

        Vector3 sieveBasePos;
        Quaternion sieveBaseRot;
        Vector3 dipperBasePos;
        Quaternion dipperBaseRot;
        Vector3 scoopLiquidRest;
        bool heldNow;
        bool everUsed;
        float shakeAmount;
        float dipTime;
        float pourTilt;
        bool dipActive;
        float basinRippleTimer;
        string overridePrompt;

        /// <summary>编辑器里做动画取图时用：>0 时用这个固定步长代替 Time.deltaTime（运行时保持 0）</summary>
        public static float DebugDeltaTime = 0f;
        bool restCaptured;

        void Awake()
        {
            // 注意：在 Play 模式下组件是先 AddComponent 再逐个赋引用的，
            // 所以 Awake 里 sieveRoot 往往还是 null。这里只做“引用已就绪”的捕获，
            // 真正的静置位姿由 RestPoseCapture 在第一个 LateUpdate 里补上。
            CaptureRestPose();
            ApplyVisuals();
            if (drips != null) { var e = drips.emission; e.enabled = false; }
            if (pourStream != null) pourStream.SetActive(false);
        }

        /// <summary>记录筛框/木瓢的静置位姿（摇筛与倾倒都相对它做偏移）</summary>
        void CaptureRestPose()
        {
            if (sieveRoot == null) return;
            sieveBasePos = sieveRoot.localPosition;
            sieveBaseRot = sieveRoot.localRotation;
            if (dipper != null)
            {
                dipperBasePos = dipper.localPosition;
                dipperBaseRot = dipper.localRotation;
            }
            if (scoopLiquid != null) scoopLiquidRest = scoopLiquid.localPosition;
            restCaptured = true;
        }

        // ——————————————— 木瓢动作 ———————————————

        Vector3 RestPos { get { return dipperRest != null ? dipperRest.localPosition : dipperBasePos; } }
        Quaternion RestRot { get { return dipperRest != null ? dipperRest.localRotation : dipperBaseRot; } }
        Vector3 PourPos { get { return dipperPour != null ? dipperPour.localPosition : RestPos + Vector3.up; } }
        Quaternion PourRot { get { return dipperPour != null ? dipperPour.localRotation * Quaternion.Euler(0f, 0f, -104f) : RestRot; } }
        Quaternion CarryRot { get { return dipperPour != null ? dipperPour.localRotation * Quaternion.Euler(0f, 0f, -14f) : RestRot; } }

        static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }

        /// <summary>一次舀浆：先探进缸里，再带水抬起来，最后落回缸里</summary>
        void DipPose(float k, out Vector3 pos, out Quaternion rot)
        {
            Vector3 pDip = RestPos + new Vector3(0f, -0.17f, 0.02f);
            Quaternion rDip = RestRot * Quaternion.Euler(16f, 0f, 0f);
            Vector3 pLift = RestPos + new Vector3(-0.03f, 0.20f, 0.03f);
            Quaternion rLift = RestRot * Quaternion.Euler(-10f, 0f, 0f);

            if (k < 0.30f)
            {
                float s = Mathf.SmoothStep(0f, 1f, k / 0.30f);
                pos = Vector3.Lerp(RestPos, pDip, s);
                rot = Quaternion.Slerp(RestRot, rDip, s);
            }
            else if (k < 0.72f)
            {
                float s = Mathf.SmoothStep(0f, 1f, (k - 0.30f) / 0.42f);
                // 抬起来带一点过冲，像手腕提了一下
                pos = Bezier(pDip, pDip + new Vector3(-0.05f, 0.26f, 0.02f), pLift + Vector3.up * 0.01f, s);
                rot = Quaternion.Slerp(rDip, rLift, s);
            }
            else
            {
                float s = Mathf.SmoothStep(0f, 1f, (k - 0.72f) / 0.28f);
                pos = Bezier(pLift, pLift + new Vector3(0.02f, 0.05f, -0.01f), RestPos, s);
                rot = Quaternion.Slerp(rLift, RestRot, s);
            }
        }

        /// <summary>从料缸里舀一下（由 SlurryTub 触发）</summary>
        public void PlayDip()
        {
            dipActive = true;
            dipTime = 0f;
        }

        // ————————————————— 交互 —————————————————
        public string Prompt
        {
            get
            {
                if (overridePrompt != null) return overridePrompt;
                switch (Current)
                {
                    case Stage.Empty:
                        return (tub != null && tub.Empty) ? "料缸已空 · 去石槽取浆" : "按住 E｜上料";
                    case Stage.Loading: return "上料中…";
                    case Stage.ReadyToSieve: return "按住 E｜摇筛";
                    case Stage.Sieving: return "摇筛中…";
                    case Stage.Done: return "按 R｜刮除筛上粗渣";
                }
                return null;
            }
        }

        public void Interact(float dt, bool pressed, bool held)
        {
            heldNow = held || pressed;
            if (pressed) everUsed = true;
        }

        /// <summary>按 R：把筛上粗渣刮到粗渣盆</summary>
        public void RequestScrape()
        {
            if (Current == Stage.Done && Residue01 > 0.02f) scraping = true;
        }

        bool scraping;

        void LateUpdate()
        {
            float dt = DebugDeltaTime > 0f ? DebugDeltaTime : Mathf.Min(Time.deltaTime, 0.05f);

            // 第一帧补捕获：此时构建器已经把筛框摆到位了，不会把筛框拽到原点
            if (!restCaptured) CaptureRestPose();

            bool held = heldNow;
            heldNow = false;

            switch (Current)
            {
                case Stage.Empty:
                    if (held && (tub == null || !tub.Empty)) Current = Stage.Loading;
                    break;

                case Stage.Loading:
                    Load01 += dt / Mathf.Max(0.05f, loadSeconds);
                    if (Load01 >= 1f)
                    {
                        Load01 = 1f;
                        Current = Stage.ReadyToSieve;
                        if (tub != null) tub.Consume(0.25f);
                        SetSplash();
                    }
                    break;

                case Stage.ReadyToSieve:
                    if (held) Current = Stage.Sieving;
                    break;

                case Stage.Sieving:
                    if (!held) Current = Stage.ReadyToSieve;
                    else
                    {
                        Purity01 += dt / Mathf.Max(0.05f, sieveSeconds);
                        Residue01 = Mathf.Clamp01(Purity01 * 1.05f);
                        if (Purity01 >= 1f)
                        {
                            Purity01 = 1f;
                            Current = Stage.Done;
                            Batches++;
                        }
                    }
                    break;

                case Stage.Done:
                    if (scraping)
                    {
                        // 刮渣：残渣被抹走，筛面重新露出来
                        Residue01 = Mathf.Max(0f, Residue01 - dt / Mathf.Max(0.05f, scrapeSeconds));
                        if (Residue01 <= 0f)
                        {
                            scraping = false;
                            Load01 = 0f;
                            Purity01 = 0f;
                            Current = Stage.Empty;
                        }
                    }
                    else if (held && (tub == null || !tub.Empty))
                    {
                        scraping = false;
                        Current = Stage.Loading;
                    }
                    break;
            }

            // 浆从筛下流进盆里
            if (Current == Stage.Sieving || Current == Stage.Done)
            {
                float target = Mathf.Clamp01(Batches * 0.5f + (Current == Stage.Sieving ? Purity01 * 0.5f : 0f));
                BasinFill = Mathf.MoveTowards(BasinFill, Mathf.Min(1f, target), dt * 0.25f);
            }

            Animate(dt);
            ApplyVisuals();
        }

        void Animate(float dt)
        {
            float t = Time.time;

            // —— 摇筛：人手的“八字”轨迹，不是单向往复 ——
            float want = (Current == Stage.Sieving) ? 1f : 0f;
            shakeAmount = Mathf.Lerp(shakeAmount, want, dt * (want > 0.5f ? 5f : 3f));

            if (sieveRoot != null)
            {
                float s = Mathf.Sin(t * 11.5f);
                float c = Mathf.Cos(t * 11.5f);
                // 圆形 / 八字：横向推拉 + 前后错相，接触面一直在变，看起来才像在“筛”
                float ax = (s + Mathf.Sin(t * 5.7f) * 0.25f);
                float az = (c * 0.75f + Mathf.Sin(t * 3.1f) * 0.2f);
                float lift = Mathf.Abs(Mathf.Sin(t * 23f));
                sieveRoot.localRotation = sieveBaseRot * Quaternion.Euler(
                    s * 1.1f * shakeAmount,
                    c * 0.45f * shakeAmount,
                    ax * 1.5f * shakeAmount);
                sieveRoot.localPosition = sieveBasePos + new Vector3(
                    ax * 0.008f * shakeAmount,
                    lift * 0.0035f * shakeAmount,
                    az * 0.010f * shakeAmount);

                // 粗渣随着筛动往一边堆（真实的筛也是往一边跑）
                if (residue != null && residue.gameObject.activeSelf)
                {
                    residue.localPosition = new Vector3(
                        -0.05f + az * 0.018f * shakeAmount,
                        residue.localPosition.y,
                        0.03f - ax * 0.022f * shakeAmount);
                }
            }

            // —— 木瓢 ——
            if (dipper != null)
            {
                Vector3 targetPos;
                Quaternion targetRot;
                bool carrying = false;

                bool pouring = Current == Stage.Loading && Load01 > 0.42f && Load01 < 1f;
                if (dipActive)
                {
                    dipTime += dt;
                    float k = Mathf.Clamp01(dipTime / dipDuration);
                    DipPose(k, out targetPos, out targetRot);
                    carrying = k > 0.40f && k < 0.98f;
                    if (k >= 1f) dipActive = false;
                }
                else if (pouring)
                {
                    // 分段：先平端着走弧线过去（0.42→0.72），到位后再翻瓢倒出来（0.72→0.92）
                    float travel = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.72f, Load01));
                    float tilt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.70f, 0.92f, Load01));

                    Vector3 a = RestPos;
                    Vector3 b = dipperPour != null ? dipperPour.localPosition : RestPos;
                    Vector3 ctrl = Vector3.Lerp(a, b, 0.5f) + new Vector3(0.10f, 0.34f, 0.06f);
                    targetPos = Bezier(a, ctrl, b, travel);

                    targetRot = Quaternion.Slerp(RestRot, CarryRot, travel);
                    targetRot = Quaternion.Slerp(targetRot, PourRot, tilt);
                    carrying = true;
                    pourTilt = tilt;
                }
                else
                {
                    // 倒完就回缸里泡着，不该一直悬在筛面上
                    targetPos = RestPos;
                    targetRot = RestRot;
                }

                // 追赶式跟随：快但带一点惯性，手感比线性插值好
                dipper.localPosition = Vector3.Lerp(dipper.localPosition, targetPos, 1f - Mathf.Exp(-14f * dt));
                dipper.localRotation = Quaternion.Slerp(dipper.localRotation, targetRot, 1f - Mathf.Exp(-14f * dt));

                // 瓢里的浆：舀起来的时候有，倒出去就没了
                if (scoopLiquid != null)
                {
                    bool on = carrying && (dipActive || pouring);
                    if (scoopLiquid.gameObject.activeSelf != on) scoopLiquid.gameObject.SetActive(on);
                    if (on)
                    {
                        float lv = pouring
                            ? Mathf.Lerp(1f, 0.15f, Mathf.Clamp01((Load01 - 0.55f) / 0.4f))
                            : Mathf.Clamp01((dipTime / dipDuration - 0.35f) / 0.2f);
                        scoopLiquid.localScale = new Vector3(1f, 1f, 1f) * Mathf.Max(0.001f, lv);
                        scoopLiquid.localPosition = scoopLiquidRest + new Vector3(0f, 0.012f * lv, 0f);
                    }
                }
            }

            // —— 浆流：挂在瓢口上，长度按瓢口到筛面的距离实时拉 ——
            if (pourStream != null)
            {
                bool pouringNow = Current == Stage.Loading && pourTilt > 0.22f && Load01 < 0.995f;
                if (pourStream.activeSelf != pouringNow)
                {
                    pourStream.SetActive(pouringNow);
                    if (pouringNow && splashes != null) splashes.Emit(10);
                }
                if (pouringNow && pourLip != null && dipper != null)
                {
                    // 瓢口的站位空间坐标（跟着瓢的姿态走，所以永远接得住）
                    Vector3 lip = transform.InverseTransformPoint(pourLip.position);
                    float bottomY = sieveBasePos.y - 0.014f;
                    float h = Mathf.Max(0.04f, lip.y - bottomY);
                    float grow = Mathf.Clamp01((pourTilt - 0.22f) / 0.28f);
                    float wob = 0.90f + Mathf.Sin(t * 26f) * 0.055f + Mathf.Sin(t * 47f) * 0.03f;
                    var tr = pourStream.transform;
                    tr.localPosition = new Vector3(lip.x, bottomY, lip.z);
                    tr.localScale = new Vector3(wob * Mathf.Lerp(0.55f, 1f, grow),
                                                (h / 0.27f) * grow,
                                                wob * Mathf.Lerp(0.55f, 1f, grow));
                }
            }

            // —— 上料时筛面被浇湿 ——
            if (Current == Stage.Loading && Load01 > 0.5f && drips != null)
            {
                var e = drips.emission;
                e.enabled = true;
                e.rateOverTime = 18f;
            }

            // —— 筛下滴落 ——
            if (drips != null)
            {
                var e = drips.emission;
                if (Current == Stage.Sieving)
                {
                    e.enabled = true;
                    e.rateOverTime = Mathf.Lerp(10f, 46f, shakeAmount);
                }
                else if (!(Current == Stage.Loading && Load01 > 0.5f))
                {
                    e.enabled = false;
                }
            }

            // —— 接浆盆水面：一边滴一边泛涟漪 ——
            if (basinRipple != null && BasinFill > 0.02f)
            {
                if (Current == Stage.Sieving)
                {
                    basinRippleTimer -= dt;
                    if (basinRippleTimer <= 0f)
                    {
                        basinRippleTimer = Mathf.Lerp(0.55f, 0.20f, shakeAmount);
                        basinRipple.Play(0.55f + BasinFill * 0.55f);
                    }
                }
                else
                {
                    basinRippleTimer = 0f;
                }
            }

            // —— 料缸液面随取浆下降 ——
            if (tubLevel != null && tub != null)
            {
                float y = Mathf.Lerp(0.055f, 0.335f, tub.Fill01);
                tubLevel.localPosition = new Vector3(tubLevel.localPosition.x, y, tubLevel.localPosition.z);
                if (Mathf.Abs(tubLevel.localScale.x - 1f) > 0.01f) tubLevel.localScale = Vector3.one;
            }

            // —— 接浆盆：浆体体积 + 液面 ——
            if (basinFill != null)
            {
                float h = Mathf.Lerp(0f, 0.155f, BasinFill);
                basinFill.localScale = new Vector3(basinFill.localScale.x, Mathf.Max(0.0001f, h), basinFill.localScale.z);
                if (basinSurface != null)
                    basinSurface.localPosition = new Vector3(basinSurface.localPosition.x, 0.012f + h, basinSurface.localPosition.z);
            }
        }

        void ApplyVisuals()
        {
            // 筛面：干 → 湿（材质切换，比调颜色更真实）
            if (clothRenderer != null && clothDry != null && clothWet != null)
            {
                bool wet = Load01 > 0.55f;
                var want = wet ? clothWet : clothDry;
                if (clothRenderer.sharedMaterial != want) clothRenderer.sharedMaterial = want;
            }
            // 筛面薄浆（整张布面被浆浸湿的程度，用透明度表达，而不是缩放）
            if (film != null && filmMat != null)
            {
                float f = Mathf.Clamp01((Load01 - 0.35f) / 0.5f) * (1f - Purity01 * 0.92f);
                bool on = f > 0.02f;
                if (film.gameObject.activeSelf != on) film.gameObject.SetActive(on);
                if (on)
                {
                    Color c = filmMat.color;
                    c.a = f * 0.55f;
                    // 越干越偏向灰白、越湿越沉
                    c.r = Mathf.Lerp(0.92f, 0.78f, f);
                    c.g = Mathf.Lerp(0.91f, 0.77f, f);
                    c.b = Mathf.Lerp(0.87f, 0.73f, f);
                    filmMat.color = c;
                }
            }
            // 筛上粗渣
            if (residue != null)
            {
                float r = Residue01;
                bool on = r > 0.02f;
                if (residue.gameObject.activeSelf != on) residue.gameObject.SetActive(on);
                if (on)
                {
                    float s = Mathf.Lerp(0.25f, 1f, r);
                    residue.localScale = new Vector3(s, Mathf.Lerp(0.2f, 1f, r), s);
                }
            }
        }

        /// <summary>复位成“空筛待上料”（动画取图用）</summary>
        public void ResetForDemo()
        {
            Current = Stage.Empty;
            Load01 = 0f;
            Purity01 = 0f;
            Residue01 = 0f;
            scraping = false;
            dipActive = false;
            BasinFill = 0f;
            ApplyVisuals();
        }

        /// <summary>把工作站推进到“正在摇筛”的可读状态（参考图 / 演示用，不改变真实玩法）</summary>
        public void SetDemoProgress(float purity)        {
            Current = Stage.Sieving;
            Load01 = 1f;
            Purity01 = Mathf.Clamp01(purity);
            Residue01 = Mathf.Clamp01(purity * 1.05f);
            if (Batches < 1) Batches = 1;
            BasinFill = Mathf.Clamp01(0.22f + purity * 0.45f);
            if (tub != null && tub.Fill01 > 0.7f) tub.Fill01 = 0.7f;
            ApplyVisuals();
        }

        void SetSplash()
        {
            if (splashes != null) splashes.Emit(6);
        }

        /// <summary>界面用的阶段名（青花蓝工序编号旁的小字）</summary>
        public string StageName
        {
            get
            {
                switch (Current)
                {
                    case Stage.Empty: return "待上料";
                    case Stage.Loading: return "上料";
                    case Stage.ReadyToSieve: return "待摇筛";
                    case Stage.Sieving: return "摇筛中";
                    case Stage.Done: return "待刮渣";
                }
                return "";
            }
        }
    }
}
