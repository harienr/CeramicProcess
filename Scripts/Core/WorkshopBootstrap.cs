using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 场景装配入口：在运行时按美术规范搭建整个"筛洗"工序工坊。
    /// 之所以用程序化装配，是因为当前项目还没有正式美术资源，
    /// 这样能先跑通玩法、验证观感，之后再逐件替换为美术资产。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WorkshopBootstrap : MonoBehaviour
    {
        static bool s_Built;
        public bool integratedOnly;

        public static WorkshopBootstrap BuildIntegrated(Transform parent)
        {
            var host = new GameObject("04 · 筛洗操作区");
            host.SetActive(false);
            host.transform.SetParent(parent, false);
            var builder = host.AddComponent<WorkshopBootstrap>();
            builder.integratedOnly = true;
            builder._root = host.transform;
            builder._style = new StyleMaterials();
            builder._style.Init();
            builder._process = host.AddComponent<SieveWashProcess>();
            builder.BuildWorkArea();
            host.SetActive(true);
            return builder;
        }
        const float MinX = -3.2f;
        const float MaxX = 3.2f;
        const float MinZ = -2.4f;
        const float MaxZ = 3.4f;
        const float Ceil = 3.1f;

        StyleMaterials _style;
        Transform _root;
        SieveWashProcess _process;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (s_Built) return;
            if (FindObjectOfType<WorkshopBootstrap>() != null) return;

            // 只在筛洗场景里兜底装配，避免影响工程里其它场景
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.name.StartsWith("SieveWash")) return;

            var go = new GameObject("瓷从泥生 · 筛洗工坊");
            go.AddComponent<WorkshopBootstrap>();
        }

        void Awake()
        {
            if (integratedOnly) return;
            if (s_Built) return;
            s_Built = true;
            Build();
        }

        void Build()
        {
            _style = new StyleMaterials();
            _style.Init();

            var rootGO = new GameObject("工坊");
            _root = rootGO.transform;

            BuildRoom();
            BuildLighting();

            _process = new GameObject("工序状态").AddComponent<SieveWashProcess>();
            _process.transform.SetParent(_root, false);

            BuildWorkArea();

            BuildPlayer(out var interactor);
            BuildHud(interactor);

            var directorGO = new GameObject("工序调度");
            directorGO.transform.SetParent(_root, false);
            var director = directorGO.AddComponent<WorkshopDirector>();
            director.process = _process;
            director.player = interactor;

            var probe = BuildReflectionProbe();
            probe.RenderProbe();

            Debug.Log("[瓷从泥生] 筛洗场景已就绪：WASD 移动，左键操作，T 静置。");
        }

        // ---------------------------------------------------------------- 房间

        void Panel(string name, Vector3 center, Vector3 normal, float w, float h, Material mat, float uv = 0.5f)
        {
            var mesh = ProcMesh.Plane(w, h, uv);
            var rot = Quaternion.LookRotation(Vector3.up, normal);
            ProcMesh.Place(name, _root, mesh, mat, center, rot);
        }

        void BuildRoom()
        {
            float cx = (MinX + MaxX) * 0.5f;
            float cz = (MinZ + MaxZ) * 0.5f;
            float spanX = MaxX - MinX;
            float spanZ = MaxZ - MinZ;

            var ceilingMat = _style.Standard("ceiling", Palette.Darken(Palette.RiceWhite, 0.28f),
                ProcTex.Plaster(), null, 0.2f, 0.03f, 0f);

            ProcMesh.Place("地面", _root, ProcMesh.Plane(spanX + 0.6f, spanZ + 0.6f, 0.42f, 4, 4),
                _style.Floor, new Vector3(cx, 0f, cz), Quaternion.identity);

            ProcMesh.Place("顶棚", _root, ProcMesh.Plane(spanX + 0.6f, spanZ + 0.6f, 0.28f),
                ceilingMat, new Vector3(cx, Ceil, cz), Quaternion.Euler(180f, 0f, 0f));

            // 后墙 / 右墙
            Panel("后墙", new Vector3(cx, Ceil * 0.5f, MaxZ), Vector3.back, spanX, Ceil, _style.Wall);
            Panel("右墙", new Vector3(MaxX, Ceil * 0.5f, cz), Vector3.left, spanZ, Ceil, _style.Wall);

            // 左墙：留出窗洞 z∈[-0.8,0.8], y∈[1.05,2.25]
            Panel("左墙下", new Vector3(MinX, 0.525f, cz), Vector3.right, spanZ, 1.05f, _style.Wall);
            Panel("左墙上", new Vector3(MinX, 2.675f, cz), Vector3.right, spanZ, 0.85f, _style.Wall);
            Panel("左墙前", new Vector3(MinX, 1.65f, -1.6f), Vector3.right, 1.6f, 1.2f, _style.Wall);
            Panel("左墙后", new Vector3(MinX, 1.65f, 2.1f), Vector3.right, 2.6f, 1.2f, _style.Wall);

            // 前墙：留出门洞 x∈[-0.75,0.75], y∈[0,2.2]
            Panel("前墙左", new Vector3(-1.975f, Ceil * 0.5f, MinZ), Vector3.forward, 2.45f, Ceil, _style.Wall);
            Panel("前墙右", new Vector3(1.975f, Ceil * 0.5f, MinZ), Vector3.forward, 2.45f, Ceil, _style.Wall);
            Panel("前墙楣", new Vector3(0f, 2.65f, MinZ), Vector3.forward, 1.5f, 0.9f, _style.Wall);

            // 门窗外的天光 / 暗部
            var lightPanel = ProcMesh.Place("窗外天光", _root, ProcMesh.Quad(1.62f, 1.22f),
                _style.EmissiveWarm, new Vector3(MinX - 0.06f, 1.65f, 0f), Quaternion.LookRotation(Vector3.right));
            lightPanel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var outside = ProcMesh.Place("门外", _root, ProcMesh.Quad(1.6f, 2.3f),
                _style.Decal(null, Palette.WithAlpha(Palette.Darken(Palette.ClayGrey, 0.62f), 1f)),
                new Vector3(0f, 1.15f, MinZ - 0.08f), Quaternion.LookRotation(Vector3.forward));
            outside.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            BuildWindowFrame();

            // 墙脚水渍
            var grime = _style.Grime;
            ProcMesh.Place("水渍_后", _root, ProcMesh.Quad(spanX, 0.78f), grime,
                new Vector3(cx, 0.39f, MaxZ - 0.012f), Quaternion.LookRotation(Vector3.back));
            ProcMesh.Place("水渍_左", _root, ProcMesh.Quad(spanZ, 0.78f), grime,
                new Vector3(MinX + 0.012f, 0.39f, cz), Quaternion.LookRotation(Vector3.right));
            ProcMesh.Place("水渍_右", _root, ProcMesh.Quad(spanZ, 0.78f), grime,
                new Vector3(MaxX - 0.012f, 0.39f, cz), Quaternion.LookRotation(Vector3.left));
            ProcMesh.Place("水渍_前", _root, ProcMesh.Quad(spanX, 0.78f), grime,
                new Vector3(cx, 0.39f, MinZ + 0.012f), Quaternion.LookRotation(Vector3.forward));

            BuildBeams();
            BuildCollision();
        }

        void BuildWindowFrame()
        {
            var m = _style.Beam;
            ProcMesh.Place("窗台", _root, ProcMesh.Box(new Vector3(0.20f, 0.075f, 1.78f), 0.008f), m,
                new Vector3(MinX + 0.03f, 1.04f, 0f));
            ProcMesh.Place("窗楣", _root, ProcMesh.Box(new Vector3(0.16f, 0.075f, 1.78f), 0.008f), m,
                new Vector3(MinX + 0.02f, 2.26f, 0f));
            ProcMesh.Place("窗框前", _root, ProcMesh.Box(new Vector3(0.14f, 1.26f, 0.075f), 0.008f), m,
                new Vector3(MinX + 0.02f, 1.65f, -0.80f));
            ProcMesh.Place("窗框后", _root, ProcMesh.Box(new Vector3(0.14f, 1.26f, 0.075f), 0.008f), m,
                new Vector3(MinX + 0.02f, 1.65f, 0.80f));
            ProcMesh.Place("窗中梃", _root, ProcMesh.Box(new Vector3(0.09f, 1.20f, 0.045f), 0.006f), m,
                new Vector3(MinX + 0.01f, 1.65f, 0f));
            ProcMesh.Place("窗横档", _root, ProcMesh.Box(new Vector3(0.09f, 0.045f, 1.60f), 0.006f), m,
                new Vector3(MinX + 0.01f, 1.62f, 0f));
        }

        void BuildBeams()
        {
            float z = (MinZ + MaxZ) * 0.5f;
            float spanZ = MaxZ - MinZ;
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Lerp(MinX + 0.75f, MaxX - 0.75f, i / 3f);
                ProcMesh.Place("梁_" + i, _root, ProcMesh.Box(new Vector3(0.16f, 0.19f, spanZ + 0.4f), 0.012f),
                    _style.Beam, new Vector3(x, Ceil - 0.10f, z));
            }
            ProcMesh.Place("枋", _root, ProcMesh.Box(new Vector3(MaxX - MinX, 0.14f, 0.14f), 0.01f),
                _style.Beam, new Vector3(0f, Ceil - 0.26f, MaxZ - 0.10f));
        }

        void BuildCollision()
        {
            float cx = (MinX + MaxX) * 0.5f;
            float cz = (MinZ + MaxZ) * 0.5f;
            float spanX = MaxX - MinX;
            float spanZ = MaxZ - MinZ;

            AddCollider(NewNode("地面碰撞"), new Vector3(spanX + 2f, 0.2f, spanZ + 2f), new Vector3(cx, -0.1f, cz));
            AddCollider(NewNode("后墙碰撞"), new Vector3(spanX + 2f, Ceil, 0.3f), new Vector3(cx, Ceil * 0.5f, MaxZ + 0.15f));
            AddCollider(NewNode("右墙碰撞"), new Vector3(0.3f, Ceil, spanZ + 2f), new Vector3(MaxX + 0.15f, Ceil * 0.5f, cz));
            AddCollider(NewNode("左墙碰撞"), new Vector3(0.3f, Ceil, spanZ + 2f), new Vector3(MinX - 0.15f, Ceil * 0.5f, cz));
            AddCollider(NewNode("前墙碰撞左"), new Vector3(spanX * 0.5f - 0.75f, Ceil, 0.3f), new Vector3(-1.975f, Ceil * 0.5f, MinZ - 0.15f));
            AddCollider(NewNode("前墙碰撞右"), new Vector3(spanX * 0.5f - 0.75f, Ceil, 0.3f), new Vector3(1.975f, Ceil * 0.5f, MinZ - 0.15f));
        }

        GameObject NewNode(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.layer = 2;
            return go;
        }

        static void AddCollider(GameObject go, Vector3 size, Vector3 center)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.center = center;
        }

        // ---------------------------------------------------------------- 光照

        void BuildLighting()
        {
            var sunGO = new GameObject("窗侧天光");
            sunGO.transform.SetParent(_root, false);
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.965f, 0.915f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.80f;
            sun.shadowBias = 0.035f;
            sun.shadowNormalBias = 0.45f;
            sunGO.transform.rotation = Quaternion.Euler(36f, 66f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.545f, 0.545f, 0.520f);
            RenderSettings.ambientEquatorColor = new Color(0.360f, 0.355f, 0.330f);
            RenderSettings.ambientGroundColor = new Color(0.175f, 0.172f, 0.155f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            // 窗口补光：模拟漫反射进光，不投影
            var fillGO = new GameObject("窗口漫射");
            fillGO.transform.SetParent(_root, false);
            fillGO.transform.position = new Vector3(MinX + 0.55f, 1.85f, 0f);
            var fill = fillGO.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.96f, 0.90f);
            fill.range = 5.2f;
            fill.intensity = 2.1f;
            fill.shadows = LightShadows.None;

            // 工作区上方的柔和补光，保证暗部仍能识别工具
            var workGO = new GameObject("工作区补光");
            workGO.transform.SetParent(_root, false);
            workGO.transform.position = new Vector3(0.2f, 2.35f, 1.55f);
            var work = workGO.AddComponent<Light>();
            work.type = LightType.Point;
            work.color = new Color(1f, 0.93f, 0.84f);
            work.range = 3.8f;
            work.intensity = 0.85f;
            work.shadows = LightShadows.None;
        }

        ReflectionProbe BuildReflectionProbe()
        {
            var go = new GameObject("环境反射探针");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(0f, 1.55f, 0.5f);
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 64;
            probe.hdr = false;
            probe.size = new Vector3(7f, 3.2f, 6.2f);
            probe.intensity = 0.55f;
            return probe;
        }

        // ---------------------------------------------------------------- 操作区

        void BuildWorkArea()
        {
            var area = new GameObject("操作区").transform;
            area.SetParent(_root, false);

            // 工作台（靠后墙）
            var bench = PropLibrary.Bench(area, _style, new Vector3(3.6f, 0.85f, 0.80f), "工作台");
            bench.transform.localPosition = new Vector3(0.1f, 0f, 2.86f);
            AddCollider(bench, new Vector3(3.6f, 0.85f, 0.80f), new Vector3(0f, 0.425f, 0f));

            BuildBenchTop(bench.transform);
            BuildSieveStation(area);
            BuildDecor(area);
        }

        void BuildBenchTop(Transform bench)
        {
            const float topY = 0.85f;

            // 粗浆缸
            var rawVat = PropLibrary.WoodenBucket(bench, _style, 0.34f, 0.21f, "粗浆缸");
            rawVat.transform.localPosition = new Vector3(-1.42f, topY, 0f);
            var rawLiquid = ProcMesh.Place("浆面", rawVat.transform, ProcMesh.Disc(0.186f, 36), _style.Slurry,
                new Vector3(0f, 0.29f, 0f));
            rawLiquid.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var rawSrc = rawVat.AddComponent<SourceVat>();
            rawSrc.content = LadleContent.Slurry;
            rawSrc.liquidSurface = rawLiquid.transform;
            rawSrc.surfaceLowY = 0.03f;
            rawSrc.surfaceHighY = 0.29f;
            rawSrc.maxDistance = 2.2f;
            AddCollider(rawVat, new Vector3(0.46f, 0.38f, 0.46f), new Vector3(0f, 0.17f, 0f));

            // 清水缸
            var waterVat = PropLibrary.WoodenBucket(bench, _style, 0.30f, 0.19f, "清水缸");
            waterVat.transform.localPosition = new Vector3(1.34f, topY, 0f);
            var waterLiquid = ProcMesh.Place("水面", waterVat.transform, ProcMesh.Disc(0.168f, 36), _style.Water,
                new Vector3(0f, 0.25f, 0f));
            waterLiquid.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var waterSrc = waterVat.AddComponent<SourceVat>();
            waterSrc.content = LadleContent.Water;
            waterSrc.liquidSurface = waterLiquid.transform;
            waterSrc.surfaceLowY = 0.03f;
            waterSrc.surfaceHighY = 0.25f;
            waterSrc.maxDistance = 2.2f;
            AddCollider(waterVat, new Vector3(0.42f, 0.34f, 0.42f), new Vector3(0f, 0.15f, 0f));

            // 粗渣盘
            var tray = PropLibrary.WoodenTray(bench, _style, 0.34f, 0.26f, 0.055f, "粗渣盘");
            tray.transform.localPosition = new Vector3(0.52f, topY, 0.06f);
            var pile = ProcMesh.Place("渣堆", tray.transform, ProcMesh.Blob(1f, 1f, 18, 4, 0.24f, 7),
                _style.Coarse, new Vector3(0f, 0.02f, 0f));
            var trayComp = tray.AddComponent<ResidueTray>();
            trayComp.pile = pile.transform;
            trayComp.pileRadius = 0.105f;
            trayComp.pileHeightMax = 0.075f;
            trayComp.maxDistance = 2.2f;
            AddCollider(tray, new Vector3(0.36f, 0.08f, 0.28f), new Vector3(0f, 0.03f, 0f));

            // 工具
            var ladle = PropLibrary.Ladle(bench, _style);
            ladle.transform.localPosition = new Vector3(-0.86f, topY + 0.012f, 2.70f - 2.86f);
            ladle.transform.localRotation = Quaternion.Euler(0f, 32f, 0f);
            ConfigureTool(ladle, ToolKind.Ladle, "木瓢",
                new Vector3(-0.02f, -0.03f, 0.13f), new Vector3(10f, -20f, -8f),
                new Vector3(0.30f, 0.10f, 0.16f), new Vector3(0.05f, 0.04f, 0f));

            var scraper = PropLibrary.Scraper(bench, _style);
            scraper.transform.localPosition = new Vector3(0.02f, topY + 0.016f, 2.70f - 2.86f);
            scraper.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);
            ConfigureTool(scraper, ToolKind.Scraper, "竹刮",
                new Vector3(0.03f, -0.02f, 0.12f), new Vector3(6f, -10f, 0f),
                new Vector3(0.24f, 0.05f, 0.09f), new Vector3(0.05f, 0.01f, 0f));

            var decant = PropLibrary.DecantLadle(bench, _style);
            decant.transform.localPosition = new Vector3(1.06f, topY + 0.012f, 2.72f - 2.86f);
            decant.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            ConfigureTool(decant, ToolKind.DecantLadle, "撇水勺",
                new Vector3(-0.02f, -0.03f, 0.13f), new Vector3(8f, -18f, -6f),
                new Vector3(0.34f, 0.10f, 0.18f), new Vector3(0.08f, 0.05f, 0f));
        }

        void ConfigureTool(GameObject go, ToolKind kind, string displayName,
                           Vector3 heldPos, Vector3 heldEuler, Vector3 colSize, Vector3 colCenter)
        {
            var tool = go.AddComponent<ToolItem>();
            tool.kind = kind;
            tool.displayName = displayName;
            tool.heldLocalPosition = heldPos;
            tool.heldLocalEuler = heldEuler;
            tool.maxDistance = 2.4f;
            AddCollider(go, colSize, colCenter);
            tool.SetRest(go.transform.parent);
            // 工具会被拾起，材质需要独立以便悬停提示
            foreach (var r in go.GetComponentsInChildren<Renderer>()) SetHighlightable(r);
        }

        static void SetHighlightable(Renderer r)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                mats[i] = StyleMaterials.MakeHighlightable(mats[i], r.name);
            }
            r.sharedMaterials = mats;
        }

        void BuildSieveStation(Transform area)
        {
            var stand = PropLibrary.SieveStand(area, _style, 1.24f, 0.42f, 0.245f, "筛架");
            stand.transform.localPosition = new Vector3(0.35f, 0f, 1.28f);

            var sieve = PropLibrary.Sieve(stand.transform, _style, 0.235f, out var cloth, "竹筛");
            sieve.transform.localPosition = new Vector3(0f, 1.266f, 0f);

            var slurryBlob = ProcMesh.Place("筛面浆", sieve.transform,
                ProcMesh.Blob(1f, 1f, 30, 6, 0.16f, 3), _style.Slurry, new Vector3(0f, 0.007f, 0f));
            slurryBlob.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var residueBlob = ProcMesh.Place("筛面渣", sieve.transform,
                ProcMesh.Blob(1f, 1f, 26, 5, 0.30f, 11), _style.Coarse, new Vector3(0f, 0.004f, 0f));
            residueBlob.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var stir = ProcMesh.Place("搅棒", sieve.transform,
                ProcMesh.Cylinder(0.007f, 0.006f, 0.155f, 8), _style.Bamboo, new Vector3(0.09f, 0.05f, 0f),
                Quaternion.Euler(64f, 0f, 0f));
            stir.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var station = sieve.AddComponent<SieveStation>();
            station.clothRenderer = cloth;
            station.slurryBlob = slurryBlob.transform;
            station.residueBlob = residueBlob.transform;
            station.stirStick = stir.transform;
            station.maxDistance = 2.35f;
            AddCollider(sieve, new Vector3(0.50f, 0.16f, 0.50f), new Vector3(0f, 0.06f, 0f));

            // 承接细浆的沉淀缸（位于筛架正下方）
            var vat = PropLibrary.GlazedVat(area, _style, 0.92f, 0.30f, "沉淀缸");
            vat.transform.localPosition = new Vector3(0.35f, 0f, 1.28f);

            var fineSurface = ProcMesh.Place("细浆面", vat.transform, ProcMesh.Disc(0.255f, 40), _style.Slurry,
                new Vector3(0f, 0.15f, 0f));
            fineSurface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var waterSurface = ProcMesh.Place("清水层", vat.transform, ProcMesh.Disc(0.262f, 40), _style.Water,
                new Vector3(0f, 0.15f, 0f));
            waterSurface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var sv = vat.AddComponent<SettlingVat>();
            sv.fineSurface = fineSurface.transform;
            sv.waterSurface = waterSurface.transform;
            sv.fineLowY = 0.10f;
            sv.fineHighY = 0.80f;
            sv.fineFullAmount = 0.72f;
            sv.waterLayerScale = 0.28f;
            sv.maxDistance = 2.3f;
            AddCollider(vat, new Vector3(0.62f, 0.92f, 0.62f), new Vector3(0f, 0.46f, 0f));
        }

        void BuildDecor(Transform area)
        {
            var stool = PropLibrary.Stool(area, _style, 0.44f, "木凳");
            stool.transform.localPosition = new Vector3(-1.95f, 0f, 1.72f);
            stool.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);

            var sack1 = PropLibrary.Sack(area, _style, 0.62f, 0.24f, 5, "原料袋");
            sack1.transform.localPosition = new Vector3(-2.62f, 0f, 2.62f);
            sack1.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            AddCollider(sack1, new Vector3(0.5f, 0.6f, 0.5f), new Vector3(0f, 0.3f, 0f));

            var sack2 = PropLibrary.Sack(area, _style, 0.54f, 0.21f, 9, "原料袋");
            sack2.transform.localPosition = new Vector3(-2.24f, 0f, 2.95f);
            sack2.transform.localRotation = Quaternion.Euler(0f, -38f, 0f);
            AddCollider(sack2, new Vector3(0.44f, 0.52f, 0.44f), new Vector3(0f, 0.26f, 0f));

            var shelf = PropLibrary.WallShelf(area, _style, 1.7f, 0.28f, 1.62f, "木架");
            shelf.transform.localPosition = new Vector3(3.02f, 0f, 1.30f);
            shelf.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var jar1 = PropLibrary.StorageJar(shelf.transform, _style, 0.26f, 0.11f, "陶罐");
            jar1.transform.localPosition = new Vector3(-0.44f, 1.64f, 0f);
            var jar2 = PropLibrary.StorageJar(shelf.transform, _style, 0.22f, 0.09f, "陶罐");
            jar2.transform.localPosition = new Vector3(0.48f, 1.64f, 0f);

            for (int i = 0; i < 2; i++)
            {
                var spare = PropLibrary.Sieve(area, _style, 0.22f, out _, "备用竹筛");
                spare.transform.localPosition = new Vector3(2.56f, 0.03f + i * 0.075f, -0.55f);
                spare.transform.localRotation = Quaternion.Euler(90f, i * 18f, 0f);
            }
        }

        // ---------------------------------------------------------------- 玩家

        void BuildPlayer(out PlayerInteractor interactor)
        {
            var playerGO = new GameObject("玩家");
            playerGO.transform.SetParent(_root, false);
            playerGO.layer = 2;
            playerGO.transform.position = new Vector3(0.1f, 0.06f, -1.15f);

            playerGO.AddComponent<CharacterController>();
            var fpc = playerGO.AddComponent<FirstPersonController>();

            var pivot = new GameObject("视点").transform;
            pivot.SetParent(playerGO.transform, false);
            pivot.localPosition = new Vector3(0f, 1.62f, 0f);

            var camGO = new GameObject("相机");
            camGO.transform.SetParent(pivot, false);
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.30f, 0.295f, 0.275f);
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 45f;
            cam.fieldOfView = 58f;
            cam.allowHDR = false;
            cam.allowMSAA = true;

            var gradeShader = Resources.Load<Shader>("Shaders/StyleGrade");
            if (gradeShader != null)
            {
                var grade = camGO.AddComponent<StyleGradeEffect>();
                grade.gradeMaterial = new Material(gradeShader) { name = "影调统一" };
            }

            fpc.cameraPivot = pivot;

            // 手部
            var rigGO = new GameObject("手部");
            rigGO.transform.SetParent(camGO.transform, false);
            var rig = rigGO.AddComponent<HandsRig>();

            var handRoot = new GameObject("root").transform;
            handRoot.SetParent(rigGO.transform, false);
            handRoot.localPosition = new Vector3(0.185f, -0.245f, 0.315f);
            handRoot.localRotation = Quaternion.Euler(6f, -14f, 0f);

            var socket = new GameObject("socket").transform;
            socket.SetParent(handRoot, false);

            rig.root = handRoot;
            rig.socket = socket;

            var hand = PropLibrary.Hand(handRoot, _style);
            hand.transform.localPosition = new Vector3(0f, -0.012f, -0.008f);

            interactor = playerGO.AddComponent<PlayerInteractor>();
            interactor.cam = cam;
            interactor.controller = fpc;
            interactor.hands = rig;

            var decal = ProcMesh.Place("指认环", null, ProcMesh.Quad(1f, 1f), _style.CursorDecal, Vector3.zero);
            decal.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            decal.receiveShadows = false;
            decal.gameObject.SetActive(false);
            interactor.cursorDecal = decal.transform;
        }

        void BuildHud(PlayerInteractor interactor)
        {
            var hudGO = new GameObject("界面");
            hudGO.transform.SetParent(_root, false);
            var hud = hudGO.AddComponent<WorkshopHUD>();
            hud.player = interactor;
            hud.process = _process;
        }
    }
}
