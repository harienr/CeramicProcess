using UnityEngine;

namespace CiCongNiSheng.Journey
{
    /// <summary>Step 5: wash solid porcelain stones. Reuses the original project's basket, materials and meshes.</summary>
    public sealed class StoneWashStation : MonoBehaviour
    {
        public Transform Sieve { get; private set; }
        public Transform RawPoint { get; private set; }
        public Transform WastePoint { get; private set; }
        public bool Loaded { get; private set; }
        public bool DebrisRemoved { get; private set; }
        public bool Taken { get; private set; }
        public float Cleanliness { get; private set; }
        Transform rawStones;
        Transform sieveStones;
        Transform debris;
        Transform wasteTray;
        Transform mud;
        Renderer[] stoneRenderers;
        Vector3 rest;
        MaterialPropertyBlock block;

        public static StoneWashStation Build(Transform parent, StyleMaterials style)
        {
            var host = new GameObject("05 · 筛洗瓷石");
            host.transform.SetParent(parent, false);
            var station = host.AddComponent<StoneWashStation>();
            station.Construct(style);
            return station;
        }

        Transform Node(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        void Construct(StyleMaterials style)
        {
            block = new MaterialPropertyBlock();
            // Shallow wash trough; the sieve contains recognisable stone pieces throughout washing.
            Box("石槽底", new Vector3(1.55f, 0.14f, 1.28f), new Vector3(0, 0.2f, 0), style.Ceramic);
            Box("槽沿前", new Vector3(1.65f, 0.35f, 0.12f), new Vector3(0, 0.4f, -0.64f), style.Ceramic);
            Box("槽沿后", new Vector3(1.65f, 0.35f, 0.12f), new Vector3(0, 0.4f, 0.64f), style.Ceramic);
            Box("槽沿左", new Vector3(0.12f, 0.35f, 1.22f), new Vector3(-0.77f, 0.4f, 0), style.Ceramic);
            Box("槽沿右", new Vector3(0.12f, 0.35f, 1.22f), new Vector3(0.77f, 0.4f, 0), style.Ceramic);
            ProcMesh.Place("浅水", transform, ProcMesh.Plane(1.4f, 1.12f), style.Water, new Vector3(0, 0.45f, 0));
            var sieve = PropLibrary.Sieve(transform, style, 0.55f, out var cloth, "竹筛 · 盛瓷石");
            Sieve = sieve.transform;
            rest = Sieve.localPosition = new Vector3(0, 0.48f, 0);
            sieveStones = Node("待洗瓷石", Sieve, new Vector3(0, 0.028f, 0));
            rawStones = Node("待洗石堆", transform, new Vector3(-1.26f, 0.63f, 0.12f));
            var rawBench = PropLibrary.Bench(transform, style, new Vector3(0.7f, 0.58f, 0.82f), "待洗石料台");
            rawBench.transform.localPosition = new Vector3(-1.26f, 0, 0.12f);
            Collider(rawBench, new Vector3(0.7f, 0.58f, 0.82f), new Vector3(0, 0.29f, 0));
            RawPoint = Node("取石位置", transform, new Vector3(-1.26f, 0.8f, 0.12f));
            for (int i = 0; i < 17; i++)
            {
                float angle = i * 2.39996f;
                float r = Mathf.Sqrt((i + 0.5f) / 17f) * 0.37f;
                float size = 0.075f + (i % 4) * 0.011f;
                var mesh = PorcelainFromClay.MeshLab.Rock(size, 130 + i, 0.72f, 18, 10);
                SmoothStone(mesh);
                var local = new Vector3(Mathf.Cos(angle) * r, size * 0.65f + (i % 3) * 0.012f, Mathf.Sin(angle) * r);
                ProcMesh.Place("瓷石_" + i, sieveStones, mesh, style.SlurryDry, local,
                    Quaternion.Euler(0, i * 29, 0));
                ProcMesh.Place("原石_" + i, rawStones, mesh, style.Coarse, local * 0.8f,
                    Quaternion.Euler(0, i * 39, 0));
            }
            stoneRenderers = sieveStones.GetComponentsInChildren<Renderer>();
            debris = Node("夹杂物", Sieve, Vector3.up * 0.06f);
            for (int i = 0; i < 5; i++)
            {
                var piece = ProcMesh.Place("杂物_" + i, debris,
                    ProcMesh.Blob(0.044f, 0.028f, 9, 3, 0.5f, i + 90), style.Coarse,
                    new Vector3(Mathf.Sin(i * 2.1f) * 0.33f, 0.04f, Mathf.Cos(i * 2.1f) * 0.3f));
                piece.transform.localScale = new Vector3(1.8f, 1, 0.6f);
            }
            var stool = PropLibrary.Stool(transform, style, 0.55f, "杂物盘架");
            stool.transform.localPosition = new Vector3(1.13f, 0, 0.12f);
            wasteTray = PropLibrary.WoodenTray(stool.transform, style, 0.46f, 0.36f, 0.065f, "废杂物盘").transform;
            wasteTray.localPosition = Vector3.up * 0.59f;
            WastePoint = Node("杂物盘位置", transform, new Vector3(1.13f, 0.74f, 0.12f));
            mud = ProcMesh.Place("附着泥沙", Sieve, ProcMesh.Disc(0.42f, 32), style.Coarse, Vector3.up * 0.012f).transform;
            sieveStones.gameObject.SetActive(false);
            debris.gameObject.SetActive(false);
            mud.gameObject.SetActive(false);
        }

        static void SmoothStone(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                normals[i] = new Vector3(vertices[i].x, vertices[i].y / (0.72f * 0.72f), vertices[i].z).normalized;
            mesh.normals = normals;
        }
        void Box(string name, Vector3 size, Vector3 position, Material material)
        {
            var go = ProcMesh.Place(name, transform, ProcMesh.Box(size, 0.02f), material, position).gameObject;
            Collider(go, size, Vector3.zero);
        }

        static void Collider(GameObject go, Vector3 size, Vector3 center)
        {
            var c = go.AddComponent<BoxCollider>();
            c.size = size;
            c.center = center;
        }

        public void Load()
        {
            if (Loaded) return;
            Loaded = true;
            rawStones.gameObject.SetActive(false);
            sieveStones.gameObject.SetActive(true);
            debris.gameObject.SetActive(true);
            mud.gameObject.SetActive(true);
            ApplyWash(0f);
        }

        public void ApplyWash(float value)
        {
            Cleanliness = Mathf.Clamp01(value);
            if (!Loaded || Taken) return;
            float t = Time.time * 7f;
            Sieve.localPosition = rest + new Vector3(Mathf.Sin(t) * 0.035f, Mathf.Sin(t * 2f) * 0.004f, Mathf.Cos(t) * 0.022f);
            Sieve.localRotation = Quaternion.Euler(Mathf.Sin(t) * 2f, 0, Mathf.Cos(t) * 2f);
            Color tint = Color.Lerp(new Color(0.48f, 0.43f, 0.34f), new Color(0.91f, 0.88f, 0.79f), Cleanliness);
            foreach (var renderer in stoneRenderers)
            {
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", tint);
                renderer.SetPropertyBlock(block);
            }
            mud.localScale = Vector3.one * Mathf.Max(0.01f, 1f - Cleanliness);
            mud.gameObject.SetActive(Cleanliness < 0.97f);
        }

        public void StopMotion()
        {
            Sieve.localPosition = rest;
            Sieve.localRotation = Quaternion.identity;
        }

        public void RemoveDebris()
        {
            if (Cleanliness < 1f || DebrisRemoved) return;
            DebrisRemoved = true;
            debris.SetParent(wasteTray, false);
            debris.localPosition = new Vector3(0, 0.01f, 0);
            debris.localScale = Vector3.one * 0.48f;
        }

        public void Take()
        {
            if (!DebrisRemoved || Taken) return;
            Taken = true;
            sieveStones.gameObject.SetActive(false);
        }
    }
}
