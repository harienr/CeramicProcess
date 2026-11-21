using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 筛架：上浆、徒手搅洗、竹刮清渣三个动作都在筛面上完成。
    /// 筛网湿态、筛面浆层、粗渣层都是材料状态的直接可视化。
    /// </summary>
    public class SieveStation : Interactable
    {
        [Header("筛面")]
        public Renderer clothRenderer;
        public Transform slurryBlob;
        public Transform residueBlob;
        public Transform stirStick;
        public float slurryRadiusMax = 0.175f;
        public float residueRadiusMax = 0.155f;
        public float slurryFullAmount = 0.30f;

        Material _clothMat;
        float _washPulse;
        bool _washing;
        float _stirAngle;
        float _lastAngle;
        float _angVel;
        float _splashFlash;

        public bool Splashing => _splashFlash > 0.35f;

        SieveWashProcess Proc => SieveWashProcess.I;

        protected override void Awake()
        {
            base.Awake();
            SetMaterialsHighlightable(GetComponentsInChildren<Renderer>());
        }

        // clothRenderer 由装配器在 AddComponent 之后赋值，因此材质实例延迟创建
        void EnsureClothMaterial()
        {
            if (_clothMat != null || clothRenderer == null) return;
            _clothMat = new Material(clothRenderer.sharedMaterial) { name = "sieve_cloth_runtime" };
            clothRenderer.sharedMaterial = _clothMat;
        }

        void Update()
        {
            var p = Proc;
            if (p == null) return;
            float dt = Time.deltaTime;
            EnsureClothMaterial();

            _washPulse = Mathf.Lerp(_washPulse, _washing ? 1f : 0f, 1f - Mathf.Exp(-9f * dt));
            _washing = false;
            _splashFlash = Mathf.Max(0f, _splashFlash - dt * 1.8f);

            float wet = Mathf.Clamp01(p.sieveSlurry / Mathf.Max(0.01f, p.sieveCapacity) * 1.5f + p.sieveWater * 0.25f);
            if (_clothMat != null) _clothMat.SetFloat("_Wet", wet);

            if (slurryBlob != null)
            {
                float t = Mathf.Clamp01(p.sieveSlurry / Mathf.Max(0.01f, slurryFullAmount));
                slurryBlob.gameObject.SetActive(t > 0.008f);
                float r = Mathf.Lerp(0.34f, 1f, Mathf.Sqrt(t)) * slurryRadiusMax;
                float h = Mathf.Lerp(0.018f, 0.032f, t) * (1f + _washPulse * 0.12f);
                slurryBlob.localScale = new Vector3(r, h, r);
                slurryBlob.localRotation = Quaternion.Euler(0f, _stirAngle, 0f);
            }

            if (residueBlob != null)
            {
                float t = Mathf.Clamp01(p.sieveResidue / Mathf.Max(0.01f, p.clogThreshold));
                residueBlob.gameObject.SetActive(t > 0.02f);
                float r = Mathf.Lerp(0.40f, 1f, Mathf.Sqrt(t)) * residueRadiusMax;
                residueBlob.localScale = new Vector3(r, Mathf.Lerp(0.014f, 0.030f, t), r);
                residueBlob.localRotation = Quaternion.Euler(0f, -_stirAngle * 0.5f, 0f);
            }

            if (stirStick != null)
            {
                bool show = _washPulse > 0.05f;
                stirStick.gameObject.SetActive(show);
                if (show)
                {
                    float ang = _stirAngle * Mathf.Deg2Rad;
                    float radius = 0.10f + Mathf.Sin(_stirAngle * 0.7f) * 0.02f;
                    stirStick.localPosition = new Vector3(Mathf.Cos(ang) * radius, 0.045f, Mathf.Sin(ang) * radius);
                    stirStick.localRotation = Quaternion.Euler(62f, _stirAngle, 0f);
                }
            }
        }

        // ---------------------------------------------------------------- 提示

        public override string Prompt(PlayerInteractor player)
        {
            var p = Proc;
            if (p == null) return string.Empty;
            if (p.completed) return "筛洗已完成";

            if (player.HeldTool == ToolKind.Ladle)
            {
                if (p.ladleContent == LadleContent.Slurry)
                {
                    if (p.sieveSlurry >= p.sieveCapacity * 0.93f) return "筛面浆料已满，先搅洗";
                    return "按住左键倒浆上筛";
                }
                if (p.ladleContent == LadleContent.Water) return "按住左键往筛面加清水";
                return "瓢已倒空 · 右键放下木瓢后可徒手搅洗";
            }
            if (player.HeldTool == ToolKind.Scraper)
            {
                return p.sieveResidue > 0.005f ? "按住左键左右拖动，把粗渣刮入渣盘" : "筛面干净，无需清渣";
            }
            if (player.HeldTool == ToolKind.DecantLadle)
            {
                return "撇水勺在沉淀缸那边用";
            }

            if (p.sieveResidue >= p.clogThreshold) return "筛面被粗渣堵住，先用竹刮清理";
            if (p.sieveSlurry <= 0.012f) return "空手 · 先舀一瓢粗浆倒上筛面";
            if (p.sieveWater < 0.22f) return "空手 · 按住左键画圈搅洗（可先舀清水稀释）";
            return "空手 · 按住左键在筛面画圈搅洗";
        }

        public override bool CanPress(PlayerInteractor player)
        {
            var p = Proc;
            if (p == null || p.completed) return false;

            if (player.HeldTool == ToolKind.Ladle)
            {
                if (p.ladleFill <= 0.01f) return false;
                return p.sieveSlurry < p.sieveCapacity * 0.97f;
            }
            if (player.HeldTool == ToolKind.Scraper) return p.sieveResidue > 0.004f;
            if (player.HeldTool == ToolKind.DecantLadle) return false;
            return p.sieveSlurry > 0.012f;
        }

        // ---------------------------------------------------------------- 动作

        public override void OnPrimaryHold(PlayerInteractor player, float dt)
        {
            var p = Proc;
            if (p == null) return;
            TrackOrbit(player.MouseMotion, dt);

            if (player.HeldTool == ToolKind.Ladle)
            {
                if (p.ladleFill <= 0.01f) return;
                p.Pour(p.pourRate * dt);
            }
            else if (player.HeldTool == ToolKind.Scraper)
            {
                float input = Mathf.Clamp01(Mathf.InverseLerp(12f, 90f, player.MouseSpeed));
                if (input <= 0.02f) return;
                p.Scrape(dt, input);
                _stirAngle += player.MouseMotion.x * 3.2f;
                if (player.MouseSpeed > p.splashSpeed * 1.4f) _splashFlash = 1f;
            }
            else if (player.HeldTool == ToolKind.None)
            {
                float speed01 = Mathf.Clamp01(Mathf.InverseLerp(15f, 110f, player.MouseSpeed));
                if (speed01 <= 0.02f) return;

                float coherence = Mathf.Clamp01(Mathf.Abs(_angVel) / 260f);
                float input = speed01 * (0.85f + 0.4f * coherence);

                _washing = true;
                p.Wash(dt, input, player.MouseSpeed);
                _stirAngle += player.MouseMotion.x * 2.6f + player.MouseMotion.y * 0.8f;

                if (player.MouseSpeed > p.splashSpeed) _splashFlash = 1f;
            }
        }

        void TrackOrbit(Vector2 d, float dt)
        {
            if (dt <= 0f) return;
            if (d.sqrMagnitude < 0.0025f)
            {
                _angVel = Mathf.Lerp(_angVel, 0f, 1f - Mathf.Exp(-5f * dt));
                return;
            }
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(_lastAngle, ang);
            _lastAngle = ang;
            _angVel = Mathf.Lerp(_angVel, delta / dt, 0.25f);
        }
    }
}
