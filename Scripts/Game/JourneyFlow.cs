using UnityEngine;

namespace CiCongNiSheng.Journey
{
    // Ordinals follow the supplied article, steps 5 through 8.
    public enum JourneyStep
    {
        LoadRawStones, RinseStones, RemoveDebris, TakeCleanStones,
        LoadMortars, OpenGate, WatchHammers, CloseGate, ScoopPowder, CarryPowder,
        AddWater, Stir, RemoveCoarse, ReturnCoarse, ReopenGate, WatchRegrind,
        CloseRegrindGate, TakeRegroundPowder, ReturnRegroundPowder, StirAgain, TakeRoughSlurry,
        CarrySlurry, LoadSieve, ShakeSieve, ScrapeSieve, InspectPool, Complete
    }

    public enum CargoKind { None, CleanStones, StonePowder, CoarseResidue, RegroundPowder, RoughSlurry }

    /// <summary>Material handoffs and chapter order, based on the user's article screenshot.</summary>
    public sealed class JourneyFlow
    {
        public JourneyStep Step { get; private set; }
        public CargoKind Material { get; private set; }
        public float Work { get; private set; }
        public int Scoops { get; private set; }
        public int Revision { get; private set; }
        public bool Reground { get; private set; }
        int scoopMask;
        public int Chapter => Step < JourneyStep.LoadMortars ? 0 : Step < JourneyStep.AddWater ? 1 : Step < JourneyStep.CarrySlurry ? 2 : 3;
        public int ArticleNumber => Chapter + 5;
        public bool Completed => Step == JourneyStep.Complete;
        public bool HasCargo => Material != CargoKind.None;
        public string Cargo => CargoNames[(int)Material];
        public float Progress => (int)Step / (float)JourneyStep.Complete;
        public string Objective => Tasks[(int)Step];
        public static readonly string[] Names = { "筛洗", "舂石", "淘洗", "过筛" };
        public static readonly string[] CargoNames = { "空手", "筛洗净石", "舂后瓷石粉", "淘洗粗渣 · 待返舂", "返舂石粉", "淘洗粗泥" };
        public static readonly int[] ChapterStarts = { 0, 4, 10, 21 };
        public static readonly int[] ChapterEnds = { 3, 9, 20, 25 };
        public static readonly string[] Concepts =
        {
            "人工清洗瓷石，去掉附着泥沙和杂物，提高原料纯洁度。此时处理的是石块，净石随后送去舂石。",
            "引水驱动水碓起落，将碓臼中的净石击打成粉。待关闸、轮碓停稳后再取粉，送往淘洗池。",
            "把舂后石粉浸入淘洗池，用泥耙反复淘洗，弃去粗渣、淘成粗泥。粗渣返回碓臼继续舂打，再回到淘洗池。",
            "淘洗后的粗泥过筛，细料随浆水进入沉淀池，细渣留在筛上并清除。本段体验在过筛入池后结束。"
        };
        public static readonly string[] Tasks =
        {
            "在第 05 工位，把待洗瓷石放进竹筛。",
            "对准竹筛，按住 E 摇洗，或按住左键左右移动鼠标。",
            "将筛中杂物挑出，放入旁边的废杂物盘。",
            "从竹筛取走净石，送到第 06 工位碓臼。",
            "待轮碓停稳，把净石分放进两只碓臼。",
            "到水轮旁，按住 E 拉起进水闸板。",
            "退到工位前方标记处守碓，观察净石逐渐变成石粉。",
            "两臼已舂细。按住 E 关闸，等待轮碓停稳。",
            "对准两只碓臼，分别按住 E 舀取石粉。",
            "把石粉送到第 07 工位淘洗池，按住 E 倒入。",
            "在淘洗池旁取清水，按住 E 把石粉浸湿。",
            "对准池中的泥耙，按住 E 淘洗，或按住左键移动鼠标。",
            "用泥耙分出池中粗渣，装入返舂桶。",
            "携粗渣返回第 06 工位碓臼，停稳后倒入第一只臼。",
            "拉起水闸，让水碓继续舂打粗渣。",
            "退到工位前方标记处守碓，观察粗渣再次变细。",
            "返舂完成。关闸，等待轮碓停稳。",
            "从第一只碓臼舀取返舂石粉。",
            "把返舂石粉送回原淘洗池。",
            "用泥耙再次淘洗，把回收石粉混入粗泥。",
            "从淘洗池装取粗泥，准备送去过筛。",
            "把淘洗粗泥送到第 08 工位过筛台，倒入待筛料缸。",
            "对准过筛台，按住 E 舀取粗泥上筛。",
            "按住 E 轻摇筛面，让细料随浆水流入沉淀池。",
            "对准筛面按 R，把截留的细渣刮到弃渣盘。",
            "对准筛下沉淀池，按住 E 检查过筛结果。",
            "第 5—8 步完成：过筛浆已入沉淀池。"
        };

        public void Reset()
        {
            Step = JourneyStep.LoadRawStones;
            Material = CargoKind.None;
            Work = 0f;
            Scoops = scoopMask = 0;
            Reground = false;
            Revision++;
        }

        public bool AddWork(JourneyStep expected, float amount)
        {
            if (Step != expected || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            Work = Mathf.Clamp01(Work + amount);
            return Work >= 1f;
        }

        public bool Advance(JourneyStep expected)
        {
            if (Step != expected || Completed) return false;
            if ((expected == JourneyStep.RinseStones || expected == JourneyStep.Stir || expected == JourneyStep.StirAgain) && Work < 1f) return false;
            if (expected == JourneyStep.ScoopPowder && Scoops != 2) return false;
            CargoKind required = CargoKind.None;
            if (expected == JourneyStep.LoadMortars) required = CargoKind.CleanStones;
            if (expected == JourneyStep.CarryPowder) required = CargoKind.StonePowder;
            if (expected == JourneyStep.ReturnCoarse) required = CargoKind.CoarseResidue;
            if (expected == JourneyStep.ReturnRegroundPowder) required = CargoKind.RegroundPowder;
            if (expected == JourneyStep.CarrySlurry) required = CargoKind.RoughSlurry;
            if (required != CargoKind.None && Material != required) return false;
            if (expected == JourneyStep.TakeRoughSlurry && !Reground) return false;
            if (required != CargoKind.None) Material = CargoKind.None;
            if (expected == JourneyStep.TakeCleanStones) Material = CargoKind.CleanStones;
            if (expected == JourneyStep.ScoopPowder) Material = CargoKind.StonePowder;
            if (expected == JourneyStep.RemoveCoarse) Material = CargoKind.CoarseResidue;
            if (expected == JourneyStep.TakeRegroundPowder) Material = CargoKind.RegroundPowder;
            if (expected == JourneyStep.ReturnRegroundPowder) Reground = true;
            if (expected == JourneyStep.TakeRoughSlurry) Material = CargoKind.RoughSlurry;
            Step++;
            Work = 0f;
            Revision++;
            return true;
        }

        public bool AddScoop(int mortarIndex)
        {
            if (Step != JourneyStep.ScoopPowder || mortarIndex < 0 || mortarIndex > 1) return false;
            int bit = 1 << mortarIndex;
            if ((scoopMask & bit) != 0) return false;
            scoopMask |= bit;
            Scoops++;
            Material = CargoKind.StonePowder;
            Revision++;
            if (Scoops == 2) Advance(JourneyStep.ScoopPowder);
            return true;
        }
    }
}
