using System.Collections.Generic;
using UnityEngine;

namespace PorcelainFromClay
{
    public sealed class TaoxiVat : MonoBehaviour
    {
        public string DisplayName;
        public Transform Root;
        public Transform WaterSurface;
        public Transform Sediment;
        public Transform ClayLayer;
        public Transform PowderMound;
        public Transform CoarseGroup;
        public Material WaterMaterial;

        public float OuterWidth;
        public float OuterDepth;
        public float TopY;
        public float WallThickness;
        public float InnerWidth;
        public float InnerDepth;
        public float InnerBottom;
        public float InnerTop;
        public float CurrentFill;
        public float CurrentTurbidity;

        readonly List<Renderer> _coarseRenderers = new List<Renderer>();
        Vector3 _waterBasePosition;
        float _waterTravel;
        MaterialLibrary mats;

        public void Build(MaterialLibrary materials, Transform parent, string name, Vector3 center, float outerW, float outerD, float topY, float wall = 0.095f, float bottom = 0.09f, bool withPowder = false, bool lower = false)
        {
            mats = materials;
            DisplayName = name;
            OuterWidth = outerW;
            OuterDepth = outerD;
            TopY = topY;
            WallThickness = wall;
            InnerWidth = outerW - wall * 2f;
            InnerDepth = outerD - wall * 2f;
            InnerBottom = bottom;
            InnerTop = topY;
            _waterTravel = Mathf.Max(0.1f, topY - bottom - 0.08f);

            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
            Root.localPosition = new Vector3(center.x, 0f, center.z);

            // Outer body: one worn stone slab per side. A slight plinth and a dark shadow line
            // keep it from reading as a stack of soft blocks.
            for (int side = 0; side < 4; side++)
            {
                bool xSide = side < 2;
                float sign = side % 2 == 0 ? -1f : 1f;
                float length = xSide ? OuterDepth : OuterWidth;
                Vector3 size = xSide ? new Vector3(wall, topY, length) : new Vector3(length, topY, wall);
                Vector3 pos = xSide
                    ? new Vector3(sign * (OuterWidth * 0.5f - wall * 0.5f), topY * 0.5f, 0f)
                    : new Vector3(0f, topY * 0.5f, sign * (OuterDepth * 0.5f - wall * 0.5f));
                var block = MeshLab.MeshObject(name + "_Wall_" + side, Root, MeshLab.RoundedBox(size, 0.006f, 2.4f, 3), mats.Stone, pos, Quaternion.identity);
                MeshLab.Tint(block.GetComponent<Renderer>(), new Color(0.91f, 0.9f, 0.86f));

                // low, darker plinth course
                Vector3 psize = xSide ? new Vector3(wall + 0.025f, 0.11f, length + 0.015f) : new Vector3(length + 0.015f, 0.11f, wall + 0.025f);
                Vector3 ppos = xSide
                    ? new Vector3(sign * (OuterWidth * 0.5f - wall * 0.5f), 0.055f, 0f)
                    : new Vector3(0f, 0.055f, sign * (OuterDepth * 0.5f - wall * 0.5f));
                var plinth = MeshLab.MeshObject(name + "_Plinth_" + side, Root, MeshLab.RoundedBox(psize, 0.008f, 2.0f, 2), mats.Stone, ppos, Quaternion.identity);
                MeshLab.Tint(plinth.GetComponent<Renderer>(), new Color(0.78f, 0.77f, 0.73f));
                MeshLab.SetWetness(plinth.GetComponent<Renderer>(), 0.2f);
            }

            // inner wet liner and floor
            float liner = 0.018f;
            AddLiner(new Vector3(0f, topY * 0.5f, -InnerDepth * 0.5f + liner * 0.5f), new Vector3(InnerWidth, topY, liner), mats.StoneWet, name + "_InnerFront");
            AddLiner(new Vector3(0f, topY * 0.5f, InnerDepth * 0.5f - liner * 0.5f), new Vector3(InnerWidth, topY, liner), mats.StoneWet, name + "_InnerBack");
            AddLiner(new Vector3(-InnerWidth * 0.5f + liner * 0.5f, topY * 0.5f, 0f), new Vector3(liner, topY, InnerDepth), mats.StoneWet, name + "_InnerLeft");
            AddLiner(new Vector3(InnerWidth * 0.5f - liner * 0.5f, topY * 0.5f, 0f), new Vector3(liner, topY, InnerDepth), mats.StoneWet, name + "_InnerRight");
            var floor = MeshLab.MeshObject(name + "_InnerFloor", Root, MeshLab.RoundedBox(new Vector3(InnerWidth, bottom, InnerDepth), 0.008f, 1.4f, 3), mats.StoneWet,
                new Vector3(0f, bottom * 0.5f, 0f), Quaternion.identity);
            MeshLab.Tint(floor.GetComponent<Renderer>(), new Color(0.7f, 0.68f, 0.64f));

            // coping: a continuous worn stone rim, narrower than a picture frame
            float rimH = 0.06f;
            float rimW = 0.105f;
            AddRim(new Vector3(0f, topY + rimH * 0.5f, -OuterDepth * 0.5f + rimW * 0.5f), new Vector3(OuterWidth + rimW * 0.25f, rimH, rimW), name + "_RimFront");
            AddRim(new Vector3(0f, topY + rimH * 0.5f, OuterDepth * 0.5f - rimW * 0.5f), new Vector3(OuterWidth + rimW * 0.25f, rimH, rimW), name + "_RimBack");
            AddRim(new Vector3(-OuterWidth * 0.5f + rimW * 0.5f, topY + rimH * 0.5f, 0f), new Vector3(rimW, rimH, OuterDepth + rimW * 0.25f), name + "_RimLeft");
            AddRim(new Vector3(OuterWidth * 0.5f - rimW * 0.5f, topY + rimH * 0.5f, 0f), new Vector3(rimW, rimH, OuterDepth + rimW * 0.25f), name + "_RimRight");

            // damp shadow at the foot of the masonry
            var foot = MeshLab.MeshObject(name + "_FootDamp", Root, MeshLab.Plane(OuterWidth + 0.36f, OuterDepth + 0.36f, 1f), mats.InstantiateShadow(0.18f, 0.7f),
                new Vector3(0f, 0.004f, 0f), Quaternion.identity, false, false);
            foot.GetComponent<Renderer>().sortingOrder = -2;

            // water
            var waterMat = mats.InstantiateWater(new Color(0.1f, 0.12f, 0.115f), new Color(0.26f, 0.255f, 0.235f), 0.18f);
            waterMat.SetFloat("_Smoothness", 0.18f);
            waterMat.SetVector("_Center", new Vector4(center.x, 0f, center.z, 0f));
            WaterMaterial = waterMat;
            WaterSurface = MeshLab.MeshObject(name + "_Water", Root, MeshLab.Plane(InnerWidth - 0.025f, InnerDepth - 0.025f, 1f), waterMat,
                new Vector3(0f, InnerBottom + 0.01f, 0f), Quaternion.identity, false, false).transform;
            WaterSurface.gameObject.SetActive(false);

            // settled fine clay (lower vat) / thin silt (upper vat)
            var clayMat = lower ? mats.WetClay : mats.WetClay;
            var clayMesh = MeshLab.Rock(1f, lower ? 811 : 812, 0.12f, 20, 10);
            var clay = MeshLab.MeshObject(name + "_Silt", Root, clayMesh, clayMat, new Vector3(0f, InnerBottom + 0.015f, 0f), Quaternion.identity, false, true);
            clay.transform.localScale = new Vector3(InnerWidth * 0.33f, 0.16f, InnerDepth * 0.33f);
            MeshLab.Tint(clay.GetComponent<Renderer>(), new Color(1.22f, 1.16f, 1.04f));
            clay.SetActive(false);
            ClayLayer = clay.transform;

            var sediment = MeshLab.MeshObject(name + "_Sediment", Root, MeshLab.Rock(1f, lower ? 821 : 822, 0.08f, 18, 8), mats.WetClay,
                new Vector3(0f, InnerBottom + 0.05f, 0f), Quaternion.identity, false, true);
            sediment.transform.localScale = new Vector3(InnerWidth * 0.34f, 0.09f, InnerDepth * 0.34f);
            MeshLab.Tint(sediment.GetComponent<Renderer>(), new Color(0.72f, 0.69f, 0.63f));
            sediment.SetActive(false);
            Sediment = sediment.transform;

            if (withPowder)
            {
                var powderRoot = new GameObject(name + "_DryPowder").transform;
                powderRoot.SetParent(Root, false);
                powderRoot.localPosition = new Vector3(0f, InnerBottom + 0.012f, 0.02f);

                var mound = MeshLab.MeshObject("PowderMound", powderRoot, MeshLab.Lathe(new[]
                {
                    new Vector2(0f, 0f), new Vector2(0.13f, 0.008f), new Vector2(0.25f, 0.03f),
                    new Vector2(0.34f, 0.06f), new Vector2(0.36f, 0.092f), new Vector2(0.32f, 0.112f),
                    new Vector2(0.22f, 0.128f), new Vector2(0.1f, 0.138f), new Vector2(0f, 0.142f)
                }, 40, 1.2f, true, false), mats.DryPowder, Vector3.zero, Quaternion.identity, false, true);
                mound.transform.localScale = new Vector3(InnerWidth / 0.72f, 1f, InnerDepth / 0.66f);

                var clod = MeshLab.MeshObject("PowderClod", powderRoot, MeshLab.Rock(1f, 831, 0.3f, 14, 7), mats.DryPowder,
                    new Vector3(InnerWidth * 0.22f, 0.095f, -InnerDepth * 0.12f), Quaternion.Euler(0f, 25f, 0f), false, true);
                clod.transform.localScale = new Vector3(0.1f, 0.055f, 0.085f);
                var clod2 = MeshLab.MeshObject("PowderClod2", powderRoot, MeshLab.Rock(1f, 833, 0.25f, 12, 6), mats.DryPowder,
                    new Vector3(-InnerWidth * 0.2f, 0.085f, InnerDepth * 0.13f), Quaternion.Euler(0f, 150f, 0f), false, true);
                clod2.transform.localScale = new Vector3(0.08f, 0.04f, 0.07f);

                var scoop = new GameObject("PowderScoop").transform;
                scoop.SetParent(powderRoot, false);
                scoop.localPosition = new Vector3(InnerWidth * 0.16f, 0.17f, -0.02f);
                scoop.localRotation = Quaternion.Euler(12f, -18f, 58f);
                MeshLab.MeshObject("ScoopBowl", scoop, MeshLab.Lathe(new[]
                {
                    new Vector2(0f, 0f), new Vector2(0.04f, 0.002f), new Vector2(0.07f, 0.02f), new Vector2(0.075f, 0.04f),
                    new Vector2(0.064f, 0.042f), new Vector2(0.046f, 0.024f), new Vector2(0.016f, 0.008f), new Vector2(0f, 0.01f)
                }, 18, 1.5f, false, false), mats.Wood, Vector3.zero, Quaternion.identity, false, true);
                MeshLab.BoxObject("ScoopHandle", scoop, new Vector3(0.03f, 0.16f, 0.03f), 0.008f, mats.Wood,
                    new Vector3(0f, 0.1f, 0f), 2.4f, false);

                PowderMound = powderRoot;
            }

            // coarse sand: only becomes legible after the fine slurry is drawn off
            var coarseRoot = new GameObject(name + "_Coarse").transform;
            coarseRoot.SetParent(Root, false);
            coarseRoot.localPosition = new Vector3(0f, InnerBottom + 0.035f, 0f);
            var rr = new System.Random(lower ? 912 : 911);
            for (int i = 0; i < 26; i++)
            {
                float x = (float)(rr.NextDouble() - 0.5) * InnerWidth * 0.75f;
                float z = (float)(rr.NextDouble() - 0.5) * InnerDepth * 0.75f;
                float s = Mathf.Lerp(0.018f, 0.045f, (float)rr.NextDouble());
                var pebble = MeshLab.MeshObject("Grit" + i, coarseRoot, MeshLab.Rock(1f, 900 + i, 0.45f, 8, 5), mats.RawStone,
                    new Vector3(x, 0f, z), Quaternion.Euler(0f, (float)rr.NextDouble() * 360f, 0f), false, true);
                pebble.transform.localScale = new Vector3(s, s * 0.55f, s);
                MeshLab.Tint(pebble.GetComponent<Renderer>(), new Color(0.85f, 0.82f, 0.76f));
                _coarseRenderers.Add(pebble.GetComponent<Renderer>());
            }
            coarseRoot.gameObject.SetActive(false);
            CoarseGroup = coarseRoot;

            // a little water-darkening on the outside near the rim
            var damp = MeshLab.MeshObject(name + "_RimDamp", Root, MeshLab.Plane(OuterWidth + 0.12f, OuterDepth + 0.12f, 1f), mats.InstantiateShadow(0.14f, 0.8f),
                new Vector3(0f, topY + 0.006f, 0f), Quaternion.identity, false, false);
            damp.GetComponent<Renderer>().sortingOrder = -1;
        }

        void AddLiner(Vector3 pos, Vector3 size, Material mat, string name)
        {
            MeshLab.MeshObject(name, Root, MeshLab.RoundedBox(size, 0.009f, 1.2f, 2), mat, pos, Quaternion.identity, false, true);
        }

        void AddRim(Vector3 pos, Vector3 size, string name)
        {
            var go = MeshLab.MeshObject(name, Root, MeshLab.RoundedBox(size, 0.025f, 1.25f, 4), mats.StoneRim, pos, Quaternion.identity, true, true);
            var r = go.GetComponent<Renderer>();
            MeshLab.SetWetness(r, 0.1f);
        }

        public void ClearContents()
        {
            SetFill(0f);
            SetPowder(0f);
            SetCoarse(false);
            SetSilt(0f);
            SetClay(0f);
            SetSwirl(0f);
            SetTurbidity(0f);
        }

        public void SetFill(float fill01, bool visible = true)
        {
            CurrentFill = Mathf.Clamp01(fill01);
            if (WaterSurface == null) return;
            bool show = visible && CurrentFill > 0.012f;
            WaterSurface.gameObject.SetActive(show);
            if (show)
            {
                _waterBasePosition = new Vector3(0f, InnerBottom + 0.02f + CurrentFill * _waterTravel, 0f);
                WaterSurface.localPosition = _waterBasePosition;
            }
        }

        public void SetTurbidity(float value)
        {
            CurrentTurbidity = Mathf.Clamp01(value);
            if (WaterMaterial != null)
            {
                WaterMaterial.SetFloat("_Turbidity", CurrentTurbidity);
                WaterMaterial.SetFloat("_BaseAlpha", Mathf.Lerp(0.08f, 0.28f, CurrentTurbidity));
            }
        }

        public void SetSwirl(float value)
        {
            if (WaterMaterial != null) WaterMaterial.SetFloat("_Swirl", Mathf.Clamp01(value));
        }

        public void SetRipple(float value)
        {
            if (WaterMaterial != null) WaterMaterial.SetFloat("_RippleAmount", Mathf.Clamp01(value));
        }

        public void SetPowder(float amount)
        {
            if (PowderMound == null) return;
            bool show = amount > 0.02f;
            PowderMound.gameObject.SetActive(show);
            if (show)
            {
                PowderMound.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, amount), 1f);
                PowderMound.localPosition = new Vector3(0f, InnerBottom + Mathf.Lerp(0.006f, 0.026f, amount), 0.02f);
            }
        }

        public void SetCoarse(bool visible)
        {
            if (CoarseGroup != null) CoarseGroup.gameObject.SetActive(visible);
        }

        public void SetSilt(float amount)
        {
            if (Sediment != null)
            {
                bool show = amount > 0.01f;
                Sediment.gameObject.SetActive(show);
                if (show)
                {
                    Sediment.localScale = new Vector3(InnerWidth * Mathf.Lerp(0.14f, 0.34f, amount), Mathf.Lerp(0.02f, 0.09f, amount), InnerDepth * Mathf.Lerp(0.14f, 0.34f, amount));
                    Sediment.localPosition = new Vector3(0f, InnerBottom + Mathf.Lerp(0.02f, 0.06f, amount), 0f);
                }
            }
        }

        public void SetClay(float amount)
        {
            if (ClayLayer != null)
            {
                bool show = amount > 0.01f;
                ClayLayer.gameObject.SetActive(show);
                if (show)
                {
                    ClayLayer.localScale = new Vector3(InnerWidth * Mathf.Lerp(0.18f, 0.42f, amount), Mathf.Lerp(0.03f, 0.13f, amount), InnerDepth * Mathf.Lerp(0.18f, 0.42f, amount));
                    // the decanted clay sits as a soft mound just above the remaining shallow water
                    ClayLayer.localPosition = new Vector3(0f, InnerBottom + Mathf.Lerp(0.05f, 0.12f, amount), 0f);
                }
            }
        }

        public void SetDampness(float wet)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial == null) continue;
                if (r.sharedMaterial.name.Contains("Stone"))
                {
                    MeshLab.SetWetness(r, Mathf.Clamp01(0.08f + wet * 0.55f));
                }
            }
        }
    }
}
