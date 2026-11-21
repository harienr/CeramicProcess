using System.Collections.Generic;
using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 程序化网格库。规范要求“近景道具适度圆角、避免默认几何体的锋利边缘”。
    /// 因此所有箱体都是倒角圆角盒，木料带轻微弯曲与锥度。
    /// 单位：1 单位 = 1 米。
    /// </summary>
    public static class MeshKit
    {
        // ======================= 构建器 =======================
        public class MB
        {
            public readonly List<Vector3> V = new List<Vector3>(2048);
            public readonly List<Vector3> N = new List<Vector3>(2048);
            public readonly List<Vector2> UV = new List<Vector2>(2048);
            public readonly List<int> T = new List<int>(4096);

            public int Vert(Vector3 p, Vector3 n, Vector2 uv)
            {
                V.Add(p); N.Add(n); UV.Add(uv);
                return V.Count - 1;
            }

            /// <summary>四边面。按传入法线自动修正绕序，避免法线朝内。</summary>
            public void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
                             Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3,
                             Vector2 t0, Vector2 t1, Vector2 t2, Vector2 t3)
            {
                Vector3 gn = Vector3.Cross(p1 - p0, p2 - p0);
                Vector3 avg = n0 + n1 + n2 + n3;
                int b = V.Count;
                if (Vector3.Dot(gn, avg) >= 0f)
                {
                    Vert(p0, n0, t0); Vert(p1, n1, t1); Vert(p2, n2, t2); Vert(p3, n3, t3);
                }
                else
                {
                    Vert(p3, n3, t3); Vert(p2, n2, t2); Vert(p1, n1, t1); Vert(p0, n0, t0);
                }
                T.Add(b); T.Add(b + 1); T.Add(b + 2);
                T.Add(b); T.Add(b + 2); T.Add(b + 3);
            }

            public void Triangle(Vector3 p0, Vector3 p1, Vector3 p2,
                                 Vector3 n0, Vector3 n1, Vector3 n2,
                                 Vector2 t0, Vector2 t1, Vector2 t2)
            {
                Vector3 gn = Vector3.Cross(p1 - p0, p2 - p0);
                Vector3 avg = n0 + n1 + n2;
                int b = V.Count;
                if (Vector3.Dot(gn, avg) >= 0f)
                {
                    Vert(p0, n0, t0); Vert(p1, n1, t1); Vert(p2, n2, t2);
                }
                else
                {
                    Vert(p2, n2, t2); Vert(p1, n1, t1); Vert(p0, n0, t0);
                }
                T.Add(b); T.Add(b + 1); T.Add(b + 2);
            }

            public Mesh Build(string name, bool smoothNormals = false, bool flatNormals = false)
            {
                var m = new Mesh();
                m.name = name;
                if (flatNormals) Flat();
                m.indexFormat = V.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;
                m.SetVertices(V);
                m.SetNormals(N);
                m.SetUVs(0, UV);
                m.SetTriangles(T, 0);
                if (smoothNormals) m.RecalculateNormals();
                m.RecalculateTangents();
                m.RecalculateBounds();
                return m;
            }

            /// <summary>按三角面拆点，得到硬边（碎石、劈裂木料用）</summary>
            public void Flat()
            {
                var v2 = new List<Vector3>(T.Count);
                var n2 = new List<Vector3>(T.Count);
                var u2 = new List<Vector2>(T.Count);
                var t2 = new List<int>(T.Count);
                for (int i = 0; i < T.Count; i += 3)
                {
                    Vector3 a = V[T[i]], b = V[T[i + 1]], c = V[T[i + 2]];
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    if (n.sqrMagnitude < 1e-12f) continue;
                    n.Normalize();
                    for (int k = 0; k < 3; k++)
                    {
                        int src = T[i + k];
                        v2.Add(V[src]); n2.Add(n); u2.Add(UV[src]);
                        t2.Add(v2.Count - 1);
                    }
                }
                V.Clear(); N.Clear(); UV.Clear(); T.Clear();
                V.AddRange(v2); N.AddRange(n2); UV.AddRange(u2); T.AddRange(t2);
            }
        }

        // ======================= 盒体 =======================

        /// <summary>
        /// 圆角盒：12 条棱全部倒角。
        /// 网格用“非均匀 4×4 顶点/面”：外圈落在倒角带上、内圈落在平面内，
        /// 这样平面部分法线严格朝外（否则一整块地面会被渲染成一个个“枕头”，
        /// 出现一大片明暗棋盘的错觉）。grainAxis 决定贴图 U 方向（木纹走向）。
        /// </summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int seg = 2, float texScale = 1f, int grainAxis = 0)
        {
            float lim = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.49f;
            radius = Mathf.Clamp(radius, 0.0004f, Mathf.Max(0.0005f, lim));
            Vector3 half = size * 0.5f;
            Vector3 inner = half - Vector3.one * radius;
            inner = new Vector3(Mathf.Max(inner.x, 0.0002f), Mathf.Max(inner.y, 0.0002f), Mathf.Max(inner.z, 0.0002f));

            var mb = new MB();
            var p = new Vector3[4];
            var n = new Vector3[4];
            var uv = new Vector2[4];

            for (int a = 0; a < 3; a++)
            {
                for (int s = 0; s < 2; s++)
                {
                    int sign = s == 0 ? 1 : -1;
                    int ua, va;
                    if (a == grainAxis)
                    {
                        ua = (a + 1) % 3;
                        va = (a + 2) % 3;
                    }
                    else
                    {
                        ua = grainAxis;
                        va = 3 - a - grainAxis;
                        if (va == ua) va = ((a + 1) % 3 == ua) ? (a + 2) % 3 : (a + 1) % 3;
                    }

                    // 平面区与倒角带的分界（归一化到 [-1,1]）
                    float bu = Mathf.Clamp(1f - radius / Mathf.Max(1e-4f, half[ua]), 0.2f, 0.995f);
                    float bv = Mathf.Clamp(1f - radius / Mathf.Max(1e-4f, half[va]), 0.2f, 0.995f);
                    float[] fu = { -1f, -bu, bu, 1f };
                    float[] fv = { -1f, -bv, bv, 1f };

                    var gridP = new Vector3[4, 4];
                    var gridN = new Vector3[4, 4];
                    var gridT = new Vector2[4, 4];

                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            Vector3 q = Vector3.zero;
                            q[a] = sign * half[a];
                            q[ua] = fu[i] * half[ua];
                            q[va] = fv[j] * half[va];

                            Vector3 c = new Vector3(
                                Mathf.Clamp(q.x, -inner.x, inner.x),
                                Mathf.Clamp(q.y, -inner.y, inner.y),
                                Mathf.Clamp(q.z, -inner.z, inner.z));
                            Vector3 d = q - c;
                            Vector3 pos, nor;
                            if (d.sqrMagnitude > 1e-12f)
                            {
                                nor = d.normalized;
                                pos = c + nor * radius;
                            }
                            else
                            {
                                pos = q;
                                nor = Vector3.zero;
                                nor[a] = sign;
                            }
                            gridP[i, j] = pos;
                            gridN[i, j] = nor;
                            gridT[i, j] = new Vector2(pos[ua] / texScale, pos[va] / texScale);
                        }
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            p[0] = gridP[i, j]; p[1] = gridP[i + 1, j];
                            p[2] = gridP[i + 1, j + 1]; p[3] = gridP[i, j + 1];
                            n[0] = gridN[i, j]; n[1] = gridN[i + 1, j];
                            n[2] = gridN[i + 1, j + 1]; n[3] = gridN[i, j + 1];
                            uv[0] = gridT[i, j]; uv[1] = gridT[i + 1, j];
                            uv[2] = gridT[i + 1, j + 1]; uv[3] = gridT[i, j + 1];
                            mb.Quad(p[0], p[1], p[2], p[3], n[0], n[1], n[2], n[3],
                                    uv[0], uv[1], uv[2], uv[3]);
                        }
                    }
                }
            }
            return mb.Build("RoundedBox", false, false);
        }

        /// <summary>木板 / 木方：轻微弯曲、锥度与端头参差</summary>
        public static Mesh Timber(Vector3 size, float radius, int seg, float bend,
                                  float taper, float texScale, int seed)
        {
            var mesh = RoundedBox(size, radius, seg, texScale, 0);
            var verts = mesh.vertices;
            var rnd = new System.Random(seed);
            float hx = Mathf.Max(1e-4f, size.x * 0.5f);
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                float tx = Mathf.Clamp(v.x / hx, -1f, 1f);
                v.y += bend * (1f - tx * tx) * Mathf.Sign(v.y) * Mathf.Abs(v.y) * 4f;
                float k = 1f - taper * Mathf.Pow(Mathf.Abs(tx), 3f);
                v.y *= k; v.z *= k;
                if (Mathf.Abs(tx) > 0.97f)
                    v.x += ((float)rnd.NextDouble() - 0.5f) * size.x * 0.012f;
                verts[i] = v;
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.name = "Timber";
            return mesh;
        }

        // ======================= 回转体 =======================

        /// <summary>
        /// 车削体：profile 为 (半径, 高度) 折线，按“外底 → 外壁 → 口沿 → 内壁”顺序给出。
        /// 适合陶盆、缸、木桶、石臼、帽子等。
        /// </summary>
        public static Mesh Lathe(IList<Vector2> profile, int segments, float texScale)
        {
            segments = Mathf.Max(3, segments);
            int n = profile.Count;
            var mb = new MB();

            var pn = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = profile[Mathf.Max(0, i - 1)];
                Vector2 next = profile[Mathf.Min(n - 1, i + 1)];
                Vector2 t = next - prev;
                if (t.sqrMagnitude < 1e-9f) t = new Vector2(0f, 1f);
                t.Normalize();
                pn[i] = new Vector2(t.y, -t.x);
            }

            float circum = 0f;
            for (int i = 0; i < n; i++) circum = Mathf.Max(circum, profile[i].x * Mathf.PI * 2f);
            float uRepeat = Mathf.Max(1f, Mathf.Round(circum / texScale));

            var arc = new float[n];
            for (int i = 1; i < n; i++) arc[i] = arc[i - 1] + Vector2.Distance(profile[i], profile[i - 1]);

            for (int i = 0; i < n - 1; i++)
            {
                float r0 = profile[i].x, r1 = profile[i + 1].x;
                if (r0 < 1e-5f && r1 < 1e-5f) continue;
                for (int s = 0; s < segments; s++)
                {
                    float a0 = (float)s / segments * Mathf.PI * 2f;
                    float a1 = (float)(s + 1) / segments * Mathf.PI * 2f;
                    Vector3 p00 = new Vector3(Mathf.Cos(a0) * r0, profile[i].y, Mathf.Sin(a0) * r0);
                    Vector3 p01 = new Vector3(Mathf.Cos(a1) * r0, profile[i].y, Mathf.Sin(a1) * r0);
                    Vector3 p11 = new Vector3(Mathf.Cos(a1) * r1, profile[i + 1].y, Mathf.Sin(a1) * r1);
                    Vector3 p10 = new Vector3(Mathf.Cos(a0) * r1, profile[i + 1].y, Mathf.Sin(a0) * r1);

                    Vector3 n00 = new Vector3(pn[i].x * Mathf.Cos(a0), pn[i].y, pn[i].x * Mathf.Sin(a0));
                    Vector3 n01 = new Vector3(pn[i].x * Mathf.Cos(a1), pn[i].y, pn[i].x * Mathf.Sin(a1));
                    Vector3 n11 = new Vector3(pn[i + 1].x * Mathf.Cos(a1), pn[i + 1].y, pn[i + 1].x * Mathf.Sin(a1));
                    Vector3 n10 = new Vector3(pn[i + 1].x * Mathf.Cos(a0), pn[i + 1].y, pn[i + 1].x * Mathf.Sin(a0));

                    Vector2 t00 = new Vector2((float)s / segments * uRepeat, arc[i] / texScale);
                    Vector2 t01 = new Vector2((float)(s + 1) / segments * uRepeat, arc[i] / texScale);
                    Vector2 t11 = new Vector2((float)(s + 1) / segments * uRepeat, arc[i + 1] / texScale);
                    Vector2 t10 = new Vector2((float)s / segments * uRepeat, arc[i + 1] / texScale);

                    mb.Quad(p00, p01, p11, p10, n00, n01, n11, n10, t00, t01, t11, t10);
                }
            }
            return mb.Build("Lathe", false, false);
        }

        /// <summary>圆环（铁箍、绳圈、筛圈）</summary>
        public static Mesh Torus(float major, float minor, int majorSeg, int minorSeg, float texScale)
        {
            var mb = new MB();
            float uRep = Mathf.Max(1f, Mathf.Round(major * Mathf.PI * 2f / texScale));
            for (int i = 0; i < majorSeg; i++)
            {
                float a0 = (float)i / majorSeg * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / majorSeg * Mathf.PI * 2f;
                for (int j = 0; j < minorSeg; j++)
                {
                    float b0 = (float)j / minorSeg * Mathf.PI * 2f;
                    float b1 = (float)(j + 1) / minorSeg * Mathf.PI * 2f;
                    Vector3 c0 = new Vector3(Mathf.Cos(a0) * major, 0f, Mathf.Sin(a0) * major);
                    Vector3 c1 = new Vector3(Mathf.Cos(a1) * major, 0f, Mathf.Sin(a1) * major);
                    Vector3 r0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                    Vector3 r1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));

                    Vector3 p00 = c0 + r0 * (Mathf.Cos(b0) * minor) + Vector3.up * (Mathf.Sin(b0) * minor);
                    Vector3 p01 = c0 + r0 * (Mathf.Cos(b1) * minor) + Vector3.up * (Mathf.Sin(b1) * minor);
                    Vector3 p11 = c1 + r1 * (Mathf.Cos(b1) * minor) + Vector3.up * (Mathf.Sin(b1) * minor);
                    Vector3 p10 = c1 + r1 * (Mathf.Cos(b0) * minor) + Vector3.up * (Mathf.Sin(b0) * minor);

                    Vector3 n00 = (p00 - c0).normalized, n01 = (p01 - c0).normalized;
                    Vector3 n11 = (p11 - c1).normalized, n10 = (p10 - c1).normalized;

                    Vector2 t00 = new Vector2((float)i / majorSeg * uRep, (float)j / minorSeg);
                    Vector2 t01 = new Vector2((float)i / majorSeg * uRep, (float)(j + 1) / minorSeg);
                    Vector2 t11 = new Vector2((float)(i + 1) / majorSeg * uRep, (float)(j + 1) / minorSeg);
                    Vector2 t10 = new Vector2((float)(i + 1) / majorSeg * uRep, (float)j / minorSeg);

                    mb.Quad(p00, p01, p11, p10, n00, n01, n11, n10, t00, t01, t11, t10);
                }
            }
            return mb.Build("Torus", false, false);
        }

        /// <summary>杆件（竹竿、木柄），可弯曲</summary>
        public static Mesh Rod(float r0, float r1, float len, int seg, float bend, int rings, float texScale)
        {
            var prof = new List<Vector2>();
            for (int i = 0; i <= rings; i++)
            {
                float t = (float)i / rings;
                prof.Add(new Vector2(Mathf.Lerp(r0, r1, t), t * len));
            }
            var mesh = Lathe(prof, seg, texScale);
            if (Mathf.Abs(bend) > 1e-5f)
            {
                var v = mesh.vertices;
                for (int i = 0; i < v.Length; i++)
                {
                    float t = Mathf.Clamp01(v[i].y / Mathf.Max(0.0001f, len));
                    v[i].z += bend * (1f - Mathf.Pow(2f * t - 1f, 2f));
                }
                mesh.vertices = v;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
            }
            mesh.name = "Rod";
            return mesh;
        }

        // ======================= 石头 =======================

        static float Hash3(int x, int y, int z, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ z * 83492791 ^ seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / 2147483647f;
            }
        }

        static float Noise3(Vector3 p, int seed)
        {
            int xi = Mathf.FloorToInt(p.x), yi = Mathf.FloorToInt(p.y), zi = Mathf.FloorToInt(p.z);
            float fx = p.x - xi, fy = p.y - yi, fz = p.z - zi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy); fz = fz * fz * (3f - 2f * fz);
            float c000 = Hash3(xi, yi, zi, seed), c100 = Hash3(xi + 1, yi, zi, seed);
            float c010 = Hash3(xi, yi + 1, zi, seed), c110 = Hash3(xi + 1, yi + 1, zi, seed);
            float c001 = Hash3(xi, yi, zi + 1, seed), c101 = Hash3(xi + 1, yi, zi + 1, seed);
            float c011 = Hash3(xi, yi + 1, zi + 1, seed), c111 = Hash3(xi + 1, yi + 1, zi + 1, seed);
            float x00 = Mathf.Lerp(c000, c100, fx), x10 = Mathf.Lerp(c010, c110, fx);
            float x01 = Mathf.Lerp(c001, c101, fx), x11 = Mathf.Lerp(c011, c111, fx);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
        }

        static float Fbm3(Vector3 p, int oct, int seed)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int i = 0; i < oct; i++)
            {
                sum += Noise3(p, seed + i * 37) * amp;
                norm += amp;
                p *= 2.03f; amp *= 0.5f;
            }
            return sum / norm;
        }

        /// <summary>不规则石块（瓷石堆、垫脚石）</summary>
        public static Mesh Rock(int seed, Vector3 scale, float roughness, int div, bool flatShade, float flattenBottom)
        {
            var mb = new MB();
            var rnd = new System.Random(seed);
            float off = (float)rnd.NextDouble() * 100f;

            Vector3[][] faces = new Vector3[6][];
            for (int a = 0; a < 3; a++)
            {
                for (int s = 0; s < 2; s++)
                {
                    int sign = s == 0 ? 1 : -1;
                    int ua = (a + 1) % 3, va = (a + 2) % 3;
                    var grid = new Vector3[(div + 1) * (div + 1)];
                    for (int i = 0; i <= div; i++)
                    {
                        for (int j = 0; j <= div; j++)
                        {
                            float fu = (float)i / div * 2f - 1f;
                            float fv = (float)j / div * 2f - 1f;
                            Vector3 q = Vector3.zero;
                            q[a] = sign; q[ua] = fu; q[va] = fv;
                            grid[i * (div + 1) + j] = q.normalized;
                        }
                    }
                    faces[a * 2 + s] = grid;
                }
            }

            for (int f = 0; f < 6; f++)
            {
                int a = f / 2;
                var grid = faces[f];
                for (int i = 0; i < div; i++)
                {
                    for (int j = 0; j < div; j++)
                    {
                        var ps = new Vector3[4];
                        var ns = new Vector3[4];
                        var ts = new Vector2[4];
                        int[] idx = { i * (div + 1) + j, (i + 1) * (div + 1) + j, (i + 1) * (div + 1) + j + 1, i * (div + 1) + j + 1 };
                        for (int k = 0; k < 4; k++)
                        {
                            Vector3 dir = grid[idx[k]];
                            float n1 = Fbm3(dir * 2.1f + Vector3.one * off, 3, seed);
                            float n2 = Fbm3(dir * 5.3f + Vector3.one * off, 2, seed + 5);
                            float disp = 1f + (n1 - 0.5f) * roughness * 1.6f + (n2 - 0.5f) * roughness * 0.5f;
                            Vector3 p = dir * disp;
                            p.x *= scale.x; p.y *= scale.y; p.z *= scale.z;
                            if (flattenBottom > 0f) p.y = Mathf.Max(p.y, -scale.y * flattenBottom);
                            ps[k] = p;
                            ns[k] = dir.normalized;
                            ts[k] = new Vector2(dir.x * 0.7f + dir.z * 0.3f, dir.y * 0.7f + dir.z * 0.3f) * 1.4f;
                        }
                        mb.Quad(ps[0], ps[1], ps[2], ps[3], ns[0], ns[1], ns[2], ns[3], ts[0], ts[1], ts[2], ts[3]);
                    }
                }
            }
            return mb.Build("Rock", false, flatShade);
        }

        // ======================= 面片 =======================

        /// <summary>下凹的筛布 / 布面（带轻微褶皱）</summary>
        public static Mesh ClothRect(float w, float d, int nx, int nz, float sag, float texRepeat, float wrinkle, int seed)
        {
            var mb = new MB();
            var rnd = new System.Random(seed);
            float off = (float)rnd.NextDouble() * 50f;
            var p = new Vector3[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
            {
                for (int j = 0; j <= nz; j++)
                {
                    float u = (float)i / nx, v = (float)j / nz;
                    float x = (u - 0.5f) * w;
                    float z = (v - 0.5f) * d;
                    float bow = 4f * u * (1f - u) * 4f * v * (1f - v);
                    float s = -sag * bow;
                    float wr = (Fbm3(new Vector3(u * 3f + off, v * 3f, 0.5f), 3, seed) - 0.5f) * wrinkle * 2f;
                    s += wr * bow;
                    p[i, j] = new Vector3(x, s, z);
                }
            }
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    Vector3 a = p[i, j], b = p[i + 1, j], c = p[i + 1, j + 1], e = p[i, j + 1];
                    Vector3 n = Vector3.up;
                    var ta = new Vector2((float)i / nx * texRepeat, (float)j / nz * texRepeat);
                    var tb = new Vector2((float)(i + 1) / nx * texRepeat, (float)j / nz * texRepeat);
                    var tc = new Vector2((float)(i + 1) / nx * texRepeat, (float)(j + 1) / nz * texRepeat);
                    var te = new Vector2((float)i / nx * texRepeat, (float)(j + 1) / nz * texRepeat);
                    mb.Quad(a, b, c, e, n, n, n, n, ta, tb, tc, te);
                }
            }
            return mb.Build("Cloth", true, false);
        }

        /// <summary>圆盘 / 液面（带极轻的波纹）</summary>
        public static Mesh Disc(float r, int seg, int rings, float wave, float texScale, int seed)
        {
            var mb = new MB();
            var pts = new Vector3[rings + 1, seg];
            for (int ri = 0; ri <= rings; ri++)
            {
                float rr = r * ri / rings;
                for (int s = 0; s < seg; s++)
                {
                    float a = (float)s / seg * Mathf.PI * 2f;
                    float w = wave * Mathf.Sin(a * 5f + rr * 14f + seed) * Mathf.Sin(rr / Mathf.Max(0.001f, r) * 3.1f);
                    pts[ri, s] = new Vector3(Mathf.Cos(a) * rr, w, Mathf.Sin(a) * rr);
                }
            }
            // 圆心用一个真正的中心点扇出（之前 ri=0 的两个顶点都落在圆心，
            // 三角形退化成零面积，于是每个“液面”中间都有个洞）
            Vector3 center = new Vector3(0f, wave * Mathf.Sin(seed) * 0.35f, 0f);
            Vector3 cn = Vector3.up;
            Vector2 cuv = new Vector2(center.x / texScale, center.z / texScale);
            for (int s = 0; s < seg; s++)
            {
                int s2 = (s + 1) % seg;
                Vector3 b = pts[1, s2], c = pts[1, s];
                var tb = new Vector2(b.x / texScale, b.z / texScale);
                var tc = new Vector2(c.x / texScale, c.z / texScale);
                mb.Triangle(center, b, c, cn, cn, cn, cuv, tb, tc);
            }
            for (int ri = 1; ri < rings; ri++)
            {
                for (int s = 0; s < seg; s++)
                {
                    int s2 = (s + 1) % seg;
                    Vector3 a = pts[ri, s], b = pts[ri, s2], c = pts[ri + 1, s2], d = pts[ri + 1, s];
                    Vector3 n = Vector3.up;
                    var ta = new Vector2(a.x / texScale, a.z / texScale);
                    var tb = new Vector2(b.x / texScale, b.z / texScale);
                    var tc = new Vector2(c.x / texScale, c.z / texScale);
                    var td = new Vector2(d.x / texScale, d.z / texScale);
                    mb.Quad(a, b, c, d, n, n, n, n, ta, tb, tc, td);
                }
            }
            return mb.Build("Disc", true, false);
        }

        /// <summary>圆环面片（水面涟漪用）</summary>
        public static Mesh Annulus(float r0, float r1, int seg = 40)
        {
            var mb = new MB();
            for (int i = 0; i < seg; i++)
            {
                float a0 = (float)i / seg * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / seg * Mathf.PI * 2f;
                Vector3 p00 = new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                Vector3 p01 = new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
                Vector3 p11 = new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1);
                Vector3 p10 = new Vector3(Mathf.Cos(a0) * r1, 0f, Mathf.Sin(a0) * r1);
                Vector3 n = Vector3.up;
                mb.Quad(p00, p01, p11, p10, n, n, n, n,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            }
            return mb.Build("Annulus", false, false);
        }

        /// <summary>平面片（水渍、纸签、地面痕）</summary>
        public static Mesh Plane(float w, float h, int nx, int nz, float texRepeat)
        {
            var mb = new MB();
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float u0 = (float)i / nx, u1 = (float)(i + 1) / nx;
                    float v0 = (float)j / nz, v1 = (float)(j + 1) / nz;
                    Vector3 a = new Vector3((u0 - 0.5f) * w, 0f, (v0 - 0.5f) * h);
                    Vector3 b = new Vector3((u1 - 0.5f) * w, 0f, (v0 - 0.5f) * h);
                    Vector3 c = new Vector3((u1 - 0.5f) * w, 0f, (v1 - 0.5f) * h);
                    Vector3 d = new Vector3((u0 - 0.5f) * w, 0f, (v1 - 0.5f) * h);
                    Vector3 n = Vector3.up;
                    mb.Quad(a, b, c, d, n, n, n, n,
                        new Vector2(u0, v0) * texRepeat, new Vector2(u1, v0) * texRepeat,
                        new Vector2(u1, v1) * texRepeat, new Vector2(u0, v1) * texRepeat);
                }
            }
            return mb.Build("Plane", false, false);
        }

        /// <summary>光柱棱柱：从窗口的 4 个角沿光向投到地面</summary>
        public static Mesh LightShaft(Vector3[] windowCorners, Vector3 dir, float floorY)
        {
            var mb = new MB();
            dir = dir.normalized;
            var far = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float t = 0f;
                if (dir.y < -1e-4f) t = (windowCorners[i].y - floorY) / -dir.y;
                far[i] = windowCorners[i] + dir * t;
            }
            for (int i = 0; i < 4; i++)
            {
                int i2 = (i + 1) % 4;
                Vector3 a = windowCorners[i], b = windowCorners[i2];
                Vector3 c = far[i2], d = far[i];
                Vector3 nn = Vector3.Cross(b - a, c - a).normalized;
                mb.Quad(a, b, c, d, nn, nn, nn, nn,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            }
            Vector3 capN = -dir;
            mb.Quad(windowCorners[0], windowCorners[1], windowCorners[2], windowCorners[3],
                capN, capN, capN, capN, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            Vector3 capN2 = dir;
            mb.Quad(far[0], far[1], far[2], far[3],
                capN2, capN2, capN2, capN2, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            return mb.Build("LightShaft", false, false);
        }

        /// <summary>一张圆筛（圈 + 篾面），水平放置</summary>
        public static Mesh SieveRound(float radius, float rimH, float rimW, int seg)
        {
            var prof = new List<Vector2>
            {
                new Vector2(radius - rimW, 0f),
                new Vector2(radius, 0f),
                new Vector2(radius, rimH),
                new Vector2(radius - rimW, rimH),
                new Vector2(radius - rimW, 0f),
            };
            return Lathe(prof, seg, 0.5f);
        }
    }
}
