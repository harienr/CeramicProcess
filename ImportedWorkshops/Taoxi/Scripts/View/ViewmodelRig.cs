using System.Collections;
using UnityEngine;

namespace PorcelainFromClay
{
    public enum ViewTool
    {
        None,
        PowderScoop,
        WaterBucket,
        Paddle,
        Scraper,
        ClayScoop,
        Hands
    }

    public enum VmAction
    {
        None,
        Powder,
        Water,
        Clay,
        Scrape,
        Gate
    }

    /// <summary>
    /// A small first-person tool rig. It is rendered by a dedicated depth camera so hands and tools
    /// never clip through the vats. Geometry is intentionally simple but materially detailed.
    /// </summary>
    public sealed class ViewmodelRig : MonoBehaviour
    {
        public const int Layer = 8;

        MaterialLibrary mats;
        Camera viewCamera;
        Material nailMat;
        Material skinDark;

        Transform powderRig;
        Transform bucketRig;
        Transform paddleRig;
        Transform scraperRig;
        Transform clayRig;
        Transform handsRig;

        Transform bucketStream;
        Transform powderFill;
        Transform clayFill;
        Transform paddleRoot;
        Transform reachHand;

        Vector3 bucketHome = new Vector3(0f, -0.05f, 0.74f);
        Quaternion bucketHomeRot = Quaternion.Euler(8f, -4f, -3f);
        Vector3 scoopHome = new Vector3(0.03f, 0.02f, 0.71f);
        Quaternion scoopHomeRot = Quaternion.Euler(6f, -6f, -24f);
        Vector3 paddleHome = new Vector3(0.13f, 0.02f, 0.71f);
        Quaternion paddleHomeRot = Quaternion.Euler(3f, -6f, -45f);
        Vector3 scraperHome = new Vector3(0.03f, 0.0f, 0.71f);
        Quaternion scraperHomeRot = Quaternion.Euler(5f, -5f, -15f);
        Vector3 handsHome = new Vector3(0f, -0.08f, 0.68f);
        Quaternion handsHomeRot = Quaternion.identity;

        ViewTool currentTool = ViewTool.None;
        Coroutine raiseRoutine;
        Coroutine actionRoutine;
        float idleSeed;
        bool stirMode;
        float stirAngle;
        bool animating;

        VmAction vmAction;
        float vmTimer;
        float vmDuration;
        bool vmActive;

        public ViewTool CurrentTool { get { return currentTool; } }
        public bool ActionActive { get { return vmActive; } }

        public void Init(MaterialLibrary materials, Camera cam)
        {
            mats = materials;
            viewCamera = cam;
            idleSeed = Random.Range(0f, 10f);
            nailMat = new Material(mats.Skin);
            nailMat.SetColor("_Color", new Color(0.96f, 0.86f, 0.78f));
            skinDark = new Material(mats.Skin);
            skinDark.SetColor("_Color", new Color(0.58f, 0.48f, 0.42f));

            powderRig = MakeRoot("Rig_PowderScoop");
            BuildScoop(powderRig, true);
            bucketRig = MakeRoot("Rig_WaterBucket");
            BuildBucket(bucketRig);
            paddleRig = MakeRoot("Rig_Paddle");
            BuildPaddle(paddleRig);
            scraperRig = MakeRoot("Rig_Scraper");
            BuildScraper(scraperRig);
            clayRig = MakeRoot("Rig_ClayScoop");
            BuildScoop(clayRig, false);
            handsRig = MakeRoot("Rig_Hands");
            BuildHandsOnly(handsRig);

            foreach (var r in new[] { powderRig, bucketRig, paddleRig, scraperRig, clayRig, handsRig })
            {
                r.localScale = Vector3.one * 0.72f;
            }

            powderRig.gameObject.SetActive(false);
            bucketRig.gameObject.SetActive(false);
            paddleRig.gameObject.SetActive(false);
            scraperRig.gameObject.SetActive(false);
            clayRig.gameObject.SetActive(false);
            handsRig.gameObject.SetActive(false);

            BuildLights();
        }

        Transform MakeRoot(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.layer = Layer;
            return go.transform;
        }

        void BuildLights()
        {
            var key = new GameObject("View_Key").AddComponent<Light>();
            key.transform.SetParent(viewCamera.transform, false);
            key.transform.localRotation = Quaternion.Euler(38f, -34f, 0f);
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.93f, 0.83f);
            key.intensity = 0.82f;
            key.cullingMask = 1 << Layer;
            key.shadows = LightShadows.None;

            var fill = new GameObject("View_Fill").AddComponent<Light>();
            fill.transform.SetParent(viewCamera.transform, false);
            fill.transform.localRotation = Quaternion.Euler(12f, 145f, 0f);
            fill.type = LightType.Directional;
            fill.color = new Color(0.66f, 0.72f, 0.78f);
            fill.intensity = 0.32f;
            fill.cullingMask = 1 << Layer;
            fill.shadows = LightShadows.None;

            var rim = new GameObject("View_Rim").AddComponent<Light>();
            rim.transform.SetParent(viewCamera.transform, false);
            rim.transform.localRotation = Quaternion.Euler(-35f, 20f, 0f);
            rim.type = LightType.Directional;
            rim.color = new Color(0.85f, 0.84f, 0.78f);
            rim.intensity = 0.2f;
            rim.cullingMask = 1 << Layer;
            rim.shadows = LightShadows.None;
        }

        // --------------------------------------------------------------- building
        Transform BuildHand(Transform parent, bool left, Vector3 pos, Quaternion rot, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = rot;
            int sx = left ? -1 : 1;

            Part(root, "Palm", MeshLab.RoundedBox(new Vector3(0.078f, 0.068f, 0.04f), 0.018f, 10f, 4), mats.Skin,
                new Vector3(0f, 0f, -0.045f), Quaternion.identity);
            // The grip is centred on local z = 0, matching all tool shafts. The wrist is short:
            // a long forearm between eye and hand was what made the earlier rig read as a stump.
            Part(root, "Forearm", MeshLab.Tube(
                new[] { new Vector3(0f, -0.004f, -0.04f), new Vector3(0f, -0.016f, -0.085f), new Vector3(0f, -0.032f, -0.13f) },
                new[] { 0.034f, 0.038f, 0.041f }, 14, false), mats.Skin, Vector3.zero, Quaternion.identity);
            var cuff = Part(root, "Cuff", MeshLab.Tube(
                new[] { new Vector3(0f, -0.017f, -0.078f), new Vector3(0f, -0.024f, -0.1f) },
                new[] { 0.052f, 0.052f }, 14, false), mats.Linen, Vector3.zero, Quaternion.identity);
            MeshLab.Tint(cuff.GetComponent<Renderer>(), new Color(0.7f, 0.66f, 0.6f));

            float[] fx = { -0.028f, -0.009f, 0.009f, 0.028f };
            for (int i = 0; i < 4; i++)
            {
                float x = fx[i] * sx;
                float spread = (i - 1.5f) * 0.002f;
                var points = new[]
                {
                    new Vector3(x, 0.028f + spread, -0.026f),
                    new Vector3(x, 0.043f + spread, -0.002f),
                    new Vector3(x, 0.033f + spread, 0.021f),
                    new Vector3(x, 0.011f + spread, 0.033f)
                };
                Part(root, "Finger" + i, MeshLab.Tube(points, new[] { 0.0115f, 0.0105f, 0.0092f, 0.0074f }, 10, true),
                    mats.Skin, Vector3.zero, Quaternion.identity);
                Part(root, "Nail" + i, MeshLab.RoundedBox(new Vector3(0.009f, 0.003f, 0.014f), 0.0015f, 14f, 2),
                    nailMat, new Vector3(x, 0.013f + spread, 0.035f), Quaternion.Euler(-22f, 0f, 0f));
                Part(root, "Knuckle" + i, MeshLab.RoundedBox(new Vector3(0.018f, 0.015f, 0.026f), 0.006f, 12f, 2),
                    mats.Skin, new Vector3(x, 0.034f, -0.055f), Quaternion.identity);
                if (i < 3)
                {
                    float gx = (fx[i] + fx[i + 1]) * 0.5f * sx;
                    Part(root, "FingerCrease" + i, MeshLab.RoundedBox(new Vector3(0.0022f, 0.004f, 0.036f), 0.001f, 18f, 2),
                        skinDark, new Vector3(gx, 0.036f, -0.022f), Quaternion.identity);
                }
            }

            float thumbX = 0.044f * sx;
            Part(root, "Thumb", MeshLab.Tube(
                new[] { new Vector3(thumbX, 0.024f, -0.04f), new Vector3(thumbX * 1.2f, 0.041f, -0.004f), new Vector3(thumbX * 0.98f, 0.02f, 0.018f) },
                new[] { 0.014f, 0.0115f, 0.009f }, 10, true), mats.Skin, Vector3.zero, Quaternion.identity);
            Part(root, "ThumbPad", MeshLab.RoundedBox(new Vector3(0.019f, 0.008f, 0.015f), 0.004f, 12f, 2),
                nailMat, new Vector3(thumbX * 0.92f, 0.013f, 0.018f), Quaternion.identity);

            MeshLab.SetLayer(root.gameObject, Layer);
            return root;
        }

        GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot)
        {
            var go = MeshLab.MeshObject(name, parent, mesh, mat, pos, rot, false, true);
            go.layer = Layer;
            return go;
        }

        void BuildScoop(Transform rig, bool powder)
        {
            var profile = new[]
            {
                new Vector2(0.0f, 0.0f), new Vector2(0.035f, 0.002f), new Vector2(0.066f, 0.018f),
                new Vector2(0.087f, 0.042f), new Vector2(0.091f, 0.055f), new Vector2(0.079f, 0.057f),
                new Vector2(0.059f, 0.042f), new Vector2(0.033f, 0.021f), new Vector2(0.0f, 0.014f)
            };
            Part(rig, "Bowl", MeshLab.Lathe(profile, 24, 1.4f, false, false), mats.Wood, new Vector3(-0.17f, 0f, 0f), Quaternion.identity);
            Part(rig, "Handle", MeshLab.Cylinder(0.015f, 0.26f, 10, 1.2f, true), mats.Wood,
                new Vector3(0.02f, 0.045f, 0f), Quaternion.Euler(0f, 0f, 90f));
            Part(rig, "Grip", MeshLab.RoundedBox(new Vector3(0.18f, 0.032f, 0.034f), 0.012f, 10f, 3), mats.Wood,
                new Vector3(0.07f, 0.045f, 0f), Quaternion.identity);

            var fill = Part(rig, "Fill", MeshLab.Rock(1f, powder ? 5101 : 5102, 0.2f, 18, 8), powder ? mats.DryPowder : mats.WetClay,
                new Vector3(-0.17f, powder ? 0.055f : 0.072f, 0f), Quaternion.identity);
            fill.transform.localScale = new Vector3(0.062f, powder ? 0.03f : 0.062f, 0.062f);
            if (powder) powderFill = fill.transform; else clayFill = fill.transform;
            if (!powder)
            {
                fill.transform.localPosition = new Vector3(-0.17f, 0.088f, 0f);
                MeshLab.Tint(fill.GetComponent<Renderer>(), new Color(1.15f, 1.08f, 0.96f));
            }

            BuildHand(rig, false, new Vector3(0.075f, 0.045f, -0.005f), Quaternion.identity, "HandR");
        }

        void BuildBucket(Transform rig)
        {
            var body = new GameObject("BucketBody").transform;
            body.SetParent(rig, false);
            body.localPosition = new Vector3(0f, -0.19f, 0f);

            int staves = 16;
            for (int i = 0; i < staves; i++)
            {
                float a = i / (float)staves * Mathf.PI * 2f;
                var stave = Part(body, "Stave" + i, MeshLab.RoundedBox(new Vector3(0.032f, 0.24f, 0.012f), 0.004f, 8f, 2), mats.WoodWet,
                    new Vector3(Mathf.Cos(a) * 0.128f, 0.12f, Mathf.Sin(a) * 0.128f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f));
                MeshLab.Tint(stave.GetComponent<Renderer>(), Color.Lerp(new Color(0.86f, 0.83f, 0.78f), Color.white, (i % 4) / 4f));
            }
            Part(body, "Bottom", MeshLab.Cylinder(0.126f, 0.018f, 24, 1.2f, true), mats.WoodWet, new Vector3(0f, 0.009f, 0f), Quaternion.identity);
            Part(body, "HoopLower", MeshLab.Lathe(CircleProfile(0.134f, 0.075f, 0.007f, 8), 24, 1.4f, false, false), mats.Bamboo, new Vector3(0f, 0.07f, 0f), Quaternion.identity);
            Part(body, "HoopUpper", MeshLab.Lathe(CircleProfile(0.134f, 0.205f, 0.007f, 8), 24, 1.4f, false, false), mats.Bamboo, new Vector3(0f, 0.205f, 0f), Quaternion.identity);
            Part(body, "Handle", MeshLab.Tube(
                new[] { new Vector3(-0.145f, 0.225f, 0f), new Vector3(-0.08f, 0.29f, 0f), new Vector3(0f, 0.31f, 0f), new Vector3(0.08f, 0.29f, 0f), new Vector3(0.145f, 0.225f, 0f) },
                new[] { 0.01f, 0.011f, 0.012f, 0.011f, 0.01f }, 10, false), mats.Bamboo, Vector3.zero, Quaternion.identity);

            var water = Part(body, "Water", MeshLab.Plane(0.22f, 0.22f, 1f), mats.InstantiateWater(new Color(0.35f, 0.38f, 0.36f), new Color(0.6f, 0.62f, 0.58f), 0.22f),
                new Vector3(0f, 0.17f, 0f), Quaternion.identity);
            water.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            bucketStream = Part(rig, "PourStream", MeshLab.Cylinder(0.012f, 0.34f, 8, 1f, false), mats.InstantiateStream(0.6f),
                new Vector3(0.15f, -0.24f, 0f), Quaternion.identity).transform;
            bucketStream.gameObject.SetActive(false);

            BuildHand(rig, false, new Vector3(0.125f, 0.115f, -0.02f), Quaternion.identity, "HandR");
            BuildHand(rig, true, new Vector3(-0.125f, 0.115f, -0.02f), Quaternion.identity, "HandL");
        }

        void BuildPaddle(Transform rig)
        {
            Part(rig, "Shaft", MeshLab.Cylinder(0.017f, 0.55f, 12, 1.5f, true), mats.Wood,
                new Vector3(-0.1f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f));
            Part(rig, "Neck", MeshLab.RoundedBox(new Vector3(0.1f, 0.028f, 0.05f), 0.008f, 8f, 3), mats.WoodWet,
                new Vector3(-0.38f, -0.005f, 0f), Quaternion.Euler(0f, 0f, 4f));
            Part(rig, "Blade", MeshLab.RoundedBox(new Vector3(0.2f, 0.022f, 0.17f), 0.012f, 5f, 3), mats.WoodWet,
                new Vector3(-0.48f, -0.038f, 0f), Quaternion.Euler(0f, 0f, -8f));
            BuildHand(rig, false, new Vector3(0.06f, 0.0f, -0.005f), Quaternion.identity, "HandR");
        }

        void BuildScraper(Transform rig)
        {
            Part(rig, "Handle", MeshLab.Cylinder(0.018f, 0.24f, 10, 1.2f, true), mats.Wood,
                new Vector3(0.02f, 0.03f, 0f), Quaternion.Euler(0f, 0f, 90f));
            Part(rig, "Blade", MeshLab.RoundedBox(new Vector3(0.14f, 0.018f, 0.09f), 0.006f, 9f, 3), mats.WoodWet,
                new Vector3(-0.2f, 0.02f, 0f), Quaternion.Euler(0f, 0f, -4f));
            BuildHand(rig, false, new Vector3(0.04f, 0.03f, -0.005f), Quaternion.identity, "HandR");
        }

        void BuildHandsOnly(Transform rig)
        {
            var right = BuildHand(rig, false, new Vector3(0.12f, -0.02f, 0f), Quaternion.Euler(4f, -10f, 0f), "HandR");
            BuildHand(rig, true, new Vector3(-0.14f, -0.035f, -0.02f), Quaternion.Euler(2f, 8f, 0f), "HandL");
            reachHand = right;
        }

        Vector2[] CircleProfile(float radius, float y, float tube, int segments)
        {
            var p = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                p[i] = new Vector2(radius + Mathf.Cos(a) * tube, y + Mathf.Sin(a) * tube);
            }
            return p;
        }

        // --------------------------------------------------------------- animation
        public void Equip(ViewTool tool, bool instant = false)
        {
            if (currentTool == tool && tool != ViewTool.None) return;
            if (raiseRoutine != null) StopCoroutine(raiseRoutine);
            HideAllRigs();
            currentTool = tool;
            Transform rig = RigFor(tool);
            if (rig == null) return;
            rig.gameObject.SetActive(true);
            Vector3 home = HomeFor(tool);
            Quaternion homeRot = HomeRotFor(tool);
            rig.localPosition = instant ? home : home + new Vector3(0f, -0.2f, 0.04f);
            rig.localRotation = instant ? homeRot : homeRot * Quaternion.Euler(10f, 0f, -8f);
            if (powderFill != null) powderFill.gameObject.SetActive(tool == ViewTool.PowderScoop);
            if (clayFill != null) clayFill.gameObject.SetActive(tool == ViewTool.ClayScoop);
            if (bucketStream != null) bucketStream.gameObject.SetActive(false);
            if (!instant)
            {
                animating = true;
                raiseRoutine = StartCoroutine(RaiseRoutine(rig, home, homeRot));
            }
        }

        IEnumerator RaiseRoutine(Transform rig, Vector3 home, Quaternion homeRot)
        {
            float t = 0f;
            Vector3 from = rig.localPosition;
            Quaternion fromRot = rig.localRotation;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.32f;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                rig.localPosition = Vector3.Lerp(from, home, e);
                rig.localRotation = Quaternion.Slerp(fromRot, homeRot, e);
                yield return null;
            }
            rig.localPosition = home;
            rig.localRotation = homeRot;
            animating = false;
        }

        public void Hide(bool instant = false)
        {
            if (raiseRoutine != null) StopCoroutine(raiseRoutine);
            if (actionRoutine != null) StopCoroutine(actionRoutine);
            if (instant || currentTool == ViewTool.None)
            {
                currentTool = ViewTool.None;
                HideAllRigs();
                return;
            }
            StartCoroutine(HideRoutine(currentTool, instant));
        }

        IEnumerator HideRoutine(ViewTool tool, bool instant)
        {
            Transform rig = RigFor(tool);
            if (rig == null)
            {
                currentTool = ViewTool.None;
                HideAllRigs();
                yield break;
            }
            animating = true;
            Vector3 home = HomeFor(tool);
            float t = 0f;
            Vector3 from = rig.localPosition;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.22f;
                rig.localPosition = Vector3.Lerp(from, home + new Vector3(0f, -0.2f, 0f), t);
                yield return null;
            }
            animating = false;
            currentTool = ViewTool.None;
            HideAllRigs();
        }

        public void StartAction(VmAction action, float duration)
        {
            vmAction = action;
            vmDuration = duration;
            vmTimer = 0f;
            vmActive = true;
            animating = true;
            switch (action)
            {
                case VmAction.Powder:
                    Equip(ViewTool.PowderScoop, true);
                    if (powderFill != null) powderFill.localScale = new Vector3(0.062f, 0.03f, 0.062f);
                    break;
                case VmAction.Water:
                    Equip(ViewTool.WaterBucket, true);
                    if (bucketStream != null) bucketStream.gameObject.SetActive(false);
                    break;
                case VmAction.Clay:
                    Equip(ViewTool.ClayScoop, true);
                    if (clayFill != null) clayFill.localScale = new Vector3(0.062f, 0.055f, 0.062f);
                    break;
                case VmAction.Scrape: Equip(ViewTool.Scraper, true); break;
                case VmAction.Gate: Equip(ViewTool.Hands, true); break;
            }
        }

        void AnimateAction(float t)
        {
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            switch (vmAction)
            {
                case VmAction.Powder:
                {
                    Transform rig = powderRig;
                    rig.localPosition = scoopHome + new Vector3(-0.01f, 0.02f * e, -0.03f * e);
                    rig.localRotation = scoopHomeRot * Quaternion.Euler(0f, 0f, -48f * e);
                    if (powderFill != null)
                    {
                        var s = powderFill.localScale;
                        s.y = Mathf.Lerp(0.03f, 0.005f, e);
                        powderFill.localScale = s;
                    }
                    break;
                }
                case VmAction.Water:
                {
                    bucketRig.localPosition = bucketHome + new Vector3(0.02f * e, 0.015f * e, -0.035f * e);
                    bucketRig.localRotation = bucketHomeRot * Quaternion.Euler(0f, 0f, 62f * Mathf.Clamp01(t * 2.2f));
                    if (bucketStream != null) bucketStream.gameObject.SetActive(t > 0.25f && t < 0.85f);
                    break;
                }
                case VmAction.Clay:
                {
                    float dip = t < 0.42f ? Mathf.SmoothStep(0f, 1f, t / 0.42f) : Mathf.SmoothStep(1f, 0f, (t - 0.42f) / 0.58f);
                    clayRig.localPosition = scoopHome + new Vector3(0f, -0.055f * dip, 0.02f * dip);
                    clayRig.localRotation = scoopHomeRot * Quaternion.Euler(10f * dip, 0f, -22f * dip);
                    if (clayFill != null)
                    {
                        float s = Mathf.Clamp01(dip * 1.5f);
                        clayFill.localScale = new Vector3(0.062f, 0.055f * s, 0.062f);
                    }
                    break;
                }
                case VmAction.Scrape:
                {
                    float stroke = Mathf.Sin(e * Mathf.PI * 2f) * 0.06f;
                    scraperRig.localPosition = scraperHome + new Vector3(stroke, -0.04f, -0.02f);
                    scraperRig.localRotation = scraperHomeRot * Quaternion.Euler(12f, 0f, 0f);
                    break;
                }
                case VmAction.Gate:
                {
                    float lift = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    handsRig.localPosition = handsHome + new Vector3(0f, 0.04f * lift, -0.03f * lift);
                    handsRig.localRotation = handsHomeRot * Quaternion.Euler(-6f * lift, 0f, 0f);
                    break;
                }
            }
        }

        public void StartPourPowder() { StartAction(VmAction.Powder, 2.4f); }
        public void StartPourWater() { StartAction(VmAction.Water, 3.2f); }
        public void StartCollectClay() { StartAction(VmAction.Clay, 2.4f); }
        public void StartScrapeSand() { StartAction(VmAction.Scrape, 2.2f); }
        public void StartPullGate() { StartAction(VmAction.Gate, 1.8f); }

        public IEnumerator PourPowder()
        {
            Equip(ViewTool.PowderScoop);
            yield return new WaitForSeconds(0.22f);
            Transform rig = powderRig;
            Vector3 home = scoopHome;
            Quaternion homeRot = scoopHomeRot;
            animating = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.05f;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                rig.localPosition = home + new Vector3(-0.01f, 0.02f * e, -0.03f * e);
                rig.localRotation = homeRot * Quaternion.Euler(0f, 0f, -48f * e);
                if (powderFill != null)
                {
                    var s = powderFill.localScale;
                    s.y = Mathf.Lerp(0.026f, 0.004f, e);
                    powderFill.localScale = s;
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.55f);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.65f;
                rig.localPosition = Vector3.Lerp(home + new Vector3(-0.01f, 0.02f, -0.03f), home, t);
                rig.localRotation = Quaternion.Slerp(homeRot * Quaternion.Euler(0f, 0f, -48f), homeRot, t);
                yield return null;
            }
            animating = false;
            yield return new WaitForSeconds(0.2f);
            Hide();
        }

        public IEnumerator PourWater()
        {
            Equip(ViewTool.WaterBucket);
            yield return new WaitForSeconds(0.25f);
            animating = true;
            Vector3 home = bucketHome;
            Quaternion homeRot = bucketHomeRot;
            if (bucketStream != null) bucketStream.gameObject.SetActive(true);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.9f;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                bucketRig.localPosition = home + new Vector3(0.02f * e, 0.015f * e, -0.035f * e);
                bucketRig.localRotation = homeRot * Quaternion.Euler(0f, 0f, 62f * e);
                yield return null;
            }
            yield return new WaitForSeconds(1.45f);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.75f;
                bucketRig.localPosition = Vector3.Lerp(home + new Vector3(0.02f, 0.015f, -0.035f), home, t);
                bucketRig.localRotation = Quaternion.Slerp(homeRot * Quaternion.Euler(0f, 0f, 62f), homeRot, t);
                yield return null;
            }
            if (bucketStream != null) bucketStream.gameObject.SetActive(false);
            animating = false;
            yield return new WaitForSeconds(0.2f);
            Hide();
        }

        public IEnumerator CollectClay()
        {
            Equip(ViewTool.ClayScoop);
            yield return new WaitForSeconds(0.2f);
            animating = true;
            Vector3 home = scoopHome + new Vector3(0.04f, 0f, 0.02f);
            Quaternion homeRot = scoopHomeRot;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                clayRig.localPosition = home + new Vector3(0f, -0.06f * e, 0.02f * e);
                clayRig.localRotation = homeRot * Quaternion.Euler(10f * e, 0f, 8f * e);
                yield return null;
            }
            yield return new WaitForSeconds(0.45f);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                clayRig.localPosition = Vector3.Lerp(home + new Vector3(0f, -0.06f, 0.02f), home + new Vector3(-0.02f, 0.02f, -0.03f), t);
                clayRig.localRotation = Quaternion.Slerp(homeRot * Quaternion.Euler(10f, 0f, 8f), homeRot * Quaternion.Euler(-10f, 0f, -28f), t);
                yield return null;
            }
            if (clayFill != null) clayFill.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.4f);
            animating = false;
            Hide();
        }

        public IEnumerator ScrapeSand()
        {
            Equip(ViewTool.Scraper);
            yield return new WaitForSeconds(0.2f);
            animating = true;
            Vector3 home = scraperHome;
            Quaternion homeRot = scraperHomeRot;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.1f;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                scraperRig.localPosition = home + new Vector3(0f, -0.05f, -0.02f);
                scraperRig.localRotation = homeRot * Quaternion.Euler(12f, 0f, 0f);
                float stroke = Mathf.Sin(e * Mathf.PI * 2f) * 0.06f;
                scraperRig.localPosition += new Vector3(stroke, 0f, 0f);
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.55f;
                scraperRig.localPosition = Vector3.Lerp(home + new Vector3(0f, -0.05f, -0.02f), home, t);
                scraperRig.localRotation = Quaternion.Slerp(homeRot * Quaternion.Euler(12f, 0f, 0f), homeRot, t);
                yield return null;
            }
            animating = false;
            Hide();
        }

        public IEnumerator PullGate()
        {
            Equip(ViewTool.Hands);
            yield return new WaitForSeconds(0.2f);
            animating = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.9f;
                handsRig.localPosition = handsHome + new Vector3(0f, 0.04f * Mathf.Sin(t * Mathf.PI), -0.03f * Mathf.Sin(t * Mathf.PI));
                handsRig.localRotation = handsHomeRot * Quaternion.Euler(-6f * Mathf.Sin(t * Mathf.PI), 0f, 0f);
                yield return null;
            }
            animating = false;
            yield return new WaitForSeconds(0.25f);
            Hide();
        }

        public void BeginStir()
        {
            Equip(ViewTool.Paddle);
            stirMode = true;
        }

        public void EndStir()
        {
            stirMode = false;
            Hide();
        }

        public void SetStirAngle(float angle)
        {
            stirAngle = angle;
        }

        public void SetPreview(ViewTool tool, float phase = 0f)
        {
            Equip(tool, true);
            Transform rig = RigFor(tool);
            if (rig == null) return;
            Vector3 home = HomeFor(tool);
            Quaternion homeRot = HomeRotFor(tool);
            if (tool == ViewTool.WaterBucket)
            {
                rig.localPosition = home + new Vector3(0.02f, 0.015f, -0.03f);
                rig.localRotation = homeRot * Quaternion.Euler(0f, 0f, 45f);
            }
            else if (tool == ViewTool.Paddle)
            {
                rig.localPosition = home + new Vector3(Mathf.Cos(phase) * 0.03f, -0.03f, Mathf.Sin(phase) * 0.02f);
                rig.localRotation = homeRot * Quaternion.Euler(8f, Mathf.Sin(phase) * 8f, 0f);
            }
            else
            {
                rig.localPosition = home;
                rig.localRotation = homeRot * Quaternion.Euler(0f, 0f, -12f * phase);
            }
        }

        void Update()
        {
            if (vmActive)
            {
                vmTimer += Time.deltaTime;
                AnimateAction(vmDuration > 0.001f ? vmTimer / vmDuration : 1f);
                if (vmTimer >= vmDuration)
                {
                    vmActive = false;
                    animating = false;
                    if (bucketStream != null) bucketStream.gameObject.SetActive(false);
                    Hide(true);
                }
                return;
            }
            if (currentTool == ViewTool.None) return;
            Transform rig = RigFor(currentTool);
            if (rig == null || animating) return;

            if (stirMode && currentTool == ViewTool.Paddle)
            {
                float a = stirAngle;
                rig.localPosition = paddleHome + new Vector3(Mathf.Cos(a) * 0.045f, -0.025f + Mathf.Sin(a * 2f) * 0.01f, Mathf.Sin(a) * 0.03f);
                rig.localRotation = paddleHomeRot * Quaternion.Euler(Mathf.Sin(a) * 8f, Mathf.Cos(a) * 4f, 0f);
                return;
            }

            float t = Time.time + idleSeed;
            Vector3 sway = new Vector3(Mathf.Sin(t * 1.2f) * 0.0022f, Mathf.Sin(t * 1.55f) * 0.0018f, 0f);
            rig.localPosition = HomeFor(currentTool) + sway;
            rig.localRotation = HomeRotFor(currentTool) * Quaternion.Euler(Mathf.Sin(t * 0.8f) * 0.4f, 0f, Mathf.Sin(t * 1.1f) * 0.5f);
        }

        Transform RigFor(ViewTool tool)
        {
            switch (tool)
            {
                case ViewTool.PowderScoop: return powderRig;
                case ViewTool.WaterBucket: return bucketRig;
                case ViewTool.Paddle: return paddleRig;
                case ViewTool.Scraper: return scraperRig;
                case ViewTool.ClayScoop: return clayRig;
                case ViewTool.Hands: return handsRig;
                default: return null;
            }
        }

        Vector3 HomeFor(ViewTool tool)
        {
            switch (tool)
            {
                case ViewTool.WaterBucket: return bucketHome;
                case ViewTool.Paddle: return paddleHome;
                case ViewTool.Scraper: return scraperHome;
                case ViewTool.Hands: return handsHome;
                default: return scoopHome;
            }
        }

        Quaternion HomeRotFor(ViewTool tool)
        {
            switch (tool)
            {
                case ViewTool.WaterBucket: return bucketHomeRot;
                case ViewTool.Paddle: return paddleHomeRot;
                case ViewTool.Scraper: return scraperHomeRot;
                case ViewTool.Hands: return handsHomeRot;
                default: return scoopHomeRot;
            }
        }

        void HideAllRigs()
        {
            if (powderRig != null) powderRig.gameObject.SetActive(false);
            if (bucketRig != null) bucketRig.gameObject.SetActive(false);
            if (paddleRig != null) paddleRig.gameObject.SetActive(false);
            if (scraperRig != null) scraperRig.gameObject.SetActive(false);
            if (clayRig != null) clayRig.gameObject.SetActive(false);
            if (handsRig != null) handsRig.gameObject.SetActive(false);
        }
    }
}

