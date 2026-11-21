using System.Collections.Generic;
using UnityEngine;

namespace Cishan
{
    /// <summary>
    /// Small procedural mesh kit for the workshop blockout. Everything is built in
    /// metres with real-world proportions; UV V follows the wood grain direction.
    /// </summary>
    public static class ProcMesh
    {
        public static Mesh BevelBox(Vector3 size, float bevel, Vector3 grain,
            float uvScale = 0.55f, float jitter = 0f, int seed = 0)
        {
            var b = new Builder();
            Vector3 half = size * 0.5f;
            float c = Mathf.Min(bevel, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.45f);
            if (grain.sqrMagnitude < 0.0001f) grain = Vector3.up;
            grain.Normalize();

            // six inset faces
            for (int axis = 0; axis < 3; axis++)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    int a1 = (axis + 1) % 3;
                    int a2 = (axis + 2) % 3;
                    Vector3 n = Axis(axis) * s;
                    Vector3 center = n * half[axis];
                    Vector3 u = Axis(a1) * (half[a1] - c);
                    Vector3 v = Axis(a2) * (half[a2] - c);
                    b.Quad(center - u - v, center + u - v, center + u + v, center - u + v, n, grain, uvScale, jitter, seed);
                }
            }

            // twelve chamfer strips
            for (int a = 0; a < 3; a++)
            {
                int a2 = (a + 1) % 3;
                int a3 = (a + 2) % 3;
                for (int sa = -1; sa <= 1; sa += 2)
                {
                    for (int sb = -1; sb <= 1; sb += 2)
                    {
                        Vector3 n = (Axis(a) * sa + Axis(a2) * sb).normalized;
                        float e = half[a3] - c;
                        Vector3 p1m = Vector3.zero, p1p = Vector3.zero, p2m = Vector3.zero, p2p = Vector3.zero;
                        p1m[a] = sa * half[a];
                        p1m[a2] = sb * (half[a2] - c);
                        p1m[a3] = -e;
                        p1p = p1m;
                        p1p[a3] = e;
                        p2m[a] = sa * (half[a] - c);
                        p2m[a2] = sb * half[a2];
                        p2m[a3] = -e;
                        p2p = p2m;
                        p2p[a3] = e;
                        b.Quad(p1m, p1p, p2p, p2m, n, grain, uvScale, jitter, seed);
                    }
                }
            }

            // eight corner triangles
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 n = new Vector3(sx, sy, sz).normalized;
                        Vector3 px = new Vector3(sx * half.x, sy * (half.y - c), sz * (half.z - c));
                        Vector3 py = new Vector3(sx * (half.x - c), sy * half.y, sz * (half.z - c));
                        Vector3 pz = new Vector3(sx * (half.x - c), sy * (half.y - c), sz * half.z);
                        b.Triangle(px, py, pz, n, grain, uvScale, jitter, seed);
                    }
                }
            }

            return b.Build("Cishan_BevelBox");
        }

        /// <summary>Revolve a profile ordered counter-clockwise in (radius, height).</summary>
        public static Mesh Lathe(Vector2[] profile, int segments = 28, float uvScale = 1f,
            bool smooth = true, int seed = 0)
        {
            segments = Mathf.Max(3, segments);
            int rings = profile.Length;
            var verts = new Vector3[(segments + 1) * rings];
            var norms = new Vector3[verts.Length];
            var uvs = new Vector2[verts.Length];
            var tris = new List<int>((segments) * (rings - 1) * 6);

            var lens = new float[rings];
            for (int i = 1; i < rings; i++) lens[i] = lens[i - 1] + Vector2.Distance(profile[i - 1], profile[i]);

            for (int s = 0; s <= segments; s++)
            {
                float t = (float)s / segments;
                float ang = t * Mathf.PI * 2f;
                float cs = Mathf.Cos(ang);
                float sn = Mathf.Sin(ang);
                for (int i = 0; i < rings; i++)
                {
                    Vector2 p = profile[i];
                    Vector2 tan;
                    if (i == 0) tan = profile[1] - profile[0];
                    else if (i == rings - 1) tan = profile[rings - 1] - profile[rings - 2];
                    else tan = profile[i + 1] - profile[i - 1];
                    if (tan.sqrMagnitude < 1e-8f) tan = Vector2.up;
                    Vector2 n2 = new Vector2(tan.y, -tan.x).normalized;
                    int idx = s * rings + i;
                    verts[idx] = new Vector3(p.x * cs, p.y, p.x * sn);
                    norms[idx] = new Vector3(n2.x * cs, n2.y, n2.x * sn);
                    uvs[idx] = new Vector2(t * 2f, lens[i] * uvScale);
                }
            }

            for (int s = 0; s < segments; s++)
            {
                for (int i = 0; i < rings - 1; i++)
                {
                    int a = s * rings + i;
                    int b = (s + 1) * rings + i;
                    int c = a + 1;
                    int d = b + 1;
                    if (profile[i].x <= 0.0001f && profile[i + 1].x <= 0.0001f) continue;
                    if (profile[i].x <= 0.0001f)
                    {
                        tris.Add(a); tris.Add(d); tris.Add(c);
                    }
                    else if (profile[i + 1].x <= 0.0001f)
                    {
                        tris.Add(a); tris.Add(b); tris.Add(c);
                    }
                    else
                    {
                        tris.Add(a); tris.Add(c); tris.Add(d);
                        tris.Add(a); tris.Add(d); tris.Add(b);
                    }
                }
            }

            var mesh = new Mesh { name = "Cishan_Lathe" };
            mesh.indexFormat = verts.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = verts;
            mesh.normals = smooth ? norms : RebuildFlat(verts, tris, norms);
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Cylinder(float radius, float height, int segments = 24)
        {
            return Lathe(new[]
            {
                new Vector2(0f, 0f),
                new Vector2(radius, 0f),
                new Vector2(radius, height),
                new Vector2(0f, height)
            }, segments, 1f);
        }

        public static Mesh Disc(float radius, float thickness, int segments = 24)
        {
            return Lathe(new[]
            {
                new Vector2(0f, 0f),
                new Vector2(radius, 0f),
                new Vector2(radius, thickness),
                new Vector2(0f, thickness)
            }, segments, 1f);
        }

        /// <summary>A low irregular mound for powder / clay / small stone heaps.</summary>
        public static Mesh Pile(float radius, float height, int seed, float noise = 0.18f,
            int rings = 9, int segments = 30)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= rings; r++)
            {
                float t = (float)r / rings;
                float rad = radius * Mathf.Pow(t, 0.78f);
                float baseY = height * Mathf.Pow(1f - t, 1.35f);
                for (int s = 0; s <= segments; s++)
                {
                    float a = (float)s / segments * Mathf.PI * 2f;
                    float cs = Mathf.Cos(a);
                    float sn = Mathf.Sin(a);
                    float n = Mathf.PerlinNoise(seed * 3.17f + cs * 2.1f + t * 3.3f,
                        seed * 1.71f + sn * 2.1f + t * 1.7f) - 0.5f;
                    float y = Mathf.Max(0.002f, baseY + n * height * noise * (1f - t * 0.4f));
                    float rr = rad * (1f + n * noise * 0.25f);
                    verts.Add(new Vector3(rr * cs, y, rr * sn));
                    uvs.Add(new Vector2(cs * 0.5f + 0.5f, sn * 0.5f + 0.5f) * radius);
                }
            }

            int stride = segments + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * stride + s;
                    int b = a + stride;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
                }
            }

            var mesh = new Mesh { name = "Cishan_Pile" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            // make sure the mound faces up
            var nrm = mesh.normals;
            float avg = 0f;
            for (int i = 0; i < nrm.Length; i++) avg += nrm[i].y;
            if (avg < 0f)
            {
                var t2 = mesh.triangles;
                for (int i = 0; i < t2.Length; i += 3)
                {
                    int tmp = t2[i + 1]; t2[i + 1] = t2[i + 2]; t2[i + 2] = tmp;
                }
                mesh.triangles = t2;
                mesh.RecalculateNormals();
            }
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Plane(float width, float depth, float uvScale = 1f)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            var mesh = new Mesh { name = "Cishan_Plane" };
            mesh.vertices = new[]
            {
                new Vector3(-hw, 0f, -hd), new Vector3(-hw, 0f, hd),
                new Vector3(hw, 0f, hd), new Vector3(hw, 0f, -hd)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(0f, depth * uvScale),
                new Vector2(width * uvScale, depth * uvScale), new Vector2(width * uvScale, 0f)
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Extrude a convex / star-shaped polygon in XY along Z.</summary>
        public static Mesh Extrude(Vector2[] poly, float thickness, float uvScale = 1f)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float hz = thickness * 0.5f;

            Vector2 center = Vector2.zero;
            for (int i = 0; i < poly.Length; i++) center += poly[i];
            center /= poly.Length;

            // front (+Z) fan
            int front = 0;
            verts.Add(new Vector3(center.x, center.y, hz));
            norms.Add(Vector3.forward);
            uvs.Add(center * uvScale);
            for (int i = 0; i < poly.Length; i++)
            {
                verts.Add(new Vector3(poly[i].x, poly[i].y, hz));
                norms.Add(Vector3.forward);
                uvs.Add(poly[i] * uvScale);
            }
            for (int i = 0; i < poly.Length; i++)
            {
                tris.Add(front);
                tris.Add(front + 1 + i);
                tris.Add(front + 1 + ((i + 1) % poly.Length));
            }

            // back (-Z) fan
            int back = verts.Count;
            verts.Add(new Vector3(center.x, center.y, -hz));
            norms.Add(Vector3.back);
            uvs.Add(center * uvScale);
            for (int i = 0; i < poly.Length; i++)
            {
                verts.Add(new Vector3(poly[i].x, poly[i].y, -hz));
                norms.Add(Vector3.back);
                uvs.Add(poly[i] * uvScale);
            }
            for (int i = 0; i < poly.Length; i++)
            {
                tris.Add(back);
                tris.Add(back + 1 + ((i + 1) % poly.Length));
                tris.Add(back + 1 + i);
            }

            // sides
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Length];
                Vector2 e = b - a;
                Vector3 n = new Vector3(e.y, -e.x, 0f).normalized;
                int start = verts.Count;
                verts.Add(new Vector3(a.x, a.y, -hz));
                verts.Add(new Vector3(a.x, a.y, hz));
                verts.Add(new Vector3(b.x, b.y, hz));
                verts.Add(new Vector3(b.x, b.y, -hz));
                for (int k = 0; k < 4; k++) { norms.Add(n); uvs.Add(Vector2.zero); }
                uvs[start] = new Vector2(0f, 0f);
                uvs[start + 1] = new Vector2(thickness * uvScale, 0f);
                uvs[start + 2] = new Vector2(thickness * uvScale, e.magnitude * uvScale);
                uvs[start + 3] = new Vector2(0f, e.magnitude * uvScale);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
            }

            var mesh = new Mesh { name = "Cishan_Extrude" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>One-lobed cam: base radius + smooth lift over 140 degrees.</summary>
        public static Mesh Cam(float baseRadius, float lift, float thickness, int steps = 56)
        {
            var poly = new Vector2[steps];
            for (int i = 0; i < steps; i++)
            {
                float a = (float)i / steps * Mathf.PI * 2f;
                float r = baseRadius + lift * CamLift01(a * Mathf.Rad2Deg);
                poly[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return Extrude(poly, thickness, 1.2f);
        }

        /// <summary>Shared cam profile: 0-140 rise, 140-155 hold, 155-190 release.</summary>
        public static float CamLift01(float degrees)
        {
            float d = Mathf.Repeat(degrees, 360f);
            if (d <= 140f) return Mathf.SmoothStep(0f, 1f, d / 140f);
            if (d <= 155f) return 1f;
            if (d <= 190f) return 1f - Mathf.SmoothStep(0f, 1f, (d - 155f) / 35f);
            return 0f;
        }

        static Vector3 Axis(int i)
        {
            return i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
        }

        static Vector3[] RebuildFlat(Vector3[] verts, List<int> tris, Vector3[] smoothNorms)
        {
            // Kept simple: the lathe is normally smooth-shaded; flat shading is optional.
            return smoothNorms;
        }

        sealed class Builder
        {
            readonly List<Vector3> verts = new List<Vector3>(192);
            readonly List<Vector3> norms = new List<Vector3>(192);
            readonly List<Vector2> uvs = new List<Vector2>(192);
            readonly List<int> tris = new List<int>(288);

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n,
                Vector3 grain, float uvScale, float jitter, int seed)
            {
                // keep winding consistent with the intended normal
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0f)
                {
                    Vector3 tmp = b; b = d; d = tmp;
                }
                Vector3 uAxis, vAxis;
                UvAxes(n, grain, out uAxis, out vAxis);
                int start = verts.Count;
                AddVertex(a, n, uAxis, vAxis, uvScale, jitter, seed);
                AddVertex(b, n, uAxis, vAxis, uvScale, jitter, seed);
                AddVertex(c, n, uAxis, vAxis, uvScale, jitter, seed);
                AddVertex(d, n, uAxis, vAxis, uvScale, jitter, seed);
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
            }

            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 n,
                Vector3 grain, float uvScale, float jitter, int seed)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0f)
                {
                    Vector3 tmp = b; b = c; c = tmp;
                }
                Vector3 uAxis, vAxis;
                UvAxes(n, grain, out uAxis, out vAxis);
                int start = verts.Count;
                AddVertex(a, n, uAxis, vAxis, uvScale, jitter, seed);
                AddVertex(b, n, uAxis, vAxis, uvScale, jitter, seed);
                AddVertex(c, n, uAxis, vAxis, uvScale, jitter, seed);
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            }

            void AddVertex(Vector3 p, Vector3 n, Vector3 uAxis, Vector3 vAxis,
                float uvScale, float jitter, int seed)
            {
                Vector3 q = p;
                if (jitter > 0f)
                {
                    q += new Vector3(
                        Hash(p, seed, 0), Hash(p, seed, 1), Hash(p, seed, 2)) * jitter;
                }
                verts.Add(q);
                norms.Add(n);
                uvs.Add(new Vector2(Vector3.Dot(p, uAxis), Vector3.Dot(p, vAxis)) * uvScale
                        + new Vector2(0.37f, 0.61f));
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(verts);
                mesh.SetNormals(norms);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }

            static float Hash(Vector3 p, int seed, int channel)
            {
                int x = Mathf.RoundToInt(p.x * 1000f);
                int y = Mathf.RoundToInt(p.y * 1000f);
                int z = Mathf.RoundToInt(p.z * 1000f);
                unchecked
                {
                    int h = x * 374761393 + y * 668265263 + z * 2147483647 + seed * 1274126177 + channel * 1442695041;
                    h = (h ^ (h >> 13)) * 1274126177;
                    h ^= h >> 16;
                    return ((h & 0x7fffffff) / 2147483647f) * 2f - 1f;
                }
            }
        }

        static void UvAxes(Vector3 n, Vector3 grain, out Vector3 uAxis, out Vector3 vAxis)
        {
            n.Normalize();
            grain.Normalize();
            float d = Mathf.Abs(Vector3.Dot(n, grain));
            if (d > 0.75f)
            {
                uAxis = Vector3.Cross(n, Vector3.up);
                if (uAxis.sqrMagnitude < 0.001f) uAxis = Vector3.Cross(n, Vector3.right);
                uAxis.Normalize();
                vAxis = Vector3.Cross(n, uAxis).normalized;
            }
            else
            {
                vAxis = (grain - n * Vector3.Dot(n, grain)).normalized;
                uAxis = Vector3.Cross(vAxis, n).normalized;
            }
        }
    }
}
