using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Porcelain
{
    /// <summary>构建结果，供运行时与截图工具使用。</summary>
    public class WorkshopRefs
    {
        public GameObject root;
        public GameObject player;
        public Camera camera;
        public SievingStation station;
        public SlurryTub tub;
        public FirstPersonController fpc;
        public PlayerInteractor interactor;
        public HUD hud;
        public Transform roof;
        public Light sun;
        public ParticleSystem dustBeam;
        public ParticleSystem dustRoom;
        public Camera[] shots;
    }

    /// <summary>
    /// 程序化搭建“过筛”工坊场景。
    ///
    /// 光与构图（这是整个场景的骨架）：
    /// 玩家从正面开敞的棚口进来，视线朝 -Z；屏幕右手边是 -X，也就是大侧窗那一面。
    /// 主光从侧窗斜射进来，正好落在筛台上；正面棚口再给一层柔和的正面天光。
    /// 左侧（屏幕左）是背光的储藏区，留给石堆、陶缸与暗部层次。
    ///
    /// 材料流向：石槽（淘洗）→ 料缸（待筛）→ 筛台（过筛）→ 接浆盆（细浆）/ 粗渣盆（残渣）。
    /// </summary>
    public static class WorkshopBuilder
    {
        public static WorkshopRefs BuildIntegrated(Transform parent)
        {
            dontSave = false;
            var refs = new WorkshopRefs();
            refs.root = new GameObject("03 · 过筛操作区");
            refs.root.transform.SetParent(parent, false);
            refs.station = BuildStation(refs.root.transform, refs);
            return refs;
        }
        // 空间尺寸（米）
        public const float RX = 3.30f;
        public const float RZ = 2.80f;
        public const float WALL_H = 2.64f;
        public const float EAVE = 2.55f;
        public const float RIDGE = 3.42f;

        // 侧窗（-X 墙）：窗台高 0.95，上沿到檐口
        const float WIN_Z0 = -1.65f, WIN_Z1 = 1.65f;
        const float WIN_Y0 = 0.95f;
        const float SILL_H = 0.95f;
        const float TRANSOM_Y = 1.78f;

        // 太阳方向：从左前上方射入（-X, +Z 侧是光来处）
        static readonly Vector3 SunTravel = new Vector3(0.62f, -0.70f, -0.35f).normalized;

        // 关键锚点
        static readonly Vector3 StationPos = new Vector3(-2.10f, 0f, -0.40f);
        static readonly Vector3 TroughPos = new Vector3(-2.55f, 0f, 1.55f);
        static readonly Vector3 StonePilePos = new Vector3(2.25f, 0f, -1.50f);

        static bool dontSave;

        // ============================================================
        public static WorkshopRefs Build(bool markDontSave = false)
        {
            dontSave = markDontSave;
            var refs = new WorkshopRefs();

            var root = new GameObject("Workshop");
            refs.root = root;
            var t = root.transform;

            ApplyRenderSettings();

            var shell = Group(t, "Shell");
            BuildFloor(shell);
            BuildWalls(shell);
            BuildFrame(shell);
            BuildGate(shell);

            refs.roof = Group(t, "Roof");
            BuildRoof(refs.roof);

            var stationGo = new GameObject("Station_GuoShai");
            stationGo.transform.SetParent(t, false);
            stationGo.transform.localPosition = StationPos;
            refs.station = BuildStation(stationGo.transform, refs);

            var wet = Group(t, "WetZone");
            BuildTrough(wet);
            BuildStonePile(wet);

            BuildCourtyard(t);

            var props = Group(t, "Props");
            BuildProps(props);
            BuildWallProps(props);

            BuildDecals(t);

            var lightRoot = Group(t, "Lighting");
            BuildLighting(lightRoot, refs);

            var playerRoot = Group(t, "Player");
            BuildPlayer(playerRoot, refs);

            refs.shots = BuildShotCameras(t);

            if (markDontSave) MarkDontSave(root);
            return refs;
        }

        // ============================================================ 基础设施

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos)
        {
            return Part(parent, name, mesh, mat, pos, Vector3.zero, Vector3.one);
        }

        static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 euler)
        {
            return Part(parent, name, mesh, mat, pos, euler, Vector3.one);
        }

        static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        static GameObject Box(Transform parent, string name, Vector3 size, Material mat, Vector3 pos, Vector3 euler)
        {
            return Box(parent, name, size, mat, pos, euler, 0.006f, 2, 0.55f);
        }

        static GameObject Box(Transform parent, string name, Vector3 size, Material mat, Vector3 pos, Vector3 euler, float radius, int seg, float texScale)
        {
            var mesh = MeshKit.RoundedBox(size, radius, seg, texScale, 0);
            return Part(parent, name, mesh, mat, pos, euler, Vector3.one);
        }

        static GameObject Timber(Transform parent, string name, Vector3 size, Material mat, Vector3 pos, Vector3 euler, int seed)
        {
            var mesh = MeshKit.Timber(size, 0.009f, 2, 0.006f, 0.05f, 0.55f, seed);
            return Part(parent, name, mesh, mat, pos, euler, Vector3.one);
        }

        static GameObject Collider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            return go;
        }

        static void MarkDontSave(GameObject root)
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                tr.gameObject.hideFlags = HideFlags.DontSave;
                var mf = tr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) mf.sharedMesh.hideFlags = HideFlags.DontSave;
            }
        }

        static void ApplyRenderSettings()
        {
            // 天光偏冷、地面反弹偏暖；整体压暗，让侧窗那一束成为画面主导
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.28f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.17f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.11f, 0.095f, 0.08f);
            RenderSettings.ambientIntensity = 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.013f;
            RenderSettings.fogColor = new Color(0.50f, 0.49f, 0.46f);

            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = ProcTex.EnvCubemap();
            RenderSettings.reflectionIntensity = 0.55f;

            var sky = new Material(Shader.Find("Porcelain/Sky"));
            sky.SetColor("_Top", new Color(0.72f, 0.76f, 0.80f));
            sky.SetColor("_Bottom", new Color(0.52f, 0.51f, 0.47f));
            sky.SetColor("_SunColor", new Color(1.0f, 0.95f, 0.86f));
            sky.SetVector("_SunDir", -SunTravel);
            sky.SetFloat("_SunSize", 5f);
            RenderSettings.skybox = sky;

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 26f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.antiAliasing = 4;
            QualitySettings.pixelLightCount = 8;
        }

        // ============================================================ 地面 / 墙 / 木构

        static void BuildFloor(Transform parent)
        {
            const float cell = 0.625f;
            var rnd = new System.Random(7);
            int nx = Mathf.RoundToInt(7.4f / cell);
            int nz = Mathf.RoundToInt(6.4f / cell);
            float x0 = -3.65f, z0 = -3.05f;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float x = x0 + (i + 0.5f) * cell;
                    float z = z0 + (j + 0.5f) * cell;
                    float dy = ((float)rnd.NextDouble() - 0.5f) * 0.005f;
                    // 每块石板随机转 90°/180°/270°：石板近似方形，转过来看不出来，
                    // 但能让世界坐标 UV 错开，避免“相邻石板纹理相位一致”造成的棋盘格。
                    float rot = ((i * 7 + j * 13) % 4) * 90f + ((float)rnd.NextDouble() - 0.5f) * 1.2f;
                    var go = Box(parent, "Slab", new Vector3(cell - 0.006f, 0.10f, cell - 0.006f),
                        Mats.FloorVariant(i * 3 + j), new Vector3(x, -0.048f + dy, z), new Vector3(0f, rot, 0f),
                        0.008f, 1, 0.55f);
                    go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            Collider(parent, "FloorCollider", new Vector3(0f, -0.055f, 0.05f), new Vector3(7.6f, 0.11f, 6.6f));
        }

        static void BuildWalls(Transform parent)
        {
            var plaster = Mats.Plaster;
            var stone = Mats.Stone;

            // 后墙（-Z，挂工具与筛子的主背景面）
            Box(parent, "Wall_Back", new Vector3(2f * RX + 0.44f, WALL_H, 0.22f), plaster,
                new Vector3(0f, WALL_H * 0.5f, -RZ - 0.11f), Vector3.zero, 0.02f, 2, 1.7f);

            // 右墙（+X，屏幕左侧：背光面，留给储藏与暗部）
            Box(parent, "Wall_Right", new Vector3(0.22f, WALL_H, 2f * RZ + 0.44f), plaster,
                new Vector3(RX + 0.11f, WALL_H * 0.5f, 0f), Vector3.zero, 0.02f, 2, 1.7f);

            // 左墙（-X，屏幕右侧）：下部窗台墙 + 大侧窗洞 + 两端实墙
            float endW = WIN_Z0 - (-RZ - 0.22f);
            Box(parent, "Wall_Left_A", new Vector3(0.22f, WALL_H, endW), plaster,
                new Vector3(-RX - 0.11f, WALL_H * 0.5f, (WIN_Z0 + (-RZ - 0.22f)) * 0.5f), Vector3.zero, 0.02f, 2, 1.7f);
            Box(parent, "Wall_Left_B", new Vector3(0.22f, WALL_H, (RZ + 0.22f - WIN_Z1)), plaster,
                new Vector3(-RX - 0.11f, WALL_H * 0.5f, (RZ + 0.22f + WIN_Z1) * 0.5f), Vector3.zero, 0.02f, 2, 1.7f);
            Box(parent, "Wall_Left_Sill", new Vector3(0.22f, SILL_H, WIN_Z1 - WIN_Z0), plaster,
                new Vector3(-RX - 0.11f, SILL_H * 0.5f, 0f), Vector3.zero, 0.02f, 2, 1.7f);
            Box(parent, "Wall_Left_Head", new Vector3(0.22f, WALL_H - EAVE, WIN_Z1 - WIN_Z0), plaster,
                new Vector3(-RX - 0.11f, (WALL_H + EAVE) * 0.5f, 0f), Vector3.zero, 0.02f, 2, 1.7f);

            // 窗台木压条与一根中竖棂（窗棂越少，落在地上的光越整块，不会被切成一格一格）
            Box(parent, "Win_Sill", new Vector3(0.40f, 0.07f, WIN_Z1 - WIN_Z0 + 0.10f), Mats.PlankWood,
                new Vector3(-RX + 0.10f, SILL_H + 0.02f, 0f), Vector3.zero);
            Box(parent, "Win_Mullion", new Vector3(0.08f, EAVE - SILL_H, 0.06f), Mats.PlankWood,
                new Vector3(-RX + 0.02f, (SILL_H + EAVE) * 0.5f, WIN_Z0 * 0.5f - 0.30f), Vector3.zero);

            // 墙脚石带：湿脚、水线
            Box(parent, "Skirting_Back", new Vector3(2f * RX + 0.4f, 0.30f, 0.05f), stone,
                new Vector3(0f, 0.15f, -RZ + 0.04f), Vector3.zero, 0.008f, 1, 0.9f);
            Box(parent, "Skirting_Right", new Vector3(0.05f, 0.30f, 2f * RZ + 0.4f), stone,
                new Vector3(RX - 0.04f, 0.15f, 0f), Vector3.zero, 0.008f, 1, 0.9f);

            Collider(parent, "WallColliderBack", new Vector3(0f, 1.3f, -RZ - 0.1f), new Vector3(7.2f, 2.7f, 0.3f));
            Collider(parent, "WallColliderRight", new Vector3(RX + 0.1f, 1.3f, 0f), new Vector3(0.3f, 2.7f, 6.2f));
            Collider(parent, "WallColliderLeftA", new Vector3(-RX - 0.1f, 1.3f, (WIN_Z0 + (-RZ - 0.22f)) * 0.5f), new Vector3(0.3f, 2.7f, endW));
            Collider(parent, "WallColliderLeftB", new Vector3(-RX - 0.1f, 1.3f, (RZ + 0.22f + WIN_Z1) * 0.5f), new Vector3(0.3f, 2.7f, (RZ + 0.22f - WIN_Z1)));
            Collider(parent, "SillCollider", new Vector3(-RX - 0.1f, SILL_H * 0.5f, 0f), new Vector3(0.3f, SILL_H, WIN_Z1 - WIN_Z0));
        }

        static void BuildFrame(Transform parent)
        {
            var beam = Mats.BeamWood;
            var postA = new Vector3(0.17f, EAVE, 0.17f);

            Vector2[] posts =
            {
                new Vector2(-RX + 0.12f, -RZ + 0.12f), new Vector2(RX - 0.12f, -RZ + 0.12f),
                new Vector2(-RX + 0.12f, RZ - 0.12f), new Vector2(RX - 0.12f, RZ - 0.12f),
                new Vector2(0f, RZ - 0.12f), new Vector2(0f, -RZ + 0.12f),
                new Vector2(-RX + 0.12f, 0f), new Vector2(RX - 0.12f, 0f),
            };
            for (int i = 0; i < posts.Length; i++)
            {
                Box(parent, "Post", postA, beam,
                    new Vector3(posts[i].x, EAVE * 0.5f, posts[i].y), Vector3.zero, 0.01f, 2, 0.55f);
            }

            Timber(parent, "Tie_Front", new Vector3(2f * RX - 0.1f, 0.20f, 0.16f), beam,
                new Vector3(0f, EAVE - 0.10f, RZ - 0.12f), Vector3.zero, 3);
            Timber(parent, "Tie_Back", new Vector3(2f * RX - 0.1f, 0.20f, 0.16f), beam,
                new Vector3(0f, EAVE - 0.10f, -RZ + 0.12f), Vector3.zero, 5);
            Timber(parent, "Plate_Left", new Vector3(0.16f, 0.20f, 2f * RZ - 0.1f), beam,
                new Vector3(-RX + 0.12f, EAVE - 0.10f, 0f), Vector3.zero, 7);
            Timber(parent, "Plate_Right", new Vector3(0.16f, 0.20f, 2f * RZ - 0.1f), beam,
                new Vector3(RX - 0.12f, EAVE - 0.10f, 0f), Vector3.zero, 9);
            Timber(parent, "Ridge", new Vector3(2f * RX + 0.5f, 0.17f, 0.15f), beam,
                new Vector3(0f, RIDGE - 0.12f, 0f), Vector3.zero, 11);

            // 柱头斜撑：贴着柱子内侧、顶到枋底（之前放在半空，远看像一块漂浮的板）
            for (int i = 0; i < 4; i++)
            {
                bool left = (i % 2 == 0);
                float x = left ? -RX + 0.37f : RX - 0.37f;
                float z = (i < 2) ? RZ - 0.12f : -RZ + 0.12f;
                Box(parent, "Brace", new Vector3(0.56f, 0.075f, 0.095f), beam,
                    new Vector3(x, EAVE - 0.30f, z), new Vector3(0f, 0f, left ? 45f : -45f), 0.008f, 1, 0.55f);
            }
        }

        // ============================================================ 棚口的半高栅门

        /// <summary>
        /// 正面棚口装一道半高木栅门：拦得住人，挡不住光。
        /// 之前玩家能一直走到地面板边缘外面，然后掉进虚空 —— 现在有门槛 + 栅门 + 一道整面碰撞。
        /// </summary>
        static void BuildGate(Transform parent)
        {
            var gate = Group(parent, "Gate");
            var beam = Mats.BeamWood;
            var wood = Mats.PlankWood;
            var iron = Mats.Iron;
            float zc = RZ - 0.06f;       // 贴着前檐
            float gateH = 1.42f;
            float sillH = 0.14f;

            // 门槛（脚下那道横木）
            Timber(gate, "Threshold", new Vector3(2f * RX - 0.1f, sillH, 0.14f), beam,
                new Vector3(0f, sillH * 0.5f, zc), Vector3.zero, 61);

            // 两跨栅门（中柱两侧各一扇）
            float[] bayX = { -1.60f, 1.60f };
            float bayW = 2.96f;
            for (int b = 0; b < 2; b++)
            {
                var leaf = new GameObject("GateLeaf" + b);
                leaf.transform.SetParent(gate, false);
                leaf.transform.localPosition = new Vector3(bayX[b], 0f, zc);

                float hw = bayW * 0.5f;

                // 上下横档
                Timber(leaf.transform, "RailBot", new Vector3(bayW, 0.085f, 0.075f), wood,
                    new Vector3(0f, sillH + 0.06f, 0f), Vector3.zero, 63 + b);
                Timber(leaf.transform, "RailTop", new Vector3(bayW, 0.075f, 0.070f), wood,
                    new Vector3(0f, gateH - 0.04f, 0f), Vector3.zero, 65 + b);
                // 边梃
                for (int s = 0; s < 2; s++)
                {
                    Box(leaf.transform, "Stile", new Vector3(0.075f, gateH - sillH, 0.075f), wood,
                        new Vector3((s == 0 ? -hw : hw) + (s == 0 ? 0.038f : -0.038f), (gateH + sillH) * 0.5f, 0f),
                        Vector3.zero, 0.006f, 2, 0.5f);
                }

                // 竖条：细一点、密一点，光能从缝里进来
                int slats = Mathf.RoundToInt(bayW / 0.105f);
                for (int i = 1; i < slats; i++)
                {
                    float x = -hw + bayW * i / slats;
                    if (Mathf.Abs(x) > hw - 0.09f) continue;
                    Box(leaf.transform, "Slat", new Vector3(0.030f, gateH - sillH - 0.10f, 0.028f), wood,
                        new Vector3(x, (gateH + sillH) * 0.5f - 0.01f, 0.004f),
                        new Vector3(0f, 0f, 0.6f), 0.004f, 1, 0.45f);
                }

                // 铁件：合页 + 拉环（说明这是能开的门）
                for (int h2 = 0; h2 < 2; h2++)
                {
                    Part(leaf.transform, "Hinge", MeshKit.Torus(0.035f, 0.008f, 16, 6, 0.2f), iron,
                        new Vector3(-hw + 0.10f, h2 == 0 ? 0.34f : 1.16f, 0.05f),
                        new Vector3(90f, 0f, 0f), new Vector3(1f, 1f, 0.6f));
                }
                Part(leaf.transform, "Latch", MeshKit.Torus(0.055f, 0.007f, 20, 6, 0.2f), iron,
                    new Vector3(hw - 0.16f, 0.95f, 0.055f), new Vector3(90f, 0f, 0f), new Vector3(1f, 1f, 0.5f));
            }

            // 整面碰撞：栅门以上也拦住，防止走到地面板外面掉下去
            Collider(gate, "GateBlocker", new Vector3(0f, 1.3f, zc + 0.02f), new Vector3(2f * RX + 0.2f, 2.6f, 0.24f));
            // 门槛外侧再加一道矮挡，避免贴边滑出去
            Collider(gate, "GateBlockerLow", new Vector3(0f, 0.35f, zc + 0.18f), new Vector3(2f * RX + 0.2f, 0.7f, 0.20f));
        }

        static void BuildRoof(Transform parent)        {
            var beam = Mats.BeamWood;
            float slope = Mathf.Atan2(RIDGE - EAVE, RZ) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(RZ * RZ + (RIDGE - EAVE) * (RIDGE - EAVE));

            for (float x = -3.18f; x <= 3.19f; x += 0.53f)
            {
                for (int s = 0; s < 2; s++)
                {
                    float zc = (RZ * 0.5f) * (s == 0 ? 1f : -1f);
                    float rotX = slope * (s == 0 ? 1f : -1f);
                    Box(parent, "Rafter", new Vector3(0.065f, 0.085f, len - 0.06f), beam,
                        new Vector3(x, (EAVE + RIDGE) * 0.5f - 0.045f, zc), new Vector3(rotX, 0f, 0f), 0.008f, 1, 0.55f);
                }
            }

            for (int s = 0; s < 2; s++)
            {
                float zc = (RZ * 0.5f) * (s == 0 ? 1f : -1f);
                Box(parent, "Purlin", new Vector3(2f * RX + 0.4f, 0.10f, 0.10f), beam,
                    new Vector3(0f, (EAVE + RIDGE) * 0.5f - 0.12f, zc), Vector3.zero, 0.008f, 1, 0.55f);
            }

            for (int s = 0; s < 2; s++)
            {
                float zc = (RZ * 0.5f + 0.16f) * (s == 0 ? 1f : -1f);
                float rotX = slope * (s == 0 ? 1f : -1f);
                Box(parent, "RoofBoard", new Vector3(2f * RX + 0.6f, 0.05f, len + 0.25f), Mats.PlankWood,
                    new Vector3(0f, (EAVE + RIDGE) * 0.5f + 0.06f, zc), new Vector3(rotX, 0f, 0f), 0.01f, 1, 1.1f);
            }

            Box(parent, "Fascia_Front", new Vector3(2f * RX + 0.6f, 0.12f, 0.05f), beam,
                new Vector3(0f, EAVE - 0.02f, RZ + 0.22f), Vector3.zero, 0.008f, 1, 0.55f);
        }

        // ============================================================ 过筛工作站

        static SievingStation BuildStation(Transform parent, WorkshopRefs refs)
        {
            var st = parent.gameObject.AddComponent<SievingStation>();
            var wood = Mats.ToolWood;
            var dark = Mats.DarkToolWood;
            var joint = Mats.Flat(Color.Lerp(Palette.JiuMuHe, Palette.ShenHui, 0.45f), 0.07f);   // 榫头/压边：纯色深木，避免小面积贴图落在浅色纹理上

            // 筛架：两组 A 形脚
            float legLean = Mathf.Atan2(0.24f, 1.02f) * Mathf.Rad2Deg;
            for (int sx = 0; sx < 2; sx++)
            {
                float x = sx == 0 ? -0.46f : 0.46f;
                for (int sz = 0; sz < 2; sz++)
                {
                    float zc = sz == 0 ? 0.13f : -0.13f;
                    float rot = sz == 0 ? -legLean : legLean;
                    Box(parent, "Leg", new Vector3(0.075f, 0.99f, 0.075f), dark,
                        new Vector3(x, 0.495f, zc), new Vector3(rot, 0f, 0f), 0.006f, 2, 0.5f);
                }
                Timber(parent, "LegTie", new Vector3(0.05f, 0.05f, 0.52f), dark,
                    new Vector3(x, 0.58f, 0f), Vector3.zero, 13 + sx);
            }
            Timber(parent, "RailBack", new Vector3(1.08f, 0.075f, 0.09f), wood, new Vector3(0f, 1.015f, -0.27f), Vector3.zero, 17);
            Timber(parent, "RailFront", new Vector3(1.08f, 0.075f, 0.09f), wood, new Vector3(0f, 1.00f, 0.27f), Vector3.zero, 19);

            // 后侧垫木：筛框前缘坐在横枋上，后缘靠这块垫木顶住（7° 倾斜要有人托着）
            Timber(parent, "SieveShim", new Vector3(0.86f, 0.078f, 0.075f), dark,
                new Vector3(0f, 1.088f, -0.25f), Vector3.zero, 37);

            // 楔子：横枋与 A 形脚之间的木楔（结构上说得通，也打破“光板”感）
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.46f : 0.46f;
                float z = (i < 2) ? 0.20f : -0.20f;
                Box(parent, "Wedge", new Vector3(0.085f, 0.048f, 0.055f), dark,
                    new Vector3(x, 0.958f, z), new Vector3(0f, 0f, (i % 2 == 0) ? 8f : -8f), 0.004f, 1, 0.4f);
            }
            // 下层拉条：筛架下半截不再空着
            Timber(parent, "LowerTie", new Vector3(0.92f, 0.05f, 0.06f), dark,
                new Vector3(0f, 0.34f, 0f), Vector3.zero, 29);
            Collider(parent, "StationCollider", new Vector3(0f, 0.55f, 0f), new Vector3(1.06f, 1.1f, 0.82f));

            // 筛框总成（摇筛时整体抖动）
            var sieve = new GameObject("SieveRoot");
            sieve.transform.SetParent(parent, false);
            sieve.transform.localPosition = new Vector3(0f, 1.108f, -0.01f);
            sieve.transform.localEulerAngles = new Vector3(7f, 0f, 0f);
            st.sieveRoot = sieve.transform;

            float w = 1.00f, d = 0.76f, railT = 0.044f, railW = 0.062f;
            Box(sieve.transform, "Frame_F", new Vector3(w, railT, railW), wood,
                new Vector3(0f, 0f, d * 0.5f - railW * 0.5f), Vector3.zero, 0.006f, 2, 0.5f);
            Box(sieve.transform, "Frame_B", new Vector3(w, railT, railW), wood,
                new Vector3(0f, 0f, -d * 0.5f + railW * 0.5f), Vector3.zero, 0.006f, 2, 0.5f);
            Box(sieve.transform, "Frame_L", new Vector3(railW, railT, d - 2f * railW), wood,
                new Vector3(-w * 0.5f + railW * 0.5f, 0f, 0f), Vector3.zero, 0.006f, 2, 0.5f);
            Box(sieve.transform, "Frame_R", new Vector3(railW, railT, d - 2f * railW), wood,
                new Vector3(w * 0.5f - railW * 0.5f, 0f, 0f), Vector3.zero, 0.006f, 2, 0.5f);

            // 四角榫头：让筛框看起来是“做出来的”，不是四根棍子拼的
            for (int i = 0; i < 4; i++)
            {
                float cx = (i % 2 == 0) ? -w * 0.5f + railW * 0.5f : w * 0.5f - railW * 0.5f;
                float cz = (i < 2) ? -d * 0.5f + railW * 0.5f : d * 0.5f - railW * 0.5f;
                Box(sieve.transform, "Corner", new Vector3(railW + 0.026f, railT + 0.010f, railW + 0.026f), joint,
                    new Vector3(cx, 0.003f, cz), Vector3.zero, 0.006f, 2, 0.5f);
            }

            // 压边木条：把筛布边缘压进框里。
            // 宽度按“贴到框内壁”来定，这样布边整段藏在压条底下，不会从缝里露出来；
            // 压条底面比布面高 1mm（不能共面，否则会打架）。
            float iw = w - 2f * railW, id = d - 2f * railW;
            float bW = 0.046f;
            float bx = w * 0.5f - railW - bW * 0.5f;
            float bz = d * 0.5f - railW - bW * 0.5f;
            Box(sieve.transform, "Batten_F", new Vector3(iw, 0.015f, bW), joint,
                new Vector3(0f, -0.006f, bz), Vector3.zero, 0.004f, 1, 0.4f);
            Box(sieve.transform, "Batten_B", new Vector3(iw, 0.015f, bW), joint,
                new Vector3(0f, -0.006f, -bz), Vector3.zero, 0.004f, 1, 0.4f);
            Box(sieve.transform, "Batten_L", new Vector3(bW, 0.015f, id - 2f * bW + 0.004f), joint,
                new Vector3(-bx, -0.006f, 0f), Vector3.zero, 0.004f, 1, 0.4f);
            Box(sieve.transform, "Batten_R", new Vector3(bW, 0.015f, id - 2f * bW + 0.004f), joint,
                new Vector3(bx, -0.006f, 0f), Vector3.zero, 0.004f, 1, 0.4f);

            // 筛布：只比框口大 4mm，布边刚好塞进框内壁下面
            var clothMesh = MeshKit.ClothRect(w - 2f * railW + 0.004f, d - 2f * railW + 0.004f, 22, 16, 0.011f, 24f, 0.0035f, 31);
            var clothGo = Part(sieve.transform, "SieveCloth", clothMesh, Mats.SieveCloth,
                new Vector3(0f, -0.015f, 0f), Vector3.zero, Vector3.one);
            var clothMr = clothGo.GetComponent<MeshRenderer>();
            clothMr.shadowCastingMode = ShadowCastingMode.Off;
            st.clothRenderer = clothMr;
            st.clothDry = Mats.SieveCloth;

            var wet = new Material(Mats.SieveCloth);
            wet.name = "sieve_cloth_wet";
            wet.color = new Color(0.60f, 0.58f, 0.53f);
            wet.SetFloat("_Glossiness", 0.45f);
            st.clothWet = wet;
            st.clothRenderer.sharedMaterial = st.clothDry;

            // 筛面薄浆层
            var filmMesh = MeshKit.ClothRect(w - 2f * railW - 0.075f, d - 2f * railW - 0.075f, 14, 10, 0.006f, 2f, 0.0015f, 37);
            var filmMat = new Material(Mats.SlurryFilm);
            filmMat.name = "slurry_film_instance";
            var film = Part(sieve.transform, "SlurryFilm", filmMesh, filmMat,
                new Vector3(0f, -0.009f, 0f), Vector3.zero, Vector3.one);
            film.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            st.film = film.transform;
            st.filmMat = filmMat;

            // 筛上粗渣：薄薄一层，随筛净度堆积
            var residueMesh = MeshKit.Rock(41, new Vector3(0.26f, 0.010f, 0.19f), 0.62f, 5, true, 1f);
            var residue = Part(sieve.transform, "Residue", residueMesh, Mats.Residue,
                new Vector3(-0.05f, -0.012f, 0.03f), Vector3.zero, Vector3.one);
            st.residue = residue.transform;

            // 接浆盆（筛下）
            var basinParent = new GameObject("Basin");
            basinParent.transform.SetParent(parent, false);
            basinParent.transform.localPosition = new Vector3(0f, 0f, -0.02f);

            Part(basinParent.transform, "BasinBody", MeshKit.Lathe(BasinProfile(), 48, 0.55f),
                Mats.GlazedPot, Vector3.zero, Vector3.zero, Vector3.one);

            var fill = Part(basinParent.transform, "BasinFill",
                MeshKit.Rod(0.245f, 0.245f, 1f, 32, 0f, 2, 0.5f), Mats.Slurry,
                new Vector3(0f, 0.030f, 0f), Vector3.zero, new Vector3(1f, 0.001f, 1f));
            fill.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            st.basinFill = fill.transform;

            var surf = Part(basinParent.transform, "BasinSurface",
                MeshKit.Disc(0.245f, 40, 3, 0.0012f, 0.5f, 3), Mats.Filtrate,
                new Vector3(0f, 0.032f, 0f));
            surf.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            st.basinSurface = surf.transform;
            st.basinRipple = MakeRipple(surf.transform, "BasinRipple", 0.22f, new Vector3(0f, 0.002f, 0f), 0.14f, 0.86f);

            // 木瓢
            var rest = new GameObject("DipperRest");
            rest.transform.SetParent(parent, false);
            rest.transform.localPosition = new Vector3(1.24f, 0.60f, -0.66f);
            rest.transform.localEulerAngles = new Vector3(-24f, -14f, 0f);
            st.dipperRest = rest.transform;

            var pour = new GameObject("DipperPour");
            pour.transform.SetParent(parent, false);
            pour.transform.localPosition = new Vector3(0.02f, 1.32f, 0.02f);
            pour.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            st.dipperPour = pour.transform;

            var dipper = new GameObject("Dipper");
            dipper.transform.SetParent(parent, false);
            dipper.transform.localPosition = rest.transform.localPosition;
            dipper.transform.localRotation = rest.transform.localRotation;
            st.dipper = dipper.transform;

            // 瓢：椭圆浅碗（有口沿）+ 收敛的短木柄，柄在 -X 侧，浆从 +X 侧倒出去
            Part(dipper.transform, "Scoop", MeshKit.Lathe(DipperBowlProfile(), 36, 0.26f),
                Mats.ToolWood, Vector3.zero, Vector3.zero, new Vector3(1.22f, 1f, 0.96f));

            // 瓢里那点浆：舀起来时出现，倒完消失
            var scoopl = Part(dipper.transform, "ScoopLiquid",
                MeshKit.Disc(0.058f, 28, 3, 0.0008f, 0.2f, 7), Mats.Slurry,
                new Vector3(0f, 0.034f, 0f), Vector3.zero, new Vector3(1.20f, 1f, 0.94f));
            scoopl.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            st.scoopLiquid = scoopl.transform;
            scoopl.SetActive(false);

            // 柄：从口沿外侧斜向上伸，末端略收
            Part(dipper.transform, "DipperHandle",
                MeshKit.Rod(0.017f, 0.012f, 0.24f, 10, 0.010f, 5, 0.3f), Mats.ToolWood,
                new Vector3(-0.100f, 0.030f, 0f), new Vector3(0f, 0f, 74f));
            Part(dipper.transform, "DipperKnob",
                MeshKit.Rod(0.016f, 0.010f, 0.028f, 10, 0f, 3, 0.3f), Mats.DarkToolWood,
                new Vector3(-0.330f, 0.098f, 0f), new Vector3(0f, 0f, 74f));

            // 倒浆口：浆流从这里挂下来（跟着瓢的姿态走）
            var lip = new GameObject("PourLip");
            lip.transform.SetParent(dipper.transform, false);
            lip.transform.localPosition = new Vector3(0.088f, 0.052f, 0f);
            st.pourLip = lip.transform;

            // 浆流（长度每帧按瓢口到筛面的距离拉）
            var stream = new GameObject("PourStream");
            stream.transform.SetParent(parent, false);
            stream.transform.localPosition = new Vector3(0.06f, 1.035f, 0.12f);
            var streamGo = Part(stream.transform, "Stream", MeshKit.Rod(0.026f, 0.044f, 0.27f, 16, 0.008f, 4, 0.2f),
                Mats.Stream, Vector3.zero);
            var streamMr = streamGo.GetComponent<MeshRenderer>();
            streamMr.shadowCastingMode = ShadowCastingMode.Off;
            streamMr.receiveShadows = false;
            st.pourStream = stream;
            stream.SetActive(false);

            // 筛下滴落 / 飞溅
            st.drips = MakeParticles(parent, "Drips", new Vector3(0f, 0.97f, -0.02f),
                new Vector3(0.50f, 0.02f, 0.36f), 0.017f, 0.8f, new Color(0.82f, 0.80f, 0.74f, 1f), 1.7f, 240);
            st.splashes = MakeParticles(parent, "Splash", new Vector3(0f, 0.24f, -0.02f),
                new Vector3(0.30f, 0.02f, 0.30f), 0.010f, 0.5f, new Color(0.84f, 0.82f, 0.76f, 1f), 2.4f, 60);

            // 料缸（+X 侧，画面左），木台 + 湿木桶 + 铁箍
            var tubRoot = new GameObject("SlurryTub");
            tubRoot.transform.SetParent(parent, false);
            tubRoot.transform.localPosition = new Vector3(1.28f, 0f, -0.72f);
            var tub = tubRoot.AddComponent<SlurryTub>();

            Box(tubRoot.transform, "TubStand", new Vector3(0.46f, 0.30f, 0.46f), Mats.DarkToolWood,
                new Vector3(0f, 0.15f, 0f), Vector3.zero, 0.008f, 2, 0.5f);

            var tubParent = new GameObject("Barrel");
            tubParent.transform.SetParent(tubRoot.transform, false);
            tubParent.transform.localPosition = new Vector3(0f, 0.30f, 0f);

            var tubProfile = new List<Vector2>
            {
                new Vector2(0f, 0.02f), new Vector2(0.20f, 0.0f), new Vector2(0.235f, 0.03f),
                new Vector2(0.245f, 0.20f), new Vector2(0.235f, 0.38f), new Vector2(0.243f, 0.40f),
                new Vector2(0.225f, 0.40f), new Vector2(0.216f, 0.38f), new Vector2(0.226f, 0.20f),
                new Vector2(0.216f, 0.05f), new Vector2(0.19f, 0.035f), new Vector2(0f, 0.045f),
            };
            Part(tubParent.transform, "BarrelBody", MeshKit.Lathe(tubProfile, 40, 0.45f), Mats.DarkToolWood,
                Vector3.zero, Vector3.zero, Vector3.one);

            // 拼板：16 块竖向木板贴在外圈，从“一个圆柱”变成“箍桶”
            int staves = 16;
            for (int i = 0; i < staves; i++)
            {
                float ang = (i + 0.5f) / staves * 360f;
                float rad = ang * Mathf.Deg2Rad;
                float rr = 0.238f;
                var staveMesh = MeshKit.RoundedBox(new Vector3(0.086f, 0.372f, 0.020f), 0.004f, 2, 0.45f, 1);
                var sc = new Color(0.9f + (i % 3) * 0.045f, 0.88f + (i % 4) * 0.03f, 0.85f, 1f);
                Part(tubParent.transform, "Stave", staveMesh, Mats.DarkToolWood,
                    new Vector3(Mathf.Cos(rad) * rr, 0.196f, Mathf.Sin(rad) * rr),
                    new Vector3(0f, 90f - ang, 0f), Vector3.one);
            }

            Part(tubParent.transform, "HoopA", MeshKit.Torus(0.244f, 0.007f, 36, 7, 0.2f), Mats.Iron,
                new Vector3(0f, 0.315f, 0f));
            Part(tubParent.transform, "HoopB", MeshKit.Torus(0.238f, 0.007f, 36, 7, 0.2f), Mats.Iron,
                new Vector3(0f, 0.085f, 0f));

            var lvl = Part(tubParent.transform, "SlurryLevel",
                MeshKit.Disc(0.216f, 44, 6, 0.0018f, 0.4f, 5), Mats.Slurry,
                new Vector3(0f, 0.335f, 0f));
            lvl.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            tub.level = lvl.transform;
            st.tubLevel = lvl.transform;
            tub.ripple = MakeRipple(lvl.transform, "TubRipple", 0.19f, new Vector3(0f, 0.002f, 0f), 0.12f, 0.72f);
            tub.splash = MakeParticles(tubRoot.transform, "TubSplash",
                new Vector3(0f, 0.63f, 0f), new Vector3(0.26f, 0.015f, 0.26f),
                0.012f, 0.5f, new Color(0.86f, 0.84f, 0.78f, 1f), 2.8f, 50);
            tub.station = st;

            var cap = tubRoot.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.5f, 0f);
            cap.radius = 0.26f;
            cap.height = 1.0f;
            cap.direction = 1;

            st.tub = tub;
            refs.tub = tub;

            // 台面上的小工具
            Timber(parent, "Scraper", new Vector3(0.24f, 0.012f, 0.10f), Mats.ToolWood,
                new Vector3(0.30f, 1.075f, -0.26f), new Vector3(-4f, 18f, 0f), 47);
            Box(parent, "ScraperHandle", new Vector3(0.10f, 0.028f, 0.032f), Mats.ToolWood,
                new Vector3(0.44f, 1.085f, -0.28f), new Vector3(0f, 18f, 0f), 0.008f, 1, 0.4f);
            Part(parent, "Brush", MeshKit.Rod(0.010f, 0.007f, 0.16f, 8, 0.004f, 4, 0.3f), Mats.ToolWood,
                new Vector3(-0.30f, 1.06f, -0.24f), new Vector3(90f, 0f, 8f));

            return st;
        }

        /// <summary>木瓢的碗：椭圆浅碗 + 3mm 口沿</summary>
        static List<Vector2> DipperBowlProfile()
        {
            return new List<Vector2>
            {
                new Vector2(0f, 0.008f),
                new Vector2(0.042f, 0.004f),
                new Vector2(0.070f, 0.012f),
                new Vector2(0.084f, 0.030f),
                new Vector2(0.090f, 0.058f),   // 口沿外侧
                new Vector2(0.083f, 0.060f),   // 口沿顶面
                new Vector2(0.078f, 0.032f),   // 内壁
                new Vector2(0.052f, 0.018f),
                new Vector2(0f, 0.024f),       // 内底
            };
        }

        /// <summary>接浆盆：带圈足与口沿的陶盆（比简单车削体更像真家伙）</summary>
        static List<Vector2> BasinProfile()
        {
            return new List<Vector2>
            {
                new Vector2(0f, 0.055f),      // 外底中心
                new Vector2(0.105f, 0.028f),
                new Vector2(0.135f, 0.0f),    // 圈足落地
                new Vector2(0.155f, 0.014f),  // 收进碗身
                new Vector2(0.245f, 0.058f),
                new Vector2(0.286f, 0.115f),
                new Vector2(0.292f, 0.198f),  // 外壁到口沿
                new Vector2(0.281f, 0.203f),  // 口沿顶面
                new Vector2(0.272f, 0.135f),  // 内壁
                new Vector2(0.238f, 0.075f),
                new Vector2(0.115f, 0.052f),
                new Vector2(0f, 0.070f),      // 内底
            };
        }

        static List<Vector2> BowlProfile(float rOut, float h, float thick)
        {
            return new List<Vector2>
            {
                new Vector2(0f, thick),
                new Vector2(rOut * 0.45f, 0.004f),
                new Vector2(rOut * 0.88f, 0.016f),
                new Vector2(rOut, 0.045f),
                new Vector2(rOut, h),
                new Vector2(rOut - thick, h),
                new Vector2(rOut - thick, 0.062f),
                new Vector2(0f, thick + thick),
            };
        }

        /// <summary>水面涟漪：一张带柔和环形的透明贴片，挂在液面下面跟着液面走</summary>
        static RippleRing MakeRipple(Transform parent, string name, float radius, Vector3 pos, float startScale, float endScale)
        {
            // 贴片按 1m 见方做，环带直径约为 0.7×scale，所以 scale 直接用世界尺寸
            var mesh = MeshKit.Plane(1f, 1f, 1, 1, 1f);
            var go = Part(parent, name, mesh, Mats.Ripple, pos);
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var rr = go.AddComponent<RippleRing>();
            rr.startScale = startScale;
            rr.endScale = endScale;
            rr.duration = 1.2f;
            // 立刻关掉：Awake 在编辑器里不一定跑，留着就是一块白板盖在液面上
            go.SetActive(false);
            return rr;
        }

        static ParticleSystem MakeParticles(Transform parent, string name, Vector3 pos, Vector3 box,
            float size, float lifetime, Color color, float gravity, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.enabled = false;
            em.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.55f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Porcelain/Particle"));
            mat.SetTexture("_MainTex", ProcTex.SoftDot());
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        // ============================================================ 棚外（透过侧窗能看到）

        /// <summary>
        /// 窗外的小院：一段院墙 + 天井地面 + 几块石料。
        /// 窗洞外如果只有天光，会变成一块发白的空洞；有了院子，明暗过渡才成立。
        /// </summary>
        static void BuildCourtyard(Transform parent)
        {
            var g = Group(parent, "Courtyard");
            var stone = Mats.Stone;

            // 天井地面
            var yard = Part(g, "YardFloor", MeshKit.Plane(6.0f, 9.0f, 1, 1, 4f), Mats.FloorStone,
                new Vector3(-6.0f, -0.02f, 0f));
            yard.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // 院墙（留出一个门洞，透出更远的亮）
            Box(g, "YardWall", new Vector3(0.24f, 3.05f, 9.0f), Mats.Plaster,
                new Vector3(-7.10f, 1.52f, 0f), Vector3.zero, 0.02f, 2, 1.7f);
            Box(g, "YardDoor", new Vector3(0.10f, 1.95f, 1.10f), Mats.Flat(Palette.ShenHui, 0.02f),
                new Vector3(-7.22f, 0.97f, 3.10f), Vector3.zero, 0.02f, 1, 1f);
            Box(g, "YardEave", new Vector3(0.5f, 0.12f, 9.2f), Mats.BeamWood,
                new Vector3(-7.05f, 3.08f, 0f), Vector3.zero, 0.01f, 1, 0.55f);

            // 院里的石料堆（与棚内的瓷石呼应，暗示原料从哪里来）
            var rnd = new System.Random(71);
            var meshes = new Mesh[4];
            for (int i = 0; i < meshes.Length; i++)
                meshes[i] = MeshKit.Rock(500 + i * 9, new Vector3(1f, 0.8f, 0.95f), 0.32f, 3, true, 0.55f);
            for (int i = 0; i < 9; i++)
            {
                float x = -6.35f + (float)rnd.NextDouble() * 0.9f;
                float z = -1.6f + (float)rnd.NextDouble() * 3.4f;
                float s = 0.12f + (float)rnd.NextDouble() * 0.14f;
                Part(g, "YardStone", meshes[rnd.Next(meshes.Length)], Mats.Rock,
                    new Vector3(x, s * 0.45f, z),
                    new Vector3((float)rnd.NextDouble() * 30f, (float)rnd.NextDouble() * 360f, 0f),
                    new Vector3(s, s, s));
            }

            // 靠在院墙上的竹竿与木桶（给窗外一点层次）
            var pole = new GameObject("YardPole");
            pole.transform.SetParent(g, false);
            pole.transform.localPosition = new Vector3(-6.75f, 0f, -2.30f);
            pole.transform.localEulerAngles = new Vector3(4f, 0f, 7f);
            Part(pole.transform, "Pole", MeshKit.Rod(0.02f, 0.017f, 2.6f, 9, 0.02f, 8, 0.45f), Mats.Bamboo, Vector3.zero);

            // 院子的默认天光很弱（棚里几乎没有 GI），单独用第 8 层给它补“室外漫射光”，
            // 否则窗户会变成一个发暗的黑洞。
            foreach (var tr in g.GetComponentsInChildren<Transform>(true))
                tr.gameObject.layer = CourtyardLayer;
        }

        public const int CourtyardLayer = 8;

        // ============================================================ 湿区

        static void BuildTrough(Transform parent)
        {
            var g = new GameObject("StoneTrough");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = TroughPos;
            var t = g.transform;
            float w = 1.12f, d = 0.74f, h = 0.42f, wall = 0.11f;

            Box(t, "TroughFloor", new Vector3(w, 0.09f, d), Mats.StoneWet, new Vector3(0f, 0.045f, 0f), Vector3.zero, 0.012f, 1, 0.9f);
            Box(t, "TroughWallF", new Vector3(w, h, wall), Mats.Stone, new Vector3(0f, h * 0.5f, d * 0.5f - wall * 0.5f), Vector3.zero, 0.008f, 2, 0.9f);
            Box(t, "TroughWallB", new Vector3(w, h, wall), Mats.Stone, new Vector3(0f, h * 0.5f, -d * 0.5f + wall * 0.5f), Vector3.zero, 0.008f, 2, 0.9f);
            Box(t, "TroughWallL", new Vector3(wall, h, d - 2f * wall), Mats.Stone, new Vector3(-w * 0.5f + wall * 0.5f, h * 0.5f, 0f), Vector3.zero, 0.008f, 2, 0.9f);
            Box(t, "TroughWallR", new Vector3(wall, h, d - 2f * wall), Mats.Stone, new Vector3(w * 0.5f - wall * 0.5f, h * 0.5f, 0f), Vector3.zero, 0.008f, 2, 0.9f);

            var water = Part(t, "Water", MeshKit.Plane(w - 2f * wall, d - 2f * wall, 3, 3, 1.2f), Mats.Water,
                new Vector3(0f, 0.31f, 0f));
            water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var rnd = new System.Random(19);
            for (int i = 0; i < 6; i++)
            {
                float x = (float)(rnd.NextDouble() - 0.5) * 0.7f;
                float z = (float)(rnd.NextDouble() - 0.5) * 0.42f;
                float s = 0.07f + (float)rnd.NextDouble() * 0.06f;
                Part(t, "WetStone", MeshKit.Rock(100 + i, new Vector3(s, s * 0.72f, s * 0.9f), 0.30f, 3, true, 0.5f),
                    Mats.StoneWet, new Vector3(x, 0.22f + s * 0.30f, z),
                    new Vector3(0f, (float)rnd.NextDouble() * 360f, 0f));
            }

            Collider(t, "TroughCollider", new Vector3(0f, 0.22f, 0f), new Vector3(w + 0.04f, 0.44f, d + 0.04f));
        }

        static void BuildStonePile(Transform parent)
        {
            var g = new GameObject("StonePile");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = StonePilePos;
            var t = g.transform;
            var rnd = new System.Random(23);

            var meshes = new Mesh[6];
            for (int i = 0; i < meshes.Length; i++)
                meshes[i] = MeshKit.Rock(200 + i * 7, new Vector3(1f, 0.82f, 0.92f), 0.34f, 3, true, 0.55f);

            for (int i = 0; i < 22; i++)
            {
                int m = rnd.Next(meshes.Length);
                float r = (float)rnd.NextDouble();
                float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float rad = Mathf.Sqrt(r) * 0.62f;
                float s = 0.10f + (float)rnd.NextDouble() * 0.16f;
                float y = s * 0.45f + Mathf.Max(0f, 0.30f - rad) * 0.5f;
                if (i > 14) y += 0.12f;
                Part(t, "Stone", meshes[m], Mats.Rock,
                    new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad * 0.8f),
                    new Vector3((float)rnd.NextDouble() * 40f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 40f),
                    new Vector3(s, s, s));
            }
            Collider(t, "PileCollider", new Vector3(0f, 0.25f, 0f), new Vector3(1.3f, 0.5f, 1.1f));

            var shovel = new GameObject("Shovel");
            shovel.transform.SetParent(parent, false);
            shovel.transform.localPosition = new Vector3(StonePilePos.x - 0.25f, 0.06f, StonePilePos.z - 0.85f);
            shovel.transform.localEulerAngles = new Vector3(0f, 18f, -16f);
            Part(shovel.transform, "ShovelHandle", MeshKit.Rod(0.016f, 0.018f, 1.02f, 10, 0.01f, 5, 0.4f), Mats.ToolWood, Vector3.zero);
            Box(shovel.transform, "ShovelBlade", new Vector3(0.17f, 0.02f, 0.24f), Mats.DarkToolWood,
                new Vector3(0f, 0.02f, 0.10f), new Vector3(0f, 0f, 4f), 0.008f, 1, 0.5f);

            // 石臼 + 杵
            var mortar = new GameObject("Mortar");
            mortar.transform.SetParent(parent, false);
            mortar.transform.localPosition = new Vector3(1.30f, 0f, -1.80f);
            Box(mortar.transform, "MortarBody", new Vector3(0.46f, 0.40f, 0.46f), Mats.Stone,
                new Vector3(0f, 0.20f, 0f), new Vector3(0f, 6f, 0f), 0.03f, 2, 0.8f);
            var hollow = new List<Vector2>
            {
                new Vector2(0f, 0.20f), new Vector2(0.13f, 0.22f), new Vector2(0.155f, 0.30f),
                new Vector2(0.16f, 0.405f), new Vector2(0.145f, 0.405f), new Vector2(0.14f, 0.30f),
                new Vector2(0.115f, 0.245f), new Vector2(0f, 0.235f),
            };
            Part(mortar.transform, "MortarHollow", MeshKit.Lathe(hollow, 28, 0.5f), Mats.StoneWet,
                Vector3.zero, Vector3.zero, Vector3.one);
            Collider(mortar.transform, "MortarCollider", new Vector3(0f, 0.2f, 0f), new Vector3(0.48f, 0.4f, 0.48f));

            var pestle = new GameObject("Pestle");
            pestle.transform.SetParent(parent, false);
            pestle.transform.localPosition = new Vector3(1.62f, 0f, -1.62f);
            pestle.transform.localEulerAngles = new Vector3(0f, 0f, 22f);
            Part(pestle.transform, "PestleShaft", MeshKit.Rod(0.028f, 0.034f, 0.72f, 10, 0.012f, 5, 0.3f), Mats.ToolWood, Vector3.zero);
            Part(pestle.transform, "PestleHead", MeshKit.Rod(0.055f, 0.045f, 0.16f, 12, 0f, 3, 0.3f), Mats.Stone,
                new Vector3(0f, 0.70f, 0f));
        }

        // ============================================================ 陈设

        static void BuildProps(Transform parent)
        {
            var rnd = new System.Random(31);

            // 矮凳（长时间站桩的痕迹）
            var stool = new GameObject("Stool");
            stool.transform.SetParent(parent, false);
            stool.transform.localPosition = new Vector3(-0.92f, 0f, 1.18f);
            stool.transform.localEulerAngles = new Vector3(0f, -12f, 0f);
            Box(stool.transform, "StoolTop", new Vector3(0.36f, 0.05f, 0.26f), Mats.ToolWood,
                new Vector3(0f, 0.28f, 0f), Vector3.zero, 0.008f, 2, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.14f : 0.14f;
                float z = (i < 2) ? -0.09f : 0.09f;
                Box(stool.transform, "StoolLeg", new Vector3(0.035f, 0.28f, 0.035f), Mats.DarkToolWood,
                    new Vector3(x, 0.14f, z), new Vector3(0f, 0f, (i % 2 == 0) ? 3f : -3f), 0.006f, 1, 0.4f);
            }

            // 已筛好的细浆桶（材料去处）
            var bucket = new GameObject("FiltrateBucket");
            bucket.transform.SetParent(parent, false);
            bucket.transform.localPosition = new Vector3(-0.28f, 0f, -1.62f);
            bucket.transform.localEulerAngles = new Vector3(0f, 24f, 0f);
            var bp = new List<Vector2>
            {
                new Vector2(0f, 0.02f), new Vector2(0.17f, 0.0f), new Vector2(0.20f, 0.03f),
                new Vector2(0.208f, 0.15f), new Vector2(0.20f, 0.28f), new Vector2(0.206f, 0.30f),
                new Vector2(0.19f, 0.30f), new Vector2(0.184f, 0.28f), new Vector2(0.192f, 0.15f),
                new Vector2(0.184f, 0.05f), new Vector2(0f, 0.04f),
            };
            Part(bucket.transform, "BucketBody", MeshKit.Lathe(bp, 30, 0.4f), Mats.WetWood,
                Vector3.zero, Vector3.zero, Vector3.one);
            Part(bucket.transform, "BucketHoop", MeshKit.Torus(0.205f, 0.005f, 28, 6, 0.2f), Mats.Iron,
                new Vector3(0f, 0.24f, 0f));
            var bl = Part(bucket.transform, "FiltrateLevel", MeshKit.Disc(0.184f, 30, 3, 0.001f, 0.4f, 9),
                Mats.Filtrate, new Vector3(0f, 0.245f, 0f));
            bl.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var bail = Part(bucket.transform, "Bail", MeshKit.Torus(0.19f, 0.007f, 24, 6, 0.2f), Mats.Rope,
                new Vector3(0f, 0.30f, 0f), new Vector3(90f, 0f, 0f), new Vector3(1f, 1f, 0.55f));
            bail.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Collider(bucket.transform, "BucketCol", new Vector3(0f, 0.15f, 0f), new Vector3(0.44f, 0.32f, 0.44f));

            // 粗渣盆
            var coarse = new GameObject("CoarseTub");
            coarse.transform.SetParent(parent, false);
            coarse.transform.localPosition = new Vector3(-1.72f, 0f, -1.28f);
            coarse.transform.localEulerAngles = new Vector3(0f, -8f, 0f);
            var cp = new List<Vector2>
            {
                new Vector2(0f, 0.02f), new Vector2(0.19f, 0.0f), new Vector2(0.225f, 0.03f),
                new Vector2(0.232f, 0.13f), new Vector2(0.225f, 0.24f), new Vector2(0.232f, 0.26f),
                new Vector2(0.212f, 0.26f), new Vector2(0.205f, 0.24f), new Vector2(0.212f, 0.13f),
                new Vector2(0.205f, 0.05f), new Vector2(0f, 0.04f),
            };
            Part(coarse.transform, "CoarseBody", MeshKit.Lathe(cp, 28, 0.4f), Mats.WetWood,
                Vector3.zero, Vector3.zero, Vector3.one);
            Part(coarse.transform, "CoarseFill", MeshKit.Disc(0.205f, 28, 3, 0.002f, 0.35f, 11),
                Mats.Residue, new Vector3(0f, 0.19f, 0f));
            Part(coarse.transform, "CoarseMound", MeshKit.Rock(57, new Vector3(0.16f, 0.045f, 0.15f), 0.4f, 3, false, 1f),
                Mats.Residue, new Vector3(0f, 0.19f, 0f));

            // 墙角陶缸（背光处，压住画面左边）
            for (int i = 0; i < 2; i++)
            {
                var jar = new GameObject("Jar");
                jar.transform.SetParent(parent, false);
                jar.transform.localPosition = new Vector3(2.42f + i * 0.02f, 0f, -2.22f + i * 0.58f);
                float r = i == 0 ? 0.26f : 0.22f;
                float h = i == 0 ? 0.52f : 0.44f;
                var jp = new List<Vector2>
                {
                    new Vector2(0f, 0.02f), new Vector2(r * 0.72f, 0f), new Vector2(r, 0.06f),
                    new Vector2(r * 1.02f, h * 0.45f), new Vector2(r * 0.86f, h * 0.90f),
                    new Vector2(r * 0.90f, h), new Vector2(r * 0.80f, h),
                    new Vector2(r * 0.78f, h * 0.90f), new Vector2(r * 0.94f, h * 0.45f),
                    new Vector2(r * 0.90f, 0.07f), new Vector2(r * 0.66f, 0.035f), new Vector2(0f, 0.045f),
                };
                Part(jar.transform, "JarBody", MeshKit.Lathe(jp, 30, 0.5f), Mats.GlazedPot,
                    Vector3.zero, Vector3.zero, Vector3.one);
                Part(jar.transform, "JarLid", MeshKit.Lathe(new List<Vector2>
                {
                    new Vector2(0f, 0f), new Vector2(r * 0.74f, 0.01f), new Vector2(r * 0.80f, 0.03f),
                    new Vector2(r * 0.78f, 0.045f), new Vector2(r * 0.60f, 0.055f), new Vector2(0f, 0.06f),
                }, 26, 0.5f), Mats.GlazedPot, new Vector3(0f, h - 0.01f, 0f));
                Collider(jar.transform, "JarCol", new Vector3(0f, 0.2f, 0f), new Vector3(r * 2f, 0.4f, r * 2f));
            }

            // 竹竿
            var pole = new GameObject("BambooPole");
            pole.transform.SetParent(parent, false);
            pole.transform.localPosition = new Vector3(-3.02f, 0f, -2.10f);
            pole.transform.localEulerAngles = new Vector3(6f, 0f, 9f);
            Part(pole.transform, "PoleShaft", MeshKit.Rod(0.022f, 0.018f, 2.10f, 10, 0.02f, 8, 0.45f), Mats.Bamboo, Vector3.zero);

            // 地上碎渣
            for (int i = 0; i < 8; i++)
            {
                float x = -2.9f + (float)rnd.NextDouble() * 5.6f;
                float z = -2.5f + (float)rnd.NextDouble() * 4.6f;
                if (Mathf.Abs(x - StationPos.x) < 0.65f && Mathf.Abs(z - StationPos.z) < 0.65f) continue;
                float s = 0.02f + (float)rnd.NextDouble() * 0.035f;
                Part(parent, "Chip", MeshKit.Rock(300 + i, new Vector3(s, s * 0.4f, s * 0.8f), 0.4f, 2, true, 0.3f),
                    Mats.Rock, new Vector3(x, s * 0.2f, z), new Vector3(0f, (float)rnd.NextDouble() * 360f, 0f));
            }
        }

        static void BuildWallProps(Transform parent)
        {
            // 挂在后墙的圆筛（待用工具）
            Vector3[] spots =
            {
                new Vector3(-0.15f, 1.92f, -RZ + 0.14f),
                new Vector3(0.42f, 1.96f, -RZ + 0.14f),
                new Vector3(0.96f, 1.88f, -RZ + 0.14f),
            };
            float[] radii = { 0.24f, 0.20f, 0.27f };
            for (int i = 0; i < spots.Length; i++)
            {
                var g = new GameObject("HangingSieve");
                g.transform.SetParent(parent, false);
                g.transform.localPosition = spots[i];
                g.transform.localEulerAngles = new Vector3(90f, 0f, (i - 1) * 4f);
                Part(g.transform, "Ring", MeshKit.SieveRound(radii[i], 0.045f, 0.022f, 36), Mats.Bamboo, Vector3.zero);
                var mesh = Part(g.transform, "Mesh", MeshKit.Disc(radii[i] - 0.022f, 32, 2, 0f, 0.2f, 1),
                    Mats.SieveClothLight, new Vector3(0f, 0.02f, 0f));
                mesh.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            // 后墙木架 + 小罐
            var shelf = new GameObject("Shelf");
            shelf.transform.SetParent(parent, false);
            shelf.transform.localPosition = new Vector3(1.72f, 1.24f, -RZ + 0.16f);
            Timber(shelf.transform, "ShelfBoard", new Vector3(1.10f, 0.045f, 0.26f), Mats.PlankWood,
                Vector3.zero, Vector3.zero, 53);
            for (int i = 0; i < 2; i++)
            {
                Box(shelf.transform, "Bracket", new Vector3(0.05f, 0.20f, 0.20f), Mats.DarkToolWood,
                    new Vector3(i == 0 ? -0.44f : 0.44f, -0.11f, -0.02f), Vector3.zero, 0.008f, 1, 0.4f);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = -0.34f + i * 0.34f;
                float r = 0.075f + i * 0.012f;
                var jp = new List<Vector2>
                {
                    new Vector2(0f, 0.01f), new Vector2(r * 0.7f, 0f), new Vector2(r, 0.03f),
                    new Vector2(r * 0.95f, 0.16f), new Vector2(r * 0.66f, 0.22f), new Vector2(r * 0.70f, 0.23f),
                    new Vector2(r * 0.60f, 0.23f), new Vector2(r * 0.56f, 0.16f), new Vector2(r * 0.86f, 0.03f),
                    new Vector2(r * 0.60f, 0.02f), new Vector2(0f, 0.028f),
                };
                Part(shelf.transform, "SmallJar", MeshKit.Lathe(jp, 22, 0.3f), Mats.GlazedPot,
                    new Vector3(x, 0.022f, 0f), new Vector3(0f, i * 37f, 0f));
            }

            // 挂在墙上的竹勺与刷子（让人手可及的地方有工具）
            var scoop = new GameObject("WallScoop");
            scoop.transform.SetParent(parent, false);
            scoop.transform.localPosition = new Vector3(-0.72f, 1.66f, -RZ + 0.24f);
            scoop.transform.localEulerAngles = new Vector3(102f, 0f, -18f);
            Part(scoop.transform, "Scoop", MeshKit.Lathe(BowlProfile(0.075f, 0.06f, 0.010f), 24, 0.3f),
                Mats.Bamboo, Vector3.zero, Vector3.zero, new Vector3(1.6f, 1f, 1f));
            Timber(scoop.transform, "ScoopHandle", new Vector3(0.26f, 0.02f, 0.028f), Mats.Bamboo,
                new Vector3(0.12f, 0.055f, 0f), new Vector3(0f, 0f, 10f), 59);



            // 青花蓝工序木牌（小面积点缀）


            // 麻绳圈
            Part(parent, "RopeCoil", MeshKit.Torus(0.13f, 0.016f, 22, 7, 0.3f), Mats.Rope,
                new Vector3(-0.55f, 2.05f, -1.95f), new Vector3(78f, 0f, 12f), Vector3.one);
            Part(parent, "RopeCoil2", MeshKit.Torus(0.115f, 0.016f, 22, 7, 0.3f), Mats.Rope,
                new Vector3(-0.53f, 1.99f, -1.94f), new Vector3(84f, 0f, 16f), Vector3.one);
        }

        static void BuildDecals(Transform parent)
        {
            var decals = Group(parent, "Decals");
            var stain = Mats.Stain;

            Part(decals, "WaterStain", MeshKit.Plane(1.6f, 1.2f, 2, 2, 1f), stain,
                new Vector3(-2.45f, 0.003f, 1.55f), new Vector3(0f, 8f, 0f));
            Part(decals, "WaterStain2", MeshKit.Plane(1.0f, 0.8f, 2, 2, 1f), stain,
                new Vector3(-2.10f, 0.003f, 0.72f), new Vector3(0f, -22f, 0f));
            Part(decals, "MudStain", MeshKit.Plane(0.95f, 0.8f, 2, 2, 1f), stain,
                new Vector3(-2.10f, 0.003f, -0.42f), new Vector3(0f, 6f, 0f));
            Part(decals, "MudStain2", MeshKit.Plane(0.6f, 0.5f, 2, 2, 1f), stain,
                new Vector3(-1.30f, 0.003f, -0.10f), new Vector3(0f, -14f, 0f));
            Part(decals, "SlurryStain", MeshKit.Plane(0.7f, 0.55f, 2, 2, 1f), stain,
                new Vector3(-0.80f, 0.003f, -0.95f), new Vector3(0f, 24f, 0f));
            for (int i = 0; i < 4; i++)
            {
                Part(decals, "Footprint", MeshKit.Plane(0.10f, 0.20f, 1, 1, 1f), stain,
                    new Vector3(-1.05f - i * 0.22f, 0.003f, 1.55f - i * 0.34f),
                    new Vector3(0f, -28f + i * 9f, 0f));
            }
        }

        // ============================================================ 光照 / 空气

        static void BuildLighting(Transform parent, WorkshopRefs refs)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.localRotation = Quaternion.LookRotation(SunTravel, Vector3.up);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.945f, 0.875f);
            sun.intensity = 1.90f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.90f;
            sun.shadowBias = 0.025f;
            sun.shadowNormalBias = 0.16f;
            sun.shadowResolution = LightShadowResolution.VeryHigh;
            refs.sun = sun;

            // 反弹光：不投影，只负责抬亮暗部（规范：阴影中仍能识别工具）
            AddFill(parent, "BounceFront", new Vector3(0.15f, 1.70f, 3.45f), new Color(0.86f, 0.90f, 0.96f), 0.95f, 7.5f);
            AddFill(parent, "BounceWindow", new Vector3(-2.95f, 1.95f, 0.10f), new Color(1.0f, 0.97f, 0.90f), 1.10f, 5.5f);
            AddFill(parent, "BounceRoof", new Vector3(0.30f, 2.20f, -1.20f), new Color(0.78f, 0.79f, 0.82f), 0.38f, 5f);
            AddFill(parent, "BounceFloor", new Vector3(0.90f, 0.45f, 0.60f), new Color(0.72f, 0.70f, 0.66f), 0.24f, 3.6f);

            // 棚外的天光：只照第 8 层（院子）。院子若只有环境光会黑成一块洞，
            // 这里给一盏“从棚子这边打回去的反弹光”照亮院墙，再加一盏垂直天光铺地面。
            var yardGo = new GameObject("YardBounce");
            yardGo.transform.SetParent(parent, false);
            yardGo.transform.localRotation = Quaternion.LookRotation(new Vector3(-0.80f, -0.55f, -0.22f).normalized, Vector3.up);
            var yardBounce = yardGo.AddComponent<Light>();
            yardBounce.type = LightType.Directional;
            yardBounce.color = new Color(0.98f, 0.94f, 0.88f);
            yardBounce.intensity = 3.4f;
            yardBounce.shadows = LightShadows.None;
            yardBounce.cullingMask = 1 << CourtyardLayer;

            var yardSkyGo = new GameObject("YardSky");
            yardSkyGo.transform.SetParent(parent, false);
            yardSkyGo.transform.localRotation = Quaternion.LookRotation(new Vector3(0.10f, -0.96f, -0.16f).normalized, Vector3.up);
            var yardSky = yardSkyGo.AddComponent<Light>();
            yardSky.type = LightType.Directional;
            yardSky.color = new Color(0.80f, 0.85f, 0.94f);
            yardSky.intensity = 2.0f;
            yardSky.shadows = LightShadows.None;
            yardSky.cullingMask = 1 << CourtyardLayer;

            // 操作区极弱补光：让接触点看清，不破坏侧光造型
            var work = new GameObject("WorkFill");
            work.transform.SetParent(parent, false);
            work.transform.localPosition = StationPos + new Vector3(0.10f, 2.28f, 0.20f);
            work.transform.localRotation = Quaternion.Euler(84f, 12f, 0f);
            var wl = work.AddComponent<Light>();
            wl.type = LightType.Spot;
            wl.color = new Color(1.0f, 0.95f, 0.88f);
            wl.intensity = 0.50f;
            wl.range = 3.6f;
            wl.spotAngle = 58f;
            wl.shadows = LightShadows.None;

            // 窗口的柔和光晕：比起用几何体做“光柱”，贴片光晕不会在地面和墙面上切出硬边，
            // 更符合“光束只允许极少量点缀”的要求。
            var glowMat = new Material(Shader.Find("Porcelain/Particle"));
            glowMat.SetTexture("_MainTex", ProcTex.SoftDot(128));
            glowMat.SetColor("_Color", new Color(1f, 0.96f, 0.88f, 0.10f));
            var glow = new GameObject("WindowGlow");
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = new Vector3(-RX + 0.03f, (SILL_H + EAVE) * 0.5f, 0f);
            glow.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var glowMesh = MeshKit.Plane((WIN_Z1 - WIN_Z0) * 1.05f, (EAVE - SILL_H) * 1.3f, 1, 1, 1f);
            var glowGo = Part(glow.transform, "Glow", glowMesh, glowMat, Vector3.zero);
            var glowMr = glowGo.GetComponent<MeshRenderer>();
            glowMr.shadowCastingMode = ShadowCastingMode.Off;
            glowMr.receiveShadows = false;
            glowMr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // 空气里的微尘：只在光带里明显
            refs.dustBeam = MakeDust(parent, "DustBeam", new Vector3(-1.55f, 1.30f, 0.10f),
                new Vector3(1.9f, 1.5f, 2.4f), 0.018f, 14f, 0.34f, 90, Quaternion.Euler(-44f, -62f, 0f));
            refs.dustRoom = MakeDust(parent, "DustRoom", new Vector3(0.10f, 1.45f, -0.20f),
                new Vector3(5.2f, 2.2f, 4.6f), 0.012f, 16f, 0.10f, 110, Quaternion.identity);
        }

        static void AddFill(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
        }

        static ParticleSystem MakeDust(Transform parent, string name, Vector3 pos, Vector3 box,
            float size, float life, float alpha, int max, Quaternion rot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new Color(1f, 0.98f, 0.94f, alpha);
            main.gravityModifier = -0.0015f;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = max / life * 0.9f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.03f;
            noise.frequency = 0.12f;
            noise.scrollSpeed = 0.02f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var size2 = ps.sizeOverLifetime;
            size2.enabled = true;
            size2.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Porcelain/Particle"));
            mat.SetTexture("_MainTex", ProcTex.SoftDot());
            renderer.material = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        // ============================================================ 玩家 / 相机

        static void BuildPlayer(Transform parent, WorkshopRefs refs)
        {
            var go = new GameObject("PlayerRig");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(-1.42f, 0.02f, 1.02f);
            go.transform.localEulerAngles = new Vector3(0f, -157f, 0f);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.74f;
            cc.radius = 0.27f;
            cc.center = new Vector3(0f, 0.87f, 0f);
            cc.skinWidth = 0.02f;
            cc.stepOffset = 0.25f;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.60f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 160f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.clearFlags = CameraClearFlags.Skybox;

            var post = camGo.AddComponent<PostFX>();
            post.shader = Shader.Find("Porcelain/PostFX");
            post.bloomShader = Shader.Find("Porcelain/Bloom");
            post.exposure = 0.93f;
            post.saturation = 0.92f;
            post.contrast = 1.07f;
            post.lift = 0.020f;
            post.vignette = 0.34f;
            post.grain = 0.008f;
            post.sharpen = 0.18f;
            post.bloomStrength = 0.11f;
            post.bloomThreshold = 0.88f;

            var fpc = go.AddComponent<FirstPersonController>();
            fpc.cam = cam;
            fpc.startPitch = 14f;   // 正值＝俯视（Unity 相机绕 X 正转是低头）

            var io = go.AddComponent<PlayerInteractor>();
            io.cam = cam;
            io.head = camGo.transform;
            io.Register(refs.station);
            io.Register(refs.tub);

            refs.player = go;
            refs.camera = cam;
            refs.fpc = fpc;
            refs.interactor = io;

            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(parent.parent, false);
            var hud = hudGo.AddComponent<HUD>();
            hud.station = refs.station;
            hud.interactor = io;
            hud.controller = fpc;
            refs.hud = hud;
        }

        static Camera[] BuildShotCameras(Transform parent)
        {
            var shots = new List<Camera>();
            var group = Group(parent, "Shots");

            shots.Add(MakeShot(group, "Cam_Overview", new Vector3(0.30f, 1.50f, 1.95f), new Vector3(-2.00f, 1.02f, -0.45f), 52f));
            shots.Add(MakeShot(group, "Cam_Operation", new Vector3(-1.05f, 1.30f, 0.62f), new Vector3(-2.08f, 1.03f, -0.40f), 40f));
            shots.Add(MakeShot(group, "Cam_Structure", new Vector3(0.60f, 5.40f, 5.40f), new Vector3(-1.15f, 0.42f, -0.55f), 46f));
            shots.Add(MakeShot(group, "Cam_Frame", new Vector3(-2.05f, 1.42f, 2.30f), new Vector3(-2.05f, 1.05f, -0.40f), 40f));
            // 近距：检查筛网、粗渣、木头的细节层级（规范：精度优先给近距离接触面）
            shots.Add(MakeShot(group, "Cam_Macro", new Vector3(-1.72f, 1.24f, 0.16f), new Vector3(-2.10f, 1.06f, -0.42f), 32f));
            return shots.ToArray();
        }

        static Camera MakeShot(Transform parent, string name, Vector3 pos, Vector3 look, float fov)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation((look - pos).normalized, Vector3.up);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 200f;
            cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.enabled = false;
            var post = go.AddComponent<PostFX>();
            post.shader = Shader.Find("Porcelain/PostFX");
            post.bloomShader = Shader.Find("Porcelain/Bloom");
            post.exposure = 0.93f;
            post.saturation = 0.92f;
            post.contrast = 1.07f;
            post.lift = 0.020f;
            post.vignette = 0.32f;
            post.grain = 0.006f;
            post.sharpen = 0.18f;
            post.bloomStrength = 0.11f;
            post.bloomThreshold = 0.88f;
            return cam;
        }
    }
}
