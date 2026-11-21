using System.Collections.Generic;
using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 程序化贴图库。原型阶段不依赖外部美术资源，用可平铺噪声生成
    /// 石、木、陶、泥、布的可信质感，避免默认几何体的塑料感。
    /// 所有噪声均为周期噪声，保证平铺不出现接缝。
    /// </summary>
    public static class ProcTex
    {
        const int S = 512;
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        // ---------------------------------------------------------------- 公开入口

        public static Texture2D Wood(Color body) => Cached("wood" + Key(body), () => BuildWood(body));

        public static Texture2D FloorStone() => Cached("floor", BuildFloorStone);

        public static Texture2D Plaster() => Cached("plaster", BuildPlaster);

        public static Texture2D Ceramic(Color body) => Cached("ceramic" + Key(body), () => BuildCeramic(body));

        public static Texture2D Slurry() => Cached("slurry", BuildSlurry);

        public static Texture2D Metal() => Cached("metal", BuildMetal);

        public static Texture2D Cloth() => Cached("cloth", BuildCloth);

        public static Texture2D Weave() => Cached("weave", BuildWeave);

        public static Texture2D Grime() => Cached("grime", BuildGrime);

        public static Texture2D RippleNormal() => Cached("ripple", BuildRippleNormal);

        public static Texture2D Ring() => Cached("ring", BuildRing);

        public static Texture2D SoftDisc() => Cached("softdisc", BuildSoftDisc);

        /// <summary>由明度推导法线贴图。用于给程序化材质一个柔和的表面凹凸。</summary>
        public static Texture2D NormalFromTexture(Texture2D src, float strength)
        {
            string key = "nrm" + src.GetInstanceID() + "_" + strength.ToString("F2");
            return Cached(key, () => BuildNormal(src, strength));
        }

        // ---------------------------------------------------------------- 构建器

        static Texture2D BuildWood(Color body)
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    // 纹理顺构件方向：沿 V 走木纹，轻微弯曲
                    float warp = Fbm(u * 2f, v * 2f, 3, 3, 111) - 0.5f;
                    float grain = Mathf.Abs(Mathf.Sin((u * 5.5f + warp * 1.7f) * Mathf.PI));
                    grain = Mathf.Pow(grain, 0.55f);
                    float macro = Fbm(u * 0.5f, v * 0.5f, 2, 3, 223);
                    float fine = Fbm(u * 6f, v * 6f, 24, 2, 307);
                    float shade = Mathf.Clamp01(macro * 0.42f + fine * 0.20f + (1f - grain) * 0.38f);
                    Color c = body * Mathf.Lerp(0.74f, 1.16f, shade);
                    c.r *= 1.03f;
                    c.b *= 0.95f;
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Make(px);
        }

        static Texture2D BuildFloorStone()
        {
            var px = new Color[S * S];
            const int slabPx = 64; // 每贴图 4x4 块石板
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    int ix = x / slabPx, iy = y / slabPx;
                    int lx = x % slabPx, ly = y % slabPx;
                    float u = x / (float)S, v = y / (float)S;

                    // 每块石板色差
                    float tone = Hash(ix, iy, 41);
                    Color c = Palette.StoneFloor * Mathf.Lerp(0.88f, 1.10f, tone);

                    // 石材颗粒
                    float speck = Fbm(u * 12f, v * 12f, 48, 2, 59);
                    c *= Mathf.Lerp(0.94f, 1.05f, speck);

                    // 灰缝：靠近块边的窄带加深
                    float edge = Mathf.Min(Mathf.Min(lx, slabPx - 1 - lx), Mathf.Min(ly, slabPx - 1 - ly));
                    float grout = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 3.5f));
                    c = Color.Lerp(c * 0.62f, c, grout);

                    // 干湿分区：低频噪声形成的水渍，略偏冷
                    float wet = Fbm(u, v, 3, 4, 907);
                    float wetMask = Mathf.SmoothStep(0.54f, 0.68f, wet);
                    c = Color.Lerp(c, c * new Color(0.80f, 0.82f, 0.85f, 1f), wetMask * 0.55f);

                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Make(px);
        }

        static Texture2D BuildPlaster()
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float mottle = Fbm(u, v, 2, 4, 17);
                    float fine = Fbm(u * 8f, v * 8f, 32, 2, 71);
                    float shade = 0.92f + mottle * 0.10f + (fine - 0.5f) * 0.05f;
                    Color c = Palette.RiceWhite * shade;
                    // 极轻微的泛黄，避免墙面发灰发冷
                    c.r *= 1.005f;
                    c.b *= 0.99f;
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Make(px);
        }

        static Texture2D BuildCeramic(Color body)
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    // 拉坯留下的横向旋纹
                    float ring = Mathf.Sin(v * 42f * Mathf.PI) * 0.5f + 0.5f;
                    float mottle = Fbm(u * 2f, v * 2f, 4, 3, 401);
                    float grit = Fbm(u * 10f, v * 10f, 40, 2, 419);
                    float shade = 0.90f + ring * 0.05f + mottle * 0.07f + (grit - 0.5f) * 0.06f;
                    Color c = body * shade;
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Make(px);
        }

        static Texture2D BuildSlurry()
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float cloud = Fbm(u * 2f, v * 2f, 3, 4, 613);
                    float curdle = Fbm(u * 5f, v * 5f, 12, 3, 631);
                    float shade = 0.90f + cloud * 0.14f + (curdle - 0.5f) * 0.10f;
                    Color c = Palette.SlurryWet * shade;
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Make(px);
        }

        static Texture2D BuildMetal()
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    // 拉丝沿 U 方向
                    float brush = Fbm(u * 3f, v * 90f, 12, 2, 733);
                    float patch = Fbm(u, v, 2, 3, 751);
                    Color c = Palette.Darken(Palette.ClayGrey, 0.28f) * Mathf.Lerp(0.82f, 1.15f, brush * 0.6f + patch * 0.4f);
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Make(px);
        }

        static Texture2D BuildCloth()
        {
            var px = new Color[S * S];
            const float cells = 48f;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float warp = Mathf.Abs(Mathf.Sin(u * cells * Mathf.PI));
                    float weft = Mathf.Abs(Mathf.Sin(v * cells * Mathf.PI));
                    float weave = Mathf.Max(warp, weft);
                    float dirt = Fbm(u, v, 3, 3, 811);
                    Color c = Palette.ClothRaw * Mathf.Lerp(0.84f, 1.12f, weave * 0.7f + dirt * 0.3f);
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Make(px);
        }

        /// <summary>筛网：经纬线交织，alpha 通道用于 alphaTest 挖孔。</summary>
        static Texture2D BuildWeave()
        {
            var px = new Color[S * S];
            const float threads = 16f;   // 每贴图 16 x 16 孔
            const float thickness = 0.34f; // 线粗占孔径比例
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float fu = Mathf.Repeat(u * threads, 1f);
                    float fv = Mathf.Repeat(v * threads, 1f);
                    float du = Mathf.Abs(fu - 0.5f) * 2f; // 0 线心, 1 孔心
                    float dv = Mathf.Abs(fv - 0.5f) * 2f;
                    float threadU = 1f - Mathf.SmoothStep(1f - thickness, 1f, du);
                    float threadV = 1f - Mathf.SmoothStep(1f - thickness, 1f, dv);
                    float mask = Mathf.Max(threadU, threadV);

                    // 交织处略暗，模拟上下压叠
                    float over = threadU * threadV;
                    float noise = Fbm(u * 8f, v * 8f, 24, 2, 907);
                    Color c = Palette.Bamboo * Mathf.Lerp(0.88f, 1.06f, noise);
                    c = Color.Lerp(c, c * 0.78f, over * 0.6f);
                    c.a = mask;
                    px[y * S + x] = c;
                }
            }
            return Make(px, TextureWrapMode.Repeat);
        }

        /// <summary>墙脚水渍带：纵向渐变 alpha，用于沿墙根的一条薄片。</summary>
        static Texture2D BuildGrime()
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S; // v=0 墙脚, v=1 向上
                    float blot = Fbm(u * 3f, v * 3f, 4, 3, 1009);
                    float a = Mathf.SmoothStep(0f, 1f, 1f - v) * Mathf.Lerp(0.35f, 0.85f, blot);
                    Color c = Palette.Darken(Palette.ClayGrey, 0.34f);
                    c.a = a;
                    px[y * S + x] = c;
                }
            }
            return Make(px, TextureWrapMode.Clamp);
        }

        static Texture2D BuildRippleNormal()
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    const float e = 1f / S;
                    float h = Fbm(u, v, 4, 3, 1201);
                    float hx = Fbm(u + e, v, 4, 3, 1201);
                    float hy = Fbm(u, v + e, 4, 3, 1201);
                    Vector3 n = new Vector3((h - hx) * 6f, (h - hy) * 6f, 1f).normalized;
                    px[y * S + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            return Make(px, TextureWrapMode.Repeat, true);
        }

        /// <summary>准星 / 指认环：细线圆环 + 极小中心点，青花蓝风格。</summary>
        static Texture2D BuildRing()
        {
            const int R = 128;
            var px = new Color[R * R];
            for (int y = 0; y < R; y++)
            {
                for (int x = 0; x < R; x++)
                {
                    float dx = (x + 0.5f) / R - 0.5f;
                    float dy = (y + 0.5f) / R - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // 0 中心, 1 边缘
                    float ring = 1f - Mathf.SmoothStep(0f, 0.035f, Mathf.Abs(d - 0.86f));
                    float dot = 1f - Mathf.SmoothStep(0f, 0.05f, d);
                    px[y * R + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(ring + dot * 0.85f));
                }
            }
            var t = new Texture2D(R, R, TextureFormat.RGBA32, true, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            t.SetPixels(px);
            t.Apply(true, false);
            return t;
        }

        static Texture2D BuildSoftDisc()
        {
            const int R = 128;
            var px = new Color[R * R];
            for (int y = 0; y < R; y++)
            {
                for (int x = 0; x < R; x++)
                {
                    float dx = (x + 0.5f) / R - 0.5f;
                    float dy = (y + 0.5f) / R - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float a = 1f - Mathf.SmoothStep(0.55f, 1f, d);
                    px[y * R + x] = new Color(1f, 1f, 1f, a);
                }
            }
            var t = new Texture2D(R, R, TextureFormat.RGBA32, true, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.SetPixels(px);
            t.Apply(true, false);
            return t;
        }

        // ---------------------------------------------------------------- 工具

        static Texture2D BuildNormal(Texture2D src, float strength)
        {
            int w = src.width, h = src.height;
            var srcPx = src.GetPixels();
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float l = Lum(srcPx[y * w + x]);
                    float lx = Lum(srcPx[y * w + WrapInt(x + 1, w)]);
                    float ly = Lum(srcPx[WrapInt(y + 1, h) * w + x]);
                    Vector3 n = new Vector3((l - lx) * strength, (l - ly) * strength, 1f).normalized;
                    // 与 Unity Standard 的 UnpackScaleNormal 兼容：x 存 A，y 存 G（DXT5nm 约定）
                    px[y * w + x] = new Color(0.5f, n.y * 0.5f + 0.5f, 0.5f, n.x * 0.5f + 0.5f);
                }
            }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, true);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.SetPixels(px);
            t.Apply(true, false);
            return t;
        }

        static float Lum(Color c) => c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;

        static int WrapInt(int v, int n)
        {
            int r = v % n;
            return r < 0 ? r + n : r;
        }

        static Texture2D Make(Color[] px, TextureWrapMode wrap = TextureWrapMode.Repeat, bool linear = false)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true, linear);
            t.wrapMode = wrap;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.SetPixels(px);
            t.Apply(true, false);
            return t;
        }

        static Texture2D Cached(string key, System.Func<Texture2D> factory)
        {
            if (Cache.TryGetValue(key, out var t) && t != null) return t;
            t = factory();
            t.name = key;
            Cache[key] = t;
            return t;
        }

        static string Key(Color c) => ((int)(c.r * 255)).ToString() + "_" + ((int)(c.g * 255)).ToString() + "_" + ((int)(c.b * 255)).ToString();

        // ---------------------------------------------------------------- 噪声

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFFu) / 16777215f;
            }
        }

        static float VNoiseTile(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);
            int x0 = WrapInt(xi, period), x1 = WrapInt(xi + 1, period);
            int y0 = WrapInt(yi, period), y1 = WrapInt(yi + 1, period);
            float a = Hash(x0, y0, seed), b = Hash(x1, y0, seed);
            float c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>可平铺分形噪声。u,v 取 [0,1)，baseCells 决定最低频格子数。</summary>
        static float Fbm(float u, float v, int baseCells, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int cells = Mathf.Max(1, baseCells);
            for (int i = 0; i < octaves; i++)
            {
                sum += VNoiseTile(u * cells, v * cells, cells, seed + i * 131) * amp;
                norm += amp;
                cells *= 2;
                amp *= 0.5f;
            }
            return sum / Mathf.Max(0.0001f, norm);
        }
    }
}
