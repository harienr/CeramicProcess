using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PorcelainFromClay
{
    public sealed class WorkshopBuilder
    {
        readonly MaterialLibrary mats;
        readonly Transform world;
        readonly WorkshopRefs refs = new WorkshopRefs();
        Font labelFont;
        readonly List<GameObject> contactShadows = new List<GameObject>();

        const float RoomW = 4.7f;
        const float RoomD = 6.1f;
        const float RoomH = 2.85f;
        const float WallT = 0.18f;

        public WorkshopBuilder(MaterialLibrary materials, Transform parent)
        {
            mats = materials;
            world = new GameObject("WorkshopWorld").transform;
            world.SetParent(parent, false);
            refs.Root = world;
        }

        public WorkshopRefs Build()
        {
            labelFont = Resources.Load<Font>("Fonts/SourceHanSansCN-Regular");
            if (labelFont == null) labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC" }, 32);

            BuildFloor();
            BuildWalls();
            BuildCeiling();
            BuildWindow();
            BuildDoor();
            BuildVats();
            BuildChannelAndGate();
            BuildTableAndTools();
            BuildWaterJar();
            BuildPowderBin();
            BuildStoneBasket();
            BuildShelf();
            BuildStool();
            BuildWallTools();
            BuildBroom();
            BuildTailings();
            BuildClayTray();
            BuildInteractables();
            BuildLighting();

            refs.PlayerSpawn = new GameObject("PlayerSpawn").transform;
            refs.PlayerSpawn.SetParent(world, false);
            refs.PlayerSpawn.position = new Vector3(-0.15f, 0.02f, -2.4f);
            refs.SpawnYaw = 0f;
            refs.SpawnPitch = 11f;

            return refs;
        }

        public WorkshopRefs BuildIntegrated()
        {
            labelFont = Resources.Load<Font>("Fonts/SourceHanSansCN-Regular");
            BuildVats();
            BuildTableAndTools();
            BuildTailings();
            return refs;
        }

        // ------------------------------------------------------------------ room
        void BuildFloor()
        {
            var floorRoot = new GameObject("Floor").transform;
            floorRoot.SetParent(world, false);
            var slabMesh = MeshLab.RoundedBox(new Vector3(0.785f, 0.055f, 0.767f), 0.007f, 1.75f, 2);
            var mortar = MeshLab.BoxObject("MortarBed", floorRoot, new Vector3(RoomW, 0.025f, RoomD), 0.004f, mats.StoneWet,
                new Vector3(0f, -0.014f, 0f), 1.3f, false);
            MeshLab.Tint(mortar.GetComponent<Renderer>(), new Color(0.62f, 0.61f, 0.57f));

            int n = 0;
            for (int x = 0; x < 6; x++)
            {
                for (int z = 0; z < 8; z++)
                {
                    float px = -RoomW * 0.5f + 0.39f + x * 0.782f;
                    float pz = -RoomD * 0.5f + 0.381f + z * 0.7625f;
                    float y = -0.0275f + Mathf.PerlinNoise(x * 2.1f + 2f, z * 1.8f + 5f) * 0.005f;
                    float rot = (Mathf.PerlinNoise(x * 1.3f + 7f, z * 2.7f + 3f) - 0.5f) * 0.5f;
                    var go = MeshLab.MeshObject("Slab" + n, floorRoot, slabMesh, mats.Stone,
                        new Vector3(px, y, pz), Quaternion.Euler(0f, rot, 0f), false, true);
                    float t = Mathf.PerlinNoise(x * 1.7f + 4f, z * 2.3f + 1f);
                    var tint = Color.Lerp(new Color(0.83f, 0.82f, 0.78f), new Color(0.98f, 0.96f, 0.9f), t);
                    if (z <= 2 && x >= 3) tint *= 0.9f;
                    if (x <= 1 && z >= 5) tint *= 0.94f;
                    MeshLab.Tint(go.GetComponent<Renderer>(), tint);
                    n++;
                }
            }

            // floor collision
            AddCollider(floorRoot, "FloorCollision", new Vector3(0f, -0.1f, 0f), new Vector3(RoomW, 0.2f, RoomD));

            // a shallow drain channel, flush with the slabs
            var drain = MeshLab.BoxObject("Drain", floorRoot, new Vector3(0.32f, 0.012f, 0.55f), 0.003f, mats.StoneWet,
                new Vector3(-0.15f, 0.004f, 1.62f), 1.5f, false);
            for (int i = 0; i < 6; i++)
            {
                var bar = MeshLab.BoxObject("DrainBar" + i, drain.transform, new Vector3(0.036f, 0.01f, 0.5f), 0.002f, mats.WoodWet,
                    new Vector3(-0.13f + i * 0.052f, 0.012f, 0f), 2f, false);
                bar.transform.localPosition = new Vector3(-0.13f + i * 0.052f, 0.012f, 0f);
            }

            DampPatch(new Vector3(-0.58f, 0.007f, 0.72f), 1.55f, 1.3f, 0.34f);
            DampPatch(new Vector3(0.98f, 0.007f, 1.15f), 1.3f, 1.15f, 0.3f);
            DampPatch(new Vector3(1.42f, 0.007f, -0.32f), 0.95f, 0.82f, 0.2f);
            DampPatch(new Vector3(-1.62f, 0.007f, -1.52f), 0.98f, 0.88f, 0.24f);

            // a few dry crumbs and spill marks, so the floor reads as a working floor
            var crumbRandom = new System.Random(771);
            for (int i = 0; i < 26; i++)
            {
                var p = new Vector3(-1.3f + (float)crumbRandom.NextDouble() * 2.8f, 0.012f, -0.1f + (float)crumbRandom.NextDouble() * 1.9f);
                if (Mathf.Abs(p.x + 0.56f) < 0.55f && Mathf.Abs(p.z - 0.47f) < 0.55f) continue;
                if (Mathf.Abs(p.x - 0.96f) < 0.5f && Mathf.Abs(p.z - 0.94f) < 0.48f) continue;
                float s = 0.008f + (float)crumbRandom.NextDouble() * 0.018f;
                var bit = MeshLab.MeshObject("FloorCrumb" + i, floorRoot, MeshLab.Rock(1f, 2200 + i, 0.32f, 7, 4), mats.DryClay,
                    p, Quaternion.Euler(0f, (float)crumbRandom.NextDouble() * 360f, 0f), false, true);
                bit.transform.localScale = new Vector3(s, s * 0.5f, s * 0.8f);
                MeshLab.Tint(bit.GetComponent<Renderer>(), new Color(0.8f, 0.78f, 0.72f));
            }
        }

        void DampPatch(Vector3 pos, float w, float d, float strength)
        {
            var go = MeshLab.MeshObject("DampPatch", world, MeshLab.Plane(w, d, 1f), mats.InstantiateShadow(strength, 0.78f), pos, Quaternion.identity, false, false);
            go.GetComponent<Renderer>().sortingOrder = -4;
            contactShadows.Add(go);
        }

        void BuildWalls()
        {
            var walls = new GameObject("Walls").transform;
            walls.SetParent(world, false);

            // left wall with the window opening
            WallSegment(walls, "LeftBack", new Vector3(-RoomW * 0.5f - WallT * 0.5f, RoomH * 0.5f, -1.8f), new Vector3(WallT, RoomH, 2.5f));
            WallSegment(walls, "LeftFront", new Vector3(-RoomW * 0.5f - WallT * 0.5f, RoomH * 0.5f, 1.9f), new Vector3(WallT, RoomH, 2.3f));
            WallSegment(walls, "LeftSill", new Vector3(-RoomW * 0.5f - WallT * 0.5f, 0.525f, 0.1f), new Vector3(WallT, 1.05f, 1.3f));
            WallSegment(walls, "LeftHeader", new Vector3(-RoomW * 0.5f - WallT * 0.5f, 2.55f, 0.1f), new Vector3(WallT, 0.6f, 1.3f));

            // right wall
            WallSegment(walls, "Right", new Vector3(RoomW * 0.5f + WallT * 0.5f, RoomH * 0.5f, 0f), new Vector3(WallT, RoomH, RoomD + WallT * 2f));

            // back wall
            WallSegment(walls, "Back", new Vector3(0f, RoomH * 0.5f, RoomD * 0.5f + WallT * 0.5f), new Vector3(RoomW, RoomH, WallT));

            // front wall, with the closed door opening held by the door itself
            WallSegment(walls, "FrontLeft", new Vector3(-1.05f, RoomH * 0.5f, -RoomD * 0.5f - WallT * 0.5f), new Vector3(2.6f, RoomH, WallT));
            WallSegment(walls, "FrontRight", new Vector3(1.8f, RoomH * 0.5f, -RoomD * 0.5f - WallT * 0.5f), new Vector3(1.1f, RoomH, WallT));
            WallSegment(walls, "FrontHeader", new Vector3(0.75f, 2.475f, -RoomD * 0.5f - WallT * 0.5f), new Vector3(1.0f, 0.75f, WallT));

            // stone wainscot, kept thin so the wall still reads as plaster
            Wainscot(walls, "WainscotBack", new Vector3(0f, 0.41f, RoomD * 0.5f - 0.012f), new Vector3(RoomW, 0.82f, 0.024f), Quaternion.identity);
            Wainscot(walls, "WainscotRight", new Vector3(RoomW * 0.5f - 0.012f, 0.41f, 0.2f), new Vector3(0.024f, 0.82f, 4.4f), Quaternion.identity);
            Wainscot(walls, "WainscotLeftBack", new Vector3(-RoomW * 0.5f + 0.012f, 0.41f, 2.1f), new Vector3(0.024f, 0.82f, 1.9f), Quaternion.identity);
            Wainscot(walls, "WainscotLeftFront", new Vector3(-RoomW * 0.5f + 0.012f, 0.41f, -0.9f), new Vector3(0.024f, 0.82f, 2.3f), Quaternion.identity);

            // dark trim line where plaster meets stone
            Trim(walls, "TrimBack", new Vector3(0f, 0.84f, RoomD * 0.5f - 0.025f), new Vector3(RoomW, 0.055f, 0.04f));
            Trim(walls, "TrimRight", new Vector3(RoomW * 0.5f - 0.025f, 0.84f, 0.2f), new Vector3(0.04f, 0.055f, 4.4f));

            // posts and wall plates
            Post(walls, "PostFL", new Vector3(-RoomW * 0.5f + 0.1f, RoomH * 0.5f, -RoomD * 0.5f + 0.1f));
            Post(walls, "PostFR", new Vector3(RoomW * 0.5f - 0.1f, RoomH * 0.5f, -RoomD * 0.5f + 0.1f));
            Post(walls, "PostBL", new Vector3(-RoomW * 0.5f + 0.1f, RoomH * 0.5f, RoomD * 0.5f - 0.1f));
            Post(walls, "PostBR", new Vector3(RoomW * 0.5f - 0.1f, RoomH * 0.5f, RoomD * 0.5f - 0.1f));
            Trim(walls, "PlateBack", new Vector3(0f, 2.62f, RoomD * 0.5f - 0.12f), new Vector3(RoomW, 0.1f, 0.08f));
            Trim(walls, "PlateRight", new Vector3(RoomW * 0.5f - 0.12f, 2.62f, 0.2f), new Vector3(0.08f, 0.1f, 4.4f));

            // collision envelope (includes the closed door and window wall)
            AddCollider(walls, "WallColliderLeft", new Vector3(-RoomW * 0.5f - WallT * 0.5f, 1.425f, 0f), new Vector3(WallT, RoomH, RoomD + 0.2f));
            AddCollider(walls, "WallColliderRight", new Vector3(RoomW * 0.5f + WallT * 0.5f, 1.425f, 0f), new Vector3(WallT, RoomH, RoomD + 0.2f));
            AddCollider(walls, "WallColliderBack", new Vector3(0f, 1.425f, RoomD * 0.5f + WallT * 0.5f), new Vector3(RoomW + 0.4f, RoomH, WallT));
            AddCollider(walls, "WallColliderFront", new Vector3(0f, 1.425f, -RoomD * 0.5f - WallT * 0.5f), new Vector3(RoomW + 0.4f, RoomH, WallT));
        }

        void WallSegment(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = MeshLab.MeshObject(name, parent, MeshLab.RoundedBox(size, 0.012f, 0.72f, 4), mats.Plaster, center, Quaternion.identity, true, true);
            MeshLab.Tint(go.GetComponent<Renderer>(), new Color(0.98f, 0.96f, 0.91f));
        }

        void Wainscot(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = MeshLab.MeshObject(name, parent, MeshLab.RoundedBox(size, 0.006f, 2.0f, 2), mats.Stone, center, rot, false, true);
            MeshLab.Tint(go.GetComponent<Renderer>(), new Color(0.92f, 0.9f, 0.85f));
        }

        void Trim(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = MeshLab.MeshObject(name, parent, MeshLab.RoundedBox(size, 0.01f, 2.4f, 3), mats.Wood, center, Quaternion.identity, true, true);
            MeshLab.Tint(go.GetComponent<Renderer>(), new Color(0.8f, 0.76f, 0.7f));
        }

        void Post(Transform parent, string name, Vector3 center)
        {
            MeshLab.BoxObject(name, parent, new Vector3(0.14f, RoomH, 0.14f), 0.018f, mats.Wood, center, 1.4f, true);
        }

        void BuildCeiling()
        {
            var ceiling = new GameObject("Ceiling").transform;
            ceiling.SetParent(world, false);
            var slab = MeshLab.MeshObject("CeilingPlane", ceiling, MeshLab.RoundedBox(new Vector3(RoomW + 0.2f, 0.1f, RoomD + 0.2f), 0.01f, 0.3f, 3),
                mats.Plaster, new Vector3(0f, RoomH + 0.06f, 0f), Quaternion.identity, true, true);
            MeshLab.Tint(slab.GetComponent<Renderer>(), new Color(0.72f, 0.7f, 0.65f));

            for (int i = 0; i < 5; i++)
            {
                float z = -2.4f + i * 1.2f;
                MeshLab.BoxObject("Beam" + i, ceiling, new Vector3(RoomW + 0.1f, 0.17f, 0.13f), 0.02f, mats.Wood,
                    new Vector3(0f, 2.68f, z), 1.2f, true);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = -1.95f + i * 1.3f;
                MeshLab.BoxObject("Purlin" + i, ceiling, new Vector3(0.1f, 0.1f, RoomD + 0.1f), 0.015f, mats.Wood,
                    new Vector3(x, 2.78f, 0f), 1.1f, true);
            }
        }

        void BuildWindow()
        {
            var win = new GameObject("Window").transform;
            win.SetParent(world, false);
            float xIn = -RoomW * 0.5f;
            float zC = 0.1f;
            float openZ0 = -0.55f;
            float openZ1 = 0.75f;
            float sill = 1.05f;
            float head = 2.25f;

            // the bright courtyard beyond
            var sky = MeshLab.MeshObject("Daylight", win, MeshLab.QuadXY(2.6f, 2.35f, 1f), mats.WindowSky,
                new Vector3(xIn - 0.36f, 1.62f, zC), Quaternion.Euler(0f, -90f, 0f), false, false);
            sky.GetComponent<Renderer>().sortingOrder = -10;

            // reveal, so the window reads as a thick wall rather than a painted rectangle
            var revealMat = mats.Plaster;
            MeshLab.BoxObject("RevealTop", win, new Vector3(0.34f, 0.08f, openZ1 - openZ0), 0.006f, revealMat,
                new Vector3(xIn - 0.17f, head - 0.02f, zC), 0.8f, true);
            MeshLab.BoxObject("RevealBottom", win, new Vector3(0.34f, 0.07f, openZ1 - openZ0), 0.006f, revealMat,
                new Vector3(xIn - 0.17f, sill + 0.02f, zC), 0.8f, true);
            MeshLab.BoxObject("RevealBack", win, new Vector3(0.34f, head - sill, 0.07f), 0.006f, revealMat,
                new Vector3(xIn - 0.17f, (head + sill) * 0.5f, openZ0 + 0.03f), 0.8f, true);
            MeshLab.BoxObject("RevealFront", win, new Vector3(0.34f, head - sill, 0.07f), 0.006f, revealMat,
                new Vector3(xIn - 0.17f, (head + sill) * 0.5f, openZ1 - 0.03f), 0.8f, true);

            // frame
            float frameX = xIn - 0.04f;
            MeshLab.BoxObject("FrameTop", win, new Vector3(0.11f, 0.12f, openZ1 - openZ0 + 0.14f), 0.012f, mats.Wood,
                new Vector3(frameX, head + 0.04f, zC), 1.4f, true);
            MeshLab.BoxObject("FrameBottom", win, new Vector3(0.11f, 0.1f, openZ1 - openZ0 + 0.14f), 0.012f, mats.Wood,
                new Vector3(frameX, sill - 0.02f, zC), 1.4f, true);
            MeshLab.BoxObject("FrameBack", win, new Vector3(0.11f, head - sill, 0.1f), 0.012f, mats.Wood,
                new Vector3(frameX, (head + sill) * 0.5f, openZ0 - 0.03f), 1.4f, true);
            MeshLab.BoxObject("FrameFront", win, new Vector3(0.11f, head - sill, 0.1f), 0.012f, mats.Wood,
                new Vector3(frameX, (head + sill) * 0.5f, openZ1 + 0.03f), 1.4f, true);

            // lattice: five uprights, three crossbars
            for (int i = 0; i < 5; i++)
            {
                float z = Mathf.Lerp(openZ0 + 0.08f, openZ1 - 0.08f, i / 4f);
                MeshLab.BoxObject("Mullion" + i, win, new Vector3(0.055f, head - sill - 0.12f, 0.035f), 0.006f, mats.Wood,
                    new Vector3(frameX - 0.01f, (head + sill) * 0.5f, z), 2.8f, true);
            }
            for (int i = 0; i < 3; i++)
            {
                float y = Mathf.Lerp(sill + 0.18f, head - 0.18f, i / 2f);
                MeshLab.BoxObject("Crossbar" + i, win, new Vector3(0.045f, 0.035f, openZ1 - openZ0 - 0.12f), 0.005f, mats.Wood,
                    new Vector3(frameX - 0.015f, y, zC), 2.8f, true);
            }

            // sill stone, worn at the front edge
            var sillGo = MeshLab.MeshObject("Sill", win, MeshLab.RoundedBox(new Vector3(0.44f, 0.07f, openZ1 - openZ0 + 0.3f), 0.018f, 1.4f, 4),
                mats.StoneRim, new Vector3(xIn + 0.14f, sill - 0.015f, zC), Quaternion.identity, true, true);
            MeshLab.SetWetness(sillGo.GetComponent<Renderer>(), 0.04f);
        }

        void BuildDoor()
        {
            var doorRoot = new GameObject("Door").transform;
            doorRoot.SetParent(world, false);
            float z = -RoomD * 0.5f + 0.02f;
            float x = 0.75f;
            MeshLab.BoxObject("DoorFrameL", doorRoot, new Vector3(0.1f, 2.16f, 0.12f), 0.012f, mats.Wood,
                new Vector3(0.21f, 1.08f, z), 1.5f, true);
            MeshLab.BoxObject("DoorFrameR", doorRoot, new Vector3(0.1f, 2.16f, 0.12f), 0.012f, mats.Wood,
                new Vector3(1.29f, 1.08f, z), 1.5f, true);
            MeshLab.BoxObject("DoorLintel", doorRoot, new Vector3(1.18f, 0.11f, 0.12f), 0.012f, mats.Wood,
                new Vector3(x, 2.13f, z), 1.5f, true);

            for (int i = 0; i < 5; i++)
            {
                float bx = 0.33f + i * 0.205f;
                var board = MeshLab.BoxObject("DoorBoard" + i, doorRoot, new Vector3(0.195f, 2.02f, 0.045f), 0.008f, mats.Wood,
                    new Vector3(bx, 1.02f, z + 0.035f), 2.2f, true);
                MeshLab.Tint(board.GetComponent<Renderer>(), Color.Lerp(new Color(0.78f, 0.74f, 0.68f), Color.white, (i % 3) / 4f));
            }
            MeshLab.BoxObject("DoorBraceTop", doorRoot, new Vector3(0.92f, 0.08f, 0.035f), 0.008f, mats.Wood,
                new Vector3(x, 1.6f, z + 0.065f), 2.0f, true);
            MeshLab.BoxObject("DoorBraceBottom", doorRoot, new Vector3(0.92f, 0.08f, 0.035f), 0.008f, mats.Wood,
                new Vector3(x, 0.53f, z + 0.065f), 2.0f, true);

            var gap = MeshLab.MeshObject("DoorGapLight", doorRoot, MeshLab.QuadXY(0.98f, 0.018f), mats.WindowSky,
                new Vector3(x, 0.008f, z + 0.09f), Quaternion.identity, false, false);
            gap.GetComponent<Renderer>().sortingOrder = -5;
            gap.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        // ------------------------------------------------------------------ vats
        void BuildVats()
        {
            var vats = new GameObject("Vats").transform;
            vats.SetParent(world, false);

            var upperHost = new GameObject("UpperVat");
            upperHost.transform.SetParent(vats, false);
            var upper = upperHost.AddComponent<TaoxiVat>();
            upper.Build(mats, upperHost.transform, "淘洗池", new Vector3(-0.56f, 0f, 0.47f), 1.0f, 0.92f, 0.78f, 0.095f, 0.2f, true, false);
            AddCollider(upperHost.transform, "UpperVatCollision", new Vector3(-0.56f, 0.39f, 0.47f), new Vector3(1.0f, 0.78f, 0.92f));
            refs.UpperVat = upper;
            refs.PourAnchor = new GameObject("PourAnchor").transform;
            refs.PourAnchor.SetParent(upperHost.transform, false);
            refs.PourAnchor.position = new Vector3(-0.56f, 0.85f, 0.47f);
            refs.StirAnchor = new GameObject("StirAnchor").transform;
            refs.StirAnchor.SetParent(upperHost.transform, false);
            refs.StirAnchor.position = new Vector3(-0.56f, 0.62f, 0.47f);

            var lowerHost = new GameObject("LowerVat");
            lowerHost.transform.SetParent(vats, false);
            var lower = lowerHost.AddComponent<TaoxiVat>();
            lower.Build(mats, lowerHost.transform, "沉淀池", new Vector3(0.96f, 0f, 0.94f), 0.84f, 0.78f, 0.72f, 0.085f, 0.18f, false, true);
            AddCollider(lowerHost.transform, "LowerVatCollision", new Vector3(0.96f, 0.36f, 0.94f), new Vector3(0.84f, 0.72f, 0.78f));
            refs.LowerVat = lower;
            refs.ClayAnchor = new GameObject("ClayAnchor").transform;
            refs.ClayAnchor.SetParent(lowerHost.transform, false);
            refs.ClayAnchor.position = new Vector3(0.96f, 0.58f, 0.94f);

            upper.SetFill(0f);
            upper.SetPowder(1f);
            upper.SetCoarse(false);
            lower.SetFill(0f);
            lower.SetClay(0f);
            lower.SetCoarse(false);

            // vat labels
            Shadow(new Vector3(-0.56f, 0.007f, 0.47f), 1.75f, 1.65f, 0.28f);
            Shadow(new Vector3(0.96f, 0.007f, 0.94f), 1.35f, 1.3f, 0.22f);

            AddVatLight(new Vector3(-0.56f, 1.15f, 0.47f), 1.4f, 1.7f);
            AddVatLight(new Vector3(0.96f, 1.08f, 0.94f), 1.7f, 1.8f);
        }

        void BuildChannelAndGate()
        {
            var channel = new GameObject("Sluice").transform;
            channel.SetParent(world, false);
            float y = 0.54f;
            Vector3 center = new Vector3(0.25f, y, 0.75f);

            // stone trough
            MeshLab.BoxObject("TroughBottom", channel, new Vector3(0.64f, 0.055f, 0.2f), 0.01f, mats.StoneWet, center, 1.6f, true);
            MeshLab.BoxObject("TroughBack", channel, new Vector3(0.64f, 0.12f, 0.045f), 0.008f, mats.Stone, center + new Vector3(0f, 0.065f, 0.095f), 1.6f, true);
            MeshLab.BoxObject("TroughFront", channel, new Vector3(0.64f, 0.12f, 0.045f), 0.008f, mats.Stone, center + new Vector3(0f, 0.065f, -0.095f), 1.6f, true);
            MeshLab.BoxObject("Spout", channel, new Vector3(0.22f, 0.11f, 0.18f), 0.01f, mats.StoneWet, new Vector3(-0.17f, y + 0.045f, 0.75f), 1.6f, true);

            var chWaterMat = mats.InstantiateWater(new Color(0.4f, 0.42f, 0.4f), new Color(0.7f, 0.69f, 0.64f), 0.3f);
            chWaterMat.SetVector("_Center", new Vector4(0.25f, 0f, 0.75f, 0f));
            var chWater = MeshLab.MeshObject("ChannelWater", channel, MeshLab.Plane(0.58f, 0.145f, 1f), chWaterMat,
                new Vector3(0.25f, y + 0.035f, 0.75f), Quaternion.identity, false, false);
            chWater.SetActive(false);
            refs.ChannelWater = chWater.transform;

            // gate posts and sliding board
            MeshLab.BoxObject("GatePostA", channel, new Vector3(0.075f, 0.38f, 0.055f), 0.008f, mats.Wood,
                new Vector3(-0.06f, y + 0.18f, 0.66f), 2.2f, true);
            MeshLab.BoxObject("GatePostB", channel, new Vector3(0.075f, 0.38f, 0.055f), 0.008f, mats.Wood,
                new Vector3(-0.06f, y + 0.18f, 0.84f), 2.2f, true);
            MeshLab.BoxObject("GateTop", channel, new Vector3(0.075f, 0.06f, 0.24f), 0.008f, mats.Wood,
                new Vector3(-0.06f, y + 0.35f, 0.75f), 2.2f, true);

            var board = MeshLab.BoxObject("GateBoard", channel, new Vector3(0.055f, 0.22f, 0.18f), 0.008f, mats.WoodWet,
                new Vector3(-0.06f, y + 0.1f, 0.75f), 2.4f, true);
            var handle = MeshLab.BoxObject("GateHandle", board.transform, new Vector3(0.09f, 0.035f, 0.08f), 0.008f, mats.Bamboo,
                new Vector3(0.03f, 0.12f, 0f), 3f, true);
            handle.transform.localPosition = new Vector3(0.06f, 0.13f, 0f);
            refs.GateBoard = board.transform;
            refs.GateAnchor = new GameObject("GateAnchor").transform;
            refs.GateAnchor.SetParent(channel, false);
            refs.GateAnchor.position = new Vector3(-0.04f, y + 0.12f, 0.75f);


        }

        void AddVatLight(Vector3 pos, float intensity, float range)
        {
            var go = new GameObject("VatBounce");
            go.transform.SetParent(world, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.9f, 0.88f, 0.82f);
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.cullingMask = ~(1 << ViewmodelRig.Layer);
        }

        // ------------------------------------------------------------------ props
        void BuildTableAndTools()
        {
            var table = new GameObject("WorkTable").transform;
            table.SetParent(world, false);
            float tx = 1.45f;
            float tz = -0.58f;
            float topY = 0.82f;

            var top = MeshLab.BoxObject("TableTop", table, new Vector3(0.72f, 0.075f, 1.48f), 0.018f, mats.Wood,
                new Vector3(tx, topY - 0.038f, tz), 1.1f, true);
            MeshLab.Tint(top.GetComponent<Renderer>(), new Color(0.94f, 0.9f, 0.84f));
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    MeshLab.BoxObject("TableLeg", table, new Vector3(0.075f, 0.78f, 0.075f), 0.012f, mats.Wood,
                        new Vector3(tx + sx * 0.29f, 0.39f, tz + sz * 0.63f), 2.4f, true);
                }
            }
            MeshLab.BoxObject("TableStretcher", table, new Vector3(0.06f, 0.06f, 1.2f), 0.008f, mats.Wood,
                new Vector3(tx + 0.29f, 0.18f, tz), 2.2f, true);
            AddCollider(table, "TableCollider", new Vector3(tx, 0.41f, tz), new Vector3(0.74f, 0.82f, 1.5f));
            Shadow(new Vector3(tx, 0.007f, tz), 1.2f, 1.85f, 0.25f);

            // worn linen over one corner
            var cloth = MeshLab.MeshObject("TableCloth", table, MeshLab.Cloth(0.44f, 0.6f, -0.055f, 16, 3f), mats.Linen,
                new Vector3(tx - 0.12f, topY + 0.04f, tz + 0.46f), Quaternion.Euler(0f, 7f, 0f), false, true);
            cloth.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // bucket on the table
            var bucket = MakeBucket(table, "WaterBucket", new Vector3(1.27f, topY + 0.01f, -0.18f), 0.72f, true);
            refs.BucketAnchor = bucket.transform;
            AddCollider(bucket, "BucketCollider", new Vector3(0f, 0.16f, 0f), new Vector3(0.34f, 0.34f, 0.34f));

            // wooden scraper and a spare stirring paddle
            var scraper = MeshLab.MeshObject("TableScraper", table, MeshLab.RoundedBox(new Vector3(0.24f, 0.025f, 0.08f), 0.008f, 3f, 3), mats.Wood,
                new Vector3(tx - 0.12f, topY + 0.035f, tz - 0.42f), Quaternion.Euler(0f, 12f, 0f), true, true);
            MeshLab.BoxObject("ScraperHandle", scraper.transform, new Vector3(0.03f, 0.03f, 0.22f), 0.01f, mats.Wood,
                new Vector3(0f, 0f, 0.13f), 2.5f, true);

            // long stirring paddle leaning on the wall
            var paddle = new GameObject("LeaningPaddle").transform;
            paddle.SetParent(world, false);
            MeshLab.BoxObject("PaddleShaft", paddle, new Vector3(0.045f, 1.45f, 0.045f), 0.012f, mats.Wood,
                new Vector3(2.18f, 0.72f, -1.18f), 2f, true);
            MeshLab.BoxObject("PaddleBlade", paddle, new Vector3(0.19f, 0.28f, 0.045f), 0.012f, mats.WoodWet,
                new Vector3(2.12f, 0.18f, -1.18f), 2f, true);
            paddle.localRotation = Quaternion.Euler(0f, 0f, -7f);
            AddCollider(paddle, "PaddleCollider", new Vector3(2.14f, 0.72f, -1.18f), new Vector3(0.3f, 1.4f, 0.2f));

            // small bowl of water on the table
            MeshLab.MeshObject("WaterBowl", table, MeshLab.Lathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.06f, 0.002f), new Vector2(0.1f, 0.05f), new Vector2(0.095f, 0.052f),
                new Vector2(0.07f, 0.025f), new Vector2(0.02f, 0.01f), new Vector2(0f, 0.012f)
            }, 24, 1.4f, false, false), mats.StoneWet, new Vector3(tx + 0.16f, topY + 0.03f, tz - 0.35f), Quaternion.identity, true, true);
        }

        Transform MakeBucket(Transform parent, string name, Vector3 basePos, float scale, bool water)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = basePos;
            root.localScale = Vector3.one * scale;
            var body = MeshLab.MeshObject("Body", root, MeshLab.Lathe(new[]
            {
                new Vector2(0.115f, 0f), new Vector2(0.135f, 0.025f), new Vector2(0.15f, 0.25f),
                new Vector2(0.145f, 0.27f), new Vector2(0.13f, 0.27f), new Vector2(0.115f, 0.25f)
            }, 28, 2f, false, false), mats.WoodWet, Vector3.zero, Quaternion.identity, true, true);
            MeshLab.SetWetness(body.GetComponent<Renderer>(), 0.4f);
            MeshLab.MeshObject("Bottom", root, MeshLab.Cylinder(0.12f, 0.02f, 24, 2f, true), mats.WoodWet, new Vector3(0f, 0.01f, 0f), Quaternion.identity, true, true);
            MeshLab.MeshObject("HoopA", root, MeshLab.Lathe(CircleProfile(0.137f, 0.08f, 0.007f, 7), 24, 2f, false, false), mats.Bamboo, Vector3.zero, Quaternion.identity, true, true);
            MeshLab.MeshObject("HoopB", root, MeshLab.Lathe(CircleProfile(0.148f, 0.215f, 0.007f, 7), 24, 2f, false, false), mats.Bamboo, Vector3.zero, Quaternion.identity, true, true);
            MeshLab.MeshObject("Handle", root, MeshLab.Tube(new[]
            {
                new Vector3(-0.14f, 0.26f, 0f), new Vector3(-0.08f, 0.34f, 0f), new Vector3(0f, 0.36f, 0f),
                new Vector3(0.08f, 0.34f, 0f), new Vector3(0.14f, 0.26f, 0f)
            }, new[] { 0.01f, 0.011f, 0.012f, 0.011f, 0.01f }, 10, false), mats.Bamboo, Vector3.zero, Quaternion.identity, true, true);
            if (water)
            {
                var w = MeshLab.MeshObject("Water", root, MeshLab.Plane(0.22f, 0.22f, 1f), mats.InstantiateWater(new Color(0.35f, 0.38f, 0.36f), new Color(0.55f, 0.58f, 0.55f), 0.24f),
                    new Vector3(0f, 0.215f, 0f), Quaternion.identity, false, false);
                w.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return root;
        }

        Vector2[] CircleProfile(float radius, float y, float tube, int segments)
        {
            var p = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                p[i] = new Vector2(radius + Mathf.Cos(a) * tube, y + Mathf.Sin(a) * tube);
            }
            return p;
        }

        void BuildWaterJar()
        {
            var jar = new GameObject("WaterJar").transform;
            jar.SetParent(world, false);
            var pos = new Vector3(-1.95f, 0f, -1.35f);
            MeshLab.MeshObject("JarBody", jar, MeshLab.Lathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.16f, 0f), new Vector2(0.27f, 0.06f), new Vector2(0.31f, 0.3f),
                new Vector2(0.27f, 0.55f), new Vector2(0.18f, 0.66f), new Vector2(0.19f, 0.71f),
                new Vector2(0.22f, 0.72f), new Vector2(0.2f, 0.73f), new Vector2(0.13f, 0.64f),
                new Vector2(0.02f, 0.55f), new Vector2(0f, 0.55f)
            }, 32, 1.1f, false, false), mats.WetClay, pos, Quaternion.identity, true, true);
            var jarRenderer = jar.GetComponentInChildren<Renderer>();
            if (jarRenderer != null) MeshLab.Tint(jarRenderer, new Color(0.68f, 0.66f, 0.61f));
            MeshLab.MeshObject("JarWater", jar, MeshLab.Plane(0.34f, 0.34f, 1f), mats.InstantiateWater(new Color(0.38f, 0.41f, 0.39f), new Color(0.55f, 0.57f, 0.54f), 0.28f),
                pos + new Vector3(0f, 0.62f, 0f), Quaternion.identity, false, false);
            AddCollider(jar, "JarCollider", pos + new Vector3(0f, 0.36f, 0f), new Vector3(0.62f, 0.72f, 0.62f));
            Shadow(pos, 0.9f, 0.9f, 0.24f);

            var ladle = new GameObject("BambooLadle").transform;
            ladle.SetParent(world, false);
            MeshLab.MeshObject("LadleBowl", ladle, MeshLab.Lathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.055f, 0.004f), new Vector2(0.095f, 0.035f), new Vector2(0.1f, 0.07f),
                new Vector2(0.086f, 0.072f), new Vector2(0.061f, 0.038f), new Vector2(0.02f, 0.014f), new Vector2(0f, 0.016f)
            }, 20, 1.5f, false, false), mats.Bamboo, new Vector3(-1.67f, 0.78f, -1.3f), Quaternion.Euler(0f, 0f, -24f), true, true);
            MeshLab.BoxObject("LadleHandle", ladle, new Vector3(0.035f, 0.045f, 0.55f), 0.01f, mats.Bamboo,
                new Vector3(-1.58f, 0.94f, -1.3f), 2.4f, true);
        }

        void BuildPowderBin()
        {
            var bin = new GameObject("PowderBin").transform;
            bin.SetParent(world, false);
            var basePos = new Vector3(-1.72f, 0f, 1.05f);
            float top = 0.62f;
            MeshLab.BoxObject("BinBack", bin, new Vector3(0.76f, 0.62f, 0.06f), 0.012f, mats.Wood, basePos + new Vector3(0f, top * 0.5f, 0.32f), 1.4f, true);
            MeshLab.BoxObject("BinFront", bin, new Vector3(0.76f, 0.62f, 0.06f), 0.012f, mats.Wood, basePos + new Vector3(0f, top * 0.5f, -0.32f), 1.4f, true);
            MeshLab.BoxObject("BinLeft", bin, new Vector3(0.06f, 0.62f, 0.58f), 0.012f, mats.Wood, basePos + new Vector3(-0.35f, top * 0.5f, 0f), 1.4f, true);
            MeshLab.BoxObject("BinRight", bin, new Vector3(0.06f, 0.62f, 0.58f), 0.012f, mats.Wood, basePos + new Vector3(0.35f, top * 0.5f, 0f), 1.4f, true);
            MeshLab.BoxObject("BinBottom", bin, new Vector3(0.7f, 0.06f, 0.58f), 0.01f, mats.WoodWet, basePos + new Vector3(0f, 0.03f, 0f), 1.4f, true);
            MeshLab.MeshObject("BinLiner", bin, MeshLab.Plane(0.64f, 0.53f, 1f), mats.DryPowder, basePos + new Vector3(0f, 0.08f, 0f), Quaternion.identity, false, true);

            var powder = MeshLab.MeshObject("PowderPile", bin, MeshLab.Rock(1f, 1211, 0.2f, 20, 9), mats.DryPowder,
                basePos + new Vector3(0f, 0.48f, 0f), Quaternion.identity, false, true);
            powder.transform.localScale = new Vector3(0.29f, 0.2f, 0.24f);
            var powder2 = MeshLab.MeshObject("PowderPile2", bin, MeshLab.Rock(1f, 1212, 0.16f, 18, 8), mats.DryPowder,
                basePos + new Vector3(-0.2f, 0.41f, 0.13f), Quaternion.identity, false, true);
            powder2.transform.localScale = new Vector3(0.16f, 0.1f, 0.14f);

            // a wooden scoop resting on the rim
            var scoop = new GameObject("RestingScoop").transform;
            scoop.SetParent(bin, false);
            scoop.localPosition = basePos + new Vector3(0.12f, 0.64f, -0.25f);
            scoop.localRotation = Quaternion.Euler(0f, 18f, 84f);
            MeshLab.MeshObject("ScoopBowl", scoop, MeshLab.Lathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.05f, 0.002f), new Vector2(0.085f, 0.025f), new Vector2(0.09f, 0.048f),
                new Vector2(0.078f, 0.05f), new Vector2(0.055f, 0.028f), new Vector2(0.02f, 0.01f), new Vector2(0f, 0.012f)
            }, 20, 1.6f, false, false), mats.Wood, Vector3.zero, Quaternion.identity, true, true);
            MeshLab.BoxObject("ScoopHandle", scoop, new Vector3(0.035f, 0.2f, 0.035f), 0.01f, mats.Wood, new Vector3(0f, 0.13f, 0f), 2.5f, true);

            AddCollider(bin, "BinCollider", basePos + new Vector3(0f, 0.32f, 0f), new Vector3(0.8f, 0.64f, 0.7f));
            Shadow(basePos, 1.15f, 1.05f, 0.26f);

            refs.PowderAnchor = new GameObject("PowderAnchor").transform;
            refs.PowderAnchor.SetParent(bin, false);
            refs.PowderAnchor.position = basePos + new Vector3(0f, 0.56f, 0f);
        }

        void BuildStoneBasket()
        {
            var basket = new GameObject("StoneBasket").transform;
            basket.SetParent(world, false);
            var pos = new Vector3(-1.92f, 0f, 2.18f);
            MeshLab.MeshObject("BasketBody", basket, MeshLab.Lathe(new[]
            {
                new Vector2(0.05f, 0f), new Vector2(0.26f, 0.02f), new Vector2(0.35f, 0.18f), new Vector2(0.36f, 0.2f),
                new Vector2(0.33f, 0.21f), new Vector2(0.28f, 0.16f), new Vector2(0.06f, 0.04f)
            }, 28, 1.4f, true, false), mats.Basket, pos, Quaternion.identity, true, true);
            var rr = new System.Random(441);
            for (int i = 0; i < 7; i++)
            {
                float a = (float)rr.NextDouble() * Mathf.PI * 2f;
                float r = 0.06f + (float)rr.NextDouble() * 0.2f;
                float s = 0.06f + (float)rr.NextDouble() * 0.08f;
                var rock = MeshLab.MeshObject("RawStone" + i, basket, MeshLab.Rock(1f, 1300 + i, 0.65f, 14, 8), mats.RawStone,
                    pos + new Vector3(Mathf.Cos(a) * r, 0.14f + (float)rr.NextDouble() * 0.08f, Mathf.Sin(a) * r),
                    Quaternion.Euler((float)rr.NextDouble() * 20f, (float)rr.NextDouble() * 360f, 0f), true, true);
                rock.transform.localScale = new Vector3(s, s * 0.65f, s);
            }
            AddCollider(basket, "StoneCollider", pos + new Vector3(0f, 0.2f, 0f), new Vector3(0.72f, 0.4f, 0.72f));
            Shadow(pos, 0.95f, 0.95f, 0.22f);
        }

        void BuildShelf()
        {
            var shelf = new GameObject("BackShelf").transform;
            shelf.SetParent(world, false);
            float z = 2.92f;
            float y0 = 0.45f;
            for (int i = 0; i < 3; i++)
            {
                float x = -1.6f + i * 1.45f;
                MeshLab.BoxObject("ShelfPost" + i, shelf, new Vector3(0.08f, 1.8f, 0.3f), 0.01f, mats.Wood,
                    new Vector3(x, 0.9f, z), 1.5f, true);
            }
            for (int i = 0; i < 2; i++)
            {
                float y = y0 + i * 0.55f;
                MeshLab.BoxObject("ShelfBoard" + i, shelf, new Vector3(3.15f, 0.065f, 0.34f), 0.01f, mats.Wood,
                    new Vector3(-0.15f, y, z), 1.3f, true);
            }
            // folded linen and neutral covered lumps: material storage, no later-stage equipment is implied
            for (int i = 0; i < 3; i++)
            {
                var cloth = MeshLab.MeshObject("FoldedLinen" + i, shelf, MeshLab.Cloth(0.33f, 0.26f, 0.06f, 12, i * 2.1f), mats.Linen,
                    new Vector3(-1.28f + i * 0.46f, y0 + 0.09f, z - 0.02f), Quaternion.Euler(0f, i * 17f, 0f), false, true);
                cloth.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            for (int i = 0; i < 3; i++)
            {
                var lump = MeshLab.MeshObject("ClayLump" + i, shelf, MeshLab.Rock(1f, 1500 + i, 0.55f, 16, 8), mats.DryClay,
                    new Vector3(-0.55f + i * 0.38f, y0 + 0.6f, z + 0.02f), Quaternion.identity, false, true);
                lump.transform.localScale = new Vector3(0.16f, 0.1f, 0.14f);
            }
            AddCollider(shelf, "ShelfCollider", new Vector3(-0.15f, 0.9f, z), new Vector3(3.2f, 1.8f, 0.34f));
        }

        void BuildStool()
        {
            var stool = new GameObject("Stool").transform;
            stool.SetParent(world, false);
            var pos = new Vector3(1.32f, 0f, 0.16f);
            MeshLab.BoxObject("StoolSeat", stool, new Vector3(0.42f, 0.07f, 0.34f), 0.02f, mats.Wood, pos + new Vector3(0f, 0.42f, 0f), 1.5f, true);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    MeshLab.BoxObject("StoolLeg", stool, new Vector3(0.05f, 0.4f, 0.05f), 0.01f, mats.Wood,
                        pos + new Vector3(sx * 0.15f, 0.2f, sz * 0.12f), 2.2f, true);
                }
            }
            AddCollider(stool, "StoolCollider", pos + new Vector3(0f, 0.22f, 0f), new Vector3(0.44f, 0.44f, 0.36f));
            Shadow(pos, 0.7f, 0.6f, 0.18f);
        }

        void BuildWallTools()
        {
            var wall = new GameObject("WallTools").transform;
            wall.SetParent(world, false);
            MeshLab.BoxObject("PegRail", wall, new Vector3(0.045f, 0.48f, 1.2f), 0.008f, mats.Wood,
                new Vector3(2.31f, 1.38f, -0.62f), 1.6f, true);
            for (int i = 0; i < 3; i++)
            {
                MeshLab.BoxObject("Peg" + i, wall, new Vector3(0.1f, 0.028f, 0.028f), 0.006f, mats.Bamboo,
                    new Vector3(2.24f, 1.57f, -1.0f + i * 0.4f), 2.4f, true);
            }

            var apron = MeshLab.MeshObject("HangingApron", wall, MeshLab.Cloth(0.52f, 0.42f, 0.035f, 16, 1.4f), mats.Linen,
                new Vector3(2.25f, 1.09f, -0.63f), Quaternion.Euler(0f, 0f, 90f), false, true);
            var ar = apron.GetComponent<Renderer>();
            ar.shadowCastingMode = ShadowCastingMode.Off;
            MeshLab.Tint(ar, new Color(0.8f, 0.77f, 0.7f));
            MeshLab.MeshObject("ApronStrapA", wall, MeshLab.Tube(
                new[] { new Vector3(2.24f, 1.55f, -0.82f), new Vector3(2.24f, 1.34f, -0.73f), new Vector3(2.24f, 1.22f, -0.7f) },
                new[] { 0.012f, 0.01f, 0.008f }, 8, false), mats.Linen, Vector3.zero, Quaternion.identity, false, true);
            MeshLab.MeshObject("ApronStrapB", wall, MeshLab.Tube(
                new[] { new Vector3(2.24f, 1.55f, -0.44f), new Vector3(2.24f, 1.34f, -0.53f), new Vector3(2.24f, 1.22f, -0.56f) },
                new[] { 0.012f, 0.01f, 0.008f }, 8, false), mats.Linen, Vector3.zero, Quaternion.identity, false, true);
        }

        void BuildBroom()
        {
            var broom = new GameObject("Broom").transform;
            broom.SetParent(world, false);
            broom.localPosition = new Vector3(-2.1f, 0f, 2.62f);
            broom.localRotation = Quaternion.Euler(0f, 0f, -12f);
            MeshLab.BoxObject("BroomHandle", broom, new Vector3(0.035f, 1.45f, 0.035f), 0.01f, mats.Bamboo,
                new Vector3(0f, 0.76f, 0f), 2.2f, true);
            MeshLab.MeshObject("BroomHead", broom, MeshLab.Lathe(new[]
            {
                new Vector2(0.02f, 0f), new Vector2(0.15f, 0.02f), new Vector2(0.17f, 0.18f),
                new Vector2(0.06f, 0.3f), new Vector2(0.02f, 0.31f)
            }, 18, 1.5f, true, false), mats.Straw, new Vector3(0f, 0f, 0f), Quaternion.identity, true, true);
        }

        void BuildTailings()
        {
            var bucket = MakeBucket(world, "TailingsBucket", new Vector3(-1.18f, 0.01f, 0.96f), 0.75f, false);
            refs.TailingsBucket = bucket;
            var pile = MeshLab.MeshObject("CoarseTailings", bucket, MeshLab.Rock(1f, 1601, 0.2f, 18, 8), mats.RawStone,
                new Vector3(0.02f, 0.26f, 0f), Quaternion.identity, false, true);
            pile.transform.localScale = new Vector3(0.16f, 0.06f, 0.14f);
            pile.SetActive(false);
            refs.TailingsPile = pile.transform;
            AddCollider(bucket, "TailingsCollider", new Vector3(0f, 0.2f, 0f), new Vector3(0.4f, 0.4f, 0.4f));
            Shadow(new Vector3(-1.18f, 0.007f, 0.96f), 0.7f, 0.7f, 0.16f);
        }

        void BuildClayTray()
        {
            var tray = new GameObject("ClayTray").transform;
            tray.SetParent(world, false);
            var pos = new Vector3(1.8f, 0f, 0.5f);
            MeshLab.BoxObject("TrayTop", tray, new Vector3(0.62f, 0.06f, 0.46f), 0.015f, mats.Wood, pos + new Vector3(0f, 0.38f, 0f), 1.5f, true);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    MeshLab.BoxObject("TrayLeg", tray, new Vector3(0.055f, 0.36f, 0.055f), 0.01f, mats.Wood,
                        pos + new Vector3(sx * 0.24f, 0.18f, sz * 0.16f), 2.2f, true);
                }
            }
            var cloth = MeshLab.MeshObject("TrayCloth", tray, MeshLab.Cloth(0.55f, 0.4f, 0.03f, 14, 7f), mats.Linen,
                pos + new Vector3(0f, 0.42f, 0f), Quaternion.identity, false, true);
            cloth.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            refs.ClayTray = tray.transform;
            AddCollider(tray, "TrayCollider", pos + new Vector3(0f, 0.2f, 0f), new Vector3(0.64f, 0.42f, 0.48f));

            for (int i = 0; i < 3; i++)
            {
                var lump = MeshLab.MeshObject("CollectedClay" + i, tray, MeshLab.Rock(1f, 1700 + i, 0.6f, 16, 8), mats.WetClay,
                    pos + new Vector3(-0.13f + i * 0.13f, 0.47f + (i % 2) * 0.03f, 0.02f), Quaternion.Euler(0f, i * 40f, 0f), false, true);
                lump.transform.localScale = new Vector3(0.11f, 0.08f, 0.1f);
                MeshLab.Tint(lump.GetComponent<Renderer>(), new Color(1.25f, 1.18f, 1.05f));
                lump.SetActive(false);
                refs.TrayLumps.Add(lump.transform);
            }
            Shadow(pos, 0.9f, 0.75f, 0.18f);
        }

        void BuildLighting()
        {
            var lights = new GameObject("Lighting").transform;
            lights.SetParent(world, false);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.45f, 0.47f);
            RenderSettings.ambientEquatorColor = new Color(0.31f, 0.32f, 0.31f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.15f, 0.13f);
            RenderSettings.ambientIntensity = 0.68f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0075f;
            RenderSettings.fogColor = new Color(0.4f, 0.41f, 0.4f);
            QualitySettings.shadowDistance = 16f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.antiAliasing = 4;

            var sun = new GameObject("WindowSun").AddComponent<Light>();
            sun.transform.SetParent(lights, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.intensity = 0.95f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            sun.shadowBias = 0.018f;
            sun.shadowNormalBias = 0.22f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(0.62f, -0.52f, 0.25f).normalized);
            sun.cullingMask = ~(1 << ViewmodelRig.Layer);
            refs.WindowSun = sun;

            // A warm aperture light outside the window does the heavy lifting for the side light;
            // the directional above is the cool overcast fill that keeps the shadow side readable.
            var patch = new GameObject("WindowPatch").AddComponent<Light>();
            patch.transform.SetParent(lights, false);
            patch.type = LightType.Spot;
            patch.color = new Color(1f, 0.97f, 0.9f);
            patch.intensity = 2.45f;
            patch.range = 12f;
            patch.spotAngle = 62f;
            patch.innerSpotAngle = 38f;
            patch.shadows = LightShadows.Soft;
            patch.shadowStrength = 0.82f;
            patch.shadowBias = 0.02f;
            patch.shadowNormalBias = 0.25f;
            patch.transform.position = new Vector3(-4.1f, 2.95f, 0.05f);
            patch.transform.rotation = Quaternion.LookRotation(new Vector3(0.55f, 0.2f, 0.55f) - patch.transform.position);
            patch.cullingMask = ~(1 << ViewmodelRig.Layer);

            var workFill = new GameObject("WorkFill").AddComponent<Light>();
            workFill.transform.SetParent(lights, false);
            workFill.type = LightType.Spot;
            workFill.color = new Color(1f, 0.92f, 0.82f);
            workFill.intensity = 0.42f;
            workFill.range = 4.8f;
            workFill.spotAngle = 92f;
            workFill.innerSpotAngle = 55f;
            workFill.shadows = LightShadows.None;
            workFill.transform.position = new Vector3(0.1f, 2.35f, 0.3f);
            workFill.transform.rotation = Quaternion.LookRotation(new Vector3(-0.1f, -1f, 0.1f));
            workFill.cullingMask = ~(1 << ViewmodelRig.Layer);
            refs.WorkFill = workFill;

            var doorFill = new GameObject("DoorFill").AddComponent<Light>();
            doorFill.transform.SetParent(lights, false);
            doorFill.type = LightType.Point;
            doorFill.color = new Color(0.62f, 0.68f, 0.74f);
            doorFill.intensity = 0.35f;
            doorFill.range = 3.6f;
            doorFill.shadows = LightShadows.None;
            doorFill.transform.position = new Vector3(0.8f, 1.55f, -2.7f);
            doorFill.cullingMask = ~(1 << ViewmodelRig.Layer);

            // broad, cool sky bounce from the window side, no shadows: the operation area stays readable
            var windowBounce = new GameObject("WindowBounce").AddComponent<Light>();
            windowBounce.transform.SetParent(lights, false);
            windowBounce.type = LightType.Point;
            windowBounce.color = new Color(0.72f, 0.78f, 0.82f);
            windowBounce.intensity = 0.42f;
            windowBounce.range = 6.5f;
            windowBounce.shadows = LightShadows.None;
            windowBounce.transform.position = new Vector3(-1.7f, 1.7f, 0.15f);
            windowBounce.cullingMask = ~(1 << ViewmodelRig.Layer);

            var probeGO = new GameObject("ReflectionProbe", typeof(ReflectionProbe));
            probeGO.transform.SetParent(lights, false);
            probeGO.transform.position = new Vector3(0f, 1.0f, 0.3f);
            var probe = probeGO.GetComponent<ReflectionProbe>();
            probe.size = new Vector3(4.4f, 2.6f, 5.7f);
            probe.resolution = 128;
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(0.18f, 0.18f, 0.16f);
            probe.cullingMask = ~(1 << ViewmodelRig.Layer);
            probe.RenderProbe();
        }

        // --------------------------------------------------------------- interactables
        void BuildInteractables()
        {
            refs.PowderTarget = Target("AimPowder", refs.PowderAnchor.position + new Vector3(0f, 0.1f, 0f), new Vector3(0.72f, 0.55f, 0.62f), WorkshopAction.AddPowder);
            refs.WaterTarget = Target("AimWater", refs.BucketAnchor.position + new Vector3(0f, 0.15f, 0f), new Vector3(0.36f, 0.34f, 0.36f), WorkshopAction.AddWater);
            refs.StirTarget = Target("AimStir", refs.StirAnchor.position + new Vector3(0f, 0.16f, 0f), new Vector3(1.03f, 0.3f, 0.96f), WorkshopAction.Stir);
            refs.GateTarget = Target("AimGate", refs.GateAnchor.position, new Vector3(0.3f, 0.32f, 0.3f), WorkshopAction.OpenGate);
            refs.ClayTarget = Target("AimClay", refs.ClayAnchor.position + new Vector3(0f, 0.12f, 0f), new Vector3(0.82f, 0.3f, 0.76f), WorkshopAction.CollectClay);
            refs.ScrapeTarget = Target("AimScrape", refs.StirAnchor.position + new Vector3(0f, 0.1f, 0f), new Vector3(0.72f, 0.22f, 0.66f), WorkshopAction.ScrapeSand);
            refs.ScrapeTarget.gameObject.SetActive(false);
        }

        Interactable Target(string name, Vector3 pos, Vector3 size, WorkshopAction action)
        {
            var go = new GameObject(name);
            go.transform.SetParent(world, false);
            go.transform.position = pos;
            var c = go.AddComponent<BoxCollider>();
            c.size = size;
            c.isTrigger = true;
            var ia = go.AddComponent<Interactable>();
            ia.Action = action;
            ia.FocusPoint = pos;
            return ia;
        }

        // ---------------------------------------------------------------- helpers
        void AddCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var c = go.AddComponent<BoxCollider>();
            c.center = Vector3.zero;
            c.size = size;
        }

        void Shadow(Vector3 pos, float w, float d, float strength)
        {
            var go = MeshLab.MeshObject("ContactShadow", world, MeshLab.Plane(w, d, 1f), mats.InstantiateShadow(strength, 0.65f), pos, Quaternion.identity, false, false);
            go.GetComponent<Renderer>().sortingOrder = -3;
            contactShadows.Add(go);
        }

        void Label(Transform parent, string text, Vector3 position, Quaternion rotation, Color color, float size)
        {
            var go = new GameObject("Label_" + text, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = Vector3.one * (0.00115f * size);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 3;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(232f, 84f);

            var paperGo = new GameObject("Paper", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            paperGo.transform.SetParent(go.transform, false);
            var pr = (RectTransform)paperGo.transform;
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            var paper = paperGo.GetComponent<Image>();
            paper.color = new Color(242 / 255f, 237 / 255f, 226 / 255f, 0.95f);
            paper.raycastTarget = false;

            var lineGo = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineGo.transform.SetParent(go.transform, false);
            var lr = (RectTransform)lineGo.transform;
            lr.anchorMin = new Vector2(0, 1);
            lr.anchorMax = new Vector2(1, 1);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.offsetMin = new Vector2(0, -3);
            lr.offsetMax = Vector2.zero;
            var line = lineGo.GetComponent<Image>();
            line.color = new Color(43 / 255f, 71 / 255f, 110 / 255f, 0.9f);
            line.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var tr = (RectTransform)textGo.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(0, 4);
            tr.offsetMax = new Vector2(0, -5);
            var t = textGo.GetComponent<Text>();
            t.font = labelFont;
            t.fontSize = 27;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;
        }
    }
}


