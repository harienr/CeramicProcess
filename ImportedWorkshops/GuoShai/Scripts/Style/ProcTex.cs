using System;
using System.Collections.Generic;
using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 程序化贴图：木、石、灰泥、泥浆、筛布、竹、绳、瓷石、陶土、水渍。
    /// 全部运行时生成，工程里不需要任何外部贴图资源；可平铺（tileable）。
    /// 换风格只需要改 Palette 与这里的色相/粗糙度。
    /// </summary>
    public static class ProcTex
    {
        // ======================= 噪声 =======================

        static float Hash2(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / 2147483647f;
            }
        }

        static int Wrap(int i, int period)
        {
            if (period <= 0) return i;
            int r = i % period;
            return r < 0 ? r + period : r;
        }

        static float VNoise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            float u = fx * fx * (3f - 2f * fx);
            float v = fy * fy * (3f - 2f * fy);
            int x0 = Wrap(xi, period), x1 = Wrap(xi + 1, period);
            int y0 = Wrap(yi, period), y1 = Wrap(yi + 1, period);
            float a = Hash2(x0, y0, seed), b = Hash2(x1, y0, seed);
            float c = Hash2(x0, y1, seed), d = Hash2(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>可平铺 fbm，u/v 取 [0,1)</summary>
        public static float Fbm(float u, float v, int baseFreq, int octaves, int seed, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int f = Mathf.Max(1, baseFreq);
            for (int i = 0; i < octaves; i++)
            {
                sum += VNoise(u * f, v * f, f, seed + i * 131) * amp;
                norm += amp;
                amp *= gain;
                f *= 2;
            }
            return sum / Mathf.Max(1e-5f, norm);
        }

        /// <summary>脊状噪声，用于石面开裂、木纹硬边</summary>
        public static float Ridge(float u, float v, int baseFreq, int octaves, int seed)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int f = Mathf.Max(1, baseFreq);
            for (int i = 0; i < octaves; i++)
            {
                float n = Mathf.Abs(VNoise(u * f, v * f, f, seed + i * 977) * 2f - 1f);
                sum += (1f - n) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2;
            }
            return sum / Mathf.Max(1e-5f, norm);
        }

        // ======================= 烘焙管线 =======================

        public struct Pair
        {
            public Texture2D albedo;
            public Texture2D normal;
        }

        static readonly Dictionary<string, Pair> cache = new Dictionary<string, Pair>();

        public static Pair Get(string key)
        {
            Pair p;
            return cache.TryGetValue(key, out p) ? p : default(Pair);
        }

        /// <summary>由 albedo / height 两个采样函数烘焙一对贴图（含法线）</summary>
        public static Pair Bake(int res, Func<float, float, Color> albedoFn,
                                Func<float, float, float> heightFn, float normalStrength,
                                Func<float, float, float> alphaFn = null)
        {
            var albedo = new Color[res * res];
            var h = new float[res * res];

            for (int y = 0; y < res; y++)
            {
                float v = (y + 0.5f) / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res;
                    int i = y * res + x;
                    Color c = albedoFn(u, v);
                    if (alphaFn != null) c.a = Mathf.Clamp01(alphaFn(u, v));
                    albedo[i] = c;
                    h[i] = heightFn != null ? heightFn(u, v) : 0f;
                }
            }

            // 反照率走 sRGB，法线走 linear
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, true, false);
            tex.name = "proc_albedo";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.anisoLevel = 4;
            tex.SetPixels(albedo);
            tex.Apply(true, false);

            var nrm = NormalFromHeight(h, res, normalStrength);
            var pair = new Pair();
            pair.albedo = tex;
            pair.normal = nrm;
            return pair;
        }

        static Texture2D NormalFromHeight(float[] h, int res, float strength)
        {
            var px = new Color[res * res];
            float sx = 1f / res;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    int xm = (x - 1 + res) % res, xp = (x + 1) % res;
                    int ym = (y - 1 + res) % res, yp = (y + 1) % res;
                    float hl = h[y * res + xm], hr = h[y * res + xp];
                    float hd = h[ym * res + x], hu = h[yp * res + x];
                    // 斜率按“高度差 × 强度”折算成法线倾角；z 分量归一为 1，
                    // 避免出现极端强的高光像“锤过的金属”。
                    float k = strength * 0.30f;
                    Vector3 n = new Vector3((hl - hr) * k, (hd - hu) * k, 1f);
                    n.Normalize();
                    px[y * res + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, true, true);
            tex.name = "proc_normal";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.anisoLevel = 4;
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        // ======================= 材质纹理 =======================

        /// <summary>木材：纹理沿 U 方向生长，带年轮、细裂纹、局部磨亮</summary>
        public static Pair Wood(Color baseCol, int seed, float wear = 0.35f)
        {
            string key = "wood" + baseCol.GetHashCode() + seed + wear;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color dark = Color.Lerp(baseCol, Palette.ShenHui, 0.55f);
            Color light = Color.Lerp(baseCol, Palette.MiBai, 0.20f);
            Color worn = Color.Lerp(baseCol, Palette.MiBai, 0.48f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float grain = Fbm(u * 0.35f + 0.13f, v, 3, 4, seed);
                float rings = Mathf.Sin((v * 11f + grain * 4.2f) * Mathf.PI) * 0.5f + 0.5f;
                float fine = Fbm(u, v, 34, 2, seed + 7);
                float crack = Mathf.Pow(Ridge(u * 0.6f, v, 6, 3, seed + 21), 6f);

                float t = Mathf.Clamp01(rings * 0.30f + grain * 0.45f + fine * 0.25f);
                Color c = Color.Lerp(dark, light, t);
                c = Color.Lerp(c, dark, crack * 0.55f);

                float w = Fbm(u * 0.5f, v * 0.5f, 2, 3, seed + 55);
                c = Color.Lerp(c, worn, Mathf.SmoothStep(0.55f, 0.85f, w) * wear);
                float mott = Fbm(u, v, 4, 2, seed + 91) - 0.5f;
                return c * (1f + mott * 0.14f);
            };

            Func<float, float, float> height = (u, v) =>
            {
                float grain = Fbm(u * 0.35f + 0.13f, v, 3, 4, seed);
                float rings = Mathf.Sin((v * 11f + grain * 4.2f) * Mathf.PI) * 0.5f + 0.5f;
                float fine = Fbm(u, v, 34, 2, seed + 7);
                float crack = Mathf.Pow(Ridge(u * 0.6f, v, 6, 3, seed + 21), 5f);
                return grain * 0.55f + rings * 0.12f + fine * 0.18f - crack * 0.5f;
            };

            var p = Bake(512, albedo, height, 2.2f);
            cache[key] = p;
            return p;
        }

        /// <summary>石面：麻点、细裂纹、云母亮斑</summary>
        public static Pair Stone(Color baseCol, int seed, float speckle = 1f)
        {
            string key = "stone" + baseCol.GetHashCode() + seed + speckle;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color dark = Color.Lerp(baseCol, Palette.ShenHui, 0.42f);
            Color light = Color.Lerp(baseCol, Palette.MiBai, 0.42f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float big = Fbm(u, v, 3, 4, seed);
                float med = Fbm(u, v, 11, 3, seed + 3);
                float grit = Fbm(u, v, 90, 2, seed + 9);
                float crack = Mathf.Pow(Ridge(u, v, 5, 4, seed + 17), 8f);

                float t = big * 0.55f + med * 0.30f + grit * 0.15f;
                Color c = Color.Lerp(dark, light, t);
                c = Color.Lerp(c, Palette.ShenHui, crack * 0.45f);

                float mica = Fbm(u, v, 140, 1, seed + 33);
                c = Color.Lerp(c, Palette.MiBai, Mathf.SmoothStep(0.80f, 0.98f, mica) * 0.35f * speckle);
                float dot1 = Fbm(u, v, 200, 1, seed + 41);
                c = Color.Lerp(c, Palette.ShenHui, Mathf.SmoothStep(0.86f, 1f, dot1) * 0.4f * speckle);
                return c;
            };

            Func<float, float, float> height = (u, v) =>
            {
                float big = Fbm(u, v, 3, 4, seed);
                float med = Fbm(u, v, 11, 3, seed + 3);
                float grit = Fbm(u, v, 90, 2, seed + 9);
                float crack = Mathf.Pow(Ridge(u, v, 5, 4, seed + 17), 7f);
                return big * 0.4f + med * 0.35f + grit * 0.25f - crack * 0.8f;
            };

            var p = Bake(512, albedo, height, 2.8f);
            cache[key] = p;
            return p;
        }

        /// <summary>灰泥墙：近米白，有抹痕与水渍，保留暗部层次</summary>
        public static Pair Plaster(int seed)
        {
            string key = "plaster" + seed;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color baseCol = Palette.Plaster;
            Color dark = Color.Lerp(baseCol, Palette.NiHui, 0.45f);
            Color light = Color.Lerp(baseCol, Color.white, 0.20f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float trowel = Fbm(u, v, 4, 4, seed);
                float fine = Fbm(u, v, 40, 2, seed + 5);
                float stain = Fbm(u, v, 2, 3, seed + 11);
                float t = trowel * 0.55f + fine * 0.20f + 0.12f;
                Color c = Color.Lerp(dark, light, t);
                c = Color.Lerp(c, Palette.NiHui, Mathf.SmoothStep(0.62f, 0.9f, stain) * 0.30f);
                return c;
            };

            Func<float, float, float> height = (u, v) =>
            {
                float trowel = Fbm(u, v, 4, 4, seed);
                float fine = Fbm(u, v, 40, 2, seed + 5);
                return trowel * 0.6f + fine * 0.4f;
            };

            var p = Bake(512, albedo, height, 1.6f);
            cache[key] = p;
            return p;
        }

        /// <summary>泥浆：灰白、不透明、有黏稠的流动条纹</summary>
        public static Pair Slurry(int seed, float clarity = 0f)
        {
            string key = "slurry" + seed + clarity;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color baseCol = Color.Lerp(Palette.Slurry, Palette.SlurryWet, clarity);
            Color dark = Color.Lerp(baseCol, Palette.NiHui, 0.30f);
            Color light = Color.Lerp(baseCol, Color.white, 0.28f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float flow = Fbm(u * 0.6f, v * 2.2f, 3, 4, seed);
                float blur = Fbm(u, v, 6, 3, seed + 7);
                float t = flow * 0.6f + blur * 0.4f;
                Color c = Color.Lerp(dark, light, t);
                c.a = 1f;
                return c;
            };

            Func<float, float, float> height = (u, v) =>
            {
                float flow = Fbm(u * 0.6f, v * 2.2f, 3, 4, seed);
                float blur = Fbm(u, v, 6, 3, seed + 7);
                return flow * 0.7f + blur * 0.3f;
            };

            var p = Bake(384, albedo, height, 0.9f);
            cache[key] = p;
            return p;
        }

        /// <summary>筛布：细密经纬，带透光孔（alpha 切割）</summary>
        public static Pair SieveCloth(int seed)
        {
            string key = "sieve" + seed;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color thread = Palette.SieveCloth;
            Color shadow = Color.Lerp(thread, Palette.ShenHui, 0.72f);
            const int cells = 11;   // 一个贴图周期 11 根经纬 → 屏上约 3mm 一格

            // 线在格子边缘、孔在格子中心：用三角波取“到格心的距离”，再做幂压缩成细线
            Func<float, float, float> weaveFn = (u, v) =>
            {
                float t1 = (u * cells) - Mathf.Floor(u * cells);
                float t2 = (v * cells) - Mathf.Floor(v * cells);
                float lx = Mathf.Abs(t1 - 0.5f) * 2f;   // 0=格心(孔)  1=格边(线)
                float ly = Mathf.Abs(t2 - 0.5f) * 2f;
                float w = Mathf.Max(lx, ly);
                return Mathf.Pow(w, 3.2f);              // 线细、孔大
            };

            Func<float, float, Color> albedo = (u, v) =>
            {
                float weave = weaveFn(u, v);
                float dirt = Fbm(u, v, 4, 3, seed);
                Color c = Color.Lerp(shadow, thread, weave);
                c = Color.Lerp(c, Palette.NiHui, dirt * 0.30f);
                // 经纬交叉处再亮一点，织纹才有“压过去”的感觉
                float t1 = (u * cells) - Mathf.Floor(u * cells);
                float t2 = (v * cells) - Mathf.Floor(v * cells);
                float cross = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(t1 - 0.5f) * 2f), 6f)
                            * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(t2 - 0.5f) * 2f), 6f);
                c *= 0.90f + cross * 0.55f;
                return c;
            };

            Func<float, float, float> height = (u, v) => weaveFn(u, v);

            var p = Bake(1024, albedo, height, 3.4f, null);
            cache[key] = p;
            return p;
        }

        /// <summary>竹：竖向纤维与竹节</summary>
        public static Pair Bamboo(int seed)
        {
            string key = "bamboo" + seed;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color baseCol = Palette.Bamboo;
            Color dark = Color.Lerp(baseCol, Palette.ShenHui, 0.45f);
            Color light = Color.Lerp(baseCol, Palette.MiBai, 0.28f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float fibre = Fbm(u * 8f, v, 2, 3, seed);
                float node = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 3f));
                float t = fibre * 0.7f + 0.15f;
                Color c = Color.Lerp(dark, light, t);
                c = Color.Lerp(c, dark, Mathf.SmoothStep(0.94f, 1f, node) * 0.5f);
                return c;
            };

            Func<float, float, float> height = (u, v) =>
            {
                float fibre = Fbm(u * 8f, v, 2, 3, seed);
                float node = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 3f));
                return fibre * 0.5f - Mathf.SmoothStep(0.94f, 1f, node) * 0.4f;
            };

            var p = Bake(256, albedo, height, 1.4f);
            cache[key] = p;
            return p;
        }

        /// <summary>绳麻：斜向拧纹</summary>
        public static Pair Rope(int seed)
        {
            string key = "rope" + seed;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color baseCol = Palette.Rope;
            Color dark = Color.Lerp(baseCol, Palette.JiuMuHe, 0.5f);
            Color light = Color.Lerp(baseCol, Palette.MiBai, 0.3f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float twist = Mathf.Sin((u * 6f + v * 1.1f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float fibre = Fbm(u, v, 30, 2, seed);
                float t = twist * 0.7f + fibre * 0.3f;
                return Color.Lerp(dark, light, t);
            };

            Func<float, float, float> height = (u, v) =>
            {
                float twist = Mathf.Sin((u * 6f + v * 1.1f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float fibre = Fbm(u, v, 30, 2, seed);
                return twist * 0.7f + fibre * 0.3f;
            };

            var p = Bake(256, albedo, height, 2f);
            cache[key] = p;
            return p;
        }

        /// <summary>瓷石：粗粝断面 + 细颗粒 + 少量云母</summary>
        public static Pair PorcelainStone(int seed)
        {
            string key = "pstone" + seed;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color baseCol = Palette.RockStone;
            Color dark = Color.Lerp(baseCol, Palette.ShenHui, 0.35f);
            Color light = Color.Lerp(baseCol, Color.white, 0.45f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float big = Fbm(u, v, 4, 4, seed);
                float grit = Fbm(u, v, 60, 2, seed + 6);
                float t = big * 0.5f + grit * 0.35f + 0.1f;
                Color c = Color.Lerp(dark, light, t);
                float sparkle = Mathf.SmoothStep(0.90f, 1f, Fbm(u, v, 160, 1, seed + 12));
                return Color.Lerp(c, Color.white, sparkle * 0.25f);
            };

            Func<float, float, float> height = (u, v) =>
            {
                float big = Fbm(u, v, 4, 4, seed);
                float grit = Fbm(u, v, 60, 2, seed + 6);
                return big * 0.5f + grit * 0.5f;
            };

            var p = Bake(384, albedo, height, 3.2f);
            cache[key] = p;
            return p;
        }

        /// <summary>陶土 / 未烧泥料：哑光细密</summary>
        public static Pair Clay(int seed)
        {
            string key = "clay" + seed;
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Color baseCol = Palette.ClayRaw;
            Color dark = Color.Lerp(baseCol, Palette.NiHui, 0.35f);
            Color light = Color.Lerp(baseCol, Color.white, 0.25f);

            Func<float, float, Color> albedo = (u, v) =>
            {
                float big = Fbm(u, v, 5, 3, seed);
                float fine = Fbm(u, v, 55, 2, seed + 3);
                Color c = Color.Lerp(dark, light, big * 0.5f + fine * 0.35f + 0.1f);
                float ring = Mathf.Sin(v * 90f + Fbm(u, v, 3, 2, seed + 8) * 3f);
                return c * (1f + ring * 0.03f);
            };

            Func<float, float, float> height = (u, v) =>
            {
                float big = Fbm(u, v, 5, 3, seed);
                float fine = Fbm(u, v, 55, 2, seed + 3);
                return big * 0.5f + fine * 0.5f;
            };

            var p = Bake(256, albedo, height, 1.8f);
            cache[key] = p;
            return p;
        }

        /// <summary>脏污水痕：用于地面、墙根（带 alpha，配合 Standard 透明模式）</summary>
        public static Pair Stain(Color col)
        {
            string key = "stain" + col.GetHashCode();
            var hit = Get(key);
            if (hit.albedo != null) return hit;

            Func<float, float, Color> albedo = (u, v) =>
            {
                float n = Fbm(u, v, 3, 4, 71);
                Color c = Color.Lerp(col, Color.Lerp(col, Color.black, 0.55f), n);
                c.a = 1f;
                return c;
            };

            Func<float, float, float> height = (u, v) => Fbm(u, v, 3, 4, 71);

            Func<float, float, float> alpha = (u, v) =>
            {
                float n = Fbm(u, v, 3, 4, 71);
                float edge = 1f - Mathf.Clamp01(Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 2.6f);
                return Mathf.SmoothStep(0.42f, 0.78f, n) * Mathf.SmoothStep(0f, 0.35f, edge);
            };

            var p = Bake(256, albedo, height, 0.6f, alpha);
            cache[key] = p;
            return p;
        }

        /// <summary>柔和的环形（水面涟漪用）：中间透明，环带亮，向外衰减</summary>
        public static Texture2D RippleRing(int res = 256)
        {
            var t = new Texture2D(res, res, TextureFormat.RGBA32, true, false);
            var px = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res * 2f - 1f;
                    float v = (y + 0.5f) / res * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    // 环带在 d≈0.70 处最亮，内外各 0.18 衰减
                    float ring = Mathf.Exp(-Mathf.Pow((d - 0.70f) / 0.15f, 2f));
                    float fade = Mathf.Clamp01(1f - Mathf.Abs(d - 0.70f) / 0.30f);
                    float a = Mathf.Clamp01(ring * fade * 1.35f);
                    px[y * res + x] = new Color(1f, 1f, 1f, a);
                }
            }
            t.SetPixels(px);
            t.Apply(true, false);
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        /// <summary>柔和的圆形遮罩（湿斑、光斑用）：中心实、边缘透明</summary>
        public static Texture2D SoftBlob(int res = 256, float core = 0.45f)
        {
            var t = new Texture2D(res, res, TextureFormat.RGBA32, true, false);
            var px = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res * 2f - 1f;
                    float v = (y + 0.5f) / res * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float a = 1f - Mathf.SmoothStep(core, 1f, d);
                    px[y * res + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            }
            t.SetPixels(px);
            t.Apply(true, false);
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        /// <summary>软圆点（粒子用）</summary>
        public static Texture2D SoftDot(int res = 64)
        {
            var t = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var px = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res * 2f - 1f;
                    float v = (y + 0.5f) / res * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    px[y * res + x] = new Color(1f, 1f, 1f, a);
                }
            }
            t.SetPixels(px);
            t.Apply();
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        /// <summary>环境反射用的小立方图：上方偏亮、正面有一个“门口”高光</summary>
        public static Cubemap EnvCubemap()
        {
            const int S = 32;
            var cb = new Cubemap(S, TextureFormat.RGBA32, true);
            Color sky = Color.Lerp(Palette.MiBai, Color.white, 0.15f);
            Color ground = Color.Lerp(Palette.NiHui, Palette.ShenHui, 0.30f);
            Color opening = Color.Lerp(Palette.MiBai, Color.white, 0.55f);
            Vector3 doorDir = new Vector3(0f, 0.2f, 1f).normalized;

            for (int f = 0; f < 6; f++)
            {
                var face = (CubemapFace)f;
                var px = new Color[S * S];
                for (int y = 0; y < S; y++)
                {
                    for (int x = 0; x < S; x++)
                    {
                        float u = (x + 0.5f) / S * 2f - 1f;
                        float v = (y + 0.5f) / S * 2f - 1f;
                        Vector3 d = FaceDir(face, u, v);
                        float h = d.y * 0.5f + 0.5f;
                        Color c = Color.Lerp(ground, sky, Mathf.SmoothStep(0.0f, 1.0f, h));
                        float k = Mathf.Clamp01(Vector3.Dot(d, doorDir));
                        c = Color.Lerp(c, opening, Mathf.Pow(k, 6f) * 0.75f);
                        px[y * S + x] = c;
                    }
                }
                cb.SetPixels(px, face);
            }
            cb.Apply(true, false);
            return cb;
        }

        static Vector3 FaceDir(CubemapFace face, float u, float v)
        {
            switch (face)
            {
                case CubemapFace.PositiveX: return new Vector3(1f, -v, -u).normalized;
                case CubemapFace.NegativeX: return new Vector3(-1f, -v, u).normalized;
                case CubemapFace.PositiveY: return new Vector3(u, 1f, v).normalized;
                case CubemapFace.NegativeY: return new Vector3(u, -1f, -v).normalized;
                case CubemapFace.PositiveZ: return new Vector3(u, -v, 1f).normalized;
                default: return new Vector3(-u, -v, -1f).normalized;
            }
        }
    }
}
