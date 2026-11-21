using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Porcelain
{
    /// <summary>
    /// 材质工厂：全部基于内置 Standard 着色器 + 程序化贴图，
    /// 不依赖任何渲染管线包，打开工程即可显示。
    /// 粗糙度 / 金属度 / 法线强度集中在这里，是“半写实质感”的主要旋钮。
    /// </summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader standard;

        static Shader Std
        {
            get
            {
                if (standard == null) standard = Shader.Find("Standard");
                return standard;
            }
        }

        static Material Base(string key, Color tint)
        {
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Std);
            m.name = key;
            m.color = tint;
            m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        static void Surface(Material m, float smoothness, float metallic, ProcTex.Pair tex, float normalScale)
        {
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (tex.albedo != null)
            {
                m.SetTexture("_MainTex", tex.albedo);
                m.SetTextureScale("_MainTex", Vector2.one);
            }
            if (tex.normal != null && normalScale > 0f)
            {
                m.SetTexture("_BumpMap", tex.normal);
                m.SetFloat("_BumpScale", normalScale);
                m.EnableKeyword("_NORMALMAP");
            }
        }

        static void Cutout(Material m, float cutoff)
        {
            m.SetFloat("_Mode", 1f);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.SetInt("_SrcBlend", (int)BlendMode.One);
            m.SetInt("_DstBlend", (int)BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cutoff", cutoff);
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }

        static void Transparent(Material m, float alpha)
        {
            m.SetFloat("_Mode", 3f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            Color c = m.color;
            c.a = alpha;
            m.color = c;
        }

        // ================= 材质清单 =================

        public static Material BeamWood
        {
            get
            {
                var m = Base("beam", Color.white);
                Surface(m, 0.075f, 0f, ProcTex.Wood(Palette.BeamWood, 11, 0.22f), 1.0f);
                return m;
            }
        }

        public static Material PlankWood
        {
            get
            {
                var m = Base("plank", Color.white);
                Surface(m, 0.10f, 0f, ProcTex.Wood(Palette.BeamWood2, 23, 0.30f), 1.0f);
                return m;
            }
        }

        public static Material ToolWood
        {
            get
            {
                var m = Base("tool", new Color(0.86f, 0.85f, 0.83f));
                Surface(m, 0.11f, 0f, ProcTex.Wood(Palette.ToolWood, 37, 0.45f), 0.9f);
                return m;
            }
        }

        public static Material DarkToolWood
        {
            get
            {
                var m = Base("tool_dark", new Color(0.86f, 0.85f, 0.83f));
                Surface(m, 0.09f, 0f, ProcTex.Wood(Palette.WetWood, 41, 0.15f), 0.9f);
                return m;
            }
        }

        public static Material WetWood
        {
            get
            {
                var m = Base("wetwood", Color.white);
                Surface(m, 0.42f, 0f, ProcTex.Wood(Palette.WetWood, 43, 0.20f), 0.8f);
                return m;
            }
        }

        public static Material Stone
        {
            get
            {
                var m = Base("stone", new Color(0.86f, 0.85f, 0.82f));
                Surface(m, 0.13f, 0f, ProcTex.Stone(Palette.StoneWall, 51, 1f), 1.1f);
                return m;
            }
        }

        public static Material StoneWet
        {
            get
            {
                var m = Base("stone_wet", new Color(0.80f, 0.79f, 0.76f));
                Surface(m, 0.40f, 0f, ProcTex.Stone(Palette.StoneWet, 53, 0.8f), 1.0f);
                return m;
            }
        }

        public static Material FloorStone
        {
            get
            {
                var m = Base("floor", new Color(0.84f, 0.83f, 0.80f));
                Surface(m, 0.18f, 0f, ProcTex.Stone(Palette.StoneFloor, 61, 1.2f), 1.3f);
                return m;
            }
        }

        /// <summary>地面石板微差（共享贴图，只改色调），让地面不呆板</summary>
        public static Material FloorVariant(int i)
        {
            string k = "floor_v" + i;
            Material m;
            if (cache.TryGetValue(k, out m) && m != null) return m;
            m = new Material(Std);
            m.name = k;
            Surface(m, 0.18f, 0f, ProcTex.Stone(Palette.StoneFloor, 61, 1.2f), 1.3f);
            // 极轻的明度/色相起伏，避免棋盘格
            float t = 0.86f + ((i * 37) % 7 - 3) * 0.010f;
            m.color = new Color(t * (1f - 0.008f * (i % 3)), t, t * (1f - 0.006f * (i % 4)), 1f);
            m.enableInstancing = true;
            cache[k] = m;
            return m;
        }

        public static Material Rock
        {
            get
            {
                var m = Base("rock", new Color(0.88f, 0.87f, 0.84f));
                Surface(m, 0.10f, 0f, ProcTex.PorcelainStone(71), 1.4f);
                return m;
            }
        }

        public static Material Plaster
        {
            get
            {
                var m = Base("plaster", new Color(0.84f, 0.83f, 0.80f));
                Surface(m, 0.045f, 0f, ProcTex.Plaster(81), 2.4f);
                return m;
            }
        }

        public static Material Slurry
        {
            get
            {
                var m = Base("slurry", new Color(0.70f, 0.69f, 0.65f));
                Surface(m, 0.30f, 0f, ProcTex.Slurry(91, 0f), 1.0f);
                return m;
            }
        }

        /// <summary>倒出去的浆流：比缸里略亮，带一点湿润反光</summary>
        public static Material Stream
        {
            get
            {
                var m = Base("stream", new Color(1.10f, 1.08f, 1.02f));
                Surface(m, 0.55f, 0f, ProcTex.Slurry(93, 1.0f), 0.4f);
                m.color = new Color(1.10f, 1.08f, 1.02f);
                return m;
            }
        }

        /// <summary>已过滤的细浆，越筛越白净</summary>
        public static Material Filtrate
        {
            get
            {
                var m = Base("filtrate", new Color(0.86f, 0.85f, 0.82f));
                Surface(m, 0.34f, 0f, ProcTex.Slurry(93, 0.85f), 0.9f);
                return m;
            }
        }

        public static Material Clay
        {
            get
            {
                var m = Base("clay", Color.white);
                Surface(m, 0.22f, 0f, ProcTex.Clay(101), 1.0f);
                return m;
            }
        }

        /// <summary>烧成的陶器（设备用缸盆），只给很克制的釉面反光</summary>
        public static Material GlazedPot
        {
            get
            {
                var m = Base("pot", new Color(0.93f, 0.91f, 0.87f));
                Surface(m, 0.30f, 0f, ProcTex.Clay(103), 0.8f);
                return m;
            }
        }

        public static Material SieveCloth
        {
            get
            {
                Material m;
                if (cache.TryGetValue("sieve_cloth", out m) && m != null) return m;
                m = new Material(Std);
                m.name = "sieve_cloth";
                // 不做 alpha 切割：中远景上切割会退化成噪点，反而看不出是筛布。
                // 现在靠“细密经纬的贴图 + 强法线”来读，近景仍然是一张绷紧的布面。
                Surface(m, 0.09f, 0f, ProcTex.SieveCloth(111), 2.9f);
                m.color = new Color(0.74f, 0.72f, 0.66f);
                cache["sieve_cloth"] = m;
                return m;
            }
        }

        /// <summary>挂墙圆筛的篾面：比主筛布浅一档，远看是“筛子”而不是一个洞</summary>
        public static Material SieveClothLight
        {
            get
            {
                Material m;
                if (cache.TryGetValue("sieve_cloth_light", out m) && m != null) return m;
                m = new Material(Std);
                m.name = "sieve_cloth_light";
                Surface(m, 0.14f, 0f, ProcTex.SieveCloth(112), 1.4f);
                m.color = new Color(0.78f, 0.76f, 0.70f);
                cache["sieve_cloth_light"] = m;
                return m;
            }
        }

        public static Material Bamboo
        {
            get
            {
                var m = Base("bamboo", Color.white);
                Surface(m, 0.20f, 0f, ProcTex.Bamboo(121), 0.8f);
                return m;
            }
        }

        public static Material Rope
        {
            get
            {
                var m = Base("rope", Color.white);
                Surface(m, 0.10f, 0f, ProcTex.Rope(131), 1.6f);
                return m;
            }
        }

        /// <summary>粗布 / 围裙：完全哑光，几乎不反光</summary>
        public static Material RoughCloth
        {
            get
            {
                var m = Base("cloth_rough", new Color(0.70f, 0.68f, 0.64f));
                Surface(m, 0.035f, 0f, ProcTex.Plaster(141), 1.5f);
                return m;
            }
        }

        public static Material Iron
        {
            get
            {
                var m = Base("iron", Palette.Iron);
                m.SetFloat("_Glossiness", 0.35f);
                m.SetFloat("_Metallic", 0.80f);
                return m;
            }
        }

        /// <summary>水（缸 / 槽内），半透明、低饱和，不做镜面</summary>
        public static Material Water
        {
            get
            {
                Material m;
                if (cache.TryGetValue("water", out m) && m != null) return m;
                m = new Material(Std);
                m.name = "water";
                Surface(m, 0.62f, 0f, new ProcTex.Pair(), 0f);
                m.color = new Color(Palette.Water.r, Palette.Water.g, Palette.Water.b, 0.72f);
                Transparent(m, 0.72f);
                m.SetFloat("_Glossiness", 0.62f);
                cache["water"] = m;
                return m;
            }
        }

        /// <summary>
        /// 筛面上的湿浆层：无光照贴片 + 边缘柔化遮罩。
        /// 之前用 Standard 的透明模式，在这个材质上没生效，整块变成不透明的板子，
        /// 把筛布的织纹全盖住了；换成无光照 + 圆形遮罩才真正是“一层湿痕”。
        /// </summary>
        public static Material SlurryFilm
        {
            get
            {
                Material m;
                if (cache.TryGetValue("slurry_film", out m) && m != null) return m;
                m = new Material(Shader.Find("Porcelain/Particle"));
                m.name = "slurry_film";
                m.SetTexture("_MainTex", ProcTex.SoftBlob(256, 0.30f));
                m.color = new Color(0.88f, 0.87f, 0.83f, 0.35f);
                cache["slurry_film"] = m;
                return m;
            }
        }

        public static Material Residue
        {
            get
            {
                var m = Base("residue", new Color(0.50f, 0.47f, 0.41f));
                Surface(m, 0.14f, 0f, ProcTex.Slurry(97, 0.1f), 1.6f);
                return m;
            }
        }

        /// <summary>地面 / 墙根水渍贴片（透明）</summary>
        public static Material Stain
        {
            get
            {
                Material m;
                if (cache.TryGetValue("stain", out m) && m != null) return m;
                m = new Material(Std);
                m.name = "stain";
                Surface(m, 0.30f, 0f, ProcTex.Stain(Palette.StoneWet), 0.4f);
                m.color = new Color(0.42f, 0.42f, 0.40f, 1f);
                Transparent(m, 0.55f);
                m.SetFloat("_Glossiness", 0.30f);
                cache["stain"] = m;
                return m;
            }
        }

        /// <summary>水面涟漪：无光照贴片（用粒子着色器），只做柔和的环形叠加</summary>
        public static Material Ripple
        {
            get
            {
                Material m;
                if (cache.TryGetValue("ripple", out m) && m != null) return m;
                m = new Material(Shader.Find("Porcelain/Particle"));
                m.name = "ripple";
                m.SetTexture("_MainTex", ProcTex.RippleRing());
                m.SetColor("_Color", new Color(0.95f, 0.94f, 0.90f, 0.30f));
                cache["ripple"] = m;
                return m;
            }
        }

        /// <summary>纯色材质（界面牌、标记等）</summary>
        public static Material Flat(Color c, float smoothness = 0.1f)
        {
            string k = "flat" + c.ToString() + smoothness;
            Material m;
            if (cache.TryGetValue(k, out m) && m != null) return m;
            m = new Material(Std);
            m.name = k;
            m.color = c;
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", 0f);
            cache[k] = m;
            return m;
        }

        /// <summary>自发光（窗外天光、灯芯等）</summary>
        public static Material Emissive(Color c, float intensity)
        {
            string k = "emis" + c.ToString() + intensity;
            Material m;
            if (cache.TryGetValue(k, out m) && m != null) return m;
            m = new Material(Std);
            m.name = k;
            m.color = c;
            m.SetFloat("_Glossiness", 0f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            cache[k] = m;
            return m;
        }

        public static void ClearCache()
        {
            cache.Clear();
        }
    }
}
