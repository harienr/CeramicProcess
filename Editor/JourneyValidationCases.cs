using System;
using UnityEngine;
using CiCongNiSheng.Journey;

namespace CiCongNiSheng.EditorTools
{
    public static class JourneyValidationCases
    {
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

        public static void CheckMaterialDisplays(JourneyWorkshop world)
        {
            var step = world.Flow.Step;
            bool regrinding = step >= JourneyStep.ReopenGate && step <= JourneyStep.TakeRegroundPowder;
            bool bucketsTaken = (step >= JourneyStep.CarryPowder && step <= JourneyStep.ReturnCoarse) || step >= JourneyStep.ReturnRegroundPowder;
            for (int i = 0; i < world.Mortars.Length; i++)
            {
                var mortar = world.Mortars[i];
                if (!mortar.MaterialLoaded || mortar.Scooped)
                {
                    Require(!mortar.pile.gameObject.activeInHierarchy && !mortar.stones.gameObject.activeInHierarchy, "取粉后碓臼仍有物料：" + i);
                }
                else if (regrinding || mortar.IsDone)
                {
                    Require(!mortar.stones.gameObject.activeInHierarchy && mortar.pile.gameObject.activeInHierarchy, "粉料或返舂粗渣仍显示为石块：" + i);
                }
                bool showBucket = !bucketsTaken && (regrinding ? i == 0 : !mortar.Scooped);
                Require(mortar.bucket.gameObject.activeInHierarchy == showBucket, "碓臼旁交接桶重复留存或未备好：" + i + " @ " + step);
                Require(mortar.bucket.fill == 0f && !mortar.bucket.powder.gameObject.activeInHierarchy, "已取走的石粉仍留在原桶：" + i);
            }
            var cargo = world.Camera.transform.Find("原料交接桶");
            Require(cargo != null && cargo.gameObject.activeSelf == world.Flow.HasCargo, "手持交接桶与携带状态不同步");
            if (step == JourneyStep.ScoopPowder && world.Flow.Scoops == 1)
                Require(world.Flow.HasCargo, "第一臼石粉取走后没有出现在手持桶中");
            if (step >= JourneyStep.ReturnCoarse)
                Require(!world.Taoxi.TailingsBucket.gameObject.activeInHierarchy && !world.Taoxi.TailingsPile.gameObject.activeSelf, "返舂桶与手持粗渣重复显示");
            var vat = world.Taoxi.UpperVat;
            if (step >= JourneyStep.Stir && step <= JourneyStep.TakeRoughSlurry)
                Require(vat.CurrentFill > 0.8f && vat.WaterSurface.gameObject.activeInHierarchy, "只取粗渣时提前清空了淘洗池");
            if (step >= JourneyStep.CarrySlurry)
                Require(vat.CurrentFill == 0f && !vat.WaterSurface.gameObject.activeInHierarchy &&
                    !vat.PowderMound.gameObject.activeInHierarchy && !vat.CoarseGroup.gameObject.activeInHierarchy &&
                    !vat.Sediment.gameObject.activeInHierarchy && !vat.ClayLayer.gameObject.activeInHierarchy,
                    "粗泥已收取，淘洗池仍残留液面或粉料");
        }

        public static string RunFlow()
        {
            var flow = new JourneyFlow();
            flow.Reset();
            Require(flow.ArticleNumber == 5 && JourneyFlow.Names[0] == "筛洗", "必须从第 5 步筛洗开始");
            Require(!flow.Advance(JourneyStep.CarryPowder), "允许跳过净石清洗");
            Require(!flow.AddScoop(0), "未舂石即可取粉");
            Require(!flow.AddWork(JourneyStep.Stir, 1f), "允许在清洗瓷石阶段淘泥浆");
            Require(!flow.AddWork(JourneyStep.LoadRawStones, float.NaN), "NaN 进入流程");
            Require(!flow.AddWork(JourneyStep.LoadRawStones, float.PositiveInfinity), "Infinity 进入流程");
            int[] entered = new int[4];
            foreach (JourneyStep step in Enum.GetValues(typeof(JourneyStep)))
            {
                if (step == JourneyStep.Complete) break;
                Require(flow.Step == step, "状态顺序错误：" + step);
                entered[flow.Chapter]++;
                if (step == JourneyStep.RinseStones || step == JourneyStep.Stir || step == JourneyStep.StirAgain)
                {
                    Require(!flow.Advance(step), "动作未完成就跳步：" + step);
                    Require(!flow.AddWork(step, 0.45f), "动作进度过早完成");
                    Require(flow.AddWork(step, 0.55f), "动作无法完成");
                }
                if (step == JourneyStep.ScoopPowder)
                {
                    Require(!flow.Advance(step), "未取两臼就交接");
                    Require(flow.AddScoop(0) && flow.Scoops == 1 && flow.Material == CargoKind.StonePowder && flow.Step == JourneyStep.ScoopPowder, "第一臼取粉后未显示携带石粉，或提前完成两臼取粉");
                    Require(!flow.Advance(step), "只取一臼就允许交接");
                    Require(!flow.AddScoop(0) && flow.Scoops == 1, "同一碓臼重复计数");
                    Require(!flow.AddScoop(2), "不存在的碓臼计数");
                    Require(flow.AddScoop(1), "第二臼无法取粉");
                    Require(flow.Material == CargoKind.StonePowder, "取出的不是石粉");
                }
                else Require(flow.Advance(step), "无法推进：" + step);
                if (step == JourneyStep.TakeCleanStones) Require(flow.Material == CargoKind.CleanStones, "筛洗产物必须为净石");
                if (step == JourneyStep.RemoveCoarse) Require(flow.Material == CargoKind.CoarseResidue, "淘洗粗渣丢失");
                if (step == JourneyStep.TakeRegroundPowder) Require(flow.Material == CargoKind.RegroundPowder, "返舂产物错误");
                if (step == JourneyStep.ReturnRegroundPowder) Require(flow.Reground, "返舂未回到淘洗池");
                if (step == JourneyStep.TakeRoughSlurry) Require(flow.Material == CargoKind.RoughSlurry, "送筛材料必须为淘洗粗泥");
                if (step == JourneyStep.LoadMortars || step == JourneyStep.CarryPowder || step == JourneyStep.ReturnCoarse || step == JourneyStep.ReturnRegroundPowder || step == JourneyStep.CarrySlurry)
                    Require(!flow.HasCargo, "交接后材料重复留存：" + step);
            }
            Require(flow.Completed && flow.ArticleNumber == 8 && !flow.HasCargo, "未以过筛入池结束");
            Require(!flow.Advance(JourneyStep.Complete), "完成后继续推进");
            Require(Array.TrueForAll(entered, count => count > 0), "有工序被遗漏");
            flow.Reset();
            Require(flow.Step == JourneyStep.LoadRawStones && !flow.HasCargo && !flow.Reground && flow.Scoops == 0, "重玩保留了旧进度");
            return "FLOW PASS: 05 筛洗 → 06 舂石 → 07 淘洗（粗渣返舂并回池）→ 08 过筛入沉淀池。\nMaterial handoffs, duplicate scoops, work gates, invalid input and reset verified.";
        }
    }
}
