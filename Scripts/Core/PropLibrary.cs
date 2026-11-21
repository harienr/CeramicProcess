using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>
    /// 工坊道具库。造型遵循"功能轮廓清楚、抓握与作用部位可辨"，
    /// 磨损集中在人手接触与材料摩擦处（通过材质分区近似表达）。
    /// </summary>
    public static class PropLibrary
    {
        // ---------------------------------------------------------------- 容器

        /// <summary>大陶缸：外壁上行、翻沿、内壁下行，一次回转成型。</summary>
        public static GameObject GlazedVat(Transform parent, StyleMaterials m, float height, float radius, string name = "陶缸")
        {
            float h = height, r = radius, w = radius * 0.055f;
            var profile = new[]
            {
                new Vector2(r * 0.42f, 0f),
                new Vector2(r * 0.66f, h * 0.02f),
                new Vector2(r * 0.90f, h * 0.16f),
                new Vector2(r * 1.00f, h * 0.46f),
                new Vector2(r * 0.95f, h * 0.76f),
                new Vector2(r * 0.90f, h * 0.91f),
                new Vector2(r * 0.95f, h * 0.985f),
                new Vector2(r * 0.94f, h),
                new Vector2(r * 0.88f, h),
                new Vector2(r * 0.85f, h * 0.95f),
                new Vector2(r * 0.90f, h * 0.76f),
                new Vector2(r * 0.94f, h * 0.46f),
                new Vector2(r * 0.85f, h * 0.16f),
                new Vector2(r * 0.58f, h * 0.055f),
                new Vector2(0f, h * 0.05f)
            };
            var mesh = ProcMesh.Lathe(profile, 48, false, true, false);
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            ProcMesh.Place("body", root.transform, mesh, m.Ceramic, Vector3.zero);
            return root;
        }

        /// <summary>木桶：略收分的桶身 + 两道竹/铁箍。</summary>
        public static GameObject WoodenBucket(Transform parent, StyleMaterials m, float height, float radius, string name = "木桶")
        {
            float h = height, r = radius;
            var profile = new[]
            {
                new Vector2(r * 0.50f, 0f),
                new Vector2(r * 0.68f, h * 0.01f),
                new Vector2(r * 0.88f, h * 0.05f),
                new Vector2(r * 0.95f, h * 0.35f),
                new Vector2(r * 1.00f, h * 0.86f),
                new Vector2(r * 1.02f, h * 0.96f),
                new Vector2(r * 1.01f, h),
                new Vector2(r * 0.93f, h),
                new Vector2(r * 0.92f, h * 0.95f),
                new Vector2(r * 0.90f, h * 0.86f),
                new Vector2(r * 0.85f, h * 0.35f),
                new Vector2(r * 0.78f, h * 0.06f),
                new Vector2(r * 0.55f, h * 0.035f),
                new Vector2(0f, h * 0.03f)
            };
            var mesh = ProcMesh.Lathe(profile, 44, false, true, false);

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            ProcMesh.Place("body", root.transform, mesh, m.Bench, Vector3.zero);

            var hoopLow = ProcMesh.Torus(r * 0.94f, r * 0.012f, 40, 8);
            ProcMesh.Place("hoop_low", root.transform, hoopLow, m.Metal, new Vector3(0f, h * 0.20f, 0f));
            var hoopHigh = ProcMesh.Torus(r * 0.99f, r * 0.012f, 40, 8);
            ProcMesh.Place("hoop_high", root.transform, hoopHigh, m.Metal, new Vector3(0f, h * 0.82f, 0f));
            return root;
        }

        public static GameObject WoodenTray(Transform parent, StyleMaterials m, float w, float d, float h, string name = "木盘")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            ProcMesh.Place("bottom", root.transform, ProcMesh.Box(new Vector3(w, h * 0.35f, d), 0.006f), m.Bench,
                new Vector3(0f, h * 0.18f, 0f));
            float t = h * 0.75f;
            ProcMesh.Place("rim_l", root.transform, ProcMesh.Box(new Vector3(0.012f, t, d), 0.005f), m.Bench,
                new Vector3(-w * 0.5f + 0.006f, h * 0.5f, 0f));
            ProcMesh.Place("rim_r", root.transform, ProcMesh.Box(new Vector3(0.012f, t, d), 0.005f), m.Bench,
                new Vector3(w * 0.5f - 0.006f, h * 0.5f, 0f));
            ProcMesh.Place("rim_f", root.transform, ProcMesh.Box(new Vector3(w, t, 0.012f), 0.005f), m.Bench,
                new Vector3(0f, h * 0.5f, -d * 0.5f + 0.006f));
            ProcMesh.Place("rim_b", root.transform, ProcMesh.Box(new Vector3(w, t, 0.012f), 0.005f), m.Bench,
                new Vector3(0f, h * 0.5f, d * 0.5f - 0.006f));
            return root;
        }

        // ---------------------------------------------------------------- 工具

        /// <summary>木瓢：碗形瓢头 + 斜出的木柄。原点在瓢口中心。</summary>
        public static GameObject Ladle(Transform parent, StyleMaterials m, string name = "木瓢")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var bowl = new[]
            {
                new Vector2(0.018f, 0f),
                new Vector2(0.055f, 0.006f),
                new Vector2(0.082f, 0.028f),
                new Vector2(0.094f, 0.060f),
                new Vector2(0.098f, 0.078f),
                new Vector2(0.090f, 0.080f),
                new Vector2(0.084f, 0.062f),
                new Vector2(0.070f, 0.032f),
                new Vector2(0.040f, 0.012f),
                new Vector2(0f, 0.010f)
            };
            ProcMesh.Place("bowl", root.transform, ProcMesh.Lathe(bowl, 32, false, true, false), m.ToolWood, Vector3.zero);

            var handle = ProcMesh.Place("handle", root.transform, ProcMesh.Box(new Vector3(0.020f, 0.055f, 0.022f), 0.006f),
                m.ToolWood, new Vector3(0.100f, 0.055f, 0f), Quaternion.Euler(0f, 0f, -22f));
            ProcMesh.Place("grip", handle.transform, ProcMesh.Box(new Vector3(0.021f, 0.150f, 0.023f), 0.007f),
                m.ToolWood, new Vector3(0f, 0.095f, 0f));
            return root;
        }

        /// <summary>竹刮：扁平竹片，刃口薄、握把厚。</summary>
        public static GameObject Scraper(Transform parent, StyleMaterials m, string name = "竹刮")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            ProcMesh.Place("blade", root.transform, ProcMesh.Box(new Vector3(0.115f, 0.006f, 0.055f), 0.003f),
                m.Bamboo, new Vector3(0.075f, 0f, 0f));
            ProcMesh.Place("edge", root.transform, ProcMesh.Box(new Vector3(0.020f, 0.003f, 0.055f), 0.0015f),
                m.Bamboo, new Vector3(0.142f, 0.001f, 0f));
            ProcMesh.Place("grip", root.transform, ProcMesh.Box(new Vector3(0.095f, 0.014f, 0.032f), 0.006f),
                m.Bamboo, new Vector3(-0.040f, 0.004f, 0f));
            return root;
        }

        /// <summary>撇水勺：浅口宽勺，专门撇去上层清水。</summary>
        public static GameObject DecantLadle(Transform parent, StyleMaterials m, string name = "撇水勺")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var bowl = new[]
            {
                new Vector2(0.020f, 0f),
                new Vector2(0.070f, 0.004f),
                new Vector2(0.100f, 0.020f),
                new Vector2(0.112f, 0.042f),
                new Vector2(0.114f, 0.048f),
                new Vector2(0.104f, 0.049f),
                new Vector2(0.094f, 0.030f),
                new Vector2(0.062f, 0.012f),
                new Vector2(0f, 0.008f)
            };
            ProcMesh.Place("bowl", root.transform, ProcMesh.Lathe(bowl, 32, false, true, false), m.ToolWood, Vector3.zero);

            var handle = ProcMesh.Place("handle", root.transform, ProcMesh.Box(new Vector3(0.020f, 0.055f, 0.024f), 0.006f),
                m.ToolWood, new Vector3(0.112f, 0.050f, 0f), Quaternion.Euler(0f, 0f, -16f));
            ProcMesh.Place("grip", handle.transform, ProcMesh.Box(new Vector3(0.021f, 0.230f, 0.025f), 0.007f),
                m.ToolWood, new Vector3(0f, 0.140f, 0f));
            return root;
        }

        // ---------------------------------------------------------------- 筛

        /// <summary>竹筛：薄壁篾圈 + 细密筛网。cloths 返回筛网渲染器以便驱动湿态。</summary>
        public static GameObject Sieve(Transform parent, StyleMaterials m, float radius, out Renderer cloth,
                                       string name = "竹筛")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            float r = radius;
            var hoop = new[]
            {
                new Vector2(r * 0.93f, 0f),
                new Vector2(r * 0.945f, 0.016f),
                new Vector2(r * 0.965f, 0.046f),
                new Vector2(r * 1.00f, 0.062f),
                new Vector2(r * 1.01f, 0.070f),
                new Vector2(r * 0.985f, 0.076f),
                new Vector2(r * 0.955f, 0.062f),
                new Vector2(r * 0.925f, 0.030f),
                new Vector2(r * 0.915f, 0.002f)
            };
            ProcMesh.Place("hoop", root.transform, ProcMesh.Lathe(hoop, 52, false, false, false), m.Bamboo, Vector3.zero);

            var clothMesh = ProcMesh.Disc(r * 0.93f, 44);
            cloth = ProcMesh.Place("cloth", root.transform, clothMesh, m.SieveCloth, new Vector3(0f, 0.004f, 0f));
            cloth.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return root;
        }

        /// <summary>筛架：环状承托 + 四条外撇木腿，下方可容沉淀缸。</summary>
        public static GameObject SieveStand(Transform parent, StyleMaterials m, float height, float legSpread,
                                            float ringRadius, string name = "筛架")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            float h = height;
            var ring = ProcMesh.Torus(ringRadius, 0.021f, 44, 9);
            ProcMesh.Place("ring", root.transform, ring, m.Bench, new Vector3(0f, h, 0f));

            for (int i = 0; i < 4; i++)
            {
                float sign = (i < 2) ? -1f : 1f;
                float signZ = (i % 2 == 0) ? -1f : 1f;
                Vector3 bottom = new Vector3(sign * legSpread, 0f, signZ * legSpread);
                Vector3 top = new Vector3(sign * ringRadius * 0.86f, h - 0.018f, signZ * ringRadius * 0.86f);

                var legMesh = ProcMesh.Cylinder(0.017f, 0.022f, Vector3.Distance(bottom, top), 10, true, true);
                // 圆柱网格原点在底面，因此按 bottom 定位并朝 top 旋转
                var leg = ProcMesh.Place("leg_" + i, root.transform, legMesh, m.Beam, bottom,
                    Quaternion.FromToRotation(Vector3.up, (top - bottom).normalized));
                leg.transform.localScale = new Vector3(1f, 1f, 1f);
            }

            // 中段横撑，增加结构可信度
            float braceY = h * 0.42f;
            float span = Mathf.Lerp(legSpread, ringRadius * 0.86f, braceY / h) * 2f;
            ProcMesh.Place("brace_a", root.transform, ProcMesh.Box(new Vector3(span, 0.026f, 0.022f), 0.005f),
                m.Beam, new Vector3(0f, braceY, -Mathf.Lerp(legSpread, ringRadius * 0.86f, braceY / h)));
            ProcMesh.Place("brace_b", root.transform, ProcMesh.Box(new Vector3(span, 0.026f, 0.022f), 0.005f),
                m.Beam, new Vector3(0f, braceY, Mathf.Lerp(legSpread, ringRadius * 0.86f, braceY / h)));
            return root;
        }

        // ---------------------------------------------------------------- 家具

        public static GameObject Bench(Transform parent, StyleMaterials m, Vector3 size, string name = "工作台")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            float w = size.x, h = size.y, d = size.z;
            float topT = 0.075f;

            ProcMesh.Place("top", root.transform, ProcMesh.Box(new Vector3(w, topT, d), 0.008f), m.Bench,
                new Vector3(0f, h - topT * 0.5f, 0f));
            float legT = 0.075f;
            float inset = 0.085f;
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0) ? -1f : 1f;
                float sz = (i < 2) ? -1f : 1f;
                ProcMesh.Place("leg_" + i, root.transform,
                    ProcMesh.Box(new Vector3(legT, h - topT, legT), 0.008f), m.Beam,
                    new Vector3(sx * (w * 0.5f - inset), (h - topT) * 0.5f, sz * (d * 0.5f - inset)));
            }
            ProcMesh.Place("stretcher_l", root.transform,
                ProcMesh.Box(new Vector3(0.045f, 0.05f, d - inset * 2f), 0.006f), m.Beam,
                new Vector3(-(w * 0.5f - inset), 0.2f, 0f));
            ProcMesh.Place("stretcher_r", root.transform,
                ProcMesh.Box(new Vector3(0.045f, 0.05f, d - inset * 2f), 0.006f), m.Beam,
                new Vector3(w * 0.5f - inset, 0.2f, 0f));
            return root;
        }

        public static GameObject Stool(Transform parent, StyleMaterials m, float height, string name = "木凳")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            ProcMesh.Place("seat", root.transform, ProcMesh.Box(new Vector3(0.34f, 0.045f, 0.26f), 0.008f), m.Bench,
                new Vector3(0f, height, 0f));
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0) ? -1f : 1f;
                float sz = (i < 2) ? -1f : 1f;
                ProcMesh.Place("leg_" + i, root.transform, ProcMesh.Box(new Vector3(0.035f, height, 0.035f), 0.006f),
                    m.Beam, new Vector3(sx * 0.135f, height * 0.5f, sz * 0.095f));
            }
            return root;
        }

        /// <summary>靠墙木架：两层搁板 + 斜撑，用于摆放晾干的竹筛与陶罐。</summary>
        public static GameObject WallShelf(Transform parent, StyleMaterials m, float width, float depth,
                                           float topY, string name = "木架")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            ProcMesh.Place("plank_top", root.transform, ProcMesh.Box(new Vector3(width, 0.04f, depth), 0.006f), m.Bench,
                new Vector3(0f, topY, 0f));
            ProcMesh.Place("plank_low", root.transform, ProcMesh.Box(new Vector3(width, 0.035f, depth), 0.006f), m.Bench,
                new Vector3(0f, topY - 0.44f, 0f));
            for (int i = 0; i < 2; i++)
            {
                float sx = (i == 0) ? -1f : 1f;
                ProcMesh.Place("side_" + i, root.transform, ProcMesh.Box(new Vector3(0.035f, topY, depth * 0.9f), 0.006f),
                    m.Beam, new Vector3(sx * (width * 0.5f - 0.02f), topY * 0.5f, 0f));
            }
            return root;
        }

        public static GameObject Sack(Transform parent, StyleMaterials m, float height, float radius, int seed, string name = "原料袋")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = ProcMesh.Blob(radius, height, 20, 7, 0.26f, seed);
            ProcMesh.Place("body", root.transform, body, m.Cloth, Vector3.zero);
            var knot = ProcMesh.Lathe(new[]
            {
                new Vector2(radius * 0.30f, height * 0.94f),
                new Vector2(radius * 0.20f, height * 1.02f),
                new Vector2(radius * 0.26f, height * 1.06f),
                new Vector2(0f, height * 1.05f)
            }, 18, false, false, true);
            ProcMesh.Place("knot", root.transform, knot, m.Cloth, Vector3.zero);
            return root;
        }

        public static GameObject StorageJar(Transform parent, StyleMaterials m, float height, float radius, string name = "陶罐")
        {
            float h = height, r = radius;
            var profile = new[]
            {
                new Vector2(r * 0.50f, 0f),
                new Vector2(r * 0.78f, h * 0.03f),
                new Vector2(r * 0.98f, h * 0.28f),
                new Vector2(r * 1.00f, h * 0.55f),
                new Vector2(r * 0.82f, h * 0.80f),
                new Vector2(r * 0.60f, h * 0.90f),
                new Vector2(r * 0.62f, h * 0.97f),
                new Vector2(r * 0.56f, h),
                new Vector2(r * 0.42f, h * 0.93f),
                new Vector2(0f, h * 0.92f)
            };
            var mesh = ProcMesh.Lathe(profile, 40, false, true, false);
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            ProcMesh.Place("body", root.transform, mesh, m.CeramicPale, Vector3.zero);
            return root;
        }

        // ---------------------------------------------------------------- 手

        /// <summary>
        /// 手部占位：掌 + 指团 + 拇指 + 袖口。
        /// 造型刻意简化，重点在"握住工具"的体积关系，细节交给后续美术替换。
        /// </summary>
        public static GameObject Hand(Transform parent, StyleMaterials m, string name = "手")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var skin = m.ToolWood; // 占位材质，正式美术替换为皮肤材质
            ProcMesh.Place("palm", root.transform, ProcMesh.Box(new Vector3(0.086f, 0.036f, 0.098f), 0.014f),
                skin, new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f));
            ProcMesh.Place("fingers", root.transform, ProcMesh.Box(new Vector3(0.080f, 0.052f, 0.040f), 0.016f),
                skin, new Vector3(0f, 0.016f, 0.060f), Quaternion.Euler(-14f, 0f, 0f));
            ProcMesh.Place("thumb", root.transform, ProcMesh.Box(new Vector3(0.030f, 0.028f, 0.056f), 0.012f),
                skin, new Vector3(-0.048f, 0.012f, 0.028f), Quaternion.Euler(-22f, 18f, 0f));

            var cuff = ProcMesh.Place("cuff", root.transform, ProcMesh.Cylinder(0.052f, 0.062f, 0.150f, 18, false, false),
                m.Cloth, new Vector3(0.004f, -0.010f, -0.135f), Quaternion.Euler(-78f, 0f, 0f));

            return root;
        }
    }
}
