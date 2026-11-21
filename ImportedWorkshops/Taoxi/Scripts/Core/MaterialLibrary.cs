using UnityEngine;

namespace PorcelainFromClay
{
    /// <summary>All runtime materials. Textures are generated once and shared.</summary>
    public sealed class MaterialLibrary
    {
        public Material Plaster;
        public Material Stone;
        public Material StoneRim;
        public Material StoneWet;
        public Material Wood;
        public Material WoodWet;
        public Material Bamboo;
        public Material DryPowder;
        public Material RawStone;
        public Material WetClay;
        public Material DryClay;
        public Material Linen;
        public Material Paper;
        public Material Skin;
        public Material Basket;
        public Material Straw;
        public Material Water;
        public Material Stream;
        public Material Shadow;
        public Material WindowSky;
        public Material PostGrade;

        public Texture2D WaterNormal;
        public Texture2D PaperTexture;
        public Texture2D RingTexture;
        public Sprite PaperSprite;
        public Sprite RingSprite;

        public Shader SurfaceShader { get; private set; }
        public Shader WaterShader { get; private set; }
        public Shader StreamShader { get; private set; }
        public Shader ShadowShader { get; private set; }
        public Shader WindowShader { get; private set; }
        public Shader PostShader { get; private set; }

        public void Build()
        {
            float t0 = Time.realtimeSinceStartup;

            SurfaceShader = LoadShader("Shaders/WorkshopSurface", "PorcelainWorkshop/Surface");
            WaterShader = LoadShader("Shaders/SlurryWater", "PorcelainWorkshop/SlurryWater");
            StreamShader = LoadShader("Shaders/WaterStream", "PorcelainWorkshop/WaterStream");
            ShadowShader = LoadShader("Shaders/ShadowBlob", "PorcelainWorkshop/ShadowBlob");
            WindowShader = LoadShader("Shaders/WindowSky", "PorcelainWorkshop/WindowSky");
            PostShader = LoadShader("Shaders/PostGrade", "PorcelainWorkshop/PostGrade");

            var plaster = ProcTex.Plaster(512);
            var stone = ProcTex.Stone(512);
            var wood = ProcTex.Wood(512);
            var bamboo = ProcTex.Bamboo(256);
            var powder = ProcTex.DryPowder(256);
            var rawStone = ProcTex.RawStone(256);
            var wetClay = ProcTex.WetClay(256);
            var dryClay = ProcTex.DryClay(256);
            var linen = ProcTex.Linen(256);
            var paper = ProcTex.Paper(256);
            var skin = ProcTex.Skin(128);
            var basket = ProcTex.Basket(256);
            var straw = ProcTex.Straw(128);
            WaterNormal = ProcTex.WaterNormal(256);
            PaperTexture = paper.Albedo;
            RingTexture = ProcTex.Ring(128);

            Plaster = MakeSurface("Plaster", plaster, Hex(0xD9D7CF), 1f, 0.55f, 0f, 0.45f);
            Stone = MakeSurface("Stone", stone, Hex(0xADB0AB), 1f, 0.55f, 0.05f, 0.5f);
            StoneRim = MakeSurface("StoneRim", stone, Hex(0xB7B8B2), 1.05f, 0.5f, 0.04f, 0.45f);
            StoneWet = MakeSurface("StoneWet", stone, Hex(0x767169), 1.1f, 0.6f, 0.9f, 0.55f);
            Wood = MakeSurface("OldWood", wood, Hex(0xD8C8B2), 1f, 0.7f, 0.04f, 0.55f);
            WoodWet = MakeSurface("WetWood", wood, Hex(0xAC9B84), 1.1f, 0.7f, 0.6f, 0.55f);
            Bamboo = MakeSurface("Bamboo", bamboo, Hex(0xE0D1B3), 1f, 0.55f, 0.02f, 0.4f);
            DryPowder = MakeSurface("DryPowder", powder, Hex(0xE3DDCF), 1f, 0.9f, 0f, 0.35f);
            RawStone = MakeSurface("RawStone", rawStone, Hex(0xD5D0C4), 1f, 1f, 0.03f, 0.55f);
            WetClay = MakeSurface("WetClay", wetClay, Hex(0xB9B3A7), 1f, 0.9f, 0.7f, 0.45f);
            DryClay = MakeSurface("DryClay", dryClay, Hex(0xCFC8BA), 1f, 0.9f, 0f, 0.4f);
            Linen = MakeSurface("Linen", linen, Hex(0xB4AB9B), 1f, 0.55f, 0f, 0.35f);
            Paper = MakeSurface("Paper", paper, Hex(0xE9E2D3), 1f, 0.6f, 0f, 0.35f);
            Skin = MakeSurface("Skin", skin, Hex(0xA89484), 1f, 0.7f, 0.03f, 0.4f);
            Basket = MakeSurface("Basket", basket, Hex(0xC8AE84), 1f, 1f, 0f, 0.45f);
            Straw = MakeSurface("Straw", straw, Hex(0xC5B184), 1f, 0.8f, 0f, 0.35f);

            Water = new Material(WaterShader) { name = "SlurryWater" };
            Water.SetTexture("_NormalMap", WaterNormal);
            Water.SetColor("_ShallowColor", Hex(0x6B716B));
            Water.SetColor("_TurbidColor", Hex(0xBFB9AB));
            Water.SetFloat("_BaseAlpha", 0.22f);
            Water.SetFloat("_Smoothness", 0.18f);
            Water.SetFloat("_NormalScale", 0.34f);

            Stream = new Material(StreamShader) { name = "WaterStream" };
            Stream.SetColor("_Color", Hex(0xCFD4CC));

            Shadow = new Material(ShadowShader) { name = "ContactShadow" };
            Shadow.SetColor("_Color", Hex(0x1C1711));
            Shadow.SetFloat("_Strength", 0.32f);

            WindowSky = new Material(WindowShader) { name = "WindowSky" };
            WindowSky.SetColor("_TopColor", Hex(0xBFC6C5));
            WindowSky.SetColor("_BottomColor", Hex(0xF4E8D2));
            WindowSky.SetFloat("_Intensity", 1.12f);

            PostGrade = new Material(PostShader) { name = "PostGrade" };

            PaperSprite = Sprite.Create(PaperTexture, new Rect(0, 0, PaperTexture.width, PaperTexture.height), new Vector2(0.5f, 0.5f), 100f);
            RingSprite = Sprite.Create(RingTexture, new Rect(0, 0, RingTexture.width, RingTexture.height), new Vector2(0.5f, 0.5f), 100f);

            Debug.Log($"[瓷从泥生] textures/materials generated in {(Time.realtimeSinceStartup - t0):0.00}s");
        }

        public Material MakeSurface(string name, SurfaceTex tex, Color tint, float smoothScale, float normalScale, float wetness, float occlusion)
        {
            var m = new Material(SurfaceShader) { name = name };
            m.SetColor("_Color", tint);
            m.SetTexture("_MainTex", tex.Albedo);
            m.SetTexture("_NormalMap", tex.Normal);
            m.SetTexture("_SmoothnessMap", tex.Smoothness);
            m.SetTexture("_OcclusionMap", tex.Occlusion);
            m.SetFloat("_NormalScale", normalScale);
            m.SetFloat("_Smoothness", smoothScale);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_OcclusionStrength", occlusion);
            m.SetFloat("_Wetness", wetness);
            m.SetFloat("_WetSmoothness", 0.16f);
            m.SetColor("_WetDarken", new Color(0.62f, 0.61f, 0.58f, 1f));
            m.SetFloat("_DetailStrength", 0f);
            m.EnableKeyword("_NORMALMAP");
            return m;
        }

        public Material InstantiateWater(Color shallow, Color turbid, float alpha)
        {
            var m = new Material(Water) { name = "SlurryWater_Instance" };
            m.SetColor("_ShallowColor", shallow);
            m.SetColor("_TurbidColor", turbid);
            m.SetFloat("_BaseAlpha", alpha);
            return m;
        }

        public Material InstantiateShadow(float strength, float softness = 0.55f)
        {
            var m = new Material(Shadow) { name = "ContactShadow_Instance" };
            m.SetFloat("_Strength", strength);
            m.SetFloat("_Softness", softness);
            return m;
        }

        public Material InstantiateStream(float alpha = 0.65f)
        {
            var m = new Material(Stream) { name = "WaterStream_Instance" };
            m.SetFloat("_Alpha", alpha);
            return m;
        }

        static Shader LoadShader(string resourcePath, string fallbackName)
        {
            var s = Resources.Load<Shader>(resourcePath);
            if (s == null) s = Shader.Find(fallbackName);
            if (s == null) Debug.LogError("[瓷从泥生] Missing shader: " + fallbackName);
            return s;
        }

        public static Color Hex(int hex)
        {
            return new Color(
                ((hex >> 16) & 255) / 255f,
                ((hex >> 8) & 255) / 255f,
                (hex & 255) / 255f,
                1f);
        }
    }
}
