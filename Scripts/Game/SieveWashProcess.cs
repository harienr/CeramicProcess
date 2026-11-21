using UnityEngine;

namespace CiCongNiSheng
{
    public enum LadleContent { None, Slurry, Water }

    public enum ToolKind { None, Ladle, Scraper, DecantLadle }

    /// <summary>筛洗工序的阶段性目标，由状态推导而非硬切换，避免状态机卡死。</summary>
    public enum WashPhase { Scoop, Pour, Wash, ClearResidue, Settle, Decant, Complete }

    /// <summary>
    /// 筛洗工序的唯一数据源：粗浆 → 过筛 → 搅洗 → 清渣 → 沉淀 → 撇水。
    /// 所有可交互物件只负责把玩家的动作换算成这里的一次变更，
    /// 视觉反馈统一读取这里的数值，保证"操作—材料状态"始终对应。
    /// </summary>
    public class SieveWashProcess : MonoBehaviour
    {
        public static SieveWashProcess I { get; private set; }

        [Header("容器存量（0-1 为各容器自身的满量）")]
        public float rawSlurry = 1f;      // 粗浆缸
        public float waterSupply = 1f;    // 清水缸
        public float ladleFill;           // 木瓢装量
        public LadleContent ladleContent = LadleContent.None;
        public float sieveSlurry;         // 筛面湿浆
        public float sieveResidue;        // 筛面粗渣
        public float sieveWater;          // 筛面稀释度：0 稠，1 稀
        public float vatFine;             // 沉淀缸细浆
        public float vatWater;            // 沉淀缸上层清水厚度
        public float trayFill;            // 渣盘存量

        [Header("阶段")]
        public bool settled;
        public bool settling;
        public bool completed;
        public float settleTimer;

        [Header("调校（1.0 = 一整批原料）")]
        public float ladleCapacity = 0.20f;
        public float waterPerLadle = 0.18f;
        public float sieveCapacity = 0.62f;
        public float clogThreshold = 0.26f;
        public float settleTarget = 0.42f;
        public float fineShare = 0.58f;
        public float washBaseRate = 0.055f;
        public float scoopRate = 2.4f;
        public float pourRate = 2.0f;
        public float scrapeRate = 0.42f;
        public float decantRate = 0.15f;
        public float splashSpeed = 260f;
        public float settleDuration = 2.8f;
        public float settledWaterLayer = 0.34f;

        [Header("统计")]
        public float pouredTotal;
        public float fineRecovered;
        public float coarseRemoved;
        public float spilledLoss;
        public float clayLoss;
        public float purity = 1f;
        public float elapsed;
        public int loadCount;
        public int scrapeCount;

        float _pourPending;
        bool _pourActive;
        bool _scrapeActive;

        void Awake()
        {
            I = this;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            if (settling)
            {
                settleTimer += dt;
                if (settleTimer >= settleDuration)
                {
                    settling = false;
                    settled = true;
                    vatWater = settledWaterLayer;
                }
            }

            if (settled && vatWater <= 0.02f)
            {
                completed = true;
            }
        }

        // ---------------------------------------------------------------- 阶段

        public WashPhase Phase
        {
            get
            {
                if (completed) return WashPhase.Complete;
                if (settling || settled) return WashPhase.Decant;
                if (vatFine >= settleTarget)
                {
                    if (sieveSlurry <= 0.015f) return WashPhase.Settle;
                    // 细浆够了但筛面被堵住时，先清渣，否则会卡在无法推进的"搅洗"阶段
                    return sieveResidue >= clogThreshold ? WashPhase.ClearResidue : WashPhase.Wash;
                }
                if (rawSlurry <= 0.001f && sieveSlurry <= 0.015f && vatFine > 0.05f) return WashPhase.Settle;
                if (sieveResidue >= clogThreshold) return WashPhase.ClearResidue;
                if (sieveSlurry > 0.015f) return WashPhase.Wash;
                if (ladleContent == LadleContent.Slurry && ladleFill > 0.02f) return WashPhase.Pour;
                return WashPhase.Scoop;
            }
        }

        public string PhaseTitle
        {
            get
            {
                switch (Phase)
                {
                    case WashPhase.Scoop: return "取粗浆";
                    case WashPhase.Pour: return "上筛";
                    case WashPhase.Wash: return "搅洗";
                    case WashPhase.ClearResidue: return "清渣";
                    case WashPhase.Settle: return "静置沉淀";
                    case WashPhase.Decant: return "撇水";
                    default: return "完成";
                }
            }
        }

        public string PhaseHint
        {
            get
            {
                switch (Phase)
                {
                    case WashPhase.Scoop:
                        return rawSlurry <= 0.02f ? "粗浆缸已见底，可以静置沉淀了" : "用木瓢从粗浆缸舀取粗浆";
                    case WashPhase.Pour:
                        return "把瓢中的粗浆缓缓倒到筛面中央";
                    case WashPhase.Wash:
                        if (sieveWater < 0.22f) return "浆料偏稠，可舀一瓢清水稀释后搅洗";
                        return "按住左键在筛面画圈，把细浆洗过筛网";
                    case WashPhase.ClearResidue:
                        return "筛面粗渣已积厚，用竹刮拨入渣盘";
                    case WashPhase.Settle:
                        return CanSettle ? "按 T 静置一时辰，让细浆沉淀" : "先把手上的浆料处理完";
                    case WashPhase.Decant:
                        return settled ? "用撇水勺撇去上层清水" : "静置中……";
                    default:
                        return "泥料已备好，可以去下一步了";
                }
            }
        }

        /// <summary>整道工序的完成度，用于顶部细线进度。</summary>
        public float Completion01
        {
            get
            {
                if (completed) return 1f;
                if (settled) return Mathf.Lerp(0.72f, 1f, 1f - Mathf.Clamp01(vatWater / Mathf.Max(0.01f, settledWaterLayer)));
                float fine01 = Mathf.Clamp01(vatFine / Mathf.Max(0.01f, settleTarget)) * 0.72f;
                return fine01;
            }
        }

        // ---------------------------------------------------------------- 动作

        public bool CanSettleSafely => !settled && !settling && sieveSlurry <= 0.02f && vatFine >= 0.06f;

        public bool CanSettle => CanSettleSafely;

        public void BeginSettle()
        {
            if (!CanSettle) return;
            settling = true;
            settleTimer = 0f;
        }

        /// <summary>舀取。dFill 为瓢内刻度的增量（0-1）。</summary>
        public float Scoop(LadleContent kind, float dFill)
        {
            if (dFill <= 0f) return 0f;
            if (ladleContent != LadleContent.None && ladleContent != kind) return 0f;

            float room = 1f - ladleFill;
            dFill = Mathf.Min(dFill, room);
            if (dFill <= 0f) return 0f;

            if (kind == LadleContent.Slurry)
            {
                float available = rawSlurry / Mathf.Max(0.0001f, ladleCapacity);
                dFill = Mathf.Min(dFill, available);
                if (dFill <= 0f) return 0f;
                rawSlurry = Mathf.Max(0f, rawSlurry - dFill * ladleCapacity);
            }
            else
            {
                float available = waterSupply / Mathf.Max(0.0001f, waterPerLadle);
                dFill = Mathf.Min(dFill, available);
                if (dFill <= 0f) return 0f;
                waterSupply = Mathf.Max(0f, waterSupply - dFill * waterPerLadle);
            }

            ladleFill = Mathf.Min(1f, ladleFill + dFill);
            ladleContent = kind;
            return dFill;
        }

        /// <summary>倾倒。返回溢出的比例（筛面过满时溅出）。</summary>
        public float Pour(float dFill)
        {
            if (dFill <= 0f || ladleContent == LadleContent.None || ladleFill <= 0f) return 0f;
            dFill = Mathf.Min(dFill, ladleFill);
            ladleFill -= dFill;

            float spill = 0f;
            if (ladleContent == LadleContent.Water)
            {
                sieveWater = Mathf.Clamp01(sieveWater + dFill * waterPerLadle * 2.2f);
            }
            else
            {
                float add = dFill * ladleCapacity;
                float room = Mathf.Max(0f, sieveCapacity - sieveSlurry);
                float accepted = Mathf.Min(add, room);
                spill = add - accepted;

                sieveSlurry += accepted;
                pouredTotal += accepted;
                spilledLoss += spill;
                _pourPending += accepted;
                _pourActive = true;

                // 未清渣就上浆，粗渣混入会降低细腻度
                if (sieveResidue > 0.10f)
                {
                    purity = Mathf.Clamp01(purity - accepted * 0.45f);
                }
            }

            if (ladleFill <= 0.002f)
            {
                ladleFill = 0f;
                ladleContent = LadleContent.None;
                if (_pourActive && _pourPending > 0.02f) loadCount++;
                _pourPending = 0f;
                _pourActive = false;
            }
            return spill;
        }

        /// <summary>
        /// 搅洗。input 为 0-1 的搅动强度，mouseSpeed 为鼠标移动速度（像素/秒），
        /// 过快会把浆料甩出筛沿，形成可见的损耗。
        /// </summary>
        public float Wash(float dt, float input, float mouseSpeed)
        {
            if (sieveSlurry <= 0f || dt <= 0f) return 0f;

            float dilution = Mathf.Lerp(0.52f, 1f, Mathf.Clamp01(sieveWater));
            float clog = 1f - Mathf.SmoothStep(0.5f, 1f, Mathf.Clamp01(sieveResidue / Mathf.Max(0.01f, clogThreshold)));
            float rate = washBaseRate * Mathf.Clamp(input, 0f, 1.5f) * dilution * clog;

            float mass = Mathf.Min(sieveSlurry, rate * dt);
            if (mass <= 0f) return 0f;

            float waste = 0f;
            if (mouseSpeed > splashSpeed)
            {
                waste = mass * 0.32f * Mathf.InverseLerp(splashSpeed, splashSpeed * 2.4f, mouseSpeed);
            }

            sieveSlurry = Mathf.Max(0f, sieveSlurry - mass);
            float fine = mass * fineShare - waste;
            fine = Mathf.Max(0f, fine);
            vatFine = Mathf.Max(0f, vatFine + fine);
            fineRecovered += fine;
            spilledLoss += waste;

            float coarse = mass * (1f - fineShare);
            sieveResidue = Mathf.Min(1f, sieveResidue + coarse);
            sieveWater = Mathf.Max(0f, sieveWater - mass * 1.15f);
            return mass;
        }

        /// <summary>刮渣。input 为 0-1 的刮动强度。</summary>
        public float Scrape(float dt, float input)
        {
            if (sieveResidue <= 0f || dt <= 0f) return 0f;
            float amount = Mathf.Min(sieveResidue, scrapeRate * Mathf.Clamp01(input) * dt);
            sieveResidue = Mathf.Max(0f, sieveResidue - amount);
            trayFill = Mathf.Min(1f, trayFill + amount * 0.85f);
            coarseRemoved += amount;

            if (!_scrapeActive && amount > 0f)
            {
                _scrapeActive = true;
                scrapeCount++;
            }
            if (sieveResidue <= 0.004f) _scrapeActive = false;
            return amount;
        }

        /// <summary>撇水。input 为 0-1 的撇动强度。</summary>
        public void Decant(float dt, float input)
        {
            if (!settled || dt <= 0f) return;
            float amount = decantRate * Mathf.Clamp01(input) * dt;
            float removed = Mathf.Min(vatWater, amount);
            vatWater = Mathf.Max(0f, vatWater - removed);

            // 撇过界会把泥料一起带走
            float excess = amount - removed;
            if (excess > 0f || vatWater <= 0.004f)
            {
                clayLoss += (excess + amount * 0.35f);
                vatFine = Mathf.Max(0f, vatFine - amount * 0.20f);
            }
        }

        // ---------------------------------------------------------------- 评级

        public float FinenessScore
        {
            get
            {
                float clean = Mathf.Clamp01(purity);
                float water = Mathf.Clamp01(sieveWater);
                return Mathf.Clamp01(clean * 0.8f + (1f - Mathf.Abs(water - 0.45f)) * 0.2f);
            }
        }

        public float RecoveryScore
        {
            get
            {
                float potential = Mathf.Max(0.0001f, pouredTotal * fineShare);
                return Mathf.Clamp01(fineRecovered / potential);
            }
        }

        public float WasteScore => Mathf.Clamp01(spilledLoss + clayLoss);

        public string Grade
        {
            get
            {
                float s = FinenessScore * 0.5f + RecoveryScore * 0.4f + (1f - WasteScore) * 0.1f;
                if (s >= 0.86f) return "优";
                if (s >= 0.70f) return "良";
                return "可";
            }
        }

        public void ResetAll()
        {
            rawSlurry = 1f;
            waterSupply = 1f;
            ladleFill = 0f;
            ladleContent = LadleContent.None;
            sieveSlurry = 0f;
            sieveResidue = 0f;
            sieveWater = 0f;
            vatFine = 0f;
            vatWater = 0f;
            trayFill = 0f;
            settled = false;
            settling = false;
            completed = false;
            settleTimer = 0f;
            pouredTotal = 0f;
            fineRecovered = 0f;
            coarseRemoved = 0f;
            spilledLoss = 0f;
            clayLoss = 0f;
            purity = 1f;
            elapsed = 0f;
            loadCount = 0;
            scrapeCount = 0;
            _pourPending = 0f;
            _pourActive = false;
            _scrapeActive = false;
        }
    }
}
