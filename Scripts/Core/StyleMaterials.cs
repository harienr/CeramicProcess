using System.Collections.Generic;
using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 统一材质库。所有材质在此集中生成，颜色与粗糙度严格取自美术规范，
    /// 避免各处随手调色导致风格漂移。
    /// </summary>
    public class StyleMaterials
    {
        public Material Wall;
        public Material Floor;
        public Material Beam;
        public Material Bench;
        public Material ToolWood;
        public Material Bamboo;
        public Material Ceramic;
        public Material CeramicPale;
        public Material Slurry;
        public Material SlurryDry;
        public Material Coarse;
        public Material Cloth;
        public Material Metal;
        public Material Water;
        public Material SieveCloth;
        public Material Grime;
        public Material EmissiveWarm;
        public Material CursorDecal;

        readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        static Shader FindShader(string resourcePath, string shaderName)
        {
            var s = Resources.Load<Shader>(resourcePath);
            if (s == null) s = Shader.Find(shaderName);
            if (s == null) Debug.LogWarning("[瓷从泥生] 找不到着色器：" + shaderName + "，回退到 Standard");
            return s;
        }

        public void Init()
        {
            var woodTex = ProcTex.Wood(Palette.OldWood);
            var woodNormal = ProcTex.NormalFromTexture(woodTex, 2.4f);
            var floorTex = ProcTex.FloorStone();
            var floorNormal = ProcTex.NormalFromTexture(floorTex, 1.8f);
            var plasterTex = ProcTex.Plaster();
            var plasterNormal = ProcTex.NormalFromTexture(plasterTex, 1.2f);
            var ceramicTex = ProcTex.Ceramic(Palette.ClayGrey);
            var ceramicNormal = ProcTex.NormalFromTexture(ceramicTex, 1.6f);
            var slurryTex = ProcTex.Slurry();
            var slurryNormal = ProcTex.NormalFromTexture(slurryTex, 0.9f);
            var clothTex = ProcTex.Cloth();
            var metalTex = ProcTex.Metal();

            // 墙面：米白、哑光、几乎无高光
            Wall = Standard("wall", Palette.RiceWhite, plasterTex, plasterNormal, 0.5f, 0.04f, 0f);

            // 石地：干湿分区由贴图承担，整体略暗于墙
            Floor = Standard("floor", Color.white, floorTex, floorNormal, 0.55f, 0.10f, 0f, new Vector2(1.6f, 1.6f));

            // 旧木：梁、台面、工具柄共用一张木纹，靠色调区分
            Beam = Standard("beam", Palette.Darken(Palette.OldWood, 0.16f), woodTex, woodNormal, 0.75f, 0.16f, 0f);
            Bench = Standard("bench", Palette.OldWood, woodTex, woodNormal, 0.6f, 0.22f, 0f, new Vector2(1f, 1f));
            ToolWood = Standard("toolwood", Palette.Darken(Palette.OldWood, -0.10f), woodTex, woodNormal, 0.55f, 0.26f, 0f, new Vector2(2f, 2f));
            Bamboo = Standard("bamboo", Palette.Bamboo, woodTex, woodNormal, 0.4f, 0.24f, 0f, new Vector2(2.5f, 2.5f));

            // 陶缸：未上釉，哑光、略有旋纹
            Ceramic = Standard("ceramic", Palette.ClayGrey, ceramicTex, ceramicNormal, 0.7f, 0.14f, 0f);
            CeramicPale = Standard("ceramic_pale", Palette.Darken(Palette.RiceWhite, 0.10f), ceramicTex, ceramicNormal, 0.7f, 0.13f, 0f);

            // 湿泥浆：灰白、不透明、局部细腻反光（平滑度低，绝不是镜面）
            Slurry = Standard("slurry", Palette.SlurryWet, slurryTex, slurryNormal, 0.5f, 0.28f, 0f);
            // 干泥：哑光、细密
            SlurryDry = Standard("slurry_dry", Palette.SlurryDry, slurryTex, slurryNormal, 0.35f, 0.05f, 0f);
            // 粗渣：偏褐、颗粒感强
            Coarse = Standard("coarse", Palette.CoarseGrit, slurryTex, slurryNormal, 0.6f, 0.10f, 0f);

            Cloth = Standard("cloth", Palette.ClothRaw, clothTex, null, 0.2f, 0.05f, 0f);
            Metal = Standard("metal", Color.white, metalTex, null, 0.5f, 0.42f, 0.7f);

            // 清水
            var waterShader = FindShader("Shaders/WaterSurface", "Porcelain/WaterSurface");
            Water = new Material(waterShader) { name = "water" };
            Water.SetColor("_Color", Palette.WithAlpha(Palette.WaterClear, 0.42f));
            Water.SetTexture("_NormalMap", ProcTex.RippleNormal());
            Water.SetFloat("_NormalStrength", 0.30f);
            Water.SetFloat("_Gloss", 0.42f);
            Water.SetFloat("_Clarity", 0.5f);

            // 筛网
            var clothShader = FindShader("Shaders/SieveCloth", "Porcelain/SieveCloth");
            SieveCloth = new Material(clothShader) { name = "sieve_cloth" };
            SieveCloth.SetTexture("_MainTex", ProcTex.Weave());
            SieveCloth.SetColor("_Color", Palette.Darken(Palette.Bamboo, -0.06f));
            SieveCloth.SetColor("_WetColor", Palette.Darken(Palette.Bamboo, 0.34f));
            SieveCloth.SetFloat("_Cutoff", 0.32f);
            SieveCloth.SetFloat("_Wet", 0f);
            SieveCloth.mainTextureScale = new Vector2(9f, 9f);

            // 墙脚水渍（不受光半透片）
            Grime = Decal(ProcTex.Grime(), Palette.WithAlpha(Color.white, 0.55f));
            Grime.mainTextureScale = new Vector2(3f, 1f);

            // 窗面 / 天光片：极弱自发光，仅作为"光从门窗进来"的视觉依据
            EmissiveWarm = Standard("emissive", Palette.RiceWhite, null, null, 0.1f, 0.02f, 0f);
            EmissiveWarm.EnableKeyword("_EMISSION");
            EmissiveWarm.SetColor("_EmissionColor", Palette.RiceWhite * 1.15f);

            // 世界空间指认环
            CursorDecal = Decal(ProcTex.Ring(), Palette.WithAlpha(Palette.Cobalt, 0.85f));
        }

        public Material Standard(string key, Color tint, Texture2D albedo, Texture2D normal,
                                 float normalScale, float smoothness, float metallic, Vector2? tiling = null)
        {
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Standard");
            var m = new Material(shader) { name = key };
            m.SetColor("_Color", tint);
            if (albedo != null)
            {
                m.SetTexture("_MainTex", albedo);
                if (tiling.HasValue) m.mainTextureScale = tiling.Value;
            }
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (normal != null)
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", normalScale);
            }
            _cache[key] = m;
            return m;
        }

        public Material Decal(Texture2D tex, Color tint)
        {
            var shader = FindShader("Shaders/UnlitDecal", "Porcelain/UnlitDecal");
            var m = new Material(shader) { name = "decal_" + (tex != null ? tex.name : "none") };
            if (tex != null) m.SetTexture("_MainTex", tex);
            m.SetColor("_Color", tint);
            return m;
        }

        /// <summary>
        /// 为可交互物件生成独立材质实例，并预先打开自发光通道，
        /// 以便用 MaterialPropertyBlock 做低强度悬停提示（不做霓虹描边）。
        /// </summary>
        public static Material MakeHighlightable(Material source, string suffix)
        {
            var m = new Material(source) { name = source.name + "_hl_" + suffix };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            return m;
        }
    }
}
