using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 程序化网格库：回转体（缸、桶、碗）、倒角盒、带起伏的泥团、竹竿等。
    /// 所有构件默认带小倒角 / 圆润轮廓，避免默认几何体的锋利边缘。
    /// </summary>
    public static class ProcMesh
    {
        // ---------------------------------------------------------------- 回转体

        /// <summary>
        /// 由 (半径, 高度) 剖面绕 Y 轴旋转成体。剖面自下而上排列。
        /// closedProfile 为真时，首末点也相连（用于竹箍等环形断面）。
        /// </summary>
        public static Mesh Lathe(Vector2[] profile, int segments = 40, bool closedProfile = false,
                                 bool capBottom = false, bool capTop = false)
        {
            segments = Mathf.Max(3, segments);
            int rows = profile.Length;
            int cols = segments + 1; // 末列与首列重合，保证 UV 连续

            var verts = new Vector3[rows * cols];
            var uvs = new Vector2[rows * cols];

            // 沿剖面的累计弧长，作为 V 坐标
            float totalLen = 0f;
            var rowV = new float[rows];
            for (int i = 1; i < rows; i++)
            {
                totalLen += Vector2.Distance(profile[i - 1], profile[i]);
                rowV[i] = totalLen;
            }
            for (int i = 0; i < rows; i++) rowV[i] = totalLen > 0.0001f ? rowV[i] / totalLen : 0f;

            for (int i = 0; i < rows; i++)
            {
                float r = Mathf.Max(0f, profile[i].x);
                float y = profile[i].y;
                for (int j = 0; j < cols; j++)
                {
                    float t = j / (float)segments;
                    float ang = t * Mathf.PI * 2f;
                    verts[i * cols + j] = new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r);
                    uvs[i * cols + j] = new Vector2(t, rowV[i]);
                }
            }

            int quadRows = closedProfile ? rows : rows - 1;
            var tris = new System.Collections.Generic.List<int>(quadRows * segments * 6 + segments * 6);
            for (int i = 0; i < quadRows; i++)
            {
                int i0 = i, i1 = (i + 1) % rows;
                for (int j = 0; j < segments; j++)
                {
                    int a = i0 * cols + j;
                    int b = i0 * cols + j + 1;
                    int c = i1 * cols + j + 1;
                    int d = i1 * cols + j;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
            }

            // 端盖
            int capStart = verts.Length;
            var extraVerts = new System.Collections.Generic.List<Vector3>();
            var extraUvs = new System.Collections.Generic.List<Vector2>();
            var extraTris = new System.Collections.Generic.List<int>();

            if (capBottom && profile[0].x > 0.0001f)
            {
                int center = capStart + extraVerts.Count;
                extraVerts.Add(new Vector3(0f, profile[0].y, 0f));
                extraUvs.Add(new Vector2(0.5f, 0.5f));
                for (int j = 0; j < segments; j++)
                {
                    int a = 0 * cols + j;
                    int b = 0 * cols + j + 1;
                    extraTris.Add(center); extraTris.Add(a); extraTris.Add(b);
                }
            }

            if (capTop && profile[rows - 1].x > 0.0001f)
            {
                int center = capStart + extraVerts.Count;
                extraVerts.Add(new Vector3(0f, profile[rows - 1].y, 0f));
                extraUvs.Add(new Vector2(0.5f, 0.5f));
                for (int j = 0; j < segments; j++)
                {
                    int a = (rows - 1) * cols + j;
                    int b = (rows - 1) * cols + j + 1;
                    extraTris.Add(center); extraTris.Add(b); extraTris.Add(a);
                }
            }

            var finalVerts = new Vector3[verts.Length + extraVerts.Count];
            var finalUvs = new Vector2[uvs.Length + extraUvs.Count];
            verts.CopyTo(finalVerts, 0);
            uvs.CopyTo(finalUvs, 0);
            extraVerts.CopyTo(finalVerts, verts.Length);
            extraUvs.CopyTo(finalUvs, uvs.Length);

            var allTris = new int[tris.Count + extraTris.Count];
            tris.CopyTo(allTris, 0);
            extraTris.CopyTo(allTris, tris.Count);

            var mesh = new Mesh { name = "Lathe" };
            mesh.indexFormat = finalVerts.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = finalVerts;
            mesh.uv = finalUvs;
            mesh.triangles = allTris;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Cylinder(float radiusBottom, float radiusTop, float height, int segments = 32,
                                    bool capBottom = true, bool capTop = true)
        {
            var profile = new[]
            {
                new Vector2(radiusBottom, 0f),
                new Vector2(radiusTop, height)
            };
            return Lathe(profile, segments, false, capBottom, capTop);
        }

        /// <summary>竹箍 / 木环：绕 Y 轴的圆环。</summary>
        public static Mesh Torus(float majorRadius, float tubeRadius, int segments = 40, int tubeSegments = 10)
        {
            var profile = new Vector2[tubeSegments];
            for (int i = 0; i < tubeSegments; i++)
            {
                float a = i / (float)tubeSegments * Mathf.PI * 2f;
                profile[i] = new Vector2(majorRadius + Mathf.Cos(a) * tubeRadius, Mathf.Sin(a) * tubeRadius);
            }
            return Lathe(profile, segments, true, false, false);
        }

        // ---------------------------------------------------------------- 盒体

        /// <summary>倒角盒：用球化立方体映射到圆角盒表面，法线解析求得。</summary>
        public static Mesh RoundedBox(Vector3 size, float radius = 0.004f, int seg = 3)
        {
            Vector3 h = size * 0.5f;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.98f);
            Vector3 inner = new Vector3(Mathf.Max(0f, h.x - radius), Mathf.Max(0f, h.y - radius), Mathf.Max(0f, h.z - radius));

            seg = Mathf.Max(1, seg);
            int perFace = (seg + 1) * (seg + 1);
            var verts = new Vector3[6 * perFace];
            var normals = new Vector3[6 * perFace];
            var uvs = new Vector2[6 * perFace];
            var tris = new int[6 * seg * seg * 6];

            int vc = 0, tc = 0;
            for (int f = 0; f < 6; f++)
            {
                Vector3 faceNormal = FaceNormal(f);
                Vector3 axisU = FaceAxisU(f);
                Vector3 axisV = FaceAxisV(f);

                for (int iy = 0; iy <= seg; iy++)
                {
                    for (int ix = 0; ix <= seg; ix++)
                    {
                        float u = ix / (float)seg;
                        float v = iy / (float)seg;
                        float cu = (u - 0.5f) * 2f;
                        float cv = (v - 0.5f) * 2f;

                        Vector3 p = faceNormal * Half(size, f)
                                    + axisU * (cu * HalfExtent(size, axisU))
                                    + axisV * (cv * HalfExtent(size, axisV));

                        Vector3 clamped = new Vector3(
                            Mathf.Clamp(p.x, -inner.x, inner.x),
                            Mathf.Clamp(p.y, -inner.y, inner.y),
                            Mathf.Clamp(p.z, -inner.z, inner.z));

                        Vector3 dir = p - clamped;
                        Vector3 n = dir.sqrMagnitude > 1e-10f ? dir.normalized : faceNormal;
                        Vector3 q = clamped + n * radius;

                        verts[vc] = q;
                        normals[vc] = n;
                        uvs[vc] = new Vector2(u, v);
                        vc++;
                    }
                }

                for (int iy = 0; iy < seg; iy++)
                {
                    for (int ix = 0; ix < seg; ix++)
                    {
                        int a = vc - perFace + iy * (seg + 1) + ix;
                        int b = a + 1;
                        int c = a + seg + 2;
                        int d = a + seg + 1;
                        // U x V = 面法线，故 (a,b,c) 为外向绕序
                        tris[tc++] = a; tris[tc++] = b; tris[tc++] = c;
                        tris[tc++] = a; tris[tc++] = c; tris[tc++] = d;
                    }
                }
            }

            var mesh = new Mesh { name = "RoundedBox" };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Box(Vector3 size, float bevel = 0.004f)
        {
            return RoundedBox(size, bevel, 2);
        }

        // ---------------------------------------------------------------- 面片

        /// <summary>水平面片，UV 按世界尺寸铺开，便于贴图按米平铺。</summary>
        public static Mesh Plane(float width, float depth, float uvPerMeter = 1f, int nx = 1, int nz = 1)
        {
            nx = Mathf.Max(1, nx);
            nz = Mathf.Max(1, nz);
            var verts = new Vector3[(nx + 1) * (nz + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new int[nx * nz * 6];
            for (int z = 0; z <= nz; z++)
            {
                for (int x = 0; x <= nx; x++)
                {
                    float fx = x / (float)nx - 0.5f;
                    float fz = z / (float)nz - 0.5f;
                    int i = z * (nx + 1) + x;
                    verts[i] = new Vector3(fx * width, 0f, fz * depth);
                    uvs[i] = new Vector2((fx * width) * uvPerMeter, (fz * depth) * uvPerMeter);
                }
            }
            int t = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int a = z * (nx + 1) + x;
                    int b = a + 1;
                    int c = a + nx + 2;
                    int d = a + nx + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = a; tris[t++] = d; tris[t++] = c;
                }
            }
            var mesh = new Mesh { name = "Plane" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Quad(float width, float height)
        {
            var mesh = new Mesh { name = "Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                new Vector3( width * 0.5f, -height * 0.5f, 0f),
                new Vector3( width * 0.5f,  height * 0.5f, 0f),
                new Vector3(-width * 0.5f,  height * 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            // 正面朝 +Z
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>圆盘，UV 以 0.5 半径归一化，贴圆形贴图用。</summary>
        public static Mesh Disc(float radius, int segments = 32, bool up = true)
        {
            segments = Mathf.Max(3, segments);
            var verts = new Vector3[segments + 1];
            var uvs = new Vector2[segments + 1];
            var tris = new int[segments * 3];
            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                uvs[i + 1] = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
            }
            for (int i = 0; i < segments; i++)
            {
                int a = 0, b = i + 1, c = (i + 1) % segments + 1;
                if (up) { tris[i * 3] = a; tris[i * 3 + 1] = c; tris[i * 3 + 2] = b; }
                else { tris[i * 3] = a; tris[i * 3 + 1] = b; tris[i * 3 + 2] = c; }
            }
            var mesh = new Mesh { name = "Disc" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 不规则泥团 / 渣堆：中心隆起的低矮圆丘，边缘做低频扰动。
        /// 用于筛面渣层与渣盘堆积的可视化。
        /// </summary>
        public static Mesh Blob(float radius, float height, int segments = 28, int rings = 6,
                                float irregularity = 0.22f, int seed = 1)
        {
            segments = Mathf.Max(6, segments);
            rings = Mathf.Max(2, rings);
            int cols = segments + 1;
            var verts = new Vector3[(rings + 1) * cols];
            var uvs = new Vector2[verts.Length];
            var tris = new System.Collections.Generic.List<int>(rings * segments * 6);

            for (int ri = 0; ri <= rings; ri++)
            {
                float t = ri / (float)rings;
                for (int j = 0; j <= segments; j++)
                {
                    float ang = j / (float)segments * Mathf.PI * 2f;
                    float wobble = 1f + (Mathf.PerlinNoise(Mathf.Cos(ang) * 1.7f + seed * 3.1f,
                                                           Mathf.Sin(ang) * 1.7f + seed * 1.9f) - 0.5f) * irregularity * 2f;
                    float rr = radius * t * wobble;
                    float y = height * Mathf.Cos(t * Mathf.PI * 0.5f);
                    int i = ri * cols + j;
                    verts[i] = new Vector3(Mathf.Cos(ang) * rr, y, Mathf.Sin(ang) * rr);
                    uvs[i] = new Vector2(Mathf.Cos(ang) * 0.5f * t + 0.5f, Mathf.Sin(ang) * 0.5f * t + 0.5f);
                }
            }

            for (int ri = 0; ri < rings; ri++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int a = ri * cols + j;
                    int b = a + 1;
                    int c = a + cols + 1;
                    int d = a + cols;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }

            var mesh = new Mesh { name = "Blob" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>两点之间的圆柱，返回沿 Y 轴竖直、高度为两点距离的网格。</summary>
        public static Mesh Rod(Vector3 a, Vector3 b, float radius, int segments = 10)
        {
            return Cylinder(radius, radius, Vector3.Distance(a, b), segments, true, true);
        }

        // ---------------------------------------------------------------- 摆放辅助

        public static GameObject Node(string name, Transform parent = null, Vector3 localPos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static MeshRenderer Place(string name, Transform parent, Mesh mesh, Material mat,
                                         Vector3 localPos, Quaternion? localRot = null, Vector3? localScale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            return mr;
        }

        public static void AddBoxCollider(GameObject go, Vector3 size, Vector3 center)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.center = center;
        }

        // ---------------------------------------------------------------- 面朝向工具

        static float Half(Vector3 size, int face)
        {
            // face: 0 +X, 1 -X, 2 +Y, 3 -Y, 4 +Z, 5 -Z
            switch (face)
            {
                case 0: case 1: return size.x * 0.5f;
                case 2: case 3: return size.y * 0.5f;
                default: return size.z * 0.5f;
            }
        }

        static float HalfExtent(Vector3 size, Vector3 axis)
        {
            if (Mathf.Abs(axis.x) > 0.5f) return size.x * 0.5f;
            if (Mathf.Abs(axis.y) > 0.5f) return size.y * 0.5f;
            return size.z * 0.5f;
        }

        static Vector3 FaceNormal(int f)
        {
            switch (f)
            {
                case 0: return Vector3.right;
                case 1: return Vector3.left;
                case 2: return Vector3.up;
                case 3: return Vector3.down;
                case 4: return Vector3.forward;
                default: return Vector3.back;
            }
        }

        // 每个面的 U/V 轴选取满足 U x V = 面法线，保证绕序一致朝外
        static Vector3 FaceAxisU(int f)
        {
            switch (f)
            {
                case 0: return Vector3.up;      // +X: U=+Y
                case 1: return Vector3.forward; // -X: U=+Z
                case 2: return Vector3.forward; // +Y: U=+Z
                case 3: return Vector3.right;   // -Y: U=+X
                case 4: return Vector3.right;   // +Z: U=+X
                default: return Vector3.up;     // -Z: U=+Y
            }
        }

        static Vector3 FaceAxisV(int f)
        {
            switch (f)
            {
                case 0: return Vector3.forward; // +X: V=+Z
                case 1: return Vector3.up;      // -X: V=+Y
                case 2: return Vector3.right;   // +Y: V=+X
                case 3: return Vector3.forward; // -Y: V=+Z
                case 4: return Vector3.up;      // +Z: V=+Y
                default: return Vector3.right;  // -Z: V=+X
            }
        }
    }
}
