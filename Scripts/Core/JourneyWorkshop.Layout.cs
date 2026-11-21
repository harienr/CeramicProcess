using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CiCongNiSheng.Journey
{
    public sealed partial class JourneyWorkshop
    {
        public Transform HammerSpace { get; private set; }
        public Vector3 ObservePosition => HammerSpace.TransformPoint(new Vector3(5.55f, 0.06f, 3.65f));
        public Vector3 GateControlPosition { get; private set; }
        public bool ClearRendering { get; private set; } = true;
        public const float HallMinX = -2f;
        public const float HallMaxX = 35f;
        public const float HallMinZ = -4.8f;
        public const float HallMaxZ = 8.5f;
        BoxCollider runningBarrier;
        readonly Dictionary<JourneyAction, Vector3> approaches = new Dictionary<JourneyAction, Vector3>();
        readonly Vector3[] stationCenters = { new Vector3(3, 0, 1.5f), new Vector3(11, 0, 1.5f), new Vector3(21, 0, 1.47f), new Vector3(28, 0, 1.5f) };

        Transform LegacyGroup(string prefix)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(prefix)) return t;
            return null;
        }

        void RelocateHammerAssembly()
        {
            HammerSpace = new GameObject("06 · 完整水碓机构").transform;
            HammerSpace.SetParent(transform, false);
            var gateGroup = LegacyGroup("08_水闸");
            foreach (var prefix in new[] { "06_水碓", "07_碓", "08_水闸", "09_水" })
            {
                var group = LegacyGroup(prefix);
                if (group != null) group.SetParent(HammerSpace, true);
            }
            HammerSpace.rotation = Quaternion.Euler(0, -90, 0);
            HammerSpace.position = stationCenters[1] - HammerSpace.rotation * new Vector3(7.1f, 0, 3.65f);
            // Put the gate beside the mortars, facing the same unobstructed front aisle.
            GateControlPosition = new Vector3(14.25f, 1.4f, 0.8f);
            if (gateGroup != null)
                gateGroup.position += GateControlPosition - Gate.board.TransformPoint(new Vector3(0, 1.05f, 0));
            var rail = HammerSpace.GetComponentsInChildren<Transform>(true);
            foreach (var t in rail)
                if (t.name == "护栏" || t.name.StartsWith("工序牌_") || t.name.StartsWith("指引_")) t.gameObject.SetActive(false);
            var barrier = new GameObject("开碓时隔离操作区");
            barrier.transform.SetParent(transform, false);
            barrier.transform.position = new Vector3(11, 0.65f, 0.66f);
            runningBarrier = barrier.AddComponent<BoxCollider>();
            runningBarrier.size = new Vector3(3.5f, 1.3f, 0.1f);
            runningBarrier.enabled = false;
        }

        void BuildLinearHall()
        {
            var hall = new GameObject("一字长工坊 · 前侧连续通道").transform;
            hall.SetParent(transform, false);
            float width = HallMaxX - HallMinX;
            float depth = HallMaxZ - HallMinZ;
            var floor = ProcMesh.Place("石地", hall, ProcMesh.Plane(width, depth, 0.09f, 12, 5), style.Floor,
                new Vector3((HallMaxX + HallMinX) * 0.5f, 0, (HallMaxZ + HallMinZ) * 0.5f)).gameObject;
            var floorCol = floor.AddComponent<BoxCollider>();
            floorCol.size = new Vector3(width, 0.2f, depth);
            floorCol.center = new Vector3(0, -0.1f, 0);
            HallBox(hall, "后墙", new Vector3(width, 4.6f, 0.22f), new Vector3(16.5f, 2.3f, HallMaxZ), style.Wall);
            HallBox(hall, "左墙", new Vector3(0.22f, 4.6f, depth), new Vector3(HallMinX, 2.3f, 1.85f), style.Wall);
            HallBox(hall, "右墙", new Vector3(0.22f, 4.6f, depth), new Vector3(HallMaxX, 2.3f, 1.85f), style.Wall);
            var frontBoundary = new GameObject("前沿边界");
            frontBoundary.transform.SetParent(hall, false);
            frontBoundary.transform.localPosition = new Vector3(16.5f, 1.4f, HallMinZ);
            frontBoundary.AddComponent<BoxCollider>().size = new Vector3(width, 2.8f, 0.15f);
            for (int i = 0; i < 8; i++)
            {
                float x = -1.5f + i * 5.1f;
                HallBox(hall, "后檐柱_" + i, new Vector3(0.19f, 4.6f, 0.19f), new Vector3(x, 2.3f, 7.95f), style.Beam);
                HallBox(hall, "前檐柱_" + i, new Vector3(0.19f, 4.6f, 0.19f), new Vector3(x, 2.3f, -4.35f), style.Beam);
                HallBox(hall, "横梁_" + i, new Vector3(0.19f, 0.23f, 12.7f), new Vector3(x, 4.5f, 1.8f), style.Beam, false);
            }
            HallBox(hall, "屋面", new Vector3(width + 0.4f, 0.14f, depth + 0.4f), new Vector3(16.5f, 4.78f, 1.85f), style.Standard("ceiling", Palette.Darken(Palette.RiceWhite, 0.16f), ProcTex.Plaster(), null, 0, 0.04f, 0), false);
            var sun = new GameObject("敞口日光").AddComponent<Light>();
            sun.transform.SetParent(hall, false);
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0);
            sun.type = LightType.Directional;
            sun.intensity = 1.08f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            sun.shadowBias = 0.025f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.69f, 0.71f);
            RenderSettings.ambientGroundColor = new Color(0.31f, 0.30f, 0.28f);
            RenderSettings.ambientIntensity = 1f;
            foreach (var center in stationCenters)
            {
                var fill = new GameObject("操作面漫射光").AddComponent<Light>();
                fill.transform.SetParent(hall, false);
                fill.transform.position = center + new Vector3(0, 3f, -1.4f);
                fill.type = LightType.Point;
                fill.range = 7f;
                fill.intensity = 0.75f;
                fill.color = new Color(1f, 0.97f, 0.92f);
                fill.shadows = LightShadows.None;
                ProcMesh.Place("就位标记", hall, ProcMesh.Torus(0.34f, 0.008f, 48, 8), style.Standard("guide", Palette.Cobalt, null, null, 0, 0.1f, 0),
                    new Vector3(center.x, 0.018f, -0.65f));
            }
            for (float x = 4.8f; x < 27; x += 1.7f)
            {
                HallBox(hall, "工序方向", new Vector3(0.16f, 0.006f, 0.035f), new Vector3(x, 0.015f, -2.65f), style.Standard("guide", Palette.Cobalt, null, null, 0, 0.1f, 0), false);
            }
            // A shallow stone race remains visually connected to the waterwheel behind the working line.
            HallBox(hall, "水道底", new Vector3(1.2f, 0.15f, 6.2f), new Vector3(15.5f, -0.01f, 4.6f), style.Ceramic);
            ProcMesh.Place("水道水面", hall, ProcMesh.Plane(1.05f, 6f), style.Water, new Vector3(15.5f, 0.14f, 4.6f));
        }

        void HallBox(Transform parent, string name, Vector3 size, Vector3 position, Material material, bool collision = true)
        {
            var go = ProcMesh.Place(name, parent, ProcMesh.Box(size, 0.012f), material, position).gameObject;
            if (collision) go.AddComponent<BoxCollider>().size = size;
        }

        void ArrangeTaoxiTools()
        {
            var table = Taoxi.Root.Find("WorkTable");
            if (table != null)
            {
                table.localRotation = Quaternion.Euler(0, 90, 0);
                table.localPosition = new Vector3(1.57f, 0, 2.22f);
            }
        }

        void StationSign(string heading, string detail, float x)
        {
            var post = new GameObject(heading + " · 工序牌").transform;
            post.SetParent(transform, false);
            HallBox(post, "牌柱", new Vector3(0.055f, 2.25f, 0.055f), new Vector3(x + 1.7f, 1.125f, 3.1f), style.Beam, false);
            HallBox(post, "木牌", new Vector3(2.1f, 0.64f, 0.07f), new Vector3(x, 2.1f, 3.1f), style.Wall, false);
            Sign(heading + "\n" + detail, new Vector3(x, 2.1f, 3.045f), 0.021f);
        }

        public void ApplyClarity(bool clear)
        {
            ClearRendering = clear;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.antiAliasing = clear ? 4 : 2;
            QualitySettings.pixelLightCount = clear ? 8 : 4;
            QualitySettings.shadowResolution = clear ? ShadowResolution.VeryHigh : ShadowResolution.Medium;
            QualitySettings.shadowDistance = clear ? 45f : 25f;
            QualitySettings.shadowCascades = clear ? 4 : 2;
            QualitySettings.lodBias = clear ? 2f : 1f;
            ScalableBufferManager.ResizeBuffers(1f, 1f);
            if (Camera != null)
            {
                Camera.allowDynamicResolution = false;
                Camera.allowMSAA = true;
                Camera.allowHDR = false;
                Camera.renderingPath = RenderingPath.Forward;
            }
            foreach (var texture in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                if (texture.width < 128 || texture.height < 128 || texture.name.Contains("Font")) continue;
                texture.anisoLevel = clear ? 16 : 4;
                texture.filterMode = FilterMode.Trilinear;
            }
            RenderSettings.fog = false;
        }

        public void UseNativeResolution()
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExecuteMenuItem("瓷从泥生/整合工坊/5 · 清晰 Game 视图（1x）");
#endif
                HUD.Toast("Game 视图已切换原生比例预览。可在顶部确认 Scale 为 1x。");
                return;
            }
            int w = Display.main.systemWidth;
            int h = Display.main.systemHeight;
            if (w >= 1280 && h >= 720) Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
        }

        public bool TryGetApproach(JourneyAction action, out Vector3 position) => approaches.TryGetValue(action, out position);

        void RegisterApproaches()
        {
            foreach (var target in Targets)
            {
                Vector3 point = target.transform.position;
                switch (target.action)
                {
                    case JourneyAction.RawStones: point = new Vector3(StoneWash.RawPoint.position.x, 0.06f, -0.25f); break;
                    case JourneyAction.StoneSieve:
                    case JourneyAction.Debris:
                    case JourneyAction.CleanStones: point = new Vector3(3f, 0.06f, -0.25f); break;
                    case JourneyAction.LoadMortars:
                    case JourneyAction.MortarA:
                    case JourneyAction.ReturnCoarse: point = new Vector3(Mortars[0].transform.position.x, 0.06f, -0.3f); break;
                    case JourneyAction.MortarB: point = new Vector3(Mortars[1].transform.position.x, 0.06f, -0.3f); break;
                    case JourneyAction.Gate: point = new Vector3(GateControlPosition.x, 0.06f, -1f); break;
                    case JourneyAction.Water: point = new Vector3(Taoxi.BucketAnchor.position.x, 0.06f, 0.15f); break;
                    case JourneyAction.PowderVat:
                    case JourneyAction.StirVat:
                    case JourneyAction.Coarse:
                    case JourneyAction.RoughSlurry: point = new Vector3(21f, 0.06f, -0.2f); break;
                    case JourneyAction.SieveInput: point = new Vector3(Sieve.tub.transform.position.x, 0.06f, -0.65f); break;
                    case JourneyAction.Sieve: point = new Vector3(28f, 0.06f, -0.5f); break;
                    case JourneyAction.Pool: point = new Vector3(28f, 0.06f, -0.95f); break;
                }
                approaches[target.action] = point;
            }
        }

        public bool AssistCurrentStation(bool requireNearby = true)
        {
            if (Modal || Busy || Flow.Completed) return false;
            Vector3 goal = NavigationPosition;
            var delta = goal - Player.transform.position;
            delta.y = 0;
            if (requireNearby && delta.magnitude > 5f) { HUD.Toast("先沿通道靠近当前工位，再按 F 就位。"); return false; }
            Vector3 feet = ObservePosition;
            Vector3 focus = (Mortars[0].transform.position + Mortars[1].transform.position) * 0.5f + Vector3.up * 0.7f;
            if (Flow.Step != JourneyStep.WatchHammers && Flow.Step != JourneyStep.WatchRegrind)
            {
                JourneyInteractable current = null;
                foreach (var target in Targets) if (IsRelevant(target.action)) { current = target; break; }
                if (current == null || !TryGetApproach(current.action, out feet)) return false;
                focus = current.transform.position;
            }
            // Reposition only to a clear, authored place in front of the current station.
            if (Physics.CheckCapsule(feet + Vector3.up * 0.31f, feet + Vector3.up * 1.41f, 0.27f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            { HUD.Toast("操作位暂时被遮挡，请沿通道靠近器具。"); return false; }
            Vector3 look = focus - (feet + Vector3.up * 1.62f);
            float yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(look.y, new Vector2(look.x, look.z).magnitude) * Mathf.Rad2Deg;
            MovePlayer(feet, yaw, pitch);
            Player.ClearInteractionLock();
            return true;
        }

        void UpdateRunningBarrier()
        {
            if (runningBarrier != null) runningBarrier.enabled = Gate.OpenAmount > 0.15f || !Wheel.IsSettled;
        }
    }
}