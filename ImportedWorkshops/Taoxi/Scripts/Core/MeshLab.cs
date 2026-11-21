using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PorcelainFromClay
{
    /// <summary>Small procedural mesh kit. Keeps the Unity project free of external model files.</summary>
    public static class MeshLab
    {
        public sealed class Builder
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();

            public int Add(Vector3 p, Vector3 n, Vector2 uv)
            {
                Vertices.Add(p);
                Normals.Add(n);
                Uvs.Add(uv);
                return Vertices.Count - 1;
            }

            public void Tri(int a, int b, int c)
            {
                Triangles.Add(a);
                Triangles.Add(b);
                Triangles.Add(c);
            }

            public void TriOriented(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc,
                Vector2 ua, Vector2 ub, Vector2 uc, Vector3 targetNormal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), targetNormal) < 0f)
                {
                    Vector3 tp = b; b = c; c = tp;
                    Vector3 tn = nb; nb = nc; nc = tn;
                    Vector2 tu = ub; ub = uc; uc = tu;
                }
                int ia = Add(a, na, ua);
                int ib = Add(b, nb, ub);
                int ic = Add(c, nc, uc);
                Tri(ia, ib, ic);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd,
                Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                Vector3 target = (na + nb + nc + nd).normalized;
                TriOriented(a, b, c, na, nb, nc, ua, ub, uc, target);
                TriOriented(a, c, d, na, nc, nd, ua, uc, ud, target);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(Vertices);
                mesh.SetNormals(Normals);
                mesh.SetUVs(0, Uvs);
                mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        public static void AddQuad(Builder b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n, Rect uv)
        {
            Vector2 u0 = new Vector2(uv.xMin, uv.yMin);
            Vector2 u1 = new Vector2(uv.xMin, uv.yMax);
            Vector2 u2 = new Vector2(uv.xMax, uv.yMax);
            Vector2 u3 = new Vector2(uv.xMax, uv.yMin);
            b.Quad(p0, p1, p2, p3, n, n, n, n, u0, u1, u2, u3);
        }

        public static Mesh Plane(float width, float depth, float uvScale = 1f)
        {
            var b = new Builder();
            float x = width * 0.5f;
            float z = depth * 0.5f;
            b.Quad(
                new Vector3(-x, 0, -z), new Vector3(-x, 0, z), new Vector3(x, 0, z), new Vector3(x, 0, -z),
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                new Vector2(0, 0), new Vector2(0, depth * uvScale), new Vector2(width * uvScale, depth * uvScale), new Vector2(width * uvScale, 0));
            return b.ToMesh("Plane");
        }

        public static Mesh QuadXY(float width, float height, float uvScale = 1f)
        {
            var b = new Builder();
            float x = width * 0.5f;
            float y = height * 0.5f;
            b.Quad(
                new Vector3(-x, -y, 0), new Vector3(-x, y, 0), new Vector3(x, y, 0), new Vector3(x, -y, 0),
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                new Vector2(0, 0), new Vector2(0, height * uvScale), new Vector2(width * uvScale, height * uvScale), new Vector2(width * uvScale, 0));
            return b.ToMesh("QuadXY");
        }

        /// <summary>A box with soft, worn edges. UVs are box projected in metres.</summary>
        public static Mesh RoundedBox(Vector3 size, float radius, float uvScale = 1f, int segments = 4)
        {
            var b = new Builder();
            Vector3 h = size * 0.5f;
            float maxR = Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.48f;
            radius = Mathf.Clamp(radius, 0.0005f, Mathf.Max(0.0006f, maxR));
            Vector3 inner = h - Vector3.one * radius;
            segments = Mathf.Clamp(segments, 1, 10);

            Vector3[] normals = new Vector3[6];
            for (int axis = 0; axis < 3; axis++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    int face = axis * 2 + (side > 0 ? 1 : 0);
                    for (int iy = 0; iy < segments; iy++)
                    {
                        for (int ix = 0; ix < segments; ix++)
                        {
                            Vector3[] ps = new Vector3[4];
                            Vector3[] ns = new Vector3[4];
                            Vector2[] uvs = new Vector2[4];
                            for (int k = 0; k < 4; k++)
                            {
                                int cx = ix + (k == 1 || k == 2 ? 1 : 0);
                                int cy = iy + (k >= 2 ? 1 : 0);
                                float a = Mathf.Lerp(-h[axis == 0 ? 1 : 0], h[axis == 0 ? 1 : 0], cx / (float)segments);
                                float c = Mathf.Lerp(-h[axis == 2 ? 1 : 2], h[axis == 2 ? 1 : 2], cy / (float)segments);
                                // For the three face families, build coordinates directly to keep winding stable.
                                Vector3 p;
                                if (axis == 0)
                                {
                                    p = new Vector3(side * h.x, a, c);
                                }
                                else if (axis == 1)
                                {
                                    p = new Vector3(a, side * h.y, c);
                                }
                                else
                                {
                                    p = new Vector3(a, c, side * h.z);
                                }
                                Vector3 q = new Vector3(
                                    Mathf.Clamp(p.x, -inner.x, inner.x),
                                    Mathf.Clamp(p.y, -inner.y, inner.y),
                                    Mathf.Clamp(p.z, -inner.z, inner.z));
                                Vector3 dir = p - q;
                                Vector3 n = dir.sqrMagnitude > 0.0000001f ? dir.normalized : FaceNormal(axis, side);
                                ps[k] = q + n * radius;
                                ns[k] = n;
                                uvs[k] = BoxUV(p, axis, size, uvScale);
                            }
                            b.Quad(ps[0], ps[1], ps[2], ps[3], ns[0], ns[1], ns[2], ns[3], uvs[0], uvs[1], uvs[2], uvs[3]);
                        }
                    }
                    normals[face] = FaceNormal(axis, side);
                }
            }
            return b.ToMesh("RoundedBox");
        }

        static Vector3 FaceNormal(int axis, int side)
        {
            if (axis == 0) return new Vector3(side, 0, 0);
            if (axis == 1) return new Vector3(0, side, 0);
            return new Vector3(0, 0, side);
        }

        static Vector2 BoxUV(Vector3 p, int axis, Vector3 size, float uvScale)
        {
            Vector2 uv;
            if (axis == 0)
            {
                if (size.z >= size.y) uv = new Vector2(p.z, p.y);
                else uv = new Vector2(p.y, p.z);
            }
            else if (axis == 1)
            {
                if (size.x >= size.z) uv = new Vector2(p.x, p.z);
                else uv = new Vector2(p.z, p.x);
            }
            else
            {
                if (size.x >= size.y) uv = new Vector2(p.x, p.y);
                else uv = new Vector2(p.y, p.x);
            }
            return uv * uvScale;
        }

        public static Mesh Lathe(Vector2[] profile, int segments = 24, float uvScale = 1f, bool capBottom = true, bool capTop = true)
        {
            var b = new Builder();
            segments = Mathf.Max(3, segments);
            int rings = profile.Length;
            Vector3[] ringNormals = new Vector3[rings];
            float[] vcoord = new float[rings];
            float length = 0f;
            for (int i = 0; i < rings; i++)
            {
                if (i > 0) length += Vector2.Distance(profile[i - 1], profile[i]);
                vcoord[i] = length * uvScale;
                Vector2 tangent = i == 0 ? profile[1] - profile[0] : (i == rings - 1 ? profile[i] - profile[i - 1] : profile[i + 1] - profile[i - 1]);
                Vector2 n2 = new Vector2(tangent.y, -tangent.x);
                if (n2.sqrMagnitude < 0.000001f) n2 = new Vector2(1, 0);
                n2.Normalize();
                ringNormals[i] = new Vector3(n2.x, n2.y, 0);
            }

            for (int i = 0; i < rings - 1; i++)
            {
                float r0 = profile[i].x;
                float r1 = profile[i + 1].x;
                float y0 = profile[i].y;
                float y1 = profile[i + 1].y;
                if (r0 < 0.0001f && r1 < 0.0001f) continue;
                for (int s = 0; s < segments; s++)
                {
                    float a0 = s / (float)segments * Mathf.PI * 2f;
                    float a1 = (s + 1) / (float)segments * Mathf.PI * 2f;
                    Vector3 p00 = new Vector3(Mathf.Cos(a0) * r0, y0, Mathf.Sin(a0) * r0);
                    Vector3 p01 = new Vector3(Mathf.Cos(a1) * r0, y0, Mathf.Sin(a1) * r0);
                    Vector3 p10 = new Vector3(Mathf.Cos(a0) * r1, y1, Mathf.Sin(a0) * r1);
                    Vector3 p11 = new Vector3(Mathf.Cos(a1) * r1, y1, Mathf.Sin(a1) * r1);
                    Vector3 n00 = new Vector3(Mathf.Cos(a0) * ringNormals[i].x, ringNormals[i].y, Mathf.Sin(a0) * ringNormals[i].x);
                    Vector3 n01 = new Vector3(Mathf.Cos(a1) * ringNormals[i].x, ringNormals[i].y, Mathf.Sin(a1) * ringNormals[i].x);
                    Vector3 n10 = new Vector3(Mathf.Cos(a0) * ringNormals[i + 1].x, ringNormals[i + 1].y, Mathf.Sin(a0) * ringNormals[i + 1].x);
                    Vector3 n11 = new Vector3(Mathf.Cos(a1) * ringNormals[i + 1].x, ringNormals[i + 1].y, Mathf.Sin(a1) * ringNormals[i + 1].x);
                    float u0 = s / (float)segments * uvScale;
                    float u1 = (s + 1) / (float)segments * uvScale;
                    b.Quad(p00, p01, p11, p10, n00, n01, n11, n10,
                        new Vector2(u0, vcoord[i]), new Vector2(u1, vcoord[i]),
                        new Vector2(u1, vcoord[i + 1]), new Vector2(u0, vcoord[i + 1]));
                }
            }

            if (capBottom && profile[0].x > 0.0001f)
            {
                int center = b.Add(new Vector3(0, profile[0].y, 0), Vector3.down, new Vector2(0.5f, 0.5f));
                for (int s = 0; s < segments; s++)
                {
                    float a0 = s / (float)segments * Mathf.PI * 2f;
                    float a1 = (s + 1) / (float)segments * Mathf.PI * 2f;
                    int i0 = b.Add(new Vector3(Mathf.Cos(a0) * profile[0].x, profile[0].y, Mathf.Sin(a0) * profile[0].x), Vector3.down, new Vector2(0.5f + Mathf.Cos(a0) * 0.5f, 0.5f + Mathf.Sin(a0) * 0.5f));
                    int i1 = b.Add(new Vector3(Mathf.Cos(a1) * profile[0].x, profile[0].y, Mathf.Sin(a1) * profile[0].x), Vector3.down, new Vector2(0.5f + Mathf.Cos(a1) * 0.5f, 0.5f + Mathf.Sin(a1) * 0.5f));
                    b.Tri(center, i0, i1);
                }
            }

            if (capTop && profile[rings - 1].x > 0.0001f)
            {
                int center = b.Add(new Vector3(0, profile[rings - 1].y, 0), Vector3.up, new Vector2(0.5f, 0.5f));
                for (int s = 0; s < segments; s++)
                {
                    float a0 = s / (float)segments * Mathf.PI * 2f;
                    float a1 = (s + 1) / (float)segments * Mathf.PI * 2f;
                    int i0 = b.Add(new Vector3(Mathf.Cos(a0) * profile[rings - 1].x, profile[rings - 1].y, Mathf.Sin(a0) * profile[rings - 1].x), Vector3.up, new Vector2(0.5f + Mathf.Cos(a0) * 0.5f, 0.5f + Mathf.Sin(a0) * 0.5f));
                    int i1 = b.Add(new Vector3(Mathf.Cos(a1) * profile[rings - 1].x, profile[rings - 1].y, Mathf.Sin(a1) * profile[rings - 1].x), Vector3.up, new Vector2(0.5f + Mathf.Cos(a1) * 0.5f, 0.5f + Mathf.Sin(a1) * 0.5f));
                    b.Tri(center, i1, i0);
                }
            }

            return b.ToMesh("Lathe");
        }

        public static Mesh Cylinder(float radius, float height, int segments = 20, float uvScale = 1f, bool caps = true)
        {
            return Lathe(new[]
            {
                new Vector2(radius, 0),
                new Vector2(radius, height)
            }, segments, uvScale, caps, caps);
        }

        public static Mesh Tube(IList<Vector3> points, IList<float> radii, int sides = 10, bool caps = true)
        {
            var b = new Builder();
            if (points.Count < 2) return b.ToMesh("Tube");
            Vector3 prevDir = Vector3.up;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 dir = (points[i + 1] - points[i]).normalized;
                Vector3 up = i == 0 ? Vector3.up : prevDir;
                Vector3 side = Vector3.Cross(dir, up);
                if (side.sqrMagnitude < 0.00001f) side = Vector3.Cross(dir, Vector3.right);
                side.Normalize();
                Vector3 up2 = Vector3.Cross(side, dir).normalized;
                for (int s = 0; s < sides; s++)
                {
                    float a0 = s / (float)sides * Mathf.PI * 2f;
                    float a1 = (s + 1) / (float)sides * Mathf.PI * 2f;
                    Vector3 n0 = side * Mathf.Cos(a0) + up2 * Mathf.Sin(a0);
                    Vector3 n1 = side * Mathf.Cos(a1) + up2 * Mathf.Sin(a1);
                    Vector3 p00 = points[i] + n0 * radii[i];
                    Vector3 p01 = points[i] + n1 * radii[i];
                    Vector3 p10 = points[i + 1] + n0 * radii[i + 1];
                    Vector3 p11 = points[i + 1] + n1 * radii[i + 1];
                    b.Quad(p00, p01, p11, p10, n0, n1, n1, n0,
                        new Vector2(s / (float)sides, i), new Vector2((s + 1) / (float)sides, i),
                        new Vector2((s + 1) / (float)sides, i + 1), new Vector2(s / (float)sides, i + 1));
                }
                prevDir = dir;
            }
            if (caps)
            {
                AddTubeCap(b, points[0], (points[1] - points[0]).normalized, radii[0], sides, false);
                AddTubeCap(b, points[points.Count - 1], (points[points.Count - 1] - points[points.Count - 2]).normalized, radii[radii.Count - 1], sides, true);
            }
            return b.ToMesh("Tube");
        }

        static void AddTubeCap(Builder b, Vector3 center, Vector3 dir, float radius, int sides, bool positive)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
            Vector3 side = Vector3.Cross(dir, up).normalized;
            Vector3 up2 = Vector3.Cross(side, dir).normalized;
            int c = b.Add(center, dir * (positive ? 1 : -1), new Vector2(0.5f, 0.5f));
            for (int s = 0; s < sides; s++)
            {
                float a0 = s / (float)sides * Mathf.PI * 2f;
                float a1 = (s + 1) / (float)sides * Mathf.PI * 2f;
                int i0 = b.Add(center + (side * Mathf.Cos(a0) + up2 * Mathf.Sin(a0)) * radius, dir * (positive ? 1 : -1), new Vector2(0.5f + Mathf.Cos(a0) * 0.5f, 0.5f + Mathf.Sin(a0) * 0.5f));
                int i1 = b.Add(center + (side * Mathf.Cos(a1) + up2 * Mathf.Sin(a1)) * radius, dir * (positive ? 1 : -1), new Vector2(0.5f + Mathf.Cos(a1) * 0.5f, 0.5f + Mathf.Sin(a1) * 0.5f));
                if (positive) b.Tri(c, i1, i0); else b.Tri(c, i0, i1);
            }
        }

        public static Mesh Rock(float radius, int seed, float squash = 0.65f, int segments = 18, int rings = 12)
        {
            var b = new Builder();
            var verts = new Vector3[(rings + 1) * (segments + 1)];
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings;
                float phi = v * Mathf.PI;
                for (int x = 0; x <= segments; x++)
                {
                    float u = x / (float)segments;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 unit = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    float n = Mathf.PerlinNoise(unit.x * 2.3f + seed * 3.1f, unit.z * 2.3f + seed * 1.7f);
                    float n2 = Mathf.PerlinNoise(unit.y * 5.2f + seed, unit.x * 5.2f - seed);
                    float r = radius * (0.82f + n * 0.28f + n2 * 0.1f);
                    Vector3 p = unit * r;
                    p.y *= squash;
                    p.x *= 1f + n2 * 0.18f;
                    p.z *= 1f + n * 0.12f;
                    verts[y * (segments + 1) + x] = p;
                }
            }
            for (int y = 0; y < rings; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int i00 = y * (segments + 1) + x;
                    int i01 = y * (segments + 1) + x + 1;
                    int i10 = (y + 1) * (segments + 1) + x;
                    int i11 = (y + 1) * (segments + 1) + x + 1;
                    Vector3 p00 = verts[i00], p01 = verts[i01], p10 = verts[i10], p11 = verts[i11];
                    Vector3 n = (p00 + p01 + p10 + p11).normalized;
                    if (n.sqrMagnitude < 0.001f) n = Vector3.up;
                    b.Quad(p00, p01, p11, p10, n, n, n, n,
                        new Vector2(x / (float)segments, y / (float)rings), new Vector2((x + 1) / (float)segments, y / (float)rings),
                        new Vector2((x + 1) / (float)segments, (y + 1) / (float)rings), new Vector2(x / (float)segments, (y + 1) / (float)rings));
                }
            }
            var mesh = b.ToMesh("Rock");
            mesh.RecalculateNormals();
            return mesh;
        }

        public static Mesh Cloth(float width, float depth, float sag, int res = 18, float seed = 0f)
        {
            var b = new Builder();
            var positions = new Vector3[(res + 1) * (res + 1)];
            var normals = new Vector3[positions.Length];
            for (int y = 0; y <= res; y++)
            {
                for (int x = 0; x <= res; x++)
                {
                    float u = x / (float)res;
                    float v = y / (float)res;
                    float px = (u - 0.5f) * width;
                    float pz = (v - 0.5f) * depth;
                    float edge = Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI);
                    float fold = Mathf.Sin(u * 12f + seed) * 0.012f + Mathf.Sin(v * 8f + seed * 2f) * 0.008f;
                    float yv = sag * edge + fold;
                    positions[y * (res + 1) + x] = new Vector3(px, yv, pz);
                }
            }
            for (int y = 0; y <= res; y++)
            {
                for (int x = 0; x <= res; x++)
                {
                    int xm = Mathf.Max(0, x - 1), xp = Mathf.Min(res, x + 1);
                    int ym = Mathf.Max(0, y - 1), yp = Mathf.Min(res, y + 1);
                    Vector3 dx = positions[y * (res + 1) + xp] - positions[y * (res + 1) + xm];
                    Vector3 dz = positions[yp * (res + 1) + x] - positions[ym * (res + 1) + x];
                    normals[y * (res + 1) + x] = Vector3.Cross(dz, dx).normalized;
                }
            }
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i00 = y * (res + 1) + x;
                    int i01 = y * (res + 1) + x + 1;
                    int i10 = (y + 1) * (res + 1) + x;
                    int i11 = (y + 1) * (res + 1) + x + 1;
                    b.Quad(positions[i00], positions[i01], positions[i11], positions[i10], normals[i00], normals[i01], normals[i11], normals[i10],
                        new Vector2(x / (float)res, y / (float)res), new Vector2((x + 1) / (float)res, y / (float)res),
                        new Vector2((x + 1) / (float)res, (y + 1) / (float)res), new Vector2(x / (float)res, (y + 1) / (float)res));
                }
            }
            return b.ToMesh("Cloth");
        }

        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPosition, Quaternion localRotation, bool shadows = true, bool receive = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = receive;
            return go;
        }

        public static GameObject BoxObject(string name, Transform parent, Vector3 size, float radius, Material mat, Vector3 localPosition, float uvScale = 1f, bool shadows = true)
        {
            return MeshObject(name, parent, RoundedBox(size, radius, uvScale), mat, localPosition, Quaternion.identity, shadows);
        }

        public static void AddBoxCollider(GameObject go, Vector3 center, Vector3 size)
        {
            var c = go.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
        }

        public static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayer(child.gameObject, layer);
        }

        public static void Tint(Renderer r, Color tint)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", tint);
            r.SetPropertyBlock(mpb);
        }

        public static void SetWetness(Renderer r, float wetness)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetFloat("_Wetness", wetness);
            r.SetPropertyBlock(mpb);
        }
    }
}
