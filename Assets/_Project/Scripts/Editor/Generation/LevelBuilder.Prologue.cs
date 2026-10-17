using NasconGJ.Core;
using NasconGJ.Cutscenes;
using NasconGJ.Cutscenes.Scroll;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using A = NasconGJ.EditorTools.Generation.InkArtGenerator;

namespace NasconGJ.EditorTools.Generation
{
    public static partial class LevelBuilder
    {
        // =====================================================================
        //  C01 — Prologue as a painted story scroll (shadow puppets + ink-wash landscapes).
        //  Six panels laid out far apart along X; the ScrollDirector cuts between them with ink transitions.
        //  Camera looks down +Z; z = 0 is the puppet stage, larger z is further back (parallax for free).
        // =====================================================================

        /// <summary>Rebuild only the prologue (fast iteration on the cutscene).</summary>
        public static void BuildPrologueOnly()
        {
            prefabCache.Clear();
            BuildPrologue();
        }

        private static Mesh inkQuad;
        private static Texture2D inkPaper, inkNoise;

        private static void BuildPrologue()
        {
            inkQuad = GenUtil.SaveAsset(MeshBuilder.Quad("SM_InkQuad"), $"{GenUtil.PropMeshDir}/SM_InkQuad.asset");
            inkPaper = A.Load(A.Paper);
            inkNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.StoneGritPath);
            BuildInkSharedMats();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            var systems = Group("SYSTEMS");
            var stage = Group("SCROLL");
            var audioRoot = Group("AUDIO");
            var ui = Group("UI");

            // ---------------- camera, look, overlay
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(systems, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.02f, 0.03f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 30f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<ScrollLook>();
            var featureToggle = camGo.AddComponent<RendererFeatureToggle>();
            GenUtil.SetArray(featureToggle, "renderers", new Object[] {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>("Assets/Settings/PC_Renderer.asset"),
                AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>("Assets/Settings/Mobile_Renderer.asset") });
            var vol = new GameObject("Volume_Scroll").AddComponent<Volume>();
            vol.transform.SetParent(systems, false);
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MaterialLibrary.ScrollProfilePath);

            var overlayGo = new GameObject("InkOverlay");
            overlayGo.transform.SetParent(camGo.transform, false);
            overlayGo.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            overlayGo.transform.localScale = new Vector3(1f, 0.5f, 1f);
            overlayGo.AddComponent<MeshFilter>().sharedMesh = inkQuad;
            var overlayR = overlayGo.AddComponent<MeshRenderer>();
            var overlayMat = MaterialLibrary.LoadOrCreate("M_InkTransition", "NASCON/InkTransition");
            overlayMat.SetTexture("_NoiseTex", inkNoise);
            EditorUtility.SetDirty(overlayMat);
            overlayR.sharedMaterial = overlayMat;
            overlayR.shadowCastingMode = ShadowCastingMode.Off;
            var overlay = overlayGo.AddComponent<InkTransitionOverlay>();
            GenUtil.Set(overlay, "quad", overlayR);

            // the whole film printed on old rice paper: fibres, burned edges, warm light leaks, grain
            var paperGo = new GameObject("PaperGrade");
            paperGo.transform.SetParent(camGo.transform, false);
            paperGo.transform.localPosition = new Vector3(0f, 0f, 0.29f);
            paperGo.transform.localScale = new Vector3(1f, 0.5f, 1f);
            paperGo.AddComponent<MeshFilter>().sharedMesh = inkQuad;
            var paperR = paperGo.AddComponent<MeshRenderer>();
            var paperMat = MaterialLibrary.LoadOrCreate("M_PaperGrade", "NASCON/PaperGrade");
            paperMat.SetTexture("_PaperTex", inkPaper);
            paperMat.SetTexture("_NoiseTex", inkNoise);
            EditorUtility.SetDirty(paperMat);
            paperR.sharedMaterial = paperMat;
            paperR.shadowCastingMode = ShadowCastingMode.Off;
            paperR.sortingOrder = 32000;

            var canvas = PlaceAt(UIPrefabGenerator.CutscenePath, ui, Vector3.zero, Quaternion.identity);
            var cutUI = canvas.GetComponent<CutsceneUI>();
            var fader = canvas.GetComponentInChildren<NasconGJ.UI.ScreenFader>();
            if (fader != null) GenUtil.Set(fader, "fadeInOnStart", 0.01f);

            // ---------------- panels
            BuildTownPanel(stage, 0f);
            BuildFirePanel(stage, 200f);
            BuildLegendPanel(stage, 400f);
            BuildVoidPanel(stage, 600f);

            // ---------------- audio
            AudioSource Src(string name, bool loop)
            {
                var s = Sub(name, audioRoot).gameObject.AddComponent<AudioSource>();
                s.loop = loop;
                s.playOnAwake = false;
                s.volume = 0f;
                s.spatialBlend = 0f;
                return s;
            }
            AudioClip Cc0(string file) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{GenUtil.Root}/Audio/CC0/{file}");
            var music = Src("Music", true);
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/MUS_ScrollTheme.ogg");
            var dread = Src("Dread", true);
            dread.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/AMB_VoidWind.ogg");
            var sfx = Src("SFX", false);
            sfx.volume = 1f;

            // ---------------- director
            var dirGo = new GameObject("ScrollDirector");
            dirGo.transform.SetParent(stage, false);
            var d = dirGo.AddComponent<ScrollDirector>();
            GenUtil.Set(d, "cam", cam);
            GenUtil.Set(d, "ui", cutUI);
            GenUtil.Set(d, "ink", overlay);
            GenUtil.Set(d, "ambienceA", Src("AmbienceA", true));
            GenUtil.Set(d, "ambienceB", Src("AmbienceB", true));
            GenUtil.Set(d, "music", music);
            GenUtil.Set(d, "dread", dread);
            GenUtil.Set(d, "sfx", sfx);
            GenUtil.Set(d, "birds", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/AMB_Town.ogg"));
            GenUtil.Set(d, "fireplace", Cc0("AMB_Fireplace.wav"));
            GenUtil.Set(d, "wind", Cc0("AMB_Wind.ogg"));
            GenUtil.Set(d, "rainThunder", Cc0("AMB_RainThunder.ogg"));
            GenUtil.Set(d, "paperClip", Cc0("SFX_Paper_1.ogg"));
            GenUtil.Set(d, "pageFlip", Cc0("SFX_PageFlip.ogg"));
            GenUtil.Set(d, "swishLight", Cc0("SFX_Swish_Light.wav"));
            GenUtil.Set(d, "swishHeavy", Cc0("SFX_Swish_Heavy.wav"));
            GenUtil.Set(d, "gong", Cc0("SFX_Gong_1.ogg"));
            GenUtil.Set(d, "bell", Cc0("SFX_Bell.ogg"));
            GenUtil.Set(d, "laugh", Cc0("SFX_ChildrenLaugh.ogg"));
            GenUtil.Set(d, "boom", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_ThunderStrike.ogg"));
            GenUtil.Set(d, "heartbeat", Cc0("SFX_Heartbeat.wav"));
            GenUtil.Set(d, "step", Cc0("SFX_Step_3.ogg"));
            var voice = Src("Voice", false);
            voice.volume = 1f;
            GenUtil.Set(d, "voice", voice);
            var ducker = dirGo.AddComponent<VoiceDucker>();
            GenUtil.Set(ducker, "voice", voice);
            GenUtil.SetArray(d, "voiceLines", new Object[] { AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N01_TownInTheLight.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N02_TheyWorked.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N03_OldElder.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N04_TombBeyond.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N05_WhatLived.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N06_SpiritOfShadow.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N07_NoFace.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N08_Hungered.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N09_OneDay.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N10_ChildrenLaughed.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_N11_StoppedListening.ogg") });
            GenUtil.Set(d, "igniteClip", Cc0("SFX_FireCrackleShort.wav"));

            // stills of the old tale, flashed during the fall in C01b
            Shader.SetGlobalFloat("_NasconBrushReduce", 99f);
            Shader.SetGlobalFloat("_NasconPaletteReduce", 1f);
            ComicLook.Apply(0f, 0f, 0f);
            foreach (var (mark, fov, name) in new[] { ("Cam_TownWide", 32f, "Mem_Puppet_Town"), ("Cam_Market", 24f, "Mem_Puppet_Market"),
                         ("Cam_FireClose", 26f, "Mem_Puppet_Elder"), ("Cam_LegendTo", 22f, "Mem_Puppet_Legend"), ("Cam_VoidTo", 24f, "Mem_Puppet_Void") })
            {
                var m = GameObject.Find(mark);
                if (m != null) RenderStill(cam, m.transform, fov, name);
            }
            Shader.SetGlobalFloat("_NasconBrushReduce", 0f);
            Shader.SetGlobalFloat("_NasconPaletteReduce", 0f);
            camGo.transform.position = new Vector3(-3.2f, 6.2f, -13f);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scenes + "Cutscenes/" + SceneNames.Prologue + ".unity");
        }

        // =====================================================================
        //  panels
        // =====================================================================

        // palette: gold-leaf sky, teal-blue ink mountains, vermilion sun and seals, sumi-black puppets
        private static readonly Color Sumi = new(0.09f, 0.07f, 0.075f);
        private static readonly Color PaperWarm = new(0.95f, 0.88f, 0.7f);
        private static readonly Color Gold = new(0.96f, 0.74f, 0.32f);
        private static readonly Color Vermilion = new(0.8f, 0.2f, 0.12f);
        private static readonly Color PineTeal = new(0.12f, 0.48f, 0.52f);
        private static readonly Color WindowWarm = new(1f, 0.76f, 0.4f);
        private static readonly Color PineInk = new(0.22f, 0.24f, 0.24f);
        private static readonly Color TealWash = new(0.5f, 0.6f, 0.6f), TealInk = new(0.14f, 0.24f, 0.28f);
        private static readonly Color HillGold = new(0.86f, 0.66f, 0.34f), HillUmber = new(0.42f, 0.27f, 0.13f);

        private static readonly Vector3 WindRight = new(0f, 90f, 0f), Down = new(90f, 0f, 0f), Up = new(-90f, 0f, 0f);

        private static Transform Panel(Transform stage, string name, float x, Color top, Color bottom, float clouds, float darkness)
        {
            var root = Sub(name, stage);
            root.position = new Vector3(x, 0f, 0f);
            var pm = MaterialLibrary.LoadOrCreate($"M_Ink_{name}_Paper", "NASCON/InkPaper");
            pm.SetTexture("_PaperTex", inkPaper);
            pm.SetTexture("_NoiseTex", inkNoise);
            pm.SetColor("_TopColor", top);
            pm.SetColor("_BottomColor", bottom);
            pm.SetColor("_CloudColor", Color.Lerp(top, Gold, 0.35f));
            pm.SetFloat("_Clouds", clouds);
            pm.SetFloat("_Darkness", darkness);
            EditorUtility.SetDirty(pm);
            var paper = InkQuad(root, "Paper", pm, new Vector3(x, 10f, 90f), new Vector2(170f, 100f));
            var panel = root.gameObject.AddComponent<InkPanel>();
            GenUtil.Set(panel, "paper", paper);
            return root;
        }

        /// <summary>A vertical line of brush calligraphy with a red seal under it, like an inscription on a scroll painting.</summary>
        private static void Inscription(Transform p, float x, float y, float z, float height)
        {
            var t = Sub("Inscription", p);
            Layer(t, "Calligraphy", A.Calligraphy, x, y, z, height * 0.25f, Sumi, Sumi, Sumi, 0f, opacity: 0.85f);
            Layer(t, "Seal", A.Seal, x, y - height * 0.5f - height * 0.12f, z, height * 0.2f, Vermilion, Vermilion, Vermilion, 0f, opacity: 0.9f);
        }

        private static void BuildTownPanel(Transform stage, float x)
        {
            var p = Panel(stage, "Panel_Town", x, new Color(0.93f, 0.76f, 0.46f), new Color(0.98f, 0.92f, 0.78f), 0.4f, 0f);
            Layer(p, "Sun", A.Moon, x + 7f, 12.5f, 40f, 9f, new Color(0.9f, 0.32f, 0.16f), new Color(0.7f, 0.15f, 0.08f), new Color(0.92f, 0.3f, 0.14f), 0f);
            Layer(p, "NightMoon", A.Moon, x + 7f, 12.5f, 40.5f, 8f, new Color(0.92f, 0.9f, 0.82f), new Color(0.7f, 0.68f, 0.62f), new Color(0.95f, 0.93f, 0.85f), 0f);
            Layer(p, "CloudA", A.CloudScroll, x - 9f, 12f, 32f, 13f, PaperWarm, new Color(0.7f, 0.55f, 0.3f), Gold, 0f);
            Layer(p, "CloudB", A.CloudScroll, x + 13f, 16f, 34f, 10f, PaperWarm, new Color(0.7f, 0.55f, 0.3f), Gold, 0f);
            Layer(p, "MountainsFar", A.MountainsFar, x, 2f, 45f, 130f, TealWash, TealInk, Gold, 0.4f);
            Layer(p, "Karst", A.MountainsMid, x + 2f, 1f, 28f, 90f, new Color(0.46f, 0.56f, 0.5f), TealInk, new Color(0.45f, 0.62f, 0.3f), 0.15f);
            Layer(p, "Mist", A.MistBand, x, 2f, 22f, 100f, PaperWarm, PaperWarm, PaperWarm, 0f, opacity: 0.9f);
            Layer(p, "Hills", A.Hills, x, -3f, 16f, 70f, HillGold, HillUmber, Gold, 0.1f);
            Layer(p, "Torii", A.Torii, x - 8f, 2.2f, 12f, 5.5f, new Color(0.5f, 0.2f, 0.15f), new Color(0.25f, 0.08f, 0.06f), Vermilion, 0.05f);
            Layer(p, "HousesBack", A.HouseRowBack, x + 1f, 2.4f, 9f, 24f, new Color(0.66f, 0.6f, 0.52f), new Color(0.2f, 0.18f, 0.2f), WindowWarm, 0.15f);
            Layer(p, "Houses", A.HouseRow, x, 2.85f, 4f, 22f, new Color(0.8f, 0.7f, 0.56f), Sumi, WindowWarm, 0f);
            Layer(p, "Street", A.Ground, x, -2.6f, 2.5f, 44f, new Color(0.78f, 0.62f, 0.4f), new Color(0.35f, 0.24f, 0.14f), Gold, 0f);
            Layer(p, "Well", A.Well, x - 5.2f, 1.1f, 1.8f, 2.3f, new Color(0.65f, 0.58f, 0.5f), Sumi, Gold, 0f);
            Layer(p, "Stall", A.Stall, x + 3.6f, 1.3f, 1.5f, 4f, new Color(0.85f, 0.75f, 0.6f), Sumi, new Color(0.9f, 0.35f, 0.15f), 0f);
            Layer(p, "Oven", A.Oven, x + 8.3f, 0.85f, 1.6f, 1.7f, new Color(0.7f, 0.55f, 0.45f), Sumi, new Color(1f, 0.5f, 0.15f), 0f);
            foreach (float lx in new[] { -6f, -2f, 2f, 6f })
                Layer(p, "Lantern_" + lx, A.Lantern, x + lx, 4.9f + Mathf.Abs(lx) * 0.06f, 2.5f, 0.55f, new Color(0.9f, 0.6f, 0.4f), Sumi, Vermilion, 0f);
            var pineL = Layer(p, "PineLeft", A.Pine, x - 11.5f, 5.6f, -4f, 10f, PineInk, Sumi, PineTeal, 0f);
            pineL.transform.localScale = Vector3.Scale(pineL.transform.localScale, new Vector3(-1f, 1f, 1f));
            Layer(p, "PineRight", A.Pine, x + 12.5f, 6.2f, -3f, 8f, PineInk, Sumi, PineTeal, 0f);
            Layer(p, "Grass", A.Grass, x, -2.5f, -6f, 36f, new Color(0.78f, 0.62f, 0.36f), new Color(0.35f, 0.24f, 0.13f), Gold, 0f, wobble: 0.012f);
            Inscription(p, x + 8.6f, 7.4f, -1f, 3.2f);

            // the opening lantern, hanging in the foreground
            var hero = Sub("HeroLantern", p);
            hero.position = new Vector3(x - 6.4f, 6.6f, -8f);
            Glow(hero, "Glow", new Vector3(x - 6.4f, 6.5f, -8.05f), 2.2f);
            Layer(hero, "Lantern", A.Lantern, x - 6.4f, 6.6f, -8f, 1.2f, new Color(0.95f, 0.7f, 0.45f), Sumi, Vermilion, 0f);

            // villagers (soft shadows thrown onto the house walls by the low sun)
            var sun = Point("SunLight", p, new Vector3(x - 8f, 16f, -10f));
            Puppet(p, "Vendor_1", "Vendor", new Vector3(x + 4.3f, 0f, 0f), -1f, 2.2f, sun, 3.9f, 1.06f, 0.28f);
            Puppet(p, "Baker_1", "Man", new Vector3(x + 7.3f, 0f, 0f), 1f, 2.2f, sun, 3.9f, 1.06f, 0.28f);
            Puppet(p, "Mother_1", "Woman", new Vector3(x - 1.4f, 0f, 0f), 1f, 2.2f, sun, 3.9f, 1.06f, 0.28f);
            Puppet(p, "Runner_1", "Child", new Vector3(x - 0.2f, 0f, -0.2f), 1f, 1.6f, sun, 3.9f, 1.06f, 0.28f);
            Puppet(p, "Runner_2", "Child", new Vector3(x - 3.4f, 0f, -0.3f), -1f, 1.5f, sun, 3.9f, 1.06f, 0.28f);
            Puppet(p, "Walker_1", "Man", new Vector3(x - 8f, 0f, 0.2f), 1f, 2.2f, sun, 3.9f, 1.06f, 0.28f);
            Puppet(p, "Carrier_1", "Woman", new Vector3(x + 10f, 0f, 0.1f), -1f, 2.2f, sun, 3.9f, 1.06f, 0.28f);
            Puppet(p, "Bearer", "Bearer", new Vector3(x - 12f, 0f, -0.4f), 1f, 2.2f, null, 0f, 1f, 0f, torch: true);

            // the Void passing over: a dark wash dragged across the sky
            var cloud = Sub("VoidCloud", p);
            cloud.position = new Vector3(x - 45f, 8f, 6f);
            var wash = Layer(cloud, "Wash", A.MistBand, x - 45f, 8f, 6f, 70f, Sumi, Sumi, Sumi, 0f, opacity: 0f);
            wash.transform.localScale = new Vector3(70f, 22f, 1f);
            Point("CloudFrom", p, new Vector3(x - 45f, 8f, 6f));
            Point("CloudTo", p, new Vector3(x + 45f, 8f, 6f));

            Particles(p, "Leaves", new Vector3(x - 14f, 8f, 1f), new Vector3(6f, 6f, 1f), WindRight, MatLeaf, 4f, true);
            Rays(p, "SunRays", new Vector3(x + 5f, 7.5f, 3f), new Vector2(18f, 16f), -18f, new Color(1f, 0.72f, 0.4f), 0.45f);
            Motes(p, new Vector3(x, 3f, -2f), new Vector3(22f, 6f, 6f), new Color(1f, 0.75f, 0.45f, 0.8f));

            Mark("Cam_Lantern", p, new Vector3(x - 6.4f, 6.4f, -13f), new Vector3(x - 6.4f, 6.4f, 0f));
            Mark("Cam_TownWide", p, new Vector3(x, 4.4f, -20f), new Vector3(x, 4.4f, 0f));
            Mark("Cam_Market", p, new Vector3(x + 2.6f, 2.3f, -11f), new Vector3(x + 2.6f, 2.3f, 0f));
            Mark("Cam_Street", p, new Vector3(x - 2.5f, 3.2f, -15f), new Vector3(x - 2.5f, 3.2f, 0f));
            Mark("Cam_Bearer", p, new Vector3(x - 5f, 2.2f, -10f), new Vector3(x - 5f, 2.2f, 0f));
            Point("BearerStart", p, new Vector3(x - 12f, 0f, -0.4f));
            Point("BearerStop", p, new Vector3(x - 5.6f, 0f, -0.4f));
        }

        private static void BuildFirePanel(Transform stage, float x)
        {
            var p = Panel(stage, "Panel_Fire", x, new Color(0.35f, 0.3f, 0.38f), new Color(0.5f, 0.42f, 0.38f), 0.3f, 0f);
            Layer(p, "Wall", A.Wall, x, 5.5f, 3f, 26f, new Color(0.7f, 0.56f, 0.4f), new Color(0.22f, 0.14f, 0.1f), Gold, 0f);
            Layer(p, "Ground", A.Ground, x, -1.7f, 1.5f, 40f, new Color(0.45f, 0.33f, 0.22f), new Color(0.12f, 0.08f, 0.06f), Gold, 0f);

            Inscription(p, x - 6.5f, 6.2f, 2.9f, 3.4f);
            Layer(p, "WallLantern", A.Lantern, x + 6f, 5.2f, 2.9f, 0.7f, new Color(0.9f, 0.6f, 0.4f), Sumi, Vermilion, 0f);

            var fire = Sub("Fire", p);
            fire.position = new Vector3(x - 1.5f, 0f, 0.5f);
            var logs = Layer(fire, "Logs", A.Logs, x - 1.5f, 0.22f, 0.45f, 1.6f, new Color(0.4f, 0.25f, 0.15f), Sumi, new Color(1f, 0.45f, 0.1f), 0f);
            logs.sharedMaterial.SetFloat("_AccentGlow", 1.5f);
            InkQuad(fire, "Flame", MaterialLibrary.Get(MatFire), new Vector3(x - 1.5f, 1.05f, 0.4f), new Vector2(1f, 2f));
            Glow(fire, "Glow", new Vector3(x - 1.5f, 1f, 0.3f), 9f);
            var light = Point("FireLight", fire, new Vector3(x - 1.5f, 0.9f, 0.5f));
            var embers = Particles(fire, "Embers", new Vector3(x - 1.5f, 1.2f, 0.4f), new Vector3(0.4f, 0.2f, 0.1f), Up, MatEmber, 8f, true, gravity: -0.05f);
            var em = embers.main;
            em.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2.2f);
            em.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            em.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);

            Puppet(p, "Elder", "Elder", new Vector3(x - 3.3f, 0f, 0f), 1f, 2.3f, light, 2.9f, 1.7f, 0.85f).Do(InkPuppet.Act.Story);
            Puppet(p, "Child_1", "Child", new Vector3(x + 0.1f, 0f, 0f), -1f, 1.7f, light, 2.9f, 1.7f, 0.85f).Do(InkPuppet.Act.Listen);
            Puppet(p, "Child_2", "Child", new Vector3(x + 1.0f, 0f, 0.1f), -1f, 1.55f, light, 2.9f, 1.7f, 0.85f).Do(InkPuppet.Act.Listen);
            Puppet(p, "Child_3", "Child", new Vector3(x + 1.9f, 0f, -0.1f), -1f, 1.8f, light, 2.9f, 1.7f, 0.85f).Do(InkPuppet.Act.Listen);
            HideInEditor(Puppet(p, "Man", "Man", new Vector3(x - 10f, 0f, 1.2f), 1f, 2.3f, null, 0f, 1f, 0f));
            HideInEditor(Puppet(p, "Woman", "Woman", new Vector3(x - 11f, 0f, 1.25f), 1f, 2.3f, null, 0f, 1f, 0f));

            // the Void's shadow on the wall (hidden until the story conjures it). Body 7.2 x 10.8, arm pinned at its front shoulder.
            var vs = Sub("VoidShadow", p);
            vs.position = new Vector3(x + 0.5f, -0.3f, 2.95f);
            Layer(vs, "Body", A.VoidBody, x + 0.5f, -0.3f + 5.4f, 2.95f, 7.2f, Sumi, Sumi, Sumi, 0f, opacity: 0f);
            var armPivot = Sub("Arm", vs);
            armPivot.position = new Vector3(x + 0.5f + 1.45f, -0.3f + 7.1f, 2.94f);
            Layer(armPivot, "ArmQuad", A.VoidArm, x + 0.5f + 1.45f, -0.3f + 7.1f - 2.1f, 2.94f, 1.12f, Sumi, Sumi, Sumi, 0f, opacity: 0f);
            armPivot.rotation = Quaternion.Euler(0f, 0f, 14f); // hangs just off the cloak, not inside it

            Particles(p, "Leaves", new Vector3(x - 10f, 8f, 1f), new Vector3(3f, 4f, 1f), WindRight, MatLeaf, 8f, false);
            Particles(p, "Snow", new Vector3(x, 12f, 1f), new Vector3(26f, 4f, 1f), Down, MatSnow, 30f, false, gravity: 0.03f);
            Rays(p, "FireRays", new Vector3(x - 1.5f, 1.5f, 2.6f), new Vector2(9f, 9f), 180f, new Color(1f, 0.5f, 0.2f), 0.55f); // firelight thrown up the wall
            Motes(p, new Vector3(x - 1.5f, 2.5f, 0f), new Vector3(8f, 4f, 3f), new Color(1f, 0.55f, 0.25f, 0.9f));

            Mark("Cam_FireWide", p, new Vector3(x, 3.6f, -18f), new Vector3(x, 3.6f, 0f));
            Mark("Cam_FireClose", p, new Vector3(x - 1.2f, 2.2f, -10f), new Vector3(x - 1.2f, 2.2f, 0f));
            Mark("Cam_Wall", p, new Vector3(x + 1.5f, 5f, -14f), new Vector3(x + 1.5f, 5f, 0f));
            Mark("Cam_WallClose", p, new Vector3(x + 2f, 5.8f, -9f), new Vector3(x + 2f, 5.8f, 0f));
        }

        private static void BuildLegendPanel(Transform stage, float x)
        {
            var p = Panel(stage, "Panel_Legend", x, new Color(0.92f, 0.76f, 0.46f), new Color(0.98f, 0.93f, 0.8f), 0.55f, 0f);
            Layer(p, "Moon", A.Moon, x + 13f, 14f, 50f, 10f, new Color(0.98f, 0.9f, 0.68f), new Color(0.88f, 0.68f, 0.4f), new Color(1f, 0.92f, 0.7f), 0f);
            Layer(p, "CloudA", A.CloudScroll, x - 7f, 12.5f, 32f, 13f, PaperWarm, new Color(0.7f, 0.55f, 0.3f), Gold, 0f);
            Layer(p, "CloudB", A.CloudScroll, x + 17f, 8.5f, 30f, 9f, PaperWarm, new Color(0.7f, 0.55f, 0.3f), Gold, 0f);
            Layer(p, "MountainsFar", A.MountainsFar, x, 2f, 45f, 130f, TealWash, TealInk, Gold, 0.35f);
            Layer(p, "Karst", A.MountainsMid, x - 4f, 1.5f, 28f, 90f, new Color(0.44f, 0.56f, 0.5f), TealInk, new Color(0.4f, 0.6f, 0.3f), 0.12f);
            Layer(p, "Mist", A.MistBand, x, 1.5f, 20f, 100f, PaperWarm, PaperWarm, PaperWarm, 0f, opacity: 0.9f);
            Layer(p, "Tomb", A.Tomb, x + 7.5f, 3.4f, 13f, 6.5f, new Color(0.55f, 0.5f, 0.45f), Sumi, Gold, 0.1f);
            Layer(p, "HillsFar", A.Hills, x + 6f, 0f, 14f, 70f, HillGold, HillUmber, Gold, 0.1f);
            Layer(p, "HillsNear", A.Ground, x - 3f, -1.7f, 6f, 60f, new Color(0.7f, 0.52f, 0.28f), HillUmber, Gold, 0f);
            Layer(p, "Pine", A.Pine, x - 13.5f, 5.5f, -3f, 9f, PineInk, Sumi, PineTeal, 0f);
            Layer(p, "Grass", A.Grass, x + 2f, -1.9f, -5f, 36f, new Color(0.78f, 0.62f, 0.36f), new Color(0.35f, 0.24f, 0.13f), Gold, 0f, wobble: 0.02f);
            Inscription(p, x + 7.5f, 8.2f, 2f, 3f);
            HideInEditor(Puppet(p, "Bearer", "Bearer", new Vector3(x - 3f, 0.9f, 5.5f), 1f, 1.5f, null, 0f, 1f, 0f, torch: true));
            Particles(p, "Leaves", new Vector3(x - 14f, 8f, 2f), new Vector3(6f, 6f, 1f), WindRight, MatLeaf, 5f, true);
            Rays(p, "MoonRays", new Vector3(x + 13f, 12f, 40f), new Vector2(40f, 30f), -25f, new Color(1f, 0.85f, 0.6f), 0.3f);
            Motes(p, new Vector3(x, 3f, -1f), new Vector3(24f, 6f, 6f), new Color(1f, 0.85f, 0.55f, 0.7f));
            Mark("Cam_LegendFrom", p, new Vector3(x - 5f, 6.5f, -22f), new Vector3(x - 5f, 6.5f, 0f));
            Mark("Cam_LegendTo", p, new Vector3(x + 2f, 5f, -20f), new Vector3(x + 2f, 5f, 0f));
            Mark("Cam_TombFar", p, new Vector3(x + 5f, 3.5f, -10f), new Vector3(x + 5f, 3.5f, 0f));
            Point("WalkerEnd", p, new Vector3(x + 4.5f, 0.9f, 5.5f));
        }

        private static void BuildVoidPanel(Transform stage, float x)
        {
            var p = Panel(stage, "Panel_Void", x, new Color(0.22f, 0.2f, 0.26f), new Color(0.12f, 0.1f, 0.13f), 0.5f, 0.55f);
            var shaft = Layer(p, "Shaft", A.MistBand, x + 0.4f, 8f, 14f, 30f, new Color(0.65f, 0.7f, 0.8f), new Color(0.65f, 0.7f, 0.8f), Color.white, 0f, opacity: 0.5f);
            shaft.transform.rotation = Quaternion.Euler(0f, 0f, 92f);
            var halo = Layer(p, "Halo", A.Moon, x + 0.6f, 12.5f, 14f, 16f, new Color(0.62f, 0.6f, 0.66f), new Color(0.45f, 0.43f, 0.5f), new Color(0.75f, 0.74f, 0.8f), 0f, opacity: 0.9f);
            Layer(p, "CloudDark", A.CloudScroll, x - 9f, 13f, 20f, 12f, new Color(0.2f, 0.19f, 0.24f), new Color(0.08f, 0.07f, 0.1f), new Color(0.45f, 0.42f, 0.5f), 0f, opacity: 0.8f);
            Layer(p, "Void", A.VoidBody, x, 8f, 8f, 12f, new Color(0.03f, 0.02f, 0.03f), new Color(0.01f, 0.005f, 0.01f), Color.black, 0f, opacity: 0.97f);
            // long arms hanging from the shoulders, the far one behind the cloak
            foreach (var (n, ax, ay, az, rot) in new[] { ("ArmBack", x - 2.3f, 10.9f, 8.05f, -12f), ("ArmFront", x + 2.4f, 10.9f, 7.95f, 22f) })
            {
                var pin = Sub(n, p);
                pin.position = new Vector3(ax, ay, az);
                Layer(pin, "Quad", A.VoidArm, ax, ay - 4.2f, az, 2.25f, new Color(0.03f, 0.02f, 0.03f), new Color(0.01f, 0.005f, 0.01f), Color.black, 0f, opacity: 0.97f);
                pin.rotation = Quaternion.Euler(0f, 0f, rot);
            }
            Layer(p, "Floor", A.Ground, x, -3.2f, 6f, 60f, new Color(0.12f, 0.1f, 0.13f), new Color(0.02f, 0.015f, 0.02f), Color.black, 0f);
            Particles(p, "Ash", new Vector3(x, 16f, 4f), new Vector3(22f, 6f, 1f), Down, MatSnow, 14f, true, gravity: 0.01f);
            Rays(p, "VoidShaft", new Vector3(x + 0.5f, 20f, 13f), new Vector2(14f, 28f), 0f, new Color(0.7f, 0.75f, 0.9f), 0.35f);
            Mark("Cam_VoidFrom", p, new Vector3(x, 4f, -24f), new Vector3(x, 4f, 0f));
            Mark("Cam_VoidTo", p, new Vector3(x + 1f, 11.5f, -19f), new Vector3(x + 1f, 11.5f, 0f));
        }

        private static void BuildTombPanel(Transform stage, float x)
        {
            var p = Panel(stage, "Panel_Tomb", x, new Color(0.36f, 0.37f, 0.45f), new Color(0.52f, 0.5f, 0.54f), 0.9f, 0f);
            Layer(p, "CloudStorm", A.CloudScroll, x + 7f, 13f, 26f, 14f, new Color(0.5f, 0.5f, 0.56f), new Color(0.2f, 0.2f, 0.26f), new Color(0.75f, 0.75f, 0.85f), 0f);
            Layer(p, "Karst", A.MountainsMid, x, 0f, 30f, 90f, new Color(0.3f, 0.32f, 0.38f), new Color(0.08f, 0.09f, 0.12f), new Color(0.3f, 0.36f, 0.32f), 0f);
            Layer(p, "Tomb", A.Tomb, x, 5.0f, 8f, 19f, new Color(0.55f, 0.5f, 0.46f), Sumi, Gold, 0f);
            Layer(p, "FigureRow", A.FigureRow, x, 1.7f, 7.9f, 16f, Sumi, Sumi, Sumi, 0f, opacity: 0f);
            Layer(p, "Ground", A.Ground, x, -2.1f, 7f, 50f, new Color(0.45f, 0.42f, 0.38f), new Color(0.1f, 0.09f, 0.08f), Gold, 0f);
            Puppet(p, "Bearer", "Bearer", new Vector3(x - 6f, 0f, 6.5f), 1f, 2.2f, null, 0f, 1f, 0f, torch: true);
            var rain = Particles(p, "Rain", new Vector3(x, 14f, 2f), new Vector3(36f, 8f, 1f), new Vector3(95f, 0f, 0f), MatRain, 400f, true, gravity: 1.5f);
            var rm = rain.main;
            rm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.6f);
            rm.startSpeed = new ParticleSystem.MinMaxCurve(8f, 11f);
            rm.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            rm.maxParticles = 1500;
            var rn = rain.noise;
            rn.enabled = false;
            var rr = rain.GetComponent<ParticleSystemRenderer>();
            rr.renderMode = ParticleSystemRenderMode.Stretch;
            rr.lengthScale = 6f;
            Mark("Cam_TombFrom", p, new Vector3(x - 4f, 3.5f, -20f), new Vector3(x - 4f, 3.5f, 0f));
            Mark("Cam_TombTo", p, new Vector3(x - 0.5f, 4f, -11f), new Vector3(x - 0.5f, 4f, 0f));
            Point("Threshold", p, new Vector3(x - 1f, 0f, 6.5f));
            Point("Inside", p, new Vector3(x + 0.1f, 0f, 6.5f));
        }

        private static void BuildFallPanel(Transform stage, float x)
        {
            var p = Panel(stage, "Panel_Fall", x, new Color(0.05f, 0.04f, 0.05f), new Color(0.02f, 0.015f, 0.02f), 0.2f, 0.8f);
            Layer(p, "Opening", A.Moon, x, 11f, 12f, 6f, new Color(0.9f, 0.85f, 0.75f), new Color(0.75f, 0.7f, 0.6f), Color.white, 0f);
            var beam = Layer(p, "Beam", A.MistBand, x, 4f, 10f, 26f, new Color(0.5f, 0.48f, 0.45f), new Color(0.5f, 0.48f, 0.45f), Color.white, 0f, opacity: 0.35f);
            beam.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Puppet(p, "Bearer", "Bearer", new Vector3(x, 6f, 0f), 1f, 2.2f, null, 0f, 1f, 0f, torch: true);
            Mark("Cam_Fall", p, new Vector3(x, 3f, -16f), new Vector3(x, 3f, 0f));
        }

        // =====================================================================
        //  helpers
        // =====================================================================

        /// <summary>Layers whose accents are real lights (windows, lanterns, ovens): they glow at night and die when the lamps go out.</summary>
        private static readonly string[] LampLayers = { "Houses", "Lantern", "WallLantern", "Stall", "Oven", "Logs" };

        private const string MatTorchGlow = "M_InkGlowTorch";
        private const string MatGlow = "M_InkGlow", MatLeaf = "M_InkLeaf", MatSnow = "M_InkSnow", MatEmber = "M_InkEmber", MatRain = "M_InkRain", MatFire = "M_InkFire";

        private static void BuildInkSharedMats()
        {
            var soft = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.SoftParticlePath);
            var glow = MaterialLibrary.LoadOrCreate(MatGlow, "NASCON/InkGlow");
            glow.SetColor("_Color", new Color(1f, 0.55f, 0.25f, 1f));
            glow.SetFloat("_Intensity", 0.55f);
            glow.SetFloat("_Flicker", 0.35f);
            EditorUtility.SetDirty(glow);
            var torchGlow = MaterialLibrary.LoadOrCreate(MatTorchGlow, "NASCON/InkGlow");
            torchGlow.SetColor("_Color", new Color(1f, 0.55f, 0.25f, 1f));
            torchGlow.SetFloat("_Intensity", 0.32f);
            torchGlow.SetFloat("_Flicker", 0.6f);
            EditorUtility.SetDirty(torchGlow);
            MaterialLibrary.Particle(MatLeaf, A.Load(A.Leaf), new Color(0.85f, 0.3f, 0.12f, 1f), additive: false);
            MaterialLibrary.Particle(MatSnow, soft, new Color(0.95f, 0.95f, 1f, 0.8f), additive: false);
            MaterialLibrary.Particle(MatEmber, soft, new Color(1f, 0.55f, 0.2f, 1f), additive: true);
            MaterialLibrary.Particle(MatRain, soft, new Color(0.8f, 0.85f, 1f, 0.45f), additive: false);
            var fire = MaterialLibrary.LoadOrCreate(MatFire, "NASCON/InkFire");
            fire.SetTexture("_MainTex", A.Load(A.Fire));
            fire.SetTexture("_NoiseTex", inkNoise);
            EditorUtility.SetDirty(fire);
        }

        /// <summary>A fan of soft additive light shafts hanging from a source point (quad top edge), tilted by <paramref name="angle"/>.</summary>
        private static void Rays(Transform parent, string name, Vector3 source, Vector2 size, float angle, Color color, float intensity)
        {
            var m = MaterialLibrary.LoadOrCreate($"M_Rays_{parent.name}_{name}", "NASCON/InkRays");
            m.SetColor("_Color", color);
            m.SetFloat("_Intensity", intensity);
            EditorUtility.SetDirty(m);
            var r = InkQuad(parent, name, m, source, size);
            r.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            r.transform.position = source - r.transform.up * size.y * 0.5f; // pivot at the source
        }

        /// <summary>Slow glowing dust drifting through the light, in front of and behind the puppets.</summary>
        private static void Motes(Transform parent, Vector3 pos, Vector3 box, Color color)
        {
            var name = "M_InkMote";
            var soft = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.SoftParticlePath);
            MaterialLibrary.Particle(name, soft, color, additive: true);
            var ps = Particles(parent, "Motes", pos, box, new Vector3(-90f, 0f, 0f), name, 6f, true, gravity: -0.004f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
        }

        private static MeshRenderer InkQuad(Transform parent, string name, Material mat, Vector3 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = inkQuad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = InkPuppet.OrderFor(pos.z);
            return r;
        }

        private static Material InkMat(string key, string tex, Color light, Color dark, Color accent, float atmos, float opacity, float wobble, float z)
        {
            var m = MaterialLibrary.LoadOrCreate($"M_Ink_{key}", "NASCON/InkLayer");
            m.SetTexture("_MainTex", A.Load(tex));
            m.SetTexture("_PaperTex", inkPaper);
            m.SetTexture("_NoiseTex", inkNoise);
            m.SetColor("_LightColor", light);
            m.SetColor("_DarkColor", dark);
            m.SetColor("_AccentColor", accent);
            m.SetColor("_AtmosColor", PaperWarm);
            m.SetFloat("_Atmos", atmos);
            m.SetFloat("_Opacity", opacity);
            m.SetFloat("_Wobble", wobble);
            m.SetFloat("_AccentGlow", 0f);
            m.SetFloat("_Reveal", 1f);
            m.SetFloat("_Silhouette", 0f);
            m.renderQueue = 3000; // ordering is per renderer (sortingOrder by depth)
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>An ink layer: width given, height from the texture's aspect, centred at (x, y, z).</summary>
        private static MeshRenderer Layer(Transform parent, string name, string tex, float x, float y, float z, float width,
            Color light, Color dark, Color accent, float atmos, float opacity = 1f, float wobble = 0f)
        {
            var t = A.Load(tex);
            float h = width * t.height / t.width;
            var panel = parent.GetComponentInParent<InkPanel>();
            string key = (panel != null && panel.transform != parent ? panel.name + "_" : "") + parent.name + "_" + name;
            var mat = InkMat(key, tex, light, dark, accent, atmos, opacity, wobble, z);
            bool lamp = System.Array.Exists(LampLayers, n => name.StartsWith(n));
            mat.SetFloat("_Lamp", lamp ? 1f : 0f);
            return InkQuad(parent, name, mat, new Vector3(x, y, z), new Vector2(width, h));
        }

        private static InkPuppet Puppet(Transform panel, string name, string kind, Vector3 feet, float facing, float height,
            Transform light, float wallZ, float shadowScale, float shadowOpacity, bool torch = false)
        {
            var root = Sub(name, panel);
            root.position = feet;
            var pivot = Sub("Body", root);
            pivot.localPosition = Vector3.zero;

            // translucent dyed leather, lit from behind: a coloured body, gold where light passes the filigree
            Color tint = kind switch
            {
                "Woman" => new Color(0.66f, 0.2f, 0.13f),
                "Child" => new Color(0.78f, 0.52f, 0.18f),
                "Elder" => new Color(0.27f, 0.32f, 0.46f),
                "Vendor" => new Color(0.16f, 0.44f, 0.46f),
                "Bearer" => new Color(0.26f, 0.28f, 0.34f),
                _ => new Color(0.58f, 0.32f, 0.15f),
            };
            Material Mat(string part, string tex, float shade, float zBias)
            {
                var m = InkMat($"Puppet_{panel.name}_{name}_{part}", tex, tint * shade, Sumi, Gold, 0f, 1f, 0f, feet.z - zBias);
                m.SetFloat("_EdgeDarken", 0.8f);
                m.SetFloat("_AccentGlow", 0.5f);
                return m;
            }

            var body = InkQuad(pivot, "BodyQuad", Mat("Body", A.PuppetBody(kind), 1f, 0f), Vector3.zero, new Vector2(height * 0.5f, height));
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            float armH = kind == "Child" ? height * 0.33f : height * 0.42f;
            float shoulder = A.ShoulderY(kind) * height;
            MeshRenderer Arm(Transform pin, string part, float shade, float zBias)
            {
                var r = InkQuad(pin, "Quad", Mat(part, A.PuppetArm(kind), shade, zBias), Vector3.zero, new Vector2(armH * 0.25f, armH));
                r.transform.localPosition = new Vector3(0f, -armH * 0.47f, 0f);
                return r;
            }
            var front = Sub("ArmFront", pivot);
            front.localPosition = new Vector3(height * 0.02f, shoulder, -0.01f);
            var back = Sub("ArmBack", pivot);
            back.localPosition = new Vector3(-height * 0.02f, shoulder, 0.01f);
            var fr = Arm(front, "ArmFront", 1.18f, 0.3f);
            var br = Arm(back, "ArmBack", 0.7f, -0.3f);

            Transform prop = null;
            if (torch)
            {
                // held at the hand, kept upright by InkPuppet; the hand grips a third of the way up the shaft
                prop = Sub("Torch", front);
                prop.localPosition = new Vector3(0f, -armH * 0.9f, -0.02f);
                var handleMat = Mat("Handle", A.Wall, 1f, 0.4f);
                handleMat.SetColor("_LightColor", new Color(0.4f, 0.25f, 0.13f));
                handleMat.SetFloat("_AccentGlow", 0f);
                var handle = InkQuad(prop, "Handle", handleMat, Vector3.zero, new Vector2(0.08f, 0.6f));
                handle.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                var flame = InkQuad(prop, "Flame", MaterialLibrary.Get(MatFire), Vector3.zero, new Vector2(0.32f, 0.62f));
                flame.transform.localPosition = new Vector3(0f, 0.62f, -0.01f);
                var tg = Glow(prop, "Glow", Vector3.zero, 2.6f);
                tg.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(MatTorchGlow);
                tg.localPosition = new Vector3(0f, 0.55f, 0.05f);
            }

            int order = InkPuppet.OrderFor(feet.z);
            br.sortingOrder = order - 1;
            body.sortingOrder = order;
            fr.sortingOrder = order + 1;
            if (prop != null) foreach (var pr in prop.GetComponentsInChildren<Renderer>()) pr.sortingOrder = order + (pr.name == "Handle" ? 2 : pr.name == "Flame" ? 3 : -3); // the halo sits behind the bearer

            var pup = root.gameObject.AddComponent<InkPuppet>();
            pup.Configure(pivot, body, front, fr, back, br, height, prop);
            GenUtil.Set(pup, "facing", facing);
            GenUtil.Set(pup, "phase", Mathf.Repeat(feet.x * 0.37f, 1f));
            if (light != null)
            {
                GenUtil.Set(pup, "castShadow", true);
                GenUtil.Set(pup, "lightSource", light);
                GenUtil.Set(pup, "wallZ", wallZ);
                GenUtil.Set(pup, "shadowScale", shadowScale);
                GenUtil.Set(pup, "shadowOpacity", shadowOpacity);
            }
            return pup;
        }

        /// <summary>Start a puppet invisible (its materials are per-puppet assets, so this only touches it).</summary>
        private static void HideInEditor(InkPuppet p)
        {
            foreach (var r in p.GetComponentsInChildren<MeshRenderer>())
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Opacity") && r.sharedMaterial.name.StartsWith("M_Ink_Puppet"))
                {
                    r.sharedMaterial.SetFloat("_Opacity", 0f);
                    EditorUtility.SetDirty(r.sharedMaterial);
                }
        }

        private static Transform Glow(Transform parent, string name, Vector3 pos, float size) =>
            InkQuad(parent, name, MaterialLibrary.Get(MatGlow), pos, new Vector2(size, size)).transform;

        private static ParticleSystem Particles(Transform parent, string name, Vector3 pos, Vector3 box, Vector3 euler, string mat, float rate, bool playing, float gravity = 0.02f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = playing;
            main.loop = true;
            main.prewarm = playing;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            bool leaf = mat == MatLeaf;
            main.startSize = new ParticleSystem.MinMaxCurve(leaf ? 0.2f : 0.06f, leaf ? 0.35f : 0.14f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            shape.rotation = euler;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;
            var rol = ps.rotationOverLifetime;
            rol.enabled = true;
            rol.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.Get(mat);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = InkPuppet.OrderFor(pos.z);
            return ps;
        }

    }
}
