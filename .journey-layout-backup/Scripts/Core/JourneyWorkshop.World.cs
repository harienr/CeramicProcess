using UnityEngine;
using T = PorcelainFromClay;
using G = Porcelain;

namespace CiCongNiSheng.Journey
{
    public sealed partial class JourneyWorkshop
    {
        void BuildStations()
        {
            StoneWash = StoneWashStation.Build(transform, style);
            StoneWash.transform.position = new Vector3(4.45f, 0, 1.2f);
            var taoxiRoot = new GameObject("07 · 泥耙淘洗").transform;
            taoxiRoot.SetParent(transform, false);
            var materials = new T.MaterialLibrary();
            materials.Build();
            Taoxi = new T.WorkshopBuilder(materials, taoxiRoot).BuildIntegrated();
            taoxiRoot.position = new Vector3(2.65f, 0, 5.05f);
            Taoxi.UpperVat.SetPowder(0f);
            Taoxi.LowerVat.gameObject.SetActive(false);
            rake = new GameObject("泥耙").transform;
            rake.SetParent(Taoxi.StirAnchor, false);
            rake.localPosition = new Vector3(0, -0.1f, 0);
            rake.localRotation = Quaternion.Euler(12, 0, 28);
            ProcMesh.Place("木柄", rake, ProcMesh.Cylinder(0.018f, 0.016f, 1.05f, 12), style.ToolWood, Vector3.zero);
            ProcMesh.Place("耙头", rake, ProcMesh.Box(new Vector3(0.35f, 0.065f, 0.11f), 0.012f), style.ToolWood, Vector3.zero);
            for (int i = 0; i < 5; i++)
                ProcMesh.Place("木齿_" + i, rake, ProcMesh.Box(new Vector3(0.018f, 0.14f, 0.028f), 0.005f), style.ToolWood,
                    new Vector3(-0.14f + i * 0.07f, -0.06f, 0.02f));
            var sieveRoot = new GameObject("08 · 粗泥过筛").transform;
            sieveRoot.SetParent(transform, false);
            var sieve = G.WorkshopBuilder.BuildIntegrated(sieveRoot);
            sieveRoot.position = new Vector3(1.5f, 0, 2.8f);
            Sieve = new SieveRefs { st = sieve.station, tub = sieve.tub, root = sieve.root.transform };
            Sieve.tub.Fill01 = 0f;
            Sieve.st.enabled = false;
            Sieve.st.residue.name = "截留细渣";
            Sieve.tub.gameObject.name = "淘洗粗泥 · 待筛料缸";
            // Keep the supplied sieving animation; its stream now enters a stone settling pool.
            var originalBasin = Sieve.root.Find("Basin");
            if (originalBasin != null) originalBasin.gameObject.SetActive(false);
            var poolHost = new GameObject("过筛沉淀池");
            poolHost.transform.SetParent(Sieve.root, false);
            FinalPool = poolHost.AddComponent<T.TaoxiVat>();
            FinalPool.Build(materials, poolHost.transform, "沉淀池", new Vector3(0, 0, -0.43f), 0.95f, 1.55f, 0.46f, 0.085f, 0.09f, false, true);
            FinalPool.SetFill(0f);
            FinalPool.SetTurbidity(0.85f);
            FinalPool.SetClay(0f);
            FinalPool.SetCoarse(false);
            var tray = PropLibrary.WoodenTray(Sieve.root, style, 0.48f, 0.38f, 0.07f, "细渣弃置盘");
            tray.transform.localPosition = new Vector3(-0.75f, 0.08f, -0.12f);
            fineWaste = ProcMesh.Place("筛上细渣", tray.transform, ProcMesh.Blob(1, 1, 20, 4, 0.3f, 7), style.Coarse,
                new Vector3(0, 0.025f, 0)).transform;
            fineWaste.gameObject.SetActive(false);
        }

        void BuildPlayer()
        {
            var body = new GameObject("学徒 · 第一人称玩家");
            body.transform.SetParent(transform, false);
            body.transform.position = spawn;
            body.transform.rotation = Quaternion.Euler(0, 180, 0);
            body.layer = 2;
            body.AddComponent<CharacterController>();
            Movement = body.AddComponent<FirstPersonController>();
            Movement.walkSpeed = 1.9f;
            var eye = new GameObject("视点").transform;
            eye.SetParent(body.transform, false);
            eye.localPosition = Vector3.up * 1.62f;
            Movement.cameraPivot = eye;
            Movement.SetViewAngles(180f, 12f);
            Camera = eye.gameObject.AddComponent<Camera>();
            Camera.tag = "MainCamera";
            Camera.nearClipPlane = 0.025f;
            Camera.farClipPlane = 80f;
            Camera.fieldOfView = 62f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Palette.RiceWhite;
            eye.gameObject.AddComponent<AudioListener>();
            Player = body.AddComponent<PlayerInteractor>();
            Player.controller = Movement;
            Player.cam = Camera;
            Player.reach = 2.6f;
            var hands = new GameObject("双手与工具").AddComponent<HandsRig>();
            hands.transform.SetParent(eye, false);
            hands.root = new GameObject("握持位").transform;
            hands.root.SetParent(hands.transform, false);
            hands.root.localPosition = new Vector3(0.19f, -0.26f, 0.38f);
            hands.socket = new GameObject("工具挂点").transform;
            hands.socket.SetParent(hands.root, false);
            PropLibrary.Hand(hands.root, style);
            Player.hands = hands;
            cargo = PropLibrary.WoodenBucket(eye, style, 0.22f, 0.145f, "原料交接桶");
            cargo.transform.localPosition = new Vector3(-0.24f, -0.42f, 0.49f);
            cargoSurface = ProcMesh.Place("桶内石粉或粗泥", cargo.transform, ProcMesh.Disc(0.127f, 24), style.Slurry,
                new Vector3(0, 0.18f, 0)).transform;
            cargoStones = new GameObject("桶内净石或粗渣");
            cargoStones.transform.SetParent(cargo.transform, false);
            for (int i = 0; i < 8; i++)
                ProcMesh.Place("颗粒_" + i, cargoStones.transform, T.MeshLab.Rock(0.036f, i + 50, 0.7f, 16, 9),
                    style.SlurryDry, new Vector3(Mathf.Sin(i * 2.4f) * 0.075f, 0.17f, Mathf.Cos(i * 2.4f) * 0.075f));
            cargo.SetActive(false);
        }

        JourneyInteractable Target(JourneyAction action, string label, Vector3 position, Vector3 size)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = true;
            var target = go.AddComponent<JourneyInteractable>();
            target.action = action;
            target.label = label;
            target.workshop = this;
            target.maxDistance = 2.6f;
            return target;
        }

        void BuildTargets()
        {
            var list = new System.Collections.Generic.List<JourneyInteractable>();
            list.Add(Target(JourneyAction.RawStones, "把待洗瓷石放入竹筛", StoneWash.RawPoint.position, new Vector3(0.65f, 0.5f, 0.65f)));
            list.Add(Target(JourneyAction.StoneSieve, "竹筛清洗瓷石", StoneWash.Sieve.position + Vector3.up * 0.12f, new Vector3(1.05f, 0.35f, 0.95f)));
            list.Add(Target(JourneyAction.Debris, "挑出杂物，放入废杂物盘", StoneWash.Sieve.position + Vector3.up * 0.12f, new Vector3(1.05f, 0.35f, 0.95f)));
            list.Add(Target(JourneyAction.CleanStones, "收取筛洗净石", StoneWash.Sieve.position + Vector3.up * 0.12f, new Vector3(1.05f, 0.35f, 0.95f)));
            list.Add(Target(JourneyAction.LoadMortars, "把净石分装进两只碓臼", Mortars[0].transform.position + Vector3.up * 0.5f, new Vector3(0.75f, 0.85f, 0.75f)));
            list.Add(Target(JourneyAction.Gate, "水碓进水闸", Cishan.WorkshopLayout.GateHandle, new Vector3(0.6f, 0.65f, 0.5f)));
            for (int i = 0; i < 2; i++)
                list.Add(Target(i == 0 ? JourneyAction.MortarA : JourneyAction.MortarB, "碓臼 " + (i + 1) + " · 取粉",
                    Mortars[i].transform.position + Vector3.up * 0.5f, new Vector3(0.75f, 0.85f, 0.75f)));
            list.Add(Target(JourneyAction.PowderVat, "将石粉倒入淘洗池", Taoxi.PourAnchor.position, new Vector3(0.95f, 0.45f, 0.85f)));
            list.Add(Target(JourneyAction.Water, "取清水浸湿石粉", Taoxi.BucketAnchor.position + Vector3.up * 0.15f, new Vector3(0.6f, 0.55f, 0.6f)));
            list.Add(Target(JourneyAction.StirVat, "用泥耙淘洗石粉", Taoxi.StirAnchor.position + Vector3.up * 0.15f, new Vector3(0.95f, 0.4f, 0.8f)));
            list.Add(Target(JourneyAction.Coarse, "分出粗渣，装入返舂桶", Taoxi.StirAnchor.position + Vector3.up * 0.15f, new Vector3(0.95f, 0.4f, 0.8f)));
            list.Add(Target(JourneyAction.ReturnCoarse, "将淘洗粗渣送回碓臼", Mortars[0].transform.position + Vector3.up * 0.5f, new Vector3(0.75f, 0.85f, 0.75f)));
            list.Add(Target(JourneyAction.RoughSlurry, "收取淘洗后的粗泥", Taoxi.StirAnchor.position + Vector3.up * 0.15f, new Vector3(0.95f, 0.4f, 0.8f)));
            list.Add(Target(JourneyAction.SieveInput, "将淘洗粗泥倒入过筛料缸", Sieve.tub.transform.position + Vector3.up * 0.65f, new Vector3(0.55f, 0.45f, 0.55f)));
            list.Add(Target(JourneyAction.Sieve, "粗泥过筛", Sieve.st.transform.position + Vector3.up * 1.15f, new Vector3(1.05f, 0.35f, 0.85f)));
            list.Add(Target(JourneyAction.Pool, "检查过筛浆与截留细渣", Sieve.st.transform.position + new Vector3(0, 0.53f, -0.98f), new Vector3(0.8f, 0.3f, 0.42f)));
            Targets = list.ToArray();
        }

        void BuildSigns()
        {
            Sign("瓷从泥生\n原料工坊 · 第 5—8 步", new Vector3(5.8f, 2.7f, 7), 0.021f);
            Sign("05 筛洗 · 净石", new Vector3(4.45f, 1.65f, 1.9f));
            Sign("06 舂石 · 石粉", new Vector3(7.1f, 1.25f, 2));
            Sign("07 淘洗 · 粗泥", new Vector3(2.6f, 1.65f, 6.55f));
            Sign("08 过筛 · 入沉淀池", new Vector3(1.5f, 1.75f, 3.5f));
            Sign("粗渣 → 返回碓臼", new Vector3(3.9f, 1.5f, 6.5f), 0.012f);
        }

        void Sign(string value, Vector3 position, float size = 0.014f)
        {
            var go = new GameObject(value);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var text = go.AddComponent<TextMesh>();
            text.text = value;
            text.font = UIFactory.Font;
            text.fontSize = 48;
            text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Palette.Cobalt;
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            go.AddComponent<JourneyStationLabel>();
        }
    }
}