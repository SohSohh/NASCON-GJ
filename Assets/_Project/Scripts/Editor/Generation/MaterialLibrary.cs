using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>Material slots used by generated meshes.</summary>
    public enum MatId
    {
        Stone,
        StoneDark,
        StoneFloor,
        Wood,
        Cloth,
        Bandage,
        Metal,
        Coal,
        Skin,
        Wax,
        WoodLight,
        Window,
        Plaster,
        Roof,
        PaintRed,
        PaintBlue,
        PaintYellow,
        PaintGreen,
        Grass,
    }

    /// <summary>
    /// Creates/updates every generated material, plus the post-processing volume profile.
    /// Palette pulled from the concept boards: warm brown stone, deep purple-black shadow, orange fire.
    /// </summary>
    public static class MaterialLibrary
    {
        public const string FlameMat = "M_Flame";
        public const string GuideFlameMat = "M_GuideFlame";
        public const string ShadowMat = "M_ShadowCreature";
        public const string ShadowGlobMat = "M_ShadowGlob";
        public const string VoidSkyMat = "M_VoidSky";
        public const string ShadowVineMat = "M_ShadowVine";
        public const string LightningMat = "M_Lightning";
        public const string SkyDomeMat = "M_SkyDome";
        public const string VoidVineMat = "M_VoidVine";
        public const string ShadowHeartMat = "M_ShadowHeart";
        public const string ShadowGlobEyesMat = "M_ShadowGlobEyes";
        public const string ShadowEyesMat = "M_ShadowEyes";
        public const string VolumeProfilePath = GenUtil.VolumeDir + "/VP_Tomb.asset";
        public const string OutdoorProfilePath = GenUtil.VolumeDir + "/VP_Outdoor.asset";
        public const string ScrollProfilePath = GenUtil.VolumeDir + "/VP_Scroll.asset";

        private static readonly Dictionary<MatId, Material> cache = new();

        public static void GenerateAll()
        {
            cache.Clear();
            GenUtil.EnsureFolder(GenUtil.MaterialDir);
            ShadowSheetGenerator.GenerateAll();
            var grit = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.StoneGritPath);
            var soft = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.SoftParticlePath);
            var shadow = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.ShadowPath);

            // Painterly surfaces (NASCON/StylizedLit). Grit is sampled in world space, so no base map is needed.
            Stylized(MatId.Stone, Hex("8A6A54"), grit, gritStrength: 0.5f);
            Stylized(MatId.StoneDark, Hex("53413A"), grit, gritStrength: 0.5f);
            Stylized(MatId.StoneFloor, Hex("6E5444"), grit, gritStrength: 0.45f, spec: 0.6f, specSize: 0.07f);
            Stylized(MatId.Wood, Hex("3E2618"), grit, gritStrength: 0.35f);
            Stylized(MatId.Cloth, Hex("8A4E2C"), grit, gritStrength: 0.3f);
            Stylized(MatId.Bandage, Hex("C2A888"), grit, gritStrength: 0.25f);
            Stylized(MatId.Metal, Hex("2E2420"), grit, gritStrength: 0.3f, spec: 0.35f, specSize: 0.04f);
            Stylized(MatId.Coal, Hex("160E0B"), grit, gritStrength: 0.4f, emission: new Color(1.6f, 0.42f, 0.06f));
            Stylized(MatId.Skin, Hex("D08C62"), grit, gritStrength: 0.1f);
            Stylized(MatId.Wax, Hex("E0D0AE"), grit, gritStrength: 0.1f);
            Stylized(MatId.WoodLight, Hex("8C6542"), grit, gritStrength: 0.3f);
            Stylized(MatId.Window, Hex("3A2410"), grit, gritStrength: 0.1f, emission: new Color(2.2f, 1.15f, 0.45f));
            Stylized(MatId.Plaster, Hex("A88A6E"), grit, gritStrength: 0.45f);
            Stylized(MatId.Roof, Hex("5A2E22"), grit, gritStrength: 0.5f);
            // the living town's colours (prologue)
            Stylized(MatId.PaintRed, Hex("B8432F"), grit, gritStrength: 0.25f);
            Stylized(MatId.PaintBlue, Hex("3E78A8"), grit, gritStrength: 0.25f);
            Stylized(MatId.PaintYellow, Hex("E2B44A"), grit, gritStrength: 0.25f);
            Stylized(MatId.PaintGreen, Hex("5F9150"), grit, gritStrength: 0.25f);
            // prologue grass
            Stylized(MatId.Grass, Hex("7CC95A"), grit, gritStrength: 0.12f);


            // Painterly flames (NASCON/StylizedFlame): cream core, orange body, teal rim.
            Flame(FlameMat, soft, grit, core: new Color(3.4f, 2.8f, 2.0f), rim: new Color(0.9f, 1.6f, 1.5f), intensity: 2.6f);
            Flame(GuideFlameMat, soft, grit, core: new Color(2.4f, 3.0f, 3.4f), rim: new Color(0.45f, 1.3f, 2.2f), intensity: 1.5f, coreThreshold: 0.5f, softness: 0.12f);
            Particle(ShadowEyesMat, soft, new Color(8f, 3.2f, 0.8f, 1f), additive: true);
            ShadowCreature(shadow);

            BuildVolumeProfile();
            BuildOutdoorProfile();
            BuildScrollProfile();
            BuildCinematicProfile();
            PainterlyPass(grit);
            ComicPass(grit);
            AssetDatabase.SaveAssets();
        }

        public static Material Get(MatId id)
        {
            if (cache.TryGetValue(id, out var m) && m != null) return m;
            m = AssetDatabase.LoadAssetAtPath<Material>($"{GenUtil.MaterialDir}/M_{id}.mat");
            cache[id] = m;
            return m;
        }

        public static Material Get(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{GenUtil.MaterialDir}/{name}.mat");

        public static Material[] Get(MatId[] ids)
        {
            var result = new Material[ids.Length];
            for (int i = 0; i < ids.Length; i++) result[i] = Get(ids[i]);
            return result;
        }

        // ---------------- material builders ----------------

        public static Material LoadOrCreate(string name, string shaderName)
        {
            string path = $"{GenUtil.MaterialDir}/{name}.mat";
            var shader = Shader.Find(shaderName);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            return mat;
        }

        private static void Lit(MatId id, Color color, float smoothness, float metallic, Texture2D baseMap, Color? emission = null)
        {
            var mat = LoadOrCreate($"M_{id}", "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetTexture("_MainTex", baseMap);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else mat.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            cache[id] = mat;
        }

        private static void Stylized(MatId id, Color color, Texture2D grit, float gritStrength, float spec = 0f, float specSize = 0.08f, Color? emission = null)
        {
            var mat = LoadOrCreate($"M_{id}", "NASCON/StylizedLit");
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", null);
            mat.SetTexture("_GritTex", grit);
            mat.SetFloat("_GritStrength", gritStrength);
            mat.SetFloat("_SpecStrength", spec);
            mat.SetFloat("_SpecSize", specSize);
            mat.SetColor("_EmissionColor", emission ?? Color.black);
            mat.SetColor("_ShadowTint", ShadowTint);
            mat.SetColor("_MidtoneTint", MidtoneTint);
            mat.SetFloat("_MidtoneStrength", 0.4f);
            mat.SetFloat("_AmbientStrength", 1.0f);
            EditorUtility.SetDirty(mat);
            cache[id] = mat;
        }

        private static void Flame(string name, Texture2D shape, Texture2D noise, Color core, Color rim, float intensity, float coreThreshold = 0.32f, float softness = 0.03f)
        {
            var mat = LoadOrCreate(name, "NASCON/StylizedFlame");
            mat.SetTexture("_BaseMap", shape);
            mat.SetTexture("_NoiseTex", noise);
            mat.SetColor("_CoreColor", core);
            mat.SetColor("_EdgeColor", rim);
            mat.SetFloat("_Intensity", intensity);
            mat.SetFloat("_CoreThreshold", coreThreshold);
            mat.SetFloat("_Softness", softness);
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
        }

        /// <summary>Global look knobs shared by every stylized surface.</summary>
        public static readonly Color ShadowTint = new(0.26f, 0.15f, 0.3f);
        public static readonly Color MidtoneTint = new(1.18f, 0.88f, 0.62f);

        public const string PainterlyMat = "M_PainterlyPost";
        private const string RendererPath = "Assets/Settings/PC_Renderer.asset";

        /// <summary>Creates the full-screen painterly material and installs it on the PC renderer (once).</summary>
        private static void PainterlyPass(Texture2D grit)
        {
            var mat = LoadOrCreate(PainterlyMat, "NASCON/PainterlyPost");
            mat.SetTexture("_PaperTex", grit);
            mat.SetFloat("_PaletteStrength", 0.3f);
            mat.SetColor("_PaletteShadow", new Color(0.07f, 0.04f, 0.08f));
            mat.SetColor("_PaletteMid", new Color(0.5f, 0.25f, 0.12f));
            mat.SetColor("_PaletteLight", new Color(1f, 0.64f, 0.3f));
            mat.SetColor("_PaletteHigh", new Color(1f, 0.92f, 0.76f));
            EditorUtility.SetDirty(mat);

            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) { Debug.LogWarning($"[Gen] {RendererPath} not found, painterly pass not installed."); return; }

            FullScreenPassRendererFeature feature = null;
            foreach (var f in data.rendererFeatures)
                if (f is FullScreenPassRendererFeature fs && f.name == "PainterlyPass") feature = fs;

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = "PainterlyPass";
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);
            }
            feature.passMaterial = mat;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.Color;
            feature.SetActive(true);
            data.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
        }

        public const string ComicMat = "M_ComicPost";

        /// <summary>Ink outlines, toon colour, halftone, the Void flood and impact frames; installed after the painterly pass.</summary>
        private static void ComicPass(Texture2D noise)
        {
            var mat = LoadOrCreate(ComicMat, "NASCON/ComicPost");
            mat.SetTexture("_NoiseTex", noise);
            EditorUtility.SetDirty(mat);

            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) return;
            FullScreenPassRendererFeature feature = null;
            foreach (var f in data.rendererFeatures)
                if (f is FullScreenPassRendererFeature fs && f.name == "ComicPass") feature = fs;
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = "ComicPass";
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature); // after PainterlyPass, so ink lines stay crisp
            }
            feature.passMaterial = mat;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.SetActive(true);
            data.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
        }

        private static void MakeTransparent(Material mat, bool additive)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        public static void Particle(string name, Texture2D tex, Color color, bool additive)
        {
            var mat = LoadOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", color);
            MakeTransparent(mat, additive);
            EditorUtility.SetDirty(mat);
        }

        public const string TownsfolkMat = "M_ShadowTownsfolk";
        public const string ShadowSmokeMat = "M_ShadowSmoke";
        public const string LightShaftMat = "M_LightShaft";
        public const string DustMat = "M_Dust";

        private static void ShadowCreature(Texture2D unused)
        {
            var grit = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.StoneGritPath);

            var mat = LoadOrCreate(ShadowMat, "NASCON/ShadowCreature");
            mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowSheetGenerator.CreaturePath));
            mat.SetTexture("_NoiseTex", grit);
            // a shadow, not a sticker: deep maroon-black that lets a little of the wall through, small ember eyes,
            // and a faint sheen so it still reads in dim corners
            mat.SetColor("_BodyColor", new Color(0.03f, 0.008f, 0.02f, 0.88f));
            mat.SetColor("_DetailColor", new Color(0.09f, 0.03f, 0.04f, 1f));
            mat.SetColor("_GlowColor", new Color(2.4f, 0.75f, 0.16f, 1f));
            mat.SetColor("_SheenColor", new Color(0.09f, 0.03f, 0.07f, 1f));
            mat.renderQueue = (int)RenderQueue.Transparent - 10;
            EditorUtility.SetDirty(mat);

            // the Void in the sky: pitch black against a red sky (outdoor exposure would otherwise lift it to grey)
            var voidSky = LoadOrCreate(VoidSkyMat, "NASCON/ShadowCreature");
            voidSky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowSheetGenerator.CreaturePath));
            voidSky.SetTexture("_NoiseTex", grit);
            voidSky.SetColor("_BodyColor", new Color(0.004f, 0f, 0.003f, 1f));
            voidSky.SetColor("_DetailColor", new Color(0.02f, 0.004f, 0.008f, 1f));
            voidSky.SetColor("_GlowColor", new Color(4f, 1.2f, 0.25f, 1f));
            voidSky.SetColor("_SheenColor", Color.black);
            voidSky.SetFloat("_Wobble", 0.006f);
            voidSky.renderQueue = (int)RenderQueue.Transparent - 10;
            EditorUtility.SetDirty(voidSky);

            // the vine creatures (shadows + the Void): pitch-black writhing vines, a faint blood-red edge, a burning heart
            var vineMat = LoadOrCreate(ShadowVineMat, "NASCON/ShadowGlob");
            vineMat.SetTexture("_NoiseTex", grit);
            vineMat.SetColor("_BodyColor", new Color(0.006f, 0.002f, 0.005f, 1f));
            vineMat.SetColor("_InnerColor", new Color(0.02f, 0.004f, 0.01f, 1f));
            vineMat.SetColor("_RimColor", new Color(0.9f, 0.08f, 0.05f, 1f));
            vineMat.SetFloat("_RimPower", 3.5f);
            vineMat.SetFloat("_Wobble", 0.004f);
            vineMat.SetFloat("_WobbleSpeed", 2.2f);
            EditorUtility.SetDirty(vineMat);
            Particle(LightningMat, AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.SoftParticlePath), new Color(0.85f, 0.8f, 1f, 1f) * 4f, additive: true);
            // the colossal Void: pure black against the sky (no fog, only a hairline ember edge)
            var voidVine = LoadOrCreate(VoidVineMat, "NASCON/ShadowGlob");
            voidVine.SetTexture("_NoiseTex", grit);
            voidVine.SetColor("_BodyColor", new Color(0.004f, 0.001f, 0.003f, 1f));
            voidVine.SetColor("_InnerColor", new Color(0.01f, 0.002f, 0.006f, 1f));
            voidVine.SetColor("_RimColor", new Color(0.35f, 0.03f, 0.02f, 1f));
            voidVine.SetFloat("_RimPower", 8f);
            voidVine.SetFloat("_Wobble", 0.002f);
            voidVine.SetFloat("_FogAmount", 0.15f);
            EditorUtility.SetDirty(voidVine);
            var sky = LoadOrCreate(SkyDomeMat, "NASCON/SkyDome");
            sky.SetTexture("_NoiseTex", grit);
            EditorUtility.SetDirty(sky);
            var heartMat = LoadOrCreate(ShadowHeartMat, "NASCON/ShadowGlob");
            heartMat.SetTexture("_NoiseTex", grit);
            heartMat.SetColor("_BodyColor", new Color(0.5f, 0.03f, 0.03f, 1f));
            heartMat.SetColor("_InnerColor", new Color(2.2f, 0.25f, 0.1f, 1f));
            heartMat.SetColor("_RimColor", new Color(3f, 0.5f, 0.15f, 1f));
            heartMat.SetFloat("_RimPower", 2f);
            heartMat.SetFloat("_Wobble", 0.08f);
            EditorUtility.SetDirty(heartMat);

            // the tar body: dark wobbling blobs with an ember rim, and its burning eyes
            var globMat = LoadOrCreate(ShadowGlobMat, "NASCON/ShadowGlob");
            globMat.SetTexture("_NoiseTex", grit);
            globMat.SetColor("_BodyColor", new Color(0.018f, 0.005f, 0.012f, 1f));
            globMat.SetColor("_InnerColor", new Color(0.05f, 0.008f, 0.04f, 1f));
            globMat.SetColor("_RimColor", new Color(1.2f, 0.22f, 0.05f, 1f));
            globMat.SetFloat("_RimPower", 5f); // a thin smouldering edge on black tar
            globMat.SetFloat("_Wobble", 0.16f);
            EditorUtility.SetDirty(globMat);
            var eyesMat = LoadOrCreate(ShadowGlobEyesMat, "NASCON/ShadowGlob");
            eyesMat.SetTexture("_NoiseTex", grit);
            eyesMat.SetColor("_BodyColor", new Color(3.2f, 1.0f, 0.2f, 1f));
            eyesMat.SetColor("_InnerColor", new Color(1.5f, 1.2f, 0.6f, 1f));
            eyesMat.SetColor("_RimColor", new Color(2f, 0.4f, 0.05f, 1f));
            eyesMat.SetFloat("_Wobble", 0f);
            EditorUtility.SetDirty(eyesMat);

            var town = LoadOrCreate(TownsfolkMat, "NASCON/ShadowCreature");
            town.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowSheetGenerator.TownsfolkPath));
            town.SetTexture("_NoiseTex", grit);
            town.SetColor("_BodyColor", new Color(0.02f, 0.01f, 0.015f, 0.82f));
            town.SetFloat("_Wobble", 0.004f);
            town.SetColor("_SheenColor", Color.black);
            town.renderQueue = (int)RenderQueue.Transparent - 11;
            EditorUtility.SetDirty(town);

            // dark smoke wisps peeling off a shadow: the flame shader with a black body and a faint ember rim
            // soft dark wisps (alpha-blended, so no hard edges or square quads)
            Particle(ShadowSmokeMat, AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.SoftParticlePath), new Color(0.03f, 0.006f, 0.02f, 0.75f), additive: false); // soft wisps, not hard dots

            var shaft = LoadOrCreate(LightShaftMat, "NASCON/LightShaft");
            shaft.SetTexture("_NoiseTex", grit);
            shaft.SetColor("_Color", new Color(0.4f, 0.46f, 0.72f));
            shaft.SetFloat("_Intensity", 0.3f);
            shaft.SetFloat("_EdgePower", 3f);
            EditorUtility.SetDirty(shaft);

            Particle(DustMat, AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.SoftParticlePath), new Color(1f, 0.8f, 0.6f, 0.5f), additive: true);
        }

        private static void BuildVolumeProfile()
        {
            GenUtil.EnsureFolder(GenUtil.VolumeDir);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            T Comp<T>() where T : VolumeComponent
            {
                if (!profile.TryGet(out T c))
                {
                    c = profile.Add<T>(true);
                    c.name = typeof(T).Name;
                    AssetDatabase.AddObjectToAsset(c, profile);
                }
                c.active = true;
                return c;
            }

            var tone = Comp<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);

            var bloom = Comp<Bloom>();
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.78f, 0.6f));

            var vignette = Comp<Vignette>();
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(new Color(0.03f, 0.01f, 0.03f));

            var color = Comp<ColorAdjustments>();
            color.postExposure.Override(0.35f);
            color.contrast.Override(18f);
            color.saturation.Override(-8f);
            color.colorFilter.Override(new Color(1f, 0.93f, 0.86f));

            var smh = Comp<ShadowsMidtonesHighlights>();
            smh.shadows.Override(new Vector4(0.9f, 0.85f, 1.1f, -0.05f));

            var grain = Comp<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.28f);

            EditorUtility.SetDirty(profile);
        }

        /// <summary>The painted story scroll: no filmic tonemapping (paper colours stay true), soft bloom, grain, light vignette.</summary>
        private static void BuildScrollProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ScrollProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ScrollProfilePath);
            }
            T Comp<T>() where T : VolumeComponent
            {
                if (!profile.TryGet(out T c))
                {
                    c = profile.Add<T>(true);
                    c.name = typeof(T).Name;
                    AssetDatabase.AddObjectToAsset(c, profile);
                }
                c.active = true;
                return c;
            }
            Comp<Tonemapping>().mode.Override(TonemappingMode.None);
            var bloom = Comp<Bloom>();
            bloom.threshold.Override(0.82f);
            bloom.intensity.Override(1.35f);
            bloom.scatter.Override(0.82f);
            bloom.tint.Override(new Color(1f, 0.8f, 0.55f));
            var lgg = Comp<LiftGammaGain>();
            lgg.lift.Override(new Vector4(1f, 0.96f, 0.92f, -0.02f));   // warm, slightly lifted blacks like ink on paper
            lgg.gain.Override(new Vector4(1f, 0.97f, 0.9f, 0.04f));
            var split = Comp<SplitToning>();
            split.shadows.Override(new Color(0.35f, 0.45f, 0.5f));      // teal in the shadows, gold in the lights
            split.highlights.Override(new Color(1f, 0.8f, 0.5f));
            split.balance.Override(-15f);
            var dof = Comp<DepthOfField>();                             // depth: far mountains and near grass soften
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(20f);
            dof.aperture.Override(4.5f);
            dof.focalLength.Override(70f);
            var vig = Comp<Vignette>();
            vig.intensity.Override(0.34f);
            vig.smoothness.Override(0.6f);
            vig.color.Override(new Color(0.08f, 0.04f, 0.03f));
            var grain = Comp<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.22f);
            var color = Comp<ColorAdjustments>();
            color.postExposure.Override(0f);
            color.contrast.Override(6f);
            color.saturation.Override(4f);
            var ca = Comp<ChromaticAberration>();
            ca.intensity.Override(0.08f);
            EditorUtility.SetDirty(profile);
        }

        public const string CinematicProfilePath = GenUtil.VolumeDir + "/VP_Cinematic.asset";

        /// <summary>The film grade for the 3D cutscene: soft bloom, warm split-tone, depth of field, grain, vignette.</summary>
        public static void BuildCinematicProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(CinematicProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, CinematicProfilePath);
            }
            T Comp<T>() where T : VolumeComponent
            {
                if (!profile.TryGet(out T c))
                {
                    c = profile.Add<T>(true);
                    c.name = typeof(T).Name;
                    AssetDatabase.AddObjectToAsset(c, profile);
                }
                c.active = true;
                return c;
            }
            var bloom = Comp<Bloom>();
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.86f, 0.7f));
            var dof = Comp<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Gaussian);               // the far town melts away behind whoever's speaking
            dof.gaussianStart.Override(14f);
            dof.gaussianEnd.Override(60f);
            dof.gaussianMaxRadius.Override(1.1f);
            dof.highQualitySampling.Override(true);
            var split = Comp<SplitToning>();
            split.shadows.Override(new Color(0.38f, 0.42f, 0.55f));
            split.highlights.Override(new Color(1f, 0.82f, 0.6f));
            split.balance.Override(-10f);
            var color = Comp<ColorAdjustments>();
            color.contrast.Override(12f);
            color.saturation.Override(6f);
            var wb = Comp<WhiteBalance>();
            wb.temperature.Override(8f);
            var vig = Comp<Vignette>();
            vig.intensity.Override(0.32f);
            vig.smoothness.Override(0.55f);
            var grain = Comp<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium2);
            grain.intensity.Override(0.28f);
            var ca = Comp<ChromaticAberration>();
            ca.intensity.Override(0.12f);
            var lens = Comp<LensDistortion>();
            lens.intensity.Override(-0.12f);
            EditorUtility.SetDirty(profile);
        }

        /// <summary>Layered over VP_Tomb for open-air cutscene shots: brighter, airier, a touch more colour.</summary>
        private static void BuildOutdoorProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(OutdoorProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, OutdoorProfilePath);
            }
            if (!profile.TryGet(out ColorAdjustments color))
            {
                color = profile.Add<ColorAdjustments>(true);
                color.name = nameof(ColorAdjustments);
                AssetDatabase.AddObjectToAsset(color, profile);
            }
            color.active = true;
            color.postExposure.Override(1.15f);
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            if (!profile.TryGet(out ShadowsMidtonesHighlights smh))
            {
                smh = profile.Add<ShadowsMidtonesHighlights>(true);
                smh.name = nameof(ShadowsMidtonesHighlights);
                AssetDatabase.AddObjectToAsset(smh, profile);
            }
            smh.active = true;
            smh.shadows.Override(new Vector4(0.95f, 0.88f, 1.05f, 0.12f));
            EditorUtility.SetDirty(profile);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
