using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CiCongNiSheng.Journey;

namespace CiCongNiSheng.EditorTools
{
    /// <summary>Runs the real stations in Play Mode and checks physical approaches from the shared walking area.</summary>
    internal sealed class JourneyPlaythrough
    {
        readonly Action<string> report;
        readonly Func<string[]> errors;
        readonly List<string> passed = new List<string>();
        readonly double start = EditorApplication.timeSinceStartup;
        readonly HashSet<JourneyAction> approached = new HashSet<JourneyAction>();
        readonly HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
        JourneyWorkshop world;
        JourneyStep? last;
        double stepStart;
        bool used;
        bool initialized;
        readonly HashSet<int> captures = new HashSet<int>();
        readonly HashSet<string> materialCaptures = new HashSet<string>();
        int lastFrame = -1;
        const float GridSize = 0.25f;
        const float MinX = -1.5f;
        const float MinZ = -4.4f;
        const int CountX = 145;
        const int CountZ = 50;

        public JourneyPlaythrough(Action<string> write, Func<string[]> readErrors) { report = write; errors = readErrors; }

        public bool Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (!EditorApplication.isPlaying) return false;
            world = JourneyWorkshop.Instance;
            if (world == null || !world.IsReady)
            {
                if (EditorApplication.timeSinceStartup - start > 45) return Finish(false, "场景未能初始化\n" + string.Join("\n", errors()));
                return false;
            }
            world.ValidationMode = true;
            if (world.Modal && !world.Flow.Completed) world.HUD.CloseBook();
            Time.timeScale = 3f;
            if (Time.frameCount == lastFrame) return false;
            lastFrame = Time.frameCount;
            if (EditorApplication.timeSinceStartup - start > 240) return Finish(false, "全流程超时");
            if (errors().Length > 0) return Finish(false, string.Join("\n", errors()));
            if (!initialized)
            {
                initialized = true;
                if (Camera.allCamerasCount != 1) return Finish(false, "活动相机数量不是 1");
                if (UnityEngine.Object.FindObjectsOfType<AudioListener>().Length != 1) return Finish(false, "活动 AudioListener 数量不是 1");
                if (world.Mortars.Any(m => m.MaterialLoaded)) return Finish(false, "未筛洗净石已预装进碓臼");
                world.SetModal(true);
                if (Time.timeScale != 0 || world.Movement.InputEnabled || world.Player.enabled) return Finish(false, "任务书未暂停移动与工序");
                world.HUD.ShowBook(true);
                JourneyRenderCapture.Save(world, "05-taskbook");
                world.HUD.CloseBook();
                world.SetModal(false);
                BuildReachableGrid();
                VerifyRowAndClarity();
                CaptureOverview();
                passed.Add("PASS single player / camera / listener; task book pause; initially empty mortars");
                passed.Add("PASS walkable area: " + reachable.Count + " connected grid cells");
                if (reachable.Count < 80) return Finish(false, "玩家出生点没有可通行空间");
            }
            var step = world.Flow.Step;
            if (last != step)
            {
                if (last != null) passed.Add("PASS " + last);
                last = step;
                used = false;
                stepStart = EditorApplication.timeSinceStartup;
                report("RUNNING\n" + string.Join("\n", passed) + "\nCurrent: " + step);
                if (!Application.isBatchMode && (step == JourneyStep.LoadRawStones || step == JourneyStep.OpenGate || step == JourneyStep.Stir || step == JourneyStep.ShakeSieve))
                    ScreenCapture.CaptureScreenshot("Assets/Docs/JourneyValidation/" + world.Flow.ArticleNumber + "-" + step + ".png");
                return false;
            }
            if (EditorApplication.timeSinceStartup - stepStart > 50) return Finish(false, "工序卡住：" + step + " / RPM " + world.Wheel.Rpm + " / strikes " + world.Mortars[0].Strikes + "," + world.Mortars[1].Strikes);
            if (world.Busy) return false;
            JourneyValidationCases.CheckMaterialDisplays(world);
            string materialCapture = step == JourneyStep.ScoopPowder && world.Flow.Scoops == 1 ? "material-first-scoop" :
                step == JourneyStep.CarryPowder ? "material-carry-powder" :
                step == JourneyStep.ReopenGate ? "material-regrind-loaded" :
                step == JourneyStep.ReturnRegroundPowder ? "material-regrind-collected" :
                step == JourneyStep.CarrySlurry ? "material-empty-washing-vat" : null;
            if (materialCapture != null && materialCaptures.Add(materialCapture)) JourneyRenderCapture.Save(world, materialCapture);
            if ((step == JourneyStep.RinseStones || step == JourneyStep.WatchHammers || step == JourneyStep.Stir || step == JourneyStep.ShakeSieve) && EditorApplication.timeSinceStartup - stepStart > 0.8 && captures.Add((int)step))
                JourneyRenderCapture.Save(world, world.Flow.ArticleNumber + "-" + step);
            switch (step)
            {
                case JourneyStep.LoadRawStones: Use(JourneyAction.RawStones); break;
                case JourneyStep.RinseStones:
                    Approach(JourneyAction.StoneSieve);
                    world.Rinse(Mathf.Max(Time.deltaTime, 0.01f), 1f);
                    break;
                case JourneyStep.RemoveDebris: Use(JourneyAction.Debris); break;
                case JourneyStep.TakeCleanStones: Use(JourneyAction.CleanStones); break;
                case JourneyStep.LoadMortars: Use(JourneyAction.LoadMortars); break;
                case JourneyStep.OpenGate:
                case JourneyStep.ReopenGate: Use(JourneyAction.Gate); break;
                case JourneyStep.WatchHammers:
                case JourneyStep.WatchRegrind: world.MovePlayer(world.ObservePosition, 0, 20); break;
                case JourneyStep.CloseGate:
                case JourneyStep.CloseRegrindGate:
                    if (world.Gate.targetOpen) { Approach(JourneyAction.Gate); world.Use(JourneyAction.Gate); }
                    break;
                case JourneyStep.ScoopPowder:
                    if (!world.Mortars[0].Scooped) { Approach(JourneyAction.MortarA); world.Use(JourneyAction.MortarA); }
                    else if (!world.Mortars[1].Scooped) { Approach(JourneyAction.MortarB); world.Use(JourneyAction.MortarB); }
                    break;
                case JourneyStep.CarryPowder:
                case JourneyStep.ReturnRegroundPowder: Use(JourneyAction.PowderVat); break;
                case JourneyStep.AddWater: Use(JourneyAction.Water); break;
                case JourneyStep.Stir:
                case JourneyStep.StirAgain:
                    Approach(JourneyAction.StirVat);
                    world.Stir(Mathf.Max(Time.deltaTime, 0.01f), 1f);
                    break;
                case JourneyStep.RemoveCoarse: Use(JourneyAction.Coarse); break;
                case JourneyStep.ReturnCoarse: Use(JourneyAction.ReturnCoarse); break;
                case JourneyStep.TakeRegroundPowder: Use(JourneyAction.MortarA); break;
                case JourneyStep.TakeRoughSlurry: Use(JourneyAction.RoughSlurry); break;
                case JourneyStep.CarrySlurry: Use(JourneyAction.SieveInput); break;
                case JourneyStep.LoadSieve:
                case JourneyStep.ShakeSieve:
                    Approach(JourneyAction.Sieve);
                    world.Sieve.st.Interact(0.03f, false, true);
                    break;
                case JourneyStep.ScrapeSieve:
                    Approach(JourneyAction.Sieve);
                    world.ScrapeFineResidue();
                    break;
                case JourneyStep.InspectPool: Use(JourneyAction.Pool); break;
                case JourneyStep.Complete:
                    if (!world.Flow.Reground || world.FinalPool.CurrentFill < 0.1f || world.FinalResidue < 0.9f)
                        return Finish(false, "缺少粗渣返舂、过筛入池或细渣分离");
                    passed.Add("PASS InspectPool / Complete");
                    passed.Add("PASS source buckets removed and cleared; partial powder carried; coarse powder regrind visuals; washing vat empty after collection");
                    return Finish(true, "05 → 06 → 07（真实返舂）→ 08 已完整运行。\nFront-approachable targets / F assist: " + approached.Count + "\nFinal pool: " + world.FinalPool.CurrentFill.ToString("0.00") + "\nFine residue cleared: " + world.FinalResidue.ToString("0.00"));
            }
            return false;
        }

        void Use(JourneyAction action)
        {
            if (used) return;
            if (!world.CanUse(action)) throw new Exception("不能执行 " + action + " @ " + world.Flow.Step);
            Approach(action);
            world.Use(action);
            used = true;
        }

        void Approach(JourneyAction action)
        {
            // Each state finds a connected, unobstructed standing point and an actual ray hit.
            if (used && (action == JourneyAction.StoneSieve || action == JourneyAction.StirVat || action == JourneyAction.Sieve)) return;
            var target = world.Targets.First(t => t.action == action);
            if (!target.gameObject.activeSelf) throw new Exception("目标未激活：" + action);
            if (!FindFrontApproach(target)) throw new Exception("目标无法从公共走道接近或被遮挡：" + target.label + " @ " + target.transform.position);
            if (!world.AssistCurrentStation()) throw new Exception("F 就位失败：" + action);
            approached.Add(action);
            if (action == JourneyAction.StoneSieve || action == JourneyAction.StirVat || action == JourneyAction.Sieve) used = true;
        }

        void VerifyRowAndClarity()
        {
            float[] centers = { world.StoneWash.Sieve.position.x,
                (world.Mortars[0].transform.position.x + world.Mortars[1].transform.position.x) * 0.5f,
                world.Taoxi.UpperVat.Root.position.x, world.Sieve.st.transform.position.x };
            for (int i = 1; i < centers.Length; i++)
                if (centers[i] - centers[i - 1] < 5f) throw new Exception("四工位未按顺序留足间距");
            for (float x = 0; x <= 33; x += 0.25f)
                if (!Standable(new Vector3(x, 0.08f, -2.5f))) throw new Exception("连续通道被阻断：x=" + x);
            if (world.Camera.allowDynamicResolution || !world.Camera.allowMSAA || QualitySettings.globalTextureMipmapLimit != 0 || QualitySettings.antiAliasing != 4)
                throw new Exception("清晰画质没有生效");
            world.HUD.ShowSettings();
            JourneyRenderCapture.Save(world, "display-settings");
            world.HUD.CloseBook();
            passed.Add("PASS 05–08 left-to-right row, minimum 5 m center spacing, clear 33 m front aisle");
            passed.Add("PASS native render scale, no dynamic resolution, full textures, 4x MSAA, settings panel");
        }

        void CaptureOverview() { JourneyRenderCapture.Layout(world); }

        bool FindFrontApproach(JourneyInteractable target)
        {
            if (!world.TryGetApproach(target.action, out var feet)) return false;
            if (!Standable(feet) || !reachable.Contains(Cell(feet))) return false;
            Vector3 eye = feet + Vector3.up * 1.62f;
            Vector3 delta = target.transform.position - eye;
            var hits = Physics.RaycastAll(eye, delta.normalized, world.Player.reach, ~(1 << 2), QueryTriggerInteraction.Collide).OrderBy(h => h.distance);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<JourneyInteractable>() == target)
                {
                    float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                    float pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
                    world.MovePlayer(feet, yaw, pitch);
                    return true;
                }
                if (!hit.collider.isTrigger) break;
            }
            return false;
        }
        void BuildReachableGrid()
        {
            var open = new bool[CountX, CountZ];
            for (int x = 0; x < CountX; x++)
            for (int z = 0; z < CountZ; z++) open[x, z] = Standable(Feet(new Vector2Int(x, z)));
            var startCell = Cell(world.Player.transform.position);
            if (!Valid(startCell) || !open[startCell.x, startCell.y])
            {
                bool found = false;
                for (int dx = -2; dx <= 2 && !found; dx++)
                for (int dz = -2; dz <= 2 && !found; dz++)
                {
                    var candidate = startCell + new Vector2Int(dx, dz);
                    if (Valid(candidate) && open[candidate.x, candidate.y]) { startCell = candidate; found = true; }
                }
                if (!found) return;
            }
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(startCell);
            reachable.Add(startCell);
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var delta in directions)
                {
                    var next = current + delta;
                    if (!Valid(next) || !open[next.x, next.y] || !reachable.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
        }

        bool Valid(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < CountX && c.y < CountZ;
        Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.RoundToInt((p.x - MinX) / GridSize), Mathf.RoundToInt((p.z - MinZ) / GridSize));
        Vector3 Feet(Vector2Int c) => new Vector3(MinX + c.x * GridSize, 0.08f, MinZ + c.y * GridSize);

        bool Standable(Vector3 feet)
        {
            int mask = ~(1 << 2);
            if (Physics.CheckCapsule(feet + Vector3.up * 0.31f, feet + Vector3.up * 1.41f, 0.27f, mask, QueryTriggerInteraction.Ignore)) return false;
            return Physics.Raycast(feet + Vector3.up * 0.12f, Vector3.down, out var hit, 0.45f, mask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.7f;
        }

        bool Finish(bool ok, string message)
        {
            Time.timeScale = 1f;
            report((ok ? "PLAYTHROUGH PASS" : "PLAYTHROUGH FAIL") + "\n" + JourneyValidationCases.RunFlow() + "\n" + string.Join("\n", passed) + "\n" + message +
                "\nRuntime errors: " + errors().Length + "\nDuration: " + (EditorApplication.timeSinceStartup - start).ToString("0.0") + " s");
            if (world != null && world.IsReady)
            {
                world.ValidationMode = false;
                world.HUD.ShowBook(false);
            }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            return true;
        }
    }
}
