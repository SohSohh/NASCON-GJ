using NasconGJ.Core;
using NasconGJ.Death;
using NasconGJ.Entities;
using NasconGJ.Fire;
using NasconGJ.Guiding;
using NasconGJ.Interaction;
using NasconGJ.Items;
using NasconGJ.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>Items, braziers, guiding light, shadow creature, player and dialogue assets.</summary>
    public static class GameplayPrefabGenerator
    {
        public const string Torch = "PF_Torch";
        public const string Stick = "PF_Stick";
        public const string Candle = "PF_Candle";
        public const string BrazierLit = "PF_Brazier_Lit";
        public const string BrazierUnlit = "PF_Brazier_Unlit";
        public const string GuidingLight = "PF_GuidingLight";
        public const string ShadowEnemy = "PF_ShadowEnemy";
        public const string Player = "PF_Player";

        public static string TorchPath => $"{GenUtil.ItemPrefabDir}/{Torch}.prefab";
        public static string StickPath => $"{GenUtil.ItemPrefabDir}/{Stick}.prefab";
        public static string CandlePath => $"{GenUtil.ItemPrefabDir}/{Candle}.prefab";
        public static string BrazierLitPath => $"{GenUtil.LightingPrefabDir}/{BrazierLit}.prefab";
        public static string BrazierUnlitPath => $"{GenUtil.LightingPrefabDir}/{BrazierUnlit}.prefab";
        public static string GuidingLightPath => $"{GenUtil.GuidePrefabDir}/{GuidingLight}.prefab";
        public static string ShadowEnemyPath => $"{GenUtil.ShadowPrefabDir}/{ShadowEnemy}.prefab";
        public static string PlayerPath => $"{GenUtil.PlayerPrefabDir}/{Player}.prefab";

        public static readonly Color FireColor = new(1f, 0.7f, 0.42f);
        public static readonly Color GuideColor = new(0.72f, 0.9f, 1f);

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(GenUtil.PropMeshDir);

            BuildItems();
            BuildBraziers();
            BuildGuidingLight();
            BuildShadowEnemy();
            BuildShadowFigures();
            BuildAtmosphere();
            BuildVillagers();
            CharacterImporter.ImportAll();
            BuildPlayer();
            DialogueLibrary.GenerateAll();
            AssetDatabase.SaveAssets();
        }

        // ---------------- items ----------------

        private static void BuildItems()
        {
            // Every item is a fixed part (handle) + a fuel section that BurnDown shortens and chars as it burns,
            // with an ember cap riding on top and the flame following it down.

            // torch: wooden handle with a bandage grip; the fuel is the tarred cloth head
            var torch = new MeshBuilder(201);
            torch.Prism(MatId.Wood, 6, Vector3.zero, 0.022f, 0.31f);
            torch.Prism(MatId.Bandage, 8, new Vector3(0, 0.06f, 0), 0.026f, 0.1f);
            var torchFuel = new MeshBuilder(211);
            torchFuel.Prism(MatId.Wood, 6, Vector3.zero, 0.022f, 0.14f);
            torchFuel.Prism(MatId.Cloth, 8, new Vector3(0, 0.015f, 0), 0.042f, 0.125f, new Vector3(0, 10f, 0));
            MakeItem(Torch, "Torch", torch, torchFuel, fuelBaseY: 0.31f, fuelLength: 0.14f, capRadius: 0.036f, capBeforeLit: true,
                flameScale: 1f, burnTime: 90f, range: 11f, intensity: 12f,
                size: new Vector3(0.09f, 0.48f, 0.09f), holdPos: new Vector3(0, -0.12f, 0), holdRot: Vector3.zero);

            // stick: a pale dry branch with a twig; the top two thirds burn away
            var stick = new MeshBuilder(202);
            stick.Prism(MatId.WoodLight, 5, Vector3.zero, 0.017f, 0.13f, new Vector3(0, 0, 2f));
            stick.Prism(MatId.WoodLight, 5, new Vector3(0.002f, 0.09f, 0), 0.008f, 0.11f, new Vector3(0, 0, -38f));
            var stickFuel = new MeshBuilder(212);
            stickFuel.Prism(MatId.WoodLight, 5, Vector3.zero, 0.016f, 0.2f, new Vector3(0, 0, 2f));
            MakeItem(Stick, "Stick", stick, stickFuel, fuelBaseY: 0.13f, fuelLength: 0.2f, capRadius: 0.02f, capBeforeLit: false,
                flameScale: 0.7f, burnTime: 35f, range: 8f, intensity: 8f,
                size: new Vector3(0.07f, 0.36f, 0.07f), holdPos: new Vector3(0, -0.08f, 0), holdRot: Vector3.zero);

            // candle: brass dish; the wax melts down
            var candle = new MeshBuilder(203);
            candle.Prism(MatId.Metal, 8, Vector3.zero, 0.075f, 0.016f);
            candle.Prism(MatId.Metal, 6, new Vector3(0.07f, 0.004f, 0), 0.012f, 0.008f);
            var candleFuel = new MeshBuilder(213);
            candleFuel.Prism(MatId.Wax, 8, Vector3.zero, 0.03f, 0.13f);
            MakeItem(Candle, "Candle", candle, candleFuel, fuelBaseY: 0.016f, fuelLength: 0.13f, capRadius: 0.004f, capBeforeLit: true,
                flameScale: 0.45f, burnTime: 60f, range: 6f, intensity: 5f,
                size: new Vector3(0.15f, 0.16f, 0.15f), holdPos: new Vector3(0, 0.02f, 0), holdRot: Vector3.zero, capHeight: 0.02f);
        }

        private static void MakeItem(string prefabName, string displayName, MeshBuilder handle, MeshBuilder fuelMesh,
            float fuelBaseY, float fuelLength, float capRadius, bool capBeforeLit,
            float flameScale, float burnTime, float range, float intensity,
            Vector3 size, Vector3 holdPos, Vector3 holdRot, float capHeight = 0.03f)
        {
            var go = new GameObject(prefabName) { tag = GameTags.Flammable };
            var handleMesh = SaveProp("SM_" + displayName, handle, out var slots);
            go.AddComponent<MeshFilter>().sharedMesh = handleMesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = MaterialLibrary.Get(slots);

            var fuel = GenUtil.Child("Fuel", go.transform, new Vector3(0, fuelBaseY, 0));
            fuel.AddComponent<MeshFilter>().sharedMesh = SaveProp("SM_" + displayName + "_Fuel", fuelMesh, out var fuelSlots);
            fuel.AddComponent<MeshRenderer>().sharedMaterials = MaterialLibrary.Get(fuelSlots);

            var capBuilder = new MeshBuilder(220);
            capBuilder.Prism(MatId.Coal, 7, new Vector3(0, -capHeight * 0.5f, 0), capRadius, capHeight);
            var cap = GenUtil.Child("CharCap", go.transform, new Vector3(0, fuelBaseY + fuelLength, 0));
            cap.AddComponent<MeshFilter>().sharedMesh = SaveProp("SM_" + displayName + "_Char", capBuilder, out var capSlots);
            cap.AddComponent<MeshRenderer>().sharedMaterials = MaterialLibrary.Get(capSlots);

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.6f;
            body.angularDamping = 1f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0, size.y * 0.5f, 0);
            box.size = size;

            float flameY = fuelBaseY + fuelLength + 0.02f;
            var (light, particles) = AddFlame(go.transform, new Vector3(0, flameY, 0), flameScale, range, intensity, FireColor, false);

            var flammable = go.AddComponent<FlammableItem>();
            GenUtil.Set(flammable, "displayName", displayName);
            GenUtil.Set(flammable, "maxBurnTime", burnTime);
            GenUtil.Set(flammable, "startLit", false);
            GenUtil.Set(flammable, "countsAsPlayerFire", true);
            GenUtil.Set(flammable, "flameLight", light);
            GenUtil.SetArray(flammable, "flameParticles", particles);

            var burn = go.AddComponent<BurnDown>();
            GenUtil.Set(burn, "fuel", fuel.transform);
            GenUtil.Set(burn, "fuelLength", fuelLength);
            GenUtil.Set(burn, "charCap", cap.transform);
            GenUtil.Set(burn, "capBeforeLit", capBeforeLit);
            GenUtil.Set(burn, "flame", light.transform);

            var carry = go.AddComponent<CarryableItem>();
            GenUtil.Set(carry, "itemName", displayName);
            GenUtil.Set(carry, "holdPositionOffset", holdPos);
            GenUtil.Set(carry, "holdRotationOffset", holdRot);

            GenUtil.SetLayerRecursive(go, LayerMask.NameToLayer(GameLayers.Interactable));
            GenUtil.SavePrefab(go, $"{GenUtil.ItemPrefabDir}/{prefabName}.prefab");
        }

        // ---------------- prologue townsfolk (placeholder low-poly people) ----------------

        public const string CutscenePrefabDir = GenUtil.Root + "/Prefabs/Cutscenes";
        public static string VillagerPath(int variant) => $"{CutscenePrefabDir}/PF_Villager_{(char)('A' + variant)}.prefab";
        public const int VillagerVariants = 4;

        private static void BuildVillagers()
        {
            GenUtil.EnsureFolder(CutscenePrefabDir);
            for (int v = 0; v < VillagerVariants; v++)
            {
                var b = new MeshBuilder(230 + v);
                MatId coat = v switch { 0 => MatId.Cloth, 1 => MatId.Bandage, 2 => MatId.Roof, _ => MatId.WoodLight };
                bool dress = v == 1 || v == 3;
                float tall = v == 2 ? 1.06f : v == 3 ? 0.93f : 1f;

                // legs, boots
                if (!dress)
                    foreach (float x in new[] { -0.09f, 0.09f })
                    {
                        b.Prism(MatId.Wood, 6, new Vector3(x, 0.08f, 0), 0.065f, 0.62f * tall);
                        b.Box(MatId.Metal, new Vector3(x, 0.05f, 0.03f), new Vector3(0.13f, 0.1f, 0.22f));
                    }
                // body: a coat to the knees, or a long dress
                b.Prism(coat, 8, new Vector3(0, dress ? 0.02f : 0.55f * tall, 0), dress ? 0.24f : 0.2f, dress ? 1.22f * tall : 0.7f * tall);
                b.Prism(coat, 8, new Vector3(0, 1.1f * tall, 0), 0.18f, 0.16f * tall);
                b.Box(MatId.Wood, new Vector3(0, 0.95f * tall, 0), new Vector3(0.42f, 0.07f, 0.42f));                 // belt
                b.Box(coat, new Vector3(0, 1.23f * tall, 0), new Vector3(0.5f, 0.13f, 0.26f));                       // shoulders
                foreach (float s in new[] { -1f, 1f })
                {
                    b.Prism(coat, 6, new Vector3(s * 0.27f, 0.72f * tall, 0), 0.055f, 0.55f * tall, new Vector3(0, 0, s * -4f));
                    b.Box(MatId.Skin, new Vector3(s * 0.285f, 0.68f * tall, 0), new Vector3(0.08f, 0.1f, 0.08f));     // hands
                }
                b.Prism(MatId.Skin, 6, new Vector3(0, 1.29f * tall, 0), 0.055f, 0.08f);                                 // neck
                b.Prism(MatId.Skin, 7, new Vector3(0, 1.35f * tall, 0), 0.11f, 0.22f);                                  // head
                switch (v)
                {
                    case 0: // brimmed hat
                        b.Prism(MatId.Wood, 10, new Vector3(0, 1.55f * tall, 0), 0.21f, 0.025f);
                        b.Prism(MatId.Wood, 8, new Vector3(0, 1.57f * tall, 0), 0.12f, 0.12f);
                        break;
                    case 1: // headscarf
                        b.Prism(MatId.Cloth, 8, new Vector3(0, 1.43f * tall, -0.01f), 0.125f, 0.16f);
                        break;
                    case 2: // cap
                        b.Prism(MatId.Cloth, 8, new Vector3(0, 1.52f * tall, 0), 0.12f, 0.07f);
                        break;
                    default: // hair bun
                        b.Prism(MatId.Wood, 7, new Vector3(0, 1.5f * tall, -0.04f), 0.09f, 0.08f);
                        break;
                }

                var go = new GameObject($"PF_Villager_{(char)('A' + v)}");
                go.AddComponent<MeshFilter>().sharedMesh = SaveProp($"SM_Villager_{(char)('A' + v)}", b, out var slots);
                go.AddComponent<MeshRenderer>().sharedMaterials = MaterialLibrary.Get(slots);
                go.AddComponent<NasconGJ.Cutscenes.Villager>();
                GenUtil.SavePrefab(go, VillagerPath(v));
            }
        }

        // ---------------- braziers ----------------

        private static void BuildBraziers()
        {
            var b = new MeshBuilder(204);
            b.Box(MatId.StoneDark, new Vector3(0, 0.075f, 0), new Vector3(0.62f, 0.15f, 0.62f));
            b.Box(MatId.Stone, new Vector3(0, 0.19f, 0), new Vector3(0.46f, 0.08f, 0.46f));
            b.Prism(MatId.Stone, 8, new Vector3(0, 0.23f, 0), 0.15f, 0.66f);
            b.Prism(MatId.Metal, 8, new Vector3(0, 0.88f, 0), 0.2f, 0.06f);
            b.Prism(MatId.Metal, 8, new Vector3(0, 0.94f, 0), 0.34f, 0.13f);
            b.Prism(MatId.Metal, 8, new Vector3(0, 1.06f, 0), 0.37f, 0.03f);
            for (int i = 0; i < 7; i++)
                b.Box(MatId.Coal, new Vector3(b.Range(-0.18f, 0.18f), 1.08f, b.Range(-0.18f, 0.18f)), Vector3.one * b.Range(0.06f, 0.11f),
                    new Vector3(b.Range(0, 360), b.Range(0, 360), b.Range(0, 360)));
            var mesh = SaveProp("SM_Brazier", b, out var slots);

            foreach (bool lit in new[] { true, false })
            {
                var go = new GameObject(lit ? BrazierLit : BrazierUnlit) { tag = GameTags.Flammable };
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = MaterialLibrary.Get(slots);
                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0, 0.55f, 0);
                box.size = new Vector3(0.7f, 1.1f, 0.7f);

                var (light, particles) = AddFlame(go.transform, new Vector3(0, 1.12f, 0), 2.2f, 11f, 14f, FireColor, false);

                var f = go.AddComponent<FlammableItem>();
                GenUtil.Set(f, "displayName", "Brazier");
                GenUtil.Set(f, "infiniteBurn", true);
                GenUtil.Set(f, "startLit", lit);
                GenUtil.Set(f, "countsAsPlayerFire", false);
                GenUtil.Set(f, "flameLight", light);
                GenUtil.Set(f, "gameplayRadiusFactor", 0.65f);
                GenUtil.SetArray(f, "flameParticles", particles);

                GenUtil.SetLayerRecursive(go, LayerMask.NameToLayer(GameLayers.LightBlocker));
                GenUtil.SavePrefab(go, lit ? BrazierLitPath : BrazierUnlitPath);
            }
        }

        // ---------------- guiding light ----------------

        private static void BuildGuidingLight()
        {
            var go = new GameObject(GuidingLight) { tag = GameTags.GuidingLight };
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.45f;

            var visual = GenUtil.Child("Visual", go.transform);
            var light = visual.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = GuideColor;
            light.range = 8f;
            light.intensity = 5f;
            light.shadows = LightShadows.Soft;
            light.shadowNearPlane = 0.2f;

            var mat = MaterialLibrary.Get(MaterialLibrary.GuideFlameMat);
            var wispColors = Grad(new[] { new Color(1f, 1f, 1f), new Color(0.75f, 0.95f, 1f), new Color(0.35f, 0.75f, 1f) },
                new[] { 0f, 0.4f, 1f }, new[] { 0f, 0.9f, 0f }, new[] { 0f, 0.15f, 1f });
            Particles(visual.transform, "Wisp", mat, wispColors, rate: 30f, life: new Vector2(0.7f, 1.1f), speed: new Vector2(0.05f, 0.2f),
                size: new Vector2(0.14f, 0.26f), shapeRadius: 0.08f, sphere: true, gravity: -0.06f, noise: 0.3f, playOnAwake: true);
            var coreColors = Grad(new[] { Color.white, new Color(0.85f, 0.97f, 1f) }, new[] { 0f, 1f }, new[] { 1f, 0f }, new[] { 0f, 1f });
            Particles(visual.transform, "Core", mat, coreColors, rate: 14f, life: new Vector2(0.25f, 0.35f), speed: Vector2.zero,
                size: new Vector2(0.12f, 0.16f), shapeRadius: 0.01f, sphere: true, gravity: 0f, noise: 0f, playOnAwake: true);

            foreach (var r in visual.GetComponentsInChildren<ParticleSystemRenderer>()) r.maxParticleSize = 0.08f;

            var guide = go.AddComponent<Guiding.GuidingLight>();
            GenUtil.Set(guide, "glow", light);
            GenUtil.Set(guide, "visual", visual.transform);

            GenUtil.SetLayerRecursive(go, LayerMask.NameToLayer(GameLayers.Interactable));
            GenUtil.SavePrefab(go, GuidingLightPath);
        }

        // ---------------- shadow creature ----------------

        public const string TownsfolkFigure = "PF_ShadowTownsfolk";
        public const string VoidRelief = "PF_ShadowVoidRelief";
        public const string LightShaft = "PF_LightShaft";
        public const string DustVolume = "PF_DustMotes";
        public static string TownsfolkFigurePath => $"{GenUtil.ShadowPrefabDir}/{TownsfolkFigure}.prefab";
        public static string VoidReliefPath => $"{GenUtil.ShadowPrefabDir}/{VoidRelief}.prefab";
        public static string LightShaftPath => $"{GenUtil.LightingPrefabDir}/{LightShaft}.prefab";
        public static string DustVolumePath => $"{GenUtil.LightingPrefabDir}/{DustVolume}.prefab";

        private static int SurfaceMask => LayerMask.GetMask("Default", GameLayers.ShadowSurface, GameLayers.Ground, GameLayers.LightBlocker);

        private static ShadowSurfaceSkin AddSkin(Transform parent, Material material)
        {
            var skinGo = GenUtil.Child("Skin", parent);
            skinGo.AddComponent<MeshFilter>();
            var r = skinGo.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            var skin = skinGo.AddComponent<ShadowSurfaceSkin>();
            GenUtil.Set(skin, "surfaceMask", SurfaceMask);
            return skin;
        }

        private static void BuildShadowEnemy()
        {
            var go = new GameObject(ShadowEnemy) { tag = GameTags.ShadowEnemy };
            var skin = AddSkin(go.transform, MaterialLibrary.Get(MaterialLibrary.ShadowMat));

            // dark wisps peeling off the body
            var smokeColors = Grad(new[] { Color.white, new Color(0.6f, 0.4f, 0.4f) }, new[] { 0f, 1f }, new[] { 0f, 0.9f, 0f }, new[] { 0f, 0.2f, 1f });
            var smoke = Particles(go.transform, "Smoke", MaterialLibrary.Get(MaterialLibrary.ShadowSmokeMat), smokeColors, rate: 6f,
                life: new Vector2(1.4f, 2.4f), speed: new Vector2(0.1f, 0.35f), size: new Vector2(0.4f, 0.9f), shapeRadius: 0.7f,
                sphere: true, gravity: -0.02f, noise: 0.35f, playOnAwake: true);

            var whisperGo = GenUtil.Child("Whisper", go.transform);
            var whisper = whisperGo.AddComponent<AudioSource>();
            whisper.clip = AudioGenerator.Load("SFX_ShadowWhisper");
            whisper.loop = true;
            whisper.playOnAwake = true;
            whisper.spatialBlend = 1f;
            whisper.rolloffMode = AudioRolloffMode.Linear;
            whisper.minDistance = 1.5f;
            whisper.maxDistance = 14f;
            whisper.volume = 0f;

            // the vine body is built at runtime by ShadowBody; black smoke peels off it
            var wispColors = Grad(new[] { Color.white, new Color(0.5f, 0.3f, 0.3f) }, new[] { 0f, 1f }, new[] { 0f, 0.8f, 0f }, new[] { 0f, 0.2f, 1f });
            var wisps = Particles(go.transform, "Wisps", MaterialLibrary.Get(MaterialLibrary.ShadowSmokeMat), wispColors, rate: 6f,
                life: new Vector2(1.2f, 2f), speed: new Vector2(0.05f, 0.3f), size: new Vector2(0.2f, 0.5f), shapeRadius: 0.3f,
                sphere: true, gravity: -0.05f, noise: 0.4f, playOnAwake: true);
            var wispShape = wisps.shape;
            wispShape.shapeType = ParticleSystemShapeType.Box;
            var body = go.AddComponent<ShadowBody>();
            GenUtil.Set(body, "vineMaterial", MaterialLibrary.Get(MaterialLibrary.ShadowVineMat));
            GenUtil.Set(body, "heartMaterial", MaterialLibrary.Get(MaterialLibrary.ShadowHeartMat));
            GenUtil.Set(body, "wisps", wisps);

            var enemy = go.AddComponent<ShadowEnemy>();
            GenUtil.Set(enemy, "body", body);
            GenUtil.Set(enemy, "skin", skin);
            GenUtil.Set(enemy, "smoke", smoke);
            GenUtil.Set(enemy, "whisper", whisper);
            GenUtil.Set(enemy, "lungeClip", AudioGenerator.Load("SFX_Lunge"));
            GenUtil.Set(enemy, "surfaceMask", SurfaceMask);
            GenUtil.Set(enemy, "occluderMask", (int)~LayerMask.GetMask(GameLayers.Player, GameLayers.ShadowEnemy, GameLayers.Interactable, "Ignore Raycast"));

            GenUtil.SetLayerRecursive(go, LayerMask.NameToLayer(GameLayers.ShadowEnemy));
            GenUtil.SavePrefab(go, ShadowEnemyPath);
        }

        private static void BuildShadowFigures()
        {
            var town = new GameObject(TownsfolkFigure);
            var townSkin = AddSkin(town.transform, MaterialLibrary.Get(MaterialLibrary.TownsfolkMat));
            var figure = town.AddComponent<ShadowFigure>();
            GenUtil.Set(figure, "skin", townSkin);
            GenUtil.Set(figure, "surfaceMask", SurfaceMask);
            GenUtil.SetLayerRecursive(town, LayerMask.NameToLayer(GameLayers.ShadowEnemy));
            GenUtil.SavePrefab(town, TownsfolkFigurePath);

            var relief = new GameObject(VoidRelief);
            var reliefSkin = AddSkin(relief.transform, MaterialLibrary.Get(MaterialLibrary.ShadowMat));
            var rf = relief.AddComponent<ShadowFigure>();
            GenUtil.Set(rf, "skin", reliefSkin);
            GenUtil.Set(rf, "sheet", (int)ShadowFigure.Sheet.Creature);
            GenUtil.Set(rf, "height", 9f);
            GenUtil.Set(rf, "glow", 1.3f);
            GenUtil.Set(rf, "opacity", 0.92f);
            GenUtil.Set(rf, "animateIdle", true);
            GenUtil.Set(rf, "idleFps", 0.8f);
            GenUtil.Set(rf, "surfaceMask", SurfaceMask);
            GenUtil.SetLayerRecursive(relief, LayerMask.NameToLayer(GameLayers.ShadowEnemy));
            GenUtil.SavePrefab(relief, VoidReliefPath);
        }

        // ---------------- atmosphere ----------------

        private static void BuildAtmosphere()
        {
            var mesh = GenUtil.SaveAsset(ShaftMesh(14), $"{GenUtil.PropMeshDir}/SM_LightShaft.asset");
            var shaft = new GameObject(LightShaft);
            var body = GenUtil.Child("Shaft", shaft.transform, Vector3.zero);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = body.AddComponent<MeshRenderer>();
            r.sharedMaterial = MaterialLibrary.Get(MaterialLibrary.LightShaftMat);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            var motes = DustParticles(shaft.transform, new Vector3(1.6f, 1f, 1.6f), 10f, new Color(0.75f, 0.85f, 1f, 0.7f));
            motes.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            GenUtil.SavePrefab(shaft, LightShaftPath);

            var dust = new GameObject(DustVolume);
            DustParticles(dust.transform, new Vector3(1f, 1f, 1f), 30f, new Color(1f, 0.75f, 0.5f, 0.55f));
            GenUtil.SavePrefab(dust, DustVolumePath);
        }

        private static ParticleSystem DustParticles(Transform parent, Vector3 box, float rate, Color tint)
        {
            var colors = Grad(new[] { tint, tint }, new[] { 0f, 1f }, new[] { 0f, tint.a, 0f }, new[] { 0f, 0.5f, 1f });
            var ps = Particles(parent, "Dust", MaterialLibrary.Get(MaterialLibrary.DustMat), colors, rate, new Vector2(6f, 10f),
                new Vector2(0.01f, 0.05f), new Vector2(0.012f, 0.03f), 0.5f, false, 0f, 0.08f, playOnAwake: true);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            shape.rotation = Vector3.zero;
            var main = ps.main;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.prewarm = true;
            var noise = ps.noise;
            noise.frequency = 0.3f;
            return ps;
        }

        /// <summary>Open frustum, 1 unit tall, bottom radius 1, top radius 0.45; uv.y runs bottom to top.</summary>
        private static Mesh ShaftMesh(int sides)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts.Add(dir * 1f);
                verts.Add(dir * 0.45f + Vector3.up);
                normals.Add(dir);
                normals.Add(dir);
                uvs.Add(new Vector2(i / (float)sides, 0f));
                uvs.Add(new Vector2(i / (float)sides, 1f));
            }
            for (int i = 0; i < sides; i++)
            {
                int b = i * 2;
                tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
            }
            var m = new Mesh { name = "SM_LightShaft" };
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        // ---------------- player ----------------

        // Hand pose tuning for the first-person IK (humanoid hand space). Tweak here, then re-run step 3.
        public static Vector3 RightGripEuler = new(0f, -90f, -90f);
        public static Vector3 RightGripOffset = new(0f, -0.02f, -0.05f);
        public static Vector3 LeftCoverEuler = new(80f, 0f, 90f);

        private static void BuildPlayer()
        {
            var go = new GameObject(Player) { tag = GameTags.Player };
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;

            var footsteps = go.AddComponent<AudioSource>();
            footsteps.playOnAwake = false;
            footsteps.spatialBlend = 0f;
            footsteps.volume = 0.55f;

            var cameraRoot = GenUtil.Child("CameraRoot", go.transform, new Vector3(0, 1.6f, 0));
            var camGo = GenUtil.Child("PlayerCamera", cameraRoot.transform);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var handSocket = GenUtil.Child("HandSocket", camGo.transform, new Vector3(0.21f, -0.21f, 0.4f), new Vector3(18f, 0f, 18f));
            var coverTarget = GenUtil.Child("CoverHandTarget", camGo.transform, new Vector3(0.2f, -0.75f, 0.35f));

            var seeker = GenUtil.Child("SeekerAudio", go.transform);
            var seekerAudio = seeker.AddComponent<AudioSource>();
            seekerAudio.clip = AudioGenerator.Load("SFX_ShadowWhisper");
            seekerAudio.playOnAwake = false;
            seekerAudio.loop = true;
            seekerAudio.spatialBlend = 1f;
            seekerAudio.rolloffMode = AudioRolloffMode.Linear;
            seekerAudio.minDistance = 2f;
            seekerAudio.maxDistance = 30f;

            var heartGo = GenUtil.Child("Heartbeat", go.transform);
            var heartbeat = heartGo.AddComponent<AudioSource>();
            heartbeat.clip = AudioGenerator.Real("SFX_Heartbeat.wav", "SFX_Heartbeat");
            heartbeat.loop = true;
            heartbeat.playOnAwake = false;
            heartbeat.spatialBlend = 0f;

            var input = go.AddComponent<PlayerInputReader>();
            GenUtil.Set(input, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(GenUtil.InputAsset));

            var fpc = go.AddComponent<FirstPersonController>();
            GenUtil.Set(fpc, "cameraRoot", cameraRoot.transform);
            GenUtil.Set(fpc, "footstepSource", footsteps);
            string[] stepFiles = { "SFX_Step_0.ogg", "SFX_Step_3.ogg", "SFX_Step_6.ogg" };
            var steps = new Object[stepFiles.Length];
            for (int i = 0; i < steps.Length; i++) steps[i] = AudioGenerator.Real(stepFiles[i], $"SFX_Step_{i + 1}");
            GenUtil.SetArray(fpc, "footstepClips", steps);

            var interactor = go.AddComponent<PlayerInteractor>();
            GenUtil.Set(interactor, "view", camGo.transform);

            var inventory = go.AddComponent<PlayerInventory>();
            GenUtil.Set(inventory, "handSocket", handSocket.transform);
            GenUtil.Set(inventory, "view", camGo.transform);
            GenUtil.Set(inventory, "startingItemPrefab", GenUtil.LoadPrefab(TorchPath).GetComponent<CarryableItem>());

            var carrier = go.AddComponent<FireCarrier>();
            GenUtil.Set(carrier, "view", camGo.transform);
            GenUtil.Set(carrier, "coverHand", coverTarget.transform);

            var death = go.AddComponent<PlayerDeath>();
            GenUtil.Set(death, "seekerAudio", seekerAudio);
            GenUtil.Set(death, "heartbeat", heartbeat);

            // the main character as the first-person body (arms holding the torch, legs underfoot)
            var bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterImporter.CharacterPrefabDir}/PF_MainCharacter.prefab");
            if (bodyPrefab != null)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab, go.transform);
                body.name = "Body";
                var fpBody = go.AddComponent<FirstPersonBody>();
                GenUtil.Set(fpBody, "bodyAnimator", body.GetComponentInChildren<Animator>());
                GenUtil.Set(fpBody, "bodyController", AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{CharacterImporter.AnimatorDir}/AC_MainCharacter.controller"));
                GenUtil.Set(fpBody, "view", camGo.transform);
                GenUtil.Set(fpBody, "rightHandTarget", handSocket.transform);
                GenUtil.Set(fpBody, "leftHandCoverTarget", coverTarget.transform);
                GenUtil.Set(fpBody, "firstPersonMaterial", AssetDatabase.LoadAssetAtPath<Material>($"{GenUtil.MaterialDir}/M_MainCharacter_FP.mat"));
                GenUtil.Set(fpBody, "rightGripEuler", RightGripEuler);
                GenUtil.Set(fpBody, "rightGripOffset", RightGripOffset);
                GenUtil.Set(fpBody, "leftCoverEuler", LeftCoverEuler);
            }
            else Debug.LogWarning("[Gen] PF_MainCharacter not found: player has no body. Run step 5 (Character Prefabs) first.");

            GenUtil.SetLayerRecursive(go, LayerMask.NameToLayer(GameLayers.Player));
            GenUtil.SavePrefab(go, PlayerPath);
        }

        // ---------------- fire VFX ----------------

        public static (Light, ParticleSystem[]) AddFlame(Transform parent, Vector3 localPos, float scale, float range, float intensity, Color color, bool startPlaying)
        {
            var root = GenUtil.Child("Flame", parent, localPos);
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowNearPlane = 0.1f;

            var mat = MaterialLibrary.Get(MaterialLibrary.FlameMat);
            var flameColors = Grad(
                new[] { new Color(1f, 0.95f, 0.8f), new Color(1f, 0.62f, 0.22f), new Color(0.85f, 0.22f, 0.05f), new Color(0.3f, 0.05f, 0.02f) },
                new[] { 0f, 0.3f, 0.7f, 1f }, new[] { 0f, 1f, 0.7f, 0f }, new[] { 0f, 0.12f, 0.5f, 1f });
            var flame = Particles(root.transform, "Flame_Core", mat, flameColors, rate: 38f, life: new Vector2(0.35f, 0.6f),
                speed: new Vector2(0.22f, 0.5f) * scale, size: new Vector2(0.08f, 0.16f) * scale, shapeRadius: 0.02f * scale,
                sphere: false, gravity: -0.12f, noise: 0.22f * scale);

            var emberColors = Grad(new[] { new Color(1f, 0.8f, 0.4f), new Color(1f, 0.35f, 0.08f) }, new[] { 0f, 1f },
                new[] { 1f, 0f }, new[] { 0.6f, 1f });
            var embers = Particles(root.transform, "Embers", mat, emberColors, rate: 5f * Mathf.Max(1f, scale), life: new Vector2(0.8f, 1.5f),
                speed: new Vector2(0.4f, 0.9f) * scale, size: new Vector2(0.012f, 0.025f) * Mathf.Sqrt(scale), shapeRadius: 0.03f * scale,
                sphere: false, gravity: -0.08f, noise: 0.6f * scale);

            var audioGo = GenUtil.Child("Crackle", root.transform);
            var source = audioGo.AddComponent<AudioSource>();
            source.clip = AudioGenerator.Real("AMB_Fireplace.wav", "SFX_FireCrackle");
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.8f;
            source.maxDistance = 9f + scale * 2f;
            var flameAudio = audioGo.AddComponent<NasconGJ.Audio.FlameAudio>();
            GenUtil.Set(flameAudio, "volume", Mathf.Clamp(0.35f + scale * 0.15f, 0.35f, 0.75f));
            GenUtil.Set(flameAudio, "igniteClip", AudioGenerator.Real("SFX_FireCrackleShort.wav", "SFX_Ignite"));

            light.enabled = startPlaying;
            return (light, new[] { flame, embers });
        }

        private static ParticleSystem Particles(Transform parent, string name, Material mat, Gradient colors, float rate, Vector2 life,
            Vector2 speed, Vector2 size, float shapeRadius, bool sphere, float gravity, float noise, bool playOnAwake = false)
        {
            var go = GenUtil.Child(name, parent);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = playOnAwake;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = sphere ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Cone;
            shape.angle = 7f;
            shape.radius = Mathf.Max(0.001f, shapeRadius);
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = colors;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.15f)));

            if (noise > 0f)
            {
                var n = ps.noise;
                n.enabled = true;
                n.strength = noise;
                n.frequency = 2.2f;
                n.scrollSpeed = 1.5f;
                n.quality = ParticleSystemNoiseQuality.Medium;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        public static Gradient Grad(Color[] colors, float[] colorTimes, float[] alphas, float[] alphaTimes)
        {
            var g = new Gradient();
            var ck = new GradientColorKey[colors.Length];
            for (int i = 0; i < colors.Length; i++) ck[i] = new GradientColorKey(colors[i], colorTimes[i]);
            var ak = new GradientAlphaKey[alphas.Length];
            for (int i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i], alphaTimes[i]);
            g.SetKeys(ck, ak);
            return g;
        }

        private static Mesh SaveProp(string name, MeshBuilder b, out MatId[] slots) =>
            GenUtil.SaveAsset(b.Build(name, out slots), $"{GenUtil.PropMeshDir}/{name}.asset");
    }
}
