using System;
using UnityEngine;

namespace PorcelainFromClay
{
    /// <summary>Runtime-generated, tileable texture set used by the workshop surfaces.</summary>
    public sealed class SurfaceTex
    {
        public Texture2D Albedo;
        public Texture2D Normal;
        public Texture2D Smoothness;
        public Texture2D Occlusion;
    }

    public static class ProcTex
    {
        struct Pixel
        {
            public Color Color;
            public float Height;
            public float Smooth;
        }

        static Color Hex(int hex)
        {
            return new Color(
                ((hex >> 16) & 255) / 255f,
                ((hex >> 8) & 255) / 255f,
                (hex & 255) / 255f,
                1f);
        }

        static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / Mathf.Max(0.00001f, b - a));
            return t * t * (3f - 2f * t);
        }

        static int Wrap(int v, int n)
        {
            int r = v % n;
            return r < 0 ? r + n : r;
        }

        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFFu) / 16777215f;
            }
        }

        static float HashPixel(int x, int y, int size, int seed)
        {
            return Hash(Wrap(x, size), Wrap(y, size), seed);
        }

        static float Value(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float xf = x - xi;
            float yf = y - yi;
            xf = xf * xf * (3f - 2f * xf);
            yf = yf * yf * (3f - 2f * yf);
            float a = Hash(xi, yi, seed);
            float b = Hash(xi + 1, yi, seed);
            float c = Hash(xi, yi + 1, seed);
            float d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, xf), Mathf.Lerp(c, d, xf), yf);
        }

        /// <summary>Value noise whose lattice wraps at <paramref name="period"/>.</summary>
        static float TileValue(float u, float v, int period, int seed)
        {
            float x = u * period;
            float y = v * period;
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float xf = x - xi;
            float yf = y - yi;
            xf = xf * xf * (3f - 2f * xf);
            yf = yf * yf * (3f - 2f * yf);
            float a = Hash(Wrap(xi, period), Wrap(yi, period), seed);
            float b = Hash(Wrap(xi + 1, period), Wrap(yi, period), seed);
            float c = Hash(Wrap(xi, period), Wrap(yi + 1, period), seed);
            float d = Hash(Wrap(xi + 1, period), Wrap(yi + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, xf), Mathf.Lerp(c, d, xf), yf);
        }

        public static float TileFbm(float u, float v, int baseFreq, int octaves, int seed)
        {
            float sum = 0f;
            float amp = 0.5f;
            float norm = 0f;
            int f = Mathf.Max(1, baseFreq);
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * TileValue(u, v, f, seed + i * 131);
                norm += amp;
                amp *= 0.5f;
                f *= 2;
            }
            return sum / Mathf.Max(0.00001f, norm);
        }

        static float Grain(float u, float v, int freq, int seed)
        {
            return Mathf.Abs(Mathf.Sin((v * freq + TileFbm(u, v, 4, 4, seed) * 5f) * Mathf.PI));
        }

        static Texture2D NewTex(string name, int size, bool linear)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true, linear);
            t.name = name;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 6;
            return t;
        }

        static SurfaceTex Build(string name, int size, Func<float, float, Pixel> fn, float normalStrength)
        {
            int count = size * size;
            var colors = new Color32[count];
            var heights = new float[count];
            var smooths = new Color32[count];

            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    Pixel p = fn(u, v);
                    int i = y * size + x;
                    colors[i] = p.Color;
                    heights[i] = Mathf.Clamp01(p.Height);
                    byte sm = (byte)(Mathf.Clamp01(p.Smooth) * 255f);
                    smooths[i] = new Color32(sm, sm, sm, 255);
                }
            }

            var set = new SurfaceTex();
            set.Albedo = NewTex(name + "_Albedo", size, false);
            set.Albedo.SetPixels32(colors);
            set.Albedo.Apply(true, false);

            set.Normal = NewTex(name + "_Normal", size, true);
            set.Normal.SetPixels32(HeightToNormal(heights, size, normalStrength));
            set.Normal.Apply(true, false);

            set.Smoothness = NewTex(name + "_Smooth", size, true);
            set.Smoothness.SetPixels32(smooths);
            set.Smoothness.Apply(true, false);

            set.Occlusion = NewTex(name + "_AO", size, true);
            set.Occlusion.SetPixels32(HeightToAo(heights, size));
            set.Occlusion.Apply(true, false);
            return set;
        }

        static Color32[] HeightToNormal(float[] height, int size, float strength)
        {
            var result = new Color32[height.Length];
            for (int y = 0; y < size; y++)
            {
                int ym = Wrap(y - 1, size);
                int yp = Wrap(y + 1, size);
                for (int x = 0; x < size; x++)
                {
                    int xm = Wrap(x - 1, size);
                    int xp = Wrap(x + 1, size);
                    float hl = height[y * size + xm];
                    float hr = height[y * size + xp];
                    float hd = height[ym * size + x];
                    float hu = height[yp * size + x];
                    Vector3 n = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
                    result[y * size + x] = new Color32(
                        (byte)((n.x * 0.5f + 0.5f) * 255f),
                        (byte)((n.y * 0.5f + 0.5f) * 255f),
                        (byte)((n.z * 0.5f + 0.5f) * 255f),
                        255);
                }
            }
            return result;
        }

        static Color32[] HeightToAo(float[] height, int size)
        {
            var result = new Color32[height.Length];
            for (int y = 0; y < size; y++)
            {
                int ym = Wrap(y - 1, size);
                int yp = Wrap(y + 1, size);
                for (int x = 0; x < size; x++)
                {
                    int xm = Wrap(x - 1, size);
                    int xp = Wrap(x + 1, size);
                    float avg = 0f;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int sy = Wrap(y + oy, size);
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int sx = Wrap(x + ox, size);
                            avg += height[sy * size + sx];
                        }
                    }
                    avg /= 9f;
                    float cavity = Mathf.Clamp01((avg - height[y * size + x]) * 18f);
                    byte a = (byte)(Mathf.Lerp(1f, 0.45f, cavity) * 255f);
                    result[y * size + x] = new Color32(a, a, a, 255);
                }
            }
            return result;
        }

        public static SurfaceTex Plaster(int size = 1024)
        {
            Color a = Hex(0xC7C2B6);
            Color b = Hex(0xAEA99D);
            Color patch = Hex(0xD7D2C6);
            Color dirt = Hex(0x8B867B);

            return Build("Plaster", size, (u, v) =>
            {
                float n = TileFbm(u, v, 3, 6, 101);
                float fine = TileFbm(u, v, 48, 3, 102);
                float broad = TileFbm(u, v, 2, 4, 103);
                float stain = SmoothStep(0.62f, 0.88f, TileFbm(u, v, 2, 5, 104));
                float streak = TileFbm(u * 5f, v, 6, 4, 105);
                float ridge = 1f - Mathf.Abs(TileFbm(u, v, 5, 5, 106) * 2f - 1f);
                float crack = SmoothStep(0.986f, 0.999f, ridge);

                Color c = Color.Lerp(a, b, n);
                c = Color.Lerp(c, patch, SmoothStep(0.55f, 0.8f, broad) * 0.12f);
                c = Color.Lerp(c, dirt, stain * 0.16f);
                c = Color.Lerp(c, dirt, SmoothStep(0.5f, 0.85f, streak) * 0.045f);
                c = Color.Lerp(c, Hex(0x7B766C), crack * 0.14f);
                c *= 0.972f + fine * 0.055f;

                float h = n * 0.22f + fine * 0.1f - crack * 0.1f + stain * 0.02f;
                float s = 0.035f + fine * 0.025f + stain * 0.01f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 9f);
        }

        public static SurfaceTex Stone(int size = 1024)
        {
            Color a = Hex(0xA39E93);
            Color b = Hex(0x8B877D);
            Color pale = Hex(0xBAB4A8);
            Color dark = Hex(0x67635A);

            return Build("Stone", size, (u, v) =>
            {
                float n = TileFbm(u, v, 4, 6, 201);
                float mott = TileFbm(u, v, 12, 4, 202);
                float fine = TileFbm(u, v, 64, 3, 203);
                float chalk = SmoothStep(0.6f, 0.92f, TileFbm(u, v, 7, 3, 204));
                float vein = 1f - Mathf.Abs(TileFbm(u, v, 5, 5, 205) * 2f - 1f);
                vein = SmoothStep(0.84f, 0.99f, vein);
                int px = (int)(u * size);
                int py = (int)(v * size);
                float speck = HashPixel(px, py, size, 206);
                float pore = HashPixel(px / 2, py / 2, size / 2, 207);
                float pit = speck > 0.997f ? 1f : 0f;
                float darkSpeck = pore > 0.991f ? 1f : 0f;

                Color c = Color.Lerp(a, b, n);
                c = Color.Lerp(c, pale, chalk * 0.24f);
                c = Color.Lerp(c, dark, vein * 0.13f);
                c = Color.Lerp(c, dark, darkSpeck * 0.18f);
                c = Color.Lerp(c, pale, pit * 0.09f);
                c *= 0.965f + fine * 0.07f;

                float h = n * 0.25f + mott * 0.1f + pit * 0.1f - vein * 0.06f;
                float s = 0.06f + chalk * 0.035f + fine * 0.02f - pit * 0.015f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 11f);
        }

        public static SurfaceTex Wood(int size = 1024)
        {
            Color a = Hex(0x70543B);
            Color b = Hex(0x4B3523);
            Color worn = Hex(0x9A7B59);
            Color dark = Hex(0x2F2015);

            return Build("Wood", size, (u, v) =>
            {
                float warp = TileFbm(u * 1.5f, v * 2f, 4, 4, 301);
                float slow = TileFbm(u, v, 3, 5, 302);
                float fine = TileFbm(u, v, 32, 3, 303);
                float grain = 0.5f + 0.5f * Mathf.Sin((v * 72f + warp * 8f + slow * 5f) * Mathf.PI * 2f);
                float fiber = 0.5f + 0.5f * Mathf.Sin((v * 360f + warp * 14f) * Mathf.PI * 2f);
                float streak = SmoothStep(0.68f, 0.95f, grain);
                float wornEdge = SmoothStep(0.7f, 0.95f, TileFbm(u, v, 4, 3, 304));

                // one calm knot, kept small so the tiled surface stays believable
                float ku = Mathf.Repeat(u - 0.31f, 1f);
                float kv = Mathf.Repeat(v - 0.62f, 1f);
                float kd = Mathf.Sqrt(ku * ku * 2.4f + kv * kv * 0.7f);
                float knot = SmoothStep(0.055f, 0.012f, kd) * 0.85f;
                float ring = Mathf.Abs(Mathf.Sin(kd * 110f)) * SmoothStep(0.11f, 0.04f, kd);

                Color c = Color.Lerp(a, b, slow * 0.5f + grain * 0.35f);
                c = Color.Lerp(c, dark, streak * 0.35f);
                c = Color.Lerp(c, worn, wornEdge * 0.1f);
                c = Color.Lerp(c, dark, knot * 0.7f + ring * 0.15f);
                c *= 0.93f + fine * 0.12f;

                float h = grain * 0.3f + fiber * 0.1f + knot * 0.25f - streak * 0.18f;
                float s = 0.11f + wornEdge * 0.08f + fine * 0.03f - knot * 0.03f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 15f);
        }

        public static SurfaceTex Bamboo(int size = 512)
        {
            Color a = Hex(0xA58E64);
            Color b = Hex(0x806C48);
            Color dark = Hex(0x59492F);
            return Build("Bamboo", size, (u, v) =>
            {
                float fiber = 0.5f + 0.5f * Mathf.Sin((u * 120f + TileFbm(u, v, 8, 3, 401) * 10f) * Mathf.PI);
                float n = TileFbm(u, v, 5, 4, 402);
                float node = 1f - SmoothStep(0.0f, 0.045f, Mathf.Abs(Mathf.Repeat(v - 0.27f, 0.5f) - 0.25f));
                float dust = SmoothStep(0.55f, 0.9f, TileFbm(u, v, 3, 4, 403));
                Color c = Color.Lerp(a, b, n * 0.55f + fiber * 0.25f);
                c = Color.Lerp(c, dark, node * 0.35f);
                c = Color.Lerp(c, Hex(0xB5A077), dust * 0.12f);
                float h = fiber * 0.25f + n * 0.15f - node * 0.2f;
                float s = 0.16f + dust * 0.08f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 12f);
        }

        public static SurfaceTex DryPowder(int size = 512)
        {
            Color a = Hex(0xCFC8B7);
            Color b = Hex(0xAFA797);
            Color pale = Hex(0xE0DACB);
            return Build("DryPowder", size, (u, v) =>
            {
                float n = TileFbm(u, v, 16, 5, 501);
                float clump = TileFbm(u, v, 5, 4, 502);
                int px = (int)(u * size);
                int py = (int)(v * size);
                float grit = HashPixel(px, py, size, 503);
                float stone = grit > 0.94f ? 1f : 0f;
                Color c = Color.Lerp(a, b, n * 0.55f + clump * 0.25f);
                c = Color.Lerp(c, pale, SmoothStep(0.62f, 0.92f, n) * 0.25f);
                c = Color.Lerp(c, Hex(0x8C8577), stone * 0.4f);
                float h = n * 0.4f + clump * 0.2f + stone * 0.16f;
                float s = 0.018f + n * 0.02f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 18f);
        }

        public static SurfaceTex RawStone(int size = 512)
        {
            Color a = Hex(0xB9B3A6);
            Color b = Hex(0x928C80);
            Color dark = Hex(0x615D54);
            return Build("RawStone", size, (u, v) =>
            {
                float n = TileFbm(u, v, 5, 5, 601);
                float mott = TileFbm(u, v, 14, 4, 602);
                float vein = 1f - Mathf.Abs(TileFbm(u, v, 4, 5, 603) * 2f - 1f);
                vein = SmoothStep(0.82f, 0.99f, vein);
                float fine = TileFbm(u, v, 48, 3, 604);
                Color c = Color.Lerp(a, b, n * 0.7f + mott * 0.25f);
                c = Color.Lerp(c, dark, vein * 0.3f);
                c *= 0.93f + fine * 0.11f;
                float h = n * 0.35f + mott * 0.18f - vein * 0.12f;
                float s = 0.06f + mott * 0.04f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 18f);
        }

        public static SurfaceTex WetClay(int size = 512)
        {
            Color a = Hex(0x817B70);
            Color b = Hex(0x666157);
            Color pale = Hex(0x9A9386);
            return Build("WetClay", size, (u, v) =>
            {
                float n = TileFbm(u, v, 8, 5, 701);
                float swirl = TileFbm(u * 2f, v * 2f, 6, 4, 702);
                float smear = 0.5f + 0.5f * Mathf.Sin((u * 8f + swirl * 6f) * Mathf.PI);
                float fine = TileFbm(u, v, 64, 2, 703);
                Color c = Color.Lerp(a, b, n * 0.65f + smear * 0.2f);
                c = Color.Lerp(c, pale, SmoothStep(0.6f, 0.9f, swirl) * 0.2f);
                c *= 0.95f + fine * 0.08f;
                float h = n * 0.18f + smear * 0.22f + fine * 0.06f;
                float s = 0.13f + n * 0.05f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 10f);
        }

        public static SurfaceTex DryClay(int size = 512)
        {
            Color a = Hex(0xAAA192);
            Color b = Hex(0x8C8477);
            Color crack = Hex(0x635D53);
            return Build("DryClay", size, (u, v) =>
            {
                float n = TileFbm(u, v, 7, 5, 801);
                float fine = TileFbm(u, v, 40, 3, 802);
                float ridge = 1f - Mathf.Abs(TileFbm(u, v, 9, 5, 803) * 2f - 1f);
                float cr = SmoothStep(0.94f, 0.995f, ridge);
                Color c = Color.Lerp(a, b, n * 0.5f + fine * 0.2f);
                c = Color.Lerp(c, crack, cr * 0.35f);
                float h = n * 0.18f + fine * 0.08f - cr * 0.12f;
                float s = 0.045f + n * 0.025f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 14f);
        }

        public static SurfaceTex Linen(int size = 512)
        {
            Color a = Hex(0xC1B8A8);
            Color b = Hex(0xAFA696);
            Color pale = Hex(0xD2CABB);
            return Build("Linen", size, (u, v) =>
            {
                float weaveU = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 72f));
                float weaveV = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 72f));
                float weave = Mathf.Max(weaveU, weaveV);
                float slub = TileFbm(u * 2f, v, 12, 4, 901);
                float stain = SmoothStep(0.6f, 0.9f, TileFbm(u, v, 3, 4, 902));
                Color c = Color.Lerp(a, b, (1f - weave) * 0.18f + slub * 0.2f);
                c = Color.Lerp(c, pale, weave * 0.08f);
                c = Color.Lerp(c, Hex(0x8D8372), stain * 0.1f);
                float h = weave * 0.18f + slub * 0.16f;
                float s = 0.02f + slub * 0.015f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 10f);
        }

        public static SurfaceTex Paper(int size = 512)
        {
            Color a = Hex(0xECE5D6);
            Color b = Hex(0xDDD4C2);
            Color fiber = Hex(0xC8BFAE);
            return Build("Paper", size, (u, v) =>
            {
                float n = TileFbm(u, v, 6, 5, 1001);
                float fine = TileFbm(u, v, 90, 3, 1002);
                float threads = SmoothStep(0.78f, 0.98f, TileFbm(u * 6f, v, 12, 3, 1003));
                float stain = SmoothStep(0.62f, 0.88f, TileFbm(u, v, 2, 4, 1004));
                Color c = Color.Lerp(a, b, n * 0.45f + fine * 0.2f);
                c = Color.Lerp(c, fiber, threads * 0.12f);
                c = Color.Lerp(c, Hex(0xC9BFA9), stain * 0.08f);
                float h = fine * 0.15f + threads * 0.1f;
                float s = 0.03f + fine * 0.02f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 8f);
        }

        public static SurfaceTex Skin(int size = 256)
        {
            Color a = Hex(0xB69A85);
            Color b = Hex(0x9F816C);
            Color pink = Hex(0xC4A38D);
            return Build("Skin", size, (u, v) =>
            {
                float n = TileFbm(u, v, 9, 4, 1101);
                float fine = TileFbm(u, v, 80, 3, 1102);
                float knuckle = SmoothStep(0.68f, 0.92f, TileFbm(u, v, 4, 4, 1103));
                Color c = Color.Lerp(a, b, n * 0.4f + fine * 0.13f);
                c = Color.Lerp(c, pink, knuckle * 0.15f);
                float h = fine * 0.12f + n * 0.12f;
                float s = 0.33f + fine * 0.1f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 8f);
        }

        public static SurfaceTex Basket(int size = 512)
        {
            Color a = Hex(0xA18456);
            Color b = Hex(0x7A6038);
            Color dark = Hex(0x4C3C24);
            return Build("Basket", size, (u, v) =>
            {
                float x = u * 28f;
                float y = v * 18f;
                int ix = Mathf.FloorToInt(x);
                int iy = Mathf.FloorToInt(y);
                float fx = x - ix;
                float fy = y - iy;
                bool over = ((ix + iy) & 1) == 0;
                float strip = over
                    ? SmoothStep(0.05f, 0.22f, fy) * SmoothStep(0.95f, 0.78f, fy)
                    : SmoothStep(0.05f, 0.22f, fx) * SmoothStep(0.95f, 0.78f, fx);
                float fiber = TileFbm(u * 3f, v * 3f, 40, 3, 1201);
                Color c = Color.Lerp(b, a, strip * 0.7f);
                c = Color.Lerp(c, dark, (1f - strip) * 0.55f);
                c *= 0.9f + fiber * 0.16f;
                float h = strip * 0.5f + fiber * 0.15f;
                float s = 0.08f + strip * 0.06f;
                return new Pixel { Color = c, Height = h, Smooth = s };
            }, 22f);
        }

        public static SurfaceTex Straw(int size = 256)
        {
            Color a = Hex(0xA89162);
            Color b = Hex(0x7E6A45);
            return Build("Straw", size, (u, v) =>
            {
                float fiber = 0.5f + 0.5f * Mathf.Sin((u * 210f + TileFbm(u, v, 8, 3, 1301) * 8f) * Mathf.PI);
                float n = TileFbm(u, v, 20, 3, 1302);
                Color c = Color.Lerp(a, b, fiber * 0.45f + n * 0.2f);
                float h = fiber * 0.3f + n * 0.1f;
                return new Pixel { Color = c, Height = h, Smooth = 0.1f + n * 0.04f };
            }, 16f);
        }

        public static Texture2D WaterNormal(int size = 512)
        {
            var heights = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float a = Mathf.Sin((u * 2f + v * 1f + TileFbm(u, v, 4, 4, 1401) * 0.3f) * Mathf.PI * 2f);
                    float b = Mathf.Sin((u * -1f + v * 3f + TileFbm(u, v, 7, 3, 1402) * 0.2f) * Mathf.PI * 2f);
                    float c = TileFbm(u, v, 16, 4, 1403);
                    heights[y * size + x] = a * 0.2f + b * 0.15f + c * 0.2f;
                }
            }
            var t = NewTex("Water_Normal", size, true);
            t.SetPixels32(HeightToNormal(heights, size, 26f));
            t.Apply(true, false);
            return t;
        }

        public static Texture2D RadialShadow(int size = 256)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            t.name = "RadialShadow";
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x / (size - 1f) - 0.5f) * 2f;
                    float dy = (y / (size - 1f) - 0.5f) * 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - SmoothStep(0.18f, 1f, r);
                    a *= 0.72f + 0.28f * (1f - Mathf.Abs(dx));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(true, false);
            return t;
        }

        public static Texture2D Ring(int size = 128, float thickness = 0.055f)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            t.name = "Ring";
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x / (size - 1f) - 0.5f) * 2f;
                    float dy = (y / (size - 1f) - 0.5f) * 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.86f) / thickness);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(ring * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(true, false);
            return t;
        }

        public static Texture2D SoftLine(int w = 512, int h = 64)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            t.name = "SoftLine";
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float fy = Mathf.Abs((y / (h - 1f)) - 0.5f) * 2f;
                float a = 1f - SmoothStep(0.0f, 1f, fy);
                for (int x = 0; x < w; x++)
                {
                    float fx = Mathf.Clamp01(Mathf.Min(x / 40f, (w - 1 - x) / 40f));
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * fx * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }
    }
}
