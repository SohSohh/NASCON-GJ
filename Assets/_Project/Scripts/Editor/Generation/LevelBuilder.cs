using System.Collections.Generic;
using NasconGJ.Core;
using NasconGJ.Cutscenes;
using NasconGJ.Entities;
using NasconGJ.Fire;
using NasconGJ.Guiding;
using NasconGJ.Items;
using NasconGJ.Player;
using NasconGJ.Puzzles;
using NasconGJ.Tutorial;
using NasconGJ.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using K = NasconGJ.EditorTools.Generation.EnvironmentKitGenerator;
using G = NasconGJ.EditorTools.Generation.GameplayPrefabGenerator;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Assembles the starter scenes from the generated kit. Scenes are OVERWRITTEN on rebuild,
    /// so once a dev starts hand-editing a level, stop rebuilding it (or remove it from BuildAll).
    /// </summary>
    public static partial class LevelBuilder
    {
        private const string Scenes = "Assets/_Project/Scenes/";
        private static readonly Dictionary<string, GameObject> prefabCache = new();

        private class Scaffold
        {
            public Transform Systems, Environment, Greybox, Props, Lighting, Torches, Volumes, Gameplay, Spawn, Guide, Items, Enemies, Puzzle, Tutorial, UI, Audio;
            public GameObject Hud;
        }

        public static void BuildAll()
        {
            prefabCache.Clear();
            BuildMainMenu();
            BuildPrologue();
            BuildPrologueTown();
            // L01-L03 are hand-edited by the team now: never regenerate them here (BuildL01 is still callable on its own)
            BuildPlaceholderLevel(SceneNames.DummyShadows, "L04 — Dummy Shadows", "Many props cast shadows. One of them is alive.\nFind it and block it.");
            BuildPlaceholderLevel(SceneNames.Crystal, "L05 — Crystal", "Put out every torch around the crystal,\nand your own flame, to take it.");
            BuildPlaceholderLevel(SceneNames.Escape, "L06 — Escape", "Chase sequence toward the bright light.");
            foreach (var dev in new[] { "A", "B", "C" }) BuildSandbox(dev);
            EditorSceneManager.OpenScene(Scenes + "Levels/" + SceneNames.TombEntrance + ".unity");
        }

        // =====================================================================
        //  L01 — Tomb Entrance
        //  The Fall (pit) > The Crawl (crouch) > The Narrows (pass the flame) > The Draft (shield it) > Hall of Echoes (shadows) > exit
        // =====================================================================

        private static readonly Color Moon = new(0.55f, 0.6f, 0.95f);

        private static void BuildL01()
        {
            var s = NewGameplayScene();
            AddAmbience(s, new Vector3(0, 4, 30), new Vector3(24, 10, 60));

            // ---------- A. The Fall: a 12 m shaft you dropped down, moonlight from far above
            Floor(s, 0, 0);
            for (int layer = 0; layer < 3; layer++)
            {
                WallX(s, 0, -2, layer);
                WallZ(s, -2, 0, layer);
                WallZ(s, 2, 0, layer);
                WallX(s, 0, 2, layer, door: layer == 0);
            }
            Place(K.SlabCollapsed, s.Props, new Vector3(0f, 1.32f, 2.05f), 180f);            // caved-in doorway: only a crawlspace left
            Place(K.SlabCollapsed, s.Props, new Vector3(-0.9f, 0f, -0.6f), 40f);             // what fell with you
            Place(K.RubbleB, s.Props, new Vector3(1.1f, 0, -1.3f), 30);
            Place(K.RockSmall, s.Props, new Vector3(-1.3f, 0, 1.2f), 75);
            Place(K.RubbleA, s.Props, new Vector3(1.4f, 0, 1.3f), 140);
            var moon = MakeLight("Moonlight", s.Lighting, LightType.Spot, new Vector3(0.3f, 13f, -0.4f), Moon, 45f, 16f);
            moon.transform.rotation = Quaternion.Euler(88f, 20f, 0f);
            moon.spotAngle = 30f;
            moon.innerSpotAngle = 12f;
            Shaft(s, new Vector3(0.25f, 0f, -0.35f), 12.5f, 1.2f);

            // the torch you dropped in the fall: lit, but already half spent
            var dropped = Place(G.TorchPath, s.Items, new Vector3(0.75f, 0.06f, 0.35f), 0, new Vector3(0, 30f, 90f));
            var droppedFlame = dropped.GetComponent<FlammableItem>();
            GenUtil.Set(droppedFlame, "startLit", true);
            GenUtil.Set(droppedFlame, "startFuelFraction", 0.7f);


            // ---------- B. The Crawl: 2 m wide, ceiling at your shoulders
            foreach (float z in new[] { 4f, 8f })
            {
                Floor(s, 0, z);
                WallZ(s, -1.3f, z, 0);
                WallZ(s, 1.3f, z, 0);
                Ceiling(s, 0, z, 1.45f);
            }
            Place(K.RockSmall, s.Props, new Vector3(0.75f, 0, 5.2f), 10);
            Place(K.RockSmall, s.Props, new Vector3(-0.8f, 0, 8.4f), 200);
            Place(K.RubbleA, s.Props, new Vector3(-0.6f, 0, 3.4f), 60);

            // ---------- C. The Narrows: a long tight corridor, a crack of moonlight over a stick
            foreach (float z in new[] { 12f, 16f, 20f })
            {
                Floor(s, 0, z);
                WallZ(s, -1.3f, z, 0);
                WallZ(s, 1.3f, z, 0);
                if (z != 16f) Ceiling(s, 0, z, 3f);
            }
            var crack = MakeLight("CrackLight", s.Lighting, LightType.Spot, new Vector3(0.2f, 9f, 16f), Moon, 28f, 11f);
            crack.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            crack.spotAngle = 26f;
            Shaft(s, new Vector3(0.2f, 0f, 16f), 7f, 0.75f);
            Place(K.BlockSmall, s.Props, new Vector3(0.45f, 0, 16.1f), 12);
            var stick = Place(G.StickPath, s.Items, new Vector3(0.45f, 0.68f, 16.1f), 0, new Vector3(0, 80f, 90f));
            Place(G.CandlePath, s.Items, new Vector3(-0.6f, 0.01f, 19.6f));
            Place(K.RubbleA, s.Props, new Vector3(0.6f, 0, 12.6f), 20);
            Figure(s, new Vector3(-0.98f, 0.02f, 18.4f), 90f, 2, 1.7f, false);   // the first silhouette: someone frozen mid-step

            // ---------- D. The Draft: wind from a fissure, shield the flame
            foreach (float z in new[] { 24f, 28f })
            {
                Floor(s, 0, z);
                WallZ(s, -1.3f, z, 0);
                Ceiling(s, 0, z, 3f);
            }
            WallZ(s, 1.3f, 24f, 0);
            Place(K.RockWall, s.Greybox, new Vector3(1.3f, 0, 28f), 90f);                     // the fissure the wind comes from
            Draft(s, new Vector3(0, 1.5f, 26.2f), new Vector3(2f, 3f, 6.8f));

            // ---------- E. The Hall of Echoes: vast, dark, wet; the first living shadow
            var freshTorch = BuildHall(s);

            // ---------- guiding light route
            var guide = Place(G.GuidingLightPath, s.Guide, new Vector3(0, 1.55f, 1.35f));
            SetGuideStops(guide,
                (Point("Stop_0_Fall", s.Guide, new Vector3(-0.6f, 1.9f, 1.5f)), DialogueLibrary.Load(DialogueLibrary.L01Intro)),
                (Point("Stop_1_Stick", s.Guide, new Vector3(-0.55f, 2.3f, 18.2f)), DialogueLibrary.Load(DialogueLibrary.L01Stick)),
                (Point("Stop_2_Draft", s.Guide, new Vector3(-0.5f, 2.3f, 26.5f)), DialogueLibrary.Load(DialogueLibrary.L01Draft)),
                (Point("Stop_3_Hall", s.Guide, new Vector3(-1.5f, 2.6f, 36f)), DialogueLibrary.Load(DialogueLibrary.L01Shadows)),
                (Point("Stop_4_Exit", s.Guide, new Vector3(7.4f, 2.0f, 55.6f)), DialogueLibrary.Load(DialogueLibrary.L01Onward)));

            // ---------- player: wakes on the floor, empty-handed
            var player = AddPlayer(s, new Vector3(0, 0.05f, -0.9f), 15f);
            GenUtil.Set(player.GetComponent<PlayerInventory>(), "startingItemPrefab", null);
            var wake = player.AddComponent<WakeUpSequence>();
            GenUtil.Set(wake, "wakeClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_WakeRinging.ogg"));
            GenUtil.Set(wake, "faceOnWake", dropped.transform);
            BuildIntroDirector(s, dropped, stick, freshTorch);
            GenUtil.Set(s.Hud.GetComponent<ScreenFader>(), "fadeInOnStart", 3.5f);

            Save(s, "Levels/" + SceneNames.TombEntrance);
        }

        private static GameObject BuildHall(Scaffold s)
        {
            float[] xs = { -8f, -4f, 0f, 4f, 8f };
            float[] zs = { 32f, 36f, 40f, 44f, 48f, 52f, 56f };
            foreach (float x in xs)
            foreach (float z in zs)
            {
                Floor(s, x, z);
                Ceiling(s, x, z, 16f);
            }
            for (int layer = 0; layer < 4; layer++)
            {
                foreach (float x in xs)
                {
                    WallX(s, x, 30f, layer, door: layer == 0 && x == 0f);
                    WallX(s, x, 58f, layer, door: layer == 0 && x == 8f);
                }
                foreach (float z in zs)
                {
                    bool rockWest = layer == 0 && (z == 40f || z == 52f);
                    bool rockEast = layer == 0 && (z == 36f || z == 48f);
                    // rock faces protrude along local -Z: rotate so they face into the hall
                    if (rockWest) Place(K.RockWall, s.Greybox, new Vector3(-10f, 0, z), -90f); else WallZ(s, -10f, z, layer);
                    if (rockEast) Place(K.RockWall, s.Greybox, new Vector3(10f, 0, z), 90f); else WallZ(s, 10f, z, layer);
                }
            }

            // colossal pillars into the dark, one fallen
            foreach (float x in new[] { -5f, 5f })
            foreach (float z in new[] { 36f, 44f, 52f })
            {
                if (x > 0 && z == 44f)
                {
                    Place(K.PillarBroken, s.Props, new Vector3(x, 0, z), 30f);
                    Place(K.RockLarge, s.Props, new Vector3(x - 1.2f, 0, z + 1.6f), 70f);
                    continue;
                }
                for (int i = 0; i < 4; i++) Place(K.Pillar, s.Props, new Vector3(x, i * 4f, z));
            }
            Place(K.RockLarge, s.Props, new Vector3(-7.2f, 0, 42f), 15f);
            Place(K.RockLarge, s.Props, new Vector3(7.6f, 0, 33.5f), 200f);
            Place(K.RockSmall, s.Props, new Vector3(-2.8f, 0, 48.5f), 40f);
            Place(K.RockSmall, s.Props, new Vector3(3.4f, 0, 38.6f), 110f);
            Place(K.RubbleB, s.Props, new Vector3(-6.2f, 0, 55f), 10f);
            Place(K.RubbleB, s.Props, new Vector3(1.4f, 0, 41.5f), 80f);
            Place(K.BlockLarge, s.Props, new Vector3(-2.2f, 0, 45f), 12f);
            Place(K.Obelisk, s.Props, new Vector3(-8.3f, 0, 31.6f), 8f);
            Place(K.Obelisk, s.Props, new Vector3(8.3f, 0, 31.6f), -6f);
            Place(K.Altar, s.Props, new Vector3(0f, 0, 55.4f), 0f);
            float chainYaw = 0f;
            foreach (var p in new[] { new Vector3(-2.5f, 15.8f, 40f), new Vector3(2.5f, 15.8f, 47f), new Vector3(-0.5f, 15.8f, 53f) })
                Place(K.Chain, s.Props, p, chainYaw += 37f);

            // cold light falling through cracks far above
            foreach (var (p, r) in new[] { (new Vector3(-3f, 0f, 40f), 1.6f), (new Vector3(4f, 0f, 50f), 1.3f) })
            {
                var l = MakeLight("CrackLight", s.Lighting, LightType.Spot, p + Vector3.up * 16f, Moon, 38f, 20f);
                l.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                l.spotAngle = 18f;
                Shaft(s, p, 16f, r);
            }
            // faint cold fill high above so the scale of the hall reads as silhouettes, plus two fires already burning
            var fill = MakeLight("HallFill", s.Lighting, LightType.Point, new Vector3(0f, 13f, 44f), Moon, 7f, 34f);
            fill.shadows = LightShadows.None;
            Place(G.BrazierLitPath, s.Torches, new Vector3(-7.6f, 0, 33f));
            Place(G.BrazierLitPath, s.Torches, new Vector3(-1.6f, 0, 54.6f));
            var dust = Place(G.DustVolumePath, s.Lighting, new Vector3(0, 5f, 44f));
            dust.transform.localScale = new Vector3(18f, 10f, 26f);

            // braziers: unlit ones along the way (light them to push the dark back), lit pair at the exit
            Place(G.BrazierUnlitPath, s.Torches, new Vector3(-1.8f, 0, 37f));
            Place(G.BrazierUnlitPath, s.Torches, new Vector3(2.6f, 0, 44.6f));
            Place(G.BrazierUnlitPath, s.Torches, new Vector3(5.2f, 0, 51.6f));
            Place(G.BrazierLitPath, s.Torches, new Vector3(6.6f, 0, 57f));
            Place(G.BrazierLitPath, s.Torches, new Vector3(9.3f, 0, 57f));
            Place(G.StickPath, s.Items, new Vector3(-0.8f, 0.05f, 42.2f), 0, new Vector3(0, 20f, 90f));

            // what the Void left on the walls
            Figure(s, new Vector3(-9.68f, 0.02f, 34.5f), 90f, 0, 1.75f, false);
            Figure(s, new Vector3(-9.68f, 0.02f, 36.6f), 90f, 1, 1.65f, true);
            Figure(s, new Vector3(-9.68f, 0.02f, 46.4f), 90f, 2, 1.6f, false);
            Figure(s, new Vector3(9.68f, 0.02f, 40.5f), -90f, 1, 1.7f, false);
            var relief = Place(G.VoidReliefPath, s.Enemies, new Vector3(0f, 0.6f, 57.68f), 180f);
            relief.name = "Void_Relief";

            // the living ones
            Enemy(s, new Vector3(9.68f, 0.02f, 45f), -90f, 12f);
            Enemy(s, new Vector3(4.6f, 0.02f, 57.68f), 180f, 5f);


            // exit: a warm glow beyond the north-east door
            Floor(s, 8, 60);
            WallZ(s, 6, 60, 0);
            WallZ(s, 10, 60, 0);
            WallX(s, 8, 62, 0);
            Ceiling(s, 8, 60, 4f);
            MakeLight("ExitGlow", s.Lighting, LightType.Point, new Vector3(8f, 2.4f, 61f), new Color(1f, 0.86f, 0.62f), 18f, 8f);
            Exit(s, new Vector3(8f, 1.5f, 60.3f), new Vector3(3.6f, 3f, 1f));

            // a fresh, full torch just inside the hall
            return Place(G.TorchPath, s.Items, new Vector3(1.5f, 0.06f, 33.4f), 0, new Vector3(0, 40f, 90f));
        }

        // =====================================================================
        //  L02–L06 placeholders: a 12x12 room with spawn, two braziers and an exit
        // =====================================================================

        private static void BuildPlaceholderLevel(string sceneName, string title, string brief)
        {
            var s = NewGameplayScene();
            AddAmbience(s, new Vector3(0, 3, 6), new Vector3(12, 6, 12));
            Room(s, 0, 6, 3, 3, layers: 2, ceilingY: 8f, doorNorth: true);

            Floor(s, 0, 14);
            WallZ(s, -2, 14, 0);
            WallZ(s, 2, 14, 0);
            WallX(s, 0, 16, 0);
            Ceiling(s, 0, 14, 4f);
            MakeLight("ExitGlow", s.Lighting, LightType.Point, new Vector3(0, 2.4f, 15f), new Color(1f, 0.86f, 0.62f), 15f, 7f);
            Exit(s, new Vector3(0, 1.5f, 14.4f), new Vector3(3.6f, 3f, 1f));

            Place(G.BrazierLitPath, s.Torches, new Vector3(-4.6f, 0, 10.6f));
            Place(G.BrazierLitPath, s.Torches, new Vector3(4.6f, 0, 10.6f));
            Place(G.StickPath, s.Items, new Vector3(1.5f, 0.05f, 3f), 0, new Vector3(0, 40f, 90f));

            Label(s, $"{title}\n<size=55%>{brief}\n\nWIP — build this level here.</size>", new Vector3(-5.66f, 3.2f, 6f), new Vector3(0, -90f, 0));

            AddPlayer(s, new Vector3(0, 0.05f, 1.6f), 0f);
            Save(s, "Levels/" + sceneName);
        }

        // =====================================================================
        //  Sandboxes: every mechanic in one 20x20 room
        // =====================================================================

        private static void BuildSandbox(string dev)
        {
            var s = NewGameplayScene();
            AddAmbience(s, new Vector3(0, 2, 0), new Vector3(20, 4, 20));
            Room(s, 0, 0, 5, 5, layers: 1, ceilingY: 4f, doorNorth: false);

            Place(G.TorchPath, s.Items, new Vector3(-3f, 0.05f, -5f), 0, new Vector3(0, 0, 90f));
            Place(G.StickPath, s.Items, new Vector3(-1.5f, 0.05f, -5f), 0, new Vector3(0, 0, 90f));
            Place(G.StickPath, s.Items, new Vector3(0f, 0.05f, -5f), 0, new Vector3(0, 0, 90f));
            Place(G.CandlePath, s.Items, new Vector3(1.5f, 0.0f, -5f));
            Place(G.CandlePath, s.Items, new Vector3(3f, 0.0f, -5f));

            Place(G.BrazierLitPath, s.Torches, new Vector3(-7f, 0, 0f));
            Place(G.BrazierUnlitPath, s.Torches, new Vector3(7f, 0, 0f));
            Place(G.BrazierUnlitPath, s.Torches, new Vector3(3f, 0, 7.5f));
            Place(K.Pillar, s.Props, new Vector3(-3.5f, 0, 2.5f));
            Place(K.PillarBroken, s.Props, new Vector3(3.5f, 0, 2.5f), 30);
            Place(K.BlockLarge, s.Props, new Vector3(0f, 0, 3.5f), 15);
            Place(K.BlockSmall, s.Props, new Vector3(-1.6f, 0, 5.5f));
            Place(K.Stairs, s.Props, new Vector3(-6f, 0, 5f), 0);
            Place(K.Altar, s.Props, new Vector3(6f, 0, -6.5f), 90);
            Place(K.Obelisk, s.Props, new Vector3(-8.2f, 0, -8.2f));
            Place(K.RockLarge, s.Props, new Vector3(7.5f, 0, 7.5f));
            Place(K.RubbleB, s.Props, new Vector3(-8f, 0, 8.2f));

            Enemy(s, new Vector3(0f, 0.02f, 9.68f), 180f, 9f);
            Figure(s, new Vector3(9.68f, 0.02f, -3f), -90f, 0, 1.75f, false);
            Draft(s, new Vector3(-7f, 1.5f, -7f), new Vector3(4f, 3f, 4f));

            var guide = Place(G.GuidingLightPath, s.Guide, new Vector3(-4f, 1.7f, -7f));
            SetGuideStops(guide, (Point("Stop_0", s.Guide, new Vector3(-4f, 1.7f, -7f)), DialogueLibrary.Load(DialogueLibrary.Sandbox)));

            PlaceCharacter(s.Props, new Vector3(-5.6f, 0, -1.6f), 125f, holdTorch: true, lookAt: new Vector3(0, 1.5f, 9.7f));

            Label(s, $"SANDBOX {dev}\n<size=50%>Your personal test room. Not in the build.\nBreak anything you like.\n(draft zone in the SW corner)</size>", new Vector3(-9.66f, 2.6f, 0f), new Vector3(0, -90f, 0));

            AddPlayer(s, new Vector3(0, 0.05f, -8f), 0f);
            Save(s, "Sandbox/Sandbox_Dev" + dev);
        }

        // =====================================================================
        //  Main menu: the bearer before the Void
        // =====================================================================

        private static void BuildMainMenu()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyRenderSettings();
            var env = Group("ENVIRONMENT");
            var lighting = Group("LIGHTING");
            var ui = Group("UI");
            var systems = Group("SYSTEMS");
            var audio = Group("AUDIO");

            foreach (float x in new[] { -8f, -4f, 0f, 4f, 8f })
            foreach (float z in new[] { -2f, 2f, 6f })
                Place(K.Floor, env, new Vector3(x, 0, z));
            for (int layer = 0; layer < 3; layer++)
            {
                foreach (float x in new[] { -8f, -4f, 0f, 4f, 8f }) Place(K.Wall, env, new Vector3(x, layer * 4f, 9.6f));
                foreach (float z in new[] { -2f, 2f, 6f, 10f })
                {
                    Place(K.Wall, env, new Vector3(-10f, layer * 4f, z), 90f);
                    Place(K.Wall, env, new Vector3(10f, layer * 4f, z), 90f);
                }
            }
            foreach (float x in new[] { -5.2f, 5.6f })
                for (int i = 0; i < 3; i++) Place(K.Pillar, env, new Vector3(x, i * 4f, 5.4f));
            Place(K.PillarBroken, env, new Vector3(-3.2f, 0, 2.2f), 25);
            Place(K.RockLarge, env, new Vector3(-6.8f, 0, 1.5f), 40);
            Place(K.RubbleB, env, new Vector3(0.9f, 0, 1.0f), 60);
            Place(K.RubbleA, env, new Vector3(3.4f, 0, 6.8f), 10);
            Place(K.Obelisk, env, new Vector3(7.2f, 0, 7.6f), -10);
            Place(K.Chain, env, new Vector3(-2f, 12f, 6.5f), 0);
            Place(K.Chain, env, new Vector3(2.8f, 12f, 7.5f), 45);

            Place(G.BrazierLitPath, lighting, new Vector3(-2.7f, 0, 8.2f));
            Place(G.BrazierLitPath, lighting, new Vector3(3.9f, 0, 8.3f));
            MakeLight("WallWash", lighting, LightType.Point, new Vector3(0.6f, 1.6f, 7.4f), new Color(1f, 0.62f, 0.32f), 9f, 9f); // lights the wall so the Void reads as a silhouette

            // the giant shadow on the far wall and one that moves
            var menuRelief = Place(G.VoidReliefPath, env, new Vector3(0.6f, 0.1f, 9.28f), 180f);
            GenUtil.Set(menuRelief.GetComponent<ShadowFigure>(), "height", 7.4f);
            var lurker = Place(G.ShadowEnemyPath, env, new Vector3(-7.2f, 0.02f, 9.28f), 180f);
            GenUtil.Set(lurker.GetComponent<ShadowEnemy>(), "leashRadius", 2.5f);

            // the bearer, torch raised, facing the Void
            PlaceCharacter(env, new Vector3(1.4f, 0, 3.4f), 10f, holdTorch: true, lookAt: new Vector3(0.6f, 5.5f, 9.3f));

            var shaftLight = MakeLight("Moonbeam", lighting, LightType.Spot, new Vector3(1.6f, 12f, 3.2f), Moon, 22f, 16f);
            shaftLight.transform.rotation = Quaternion.Euler(88f, 0f, 0f);
            shaftLight.spotAngle = 22f;
            var shaftGo = Place(G.LightShaftPath, lighting, new Vector3(1.5f, 0f, 3.3f));
            shaftGo.transform.localScale = new Vector3(1.1f, 12f, 1.1f);
            var dust = Place(G.DustVolumePath, lighting, new Vector3(0.5f, 3f, 4f));
            dust.transform.localScale = new Vector3(12f, 6f, 8f);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(systems, false);
            camGo.transform.position = new Vector3(-0.5f, 1.45f, -0.4f);
            camGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.9f, 2.9f, 9.3f) - camGo.transform.position);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 52f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<MenuCameraDrift>();

            var drone = new GameObject("TombDrone");
            drone.transform.SetParent(audio, false);
            var droneSource = drone.AddComponent<AudioSource>();
            droneSource.clip = AudioGenerator.Load("AMB_TombDrone");
            droneSource.loop = true;
            droneSource.playOnAwake = true;
            droneSource.volume = 0.35f;

            var volumes = new GameObject("Volumes").transform;
            volumes.SetParent(lighting, false);
            AddVolume(volumes);
            volumes.gameObject.AddComponent<ComicLook>();
            AddEventSystem(ui);
            PlaceAt(UIPrefabGenerator.MainMenuPath, ui, Vector3.zero, Quaternion.identity);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scenes + "Core/MainMenu.unity");
        }

        // =====================================================================
        //  Scaffolding
        // =====================================================================

        private static Scaffold NewGameplayScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyRenderSettings();

            var s = new Scaffold
            {
                Systems = Group("SYSTEMS"),
                Environment = Group("ENVIRONMENT"),
                Lighting = Group("LIGHTING"),
                Gameplay = Group("GAMEPLAY"),
                UI = Group("UI"),
                Audio = Group("AUDIO"),
            };
            s.Greybox = Sub("Kit", s.Environment);
            s.Props = Sub("Props_LightBlockers", s.Environment);
            Sub("Art", s.Environment);
            s.Torches = Sub("Braziers_Torches", s.Lighting);
            s.Volumes = Sub("Volumes", s.Lighting);
            s.Spawn = Sub("PlayerSpawn", s.Gameplay);
            s.Guide = Sub("GuidingLight", s.Gameplay);
            s.Items = Sub("Items", s.Gameplay);
            s.Enemies = Sub("Shadows", s.Gameplay);
            s.Puzzle = Sub("Puzzle", s.Gameplay);
            s.Tutorial = Sub("Tutorial", s.Gameplay);
            Sub("Checkpoints", s.Gameplay);

            AddVolume(s.Volumes);
            s.Volumes.gameObject.AddComponent<ComicLook>(); // ink outlines + toon colour (NASCON/ComicPost)
            AddEventSystem(s.UI);
            s.Hud = PlaceAt(UIPrefabGenerator.HUDPath, s.UI, Vector3.zero, Quaternion.identity);
            return s;
        }

        private static void ApplyRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.04f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.03f;
            RenderSettings.fogColor = new Color(0.035f, 0.02f, 0.04f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.15f;
        }

        private static void AddVolume(Transform parent)
        {
            var go = new GameObject("GlobalVolume_Tomb");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MaterialLibrary.VolumeProfilePath);
        }

        private static void AddEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Low drone everywhere plus drips somewhere in the dark.</summary>
        private static void AddAmbience(Scaffold s, Vector3 dripCenter, Vector3 dripArea)
        {
            var drone = Sub("TombDrone", s.Audio);
            var source = drone.gameObject.AddComponent<AudioSource>();
            source.clip = AudioGenerator.Load("AMB_TombDrone");
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.45f;

            source.volume = 0.25f;

            var music = Sub("Music", s.Audio);
            var amb = music.gameObject.AddComponent<AudioSource>();
            amb.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Music/MUS_MainAmbience.mp3");
            amb.loop = true;
            amb.playOnAwake = true;
            var pres = music.gameObject.AddComponent<AudioSource>();
            pres.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Music/MUS_Presence.mp3");
            pres.loop = true;
            pres.playOnAwake = true;
            pres.volume = 0f;
            var director = music.gameObject.AddComponent<Audio.MusicDirector>();
            GenUtil.Set(director, "ambience", amb);
            GenUtil.Set(director, "presence", pres);

            var drips = Sub("Drips", s.Audio);
            drips.position = dripCenter;
            var shots = drips.gameObject.AddComponent<Audio.RandomOneShots>();
            GenUtil.SetArray(shots, "clips", new Object[] { AudioGenerator.Load("SFX_Drip_1"), AudioGenerator.Load("SFX_Drip_2"), AudioGenerator.Load("SFX_Drip_3") });
            GenUtil.Set(shots, "area", dripArea);
        }

        private static GameObject AddPlayer(Scaffold s, Vector3 position, float yaw)
        {
            s.Spawn.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return PlaceAt(G.PlayerPath, s.Gameplay, position, Quaternion.Euler(0, yaw, 0));
        }

        private static void Save(Scaffold s, string relative) =>
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scenes + relative + ".unity");

        private static Transform Group(string name) => new GameObject("--- " + name + " ---").transform;

        private static Transform Sub(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Point(string name, Transform parent, Vector3 position)
        {
            var t = Sub(name, parent);
            t.position = position;
            return t;
        }

        // ---------------- kit placement ----------------

        private static GameObject Prefab(string pathOrKitName)
        {
            string path = pathOrKitName.EndsWith(".prefab") ? pathOrKitName : K.PathOf(pathOrKitName);
            if (!prefabCache.TryGetValue(path, out var prefab) || prefab == null)
                prefabCache[path] = prefab = GenUtil.LoadPrefab(path);
            return prefab;
        }

        private static GameObject PlaceAt(string pathOrKitName, Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(pathOrKitName), parent);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        private static GameObject Place(string pathOrKitName, Transform parent, Vector3 position, float yaw = 0f, Vector3? euler = null) =>
            PlaceAt(pathOrKitName, parent, position, Quaternion.Euler(euler ?? new Vector3(0, yaw, 0)));

        private static void Floor(Scaffold s, float x, float z) => Place(K.Floor, s.Greybox, new Vector3(x, 0, z));
        private static void Ceiling(Scaffold s, float x, float z, float y) => Place(K.Ceiling, s.Greybox, new Vector3(x, y, z));

        /// <summary>Wall running along X (south/north edge). Layer 0 = ground floor, 1+ = stacked on top.</summary>
        private static void WallX(Scaffold s, float x, float z, int layer, bool door = false) =>
            Place(door ? K.WallDoorway : K.Wall, s.Greybox, new Vector3(x, layer * 4f, z), 0f);

        /// <summary>Wall running along Z (west/east edge).</summary>
        private static void WallZ(Scaffold s, float x, float z, int layer, bool door = false) =>
            Place(door ? K.WallDoorway : K.Wall, s.Greybox, new Vector3(x, layer * 4f, z), 90f);

        /// <summary>Rectangular room of 4 m tiles centred on (cx, cz). Doors sit on the middle tile (use odd tile counts).</summary>
        private static void Room(Scaffold s, float cx, float cz, int tilesX, int tilesZ, int layers, float ceilingY, bool doorNorth)
        {
            float x0 = cx - tilesX * 2f, z0 = cz - tilesZ * 2f;
            for (int i = 0; i < tilesX; i++)
            for (int j = 0; j < tilesZ; j++)
            {
                Floor(s, x0 + 2 + i * 4, z0 + 2 + j * 4);
                Ceiling(s, x0 + 2 + i * 4, z0 + 2 + j * 4, ceilingY);
            }
            for (int layer = 0; layer < layers; layer++)
            {
                for (int i = 0; i < tilesX; i++)
                {
                    float x = x0 + 2 + i * 4;
                    WallX(s, x, z0, layer);
                    WallX(s, x, z0 + tilesZ * 4, layer, door: doorNorth && layer == 0 && i == tilesX / 2);
                }
                for (int j = 0; j < tilesZ; j++)
                {
                    float z = z0 + 2 + j * 4;
                    WallZ(s, x0, z, layer);
                    WallZ(s, x0 + tilesX * 4, z, layer);
                }
            }
        }

        /// <summary>Gates, zones and the IntroDirector that script the L01 onboarding.</summary>
        private static void BuildIntroDirector(Scaffold s, GameObject torch, GameObject stick, GameObject freshTorch)
        {
            var go = new GameObject("IntroDirector");
            go.transform.SetParent(s.Tutorial, false);
            var d = go.AddComponent<IntroDirector>();
            GenUtil.Set(d, "droppedTorch", torch.GetComponent<CarryableItem>());
            GenUtil.Set(d, "stick", stick.GetComponent<CarryableItem>());
            GenUtil.Set(d, "freshTorch", freshTorch.GetComponent<CarryableItem>());
            GenUtil.Set(d, "glintMaterial", MaterialLibrary.Get(MaterialLibrary.DustMat));
            GenUtil.Set(d, "pitGate", Gate(s, "Gate_Pit", new Vector3(0f, 1f, 2.05f), new Vector3(2.2f, 2.4f, 0.4f)));
            GenUtil.Set(d, "narrowsGate", Gate(s, "Gate_Narrows", new Vector3(0f, 1.5f, 21.9f), new Vector3(2.2f, 3f, 0.4f)));
            GenUtil.Set(d, "crawlStart", Zone(s, "Zone_CrawlStart", new Vector3(0f, 0.7f, 2.8f), new Vector3(2f, 1.4f, 1f)));
            GenUtil.Set(d, "narrowsStart", Zone(s, "Zone_NarrowsStart", new Vector3(0f, 1f, 10.9f), new Vector3(2f, 2f, 1f)));
            GenUtil.Set(d, "stickApproach", Zone(s, "Zone_StickApproach", new Vector3(0f, 1f, 14f), new Vector3(2f, 2f, 1.6f)));
            GenUtil.Set(d, "draftApproach", Zone(s, "Zone_DraftApproach", new Vector3(0f, 1f, 22.5f), new Vector3(2f, 2f, 0.8f)));
            GenUtil.Set(d, "draftExit", Zone(s, "Zone_DraftExit", new Vector3(0f, 1f, 29.6f), new Vector3(2f, 2f, 0.8f)));
            GenUtil.Set(d, "hallEntrance", Zone(s, "Zone_HallEntrance", new Vector3(0f, 1f, 31.6f), new Vector3(4f, 2f, 1.4f)));
            GenUtil.Set(d, "exitApproach", Zone(s, "Zone_ExitApproach", new Vector3(8f, 1f, 56.6f), new Vector3(4f, 2f, 1.6f)));
        }

        private static GameObject Gate(Scaffold s, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(s.Tutorial, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size; // invisible wall until the lesson is done
            return go;
        }

        private static TutorialZone Zone(Scaffold s, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(s.Tutorial, false);
            go.transform.position = center;
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go.AddComponent<TutorialZone>();
        }

        // ---------------- gameplay helpers ----------------

        private static Light MakeLight(string name, Transform parent, LightType type, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.Soft;
            return l;
        }

        private static void Shaft(Scaffold s, Vector3 basePos, float height, float radius)
        {
            var go = Place(G.LightShaftPath, s.Lighting, basePos);
            go.transform.localScale = new Vector3(radius, height, radius);
        }

        private static void Figure(Scaffold s, Vector3 pos, float yaw, int frame, float height, bool flip)
        {
            var go = Place(G.TownsfolkFigurePath, s.Enemies, pos, yaw);
            var f = go.GetComponent<ShadowFigure>();
            GenUtil.Set(f, "frame", frame);
            GenUtil.Set(f, "height", height);
            GenUtil.Set(f, "flip", flip);
        }

        private static void Enemy(Scaffold s, Vector3 pos, float yaw, float leash)
        {
            var go = Place(G.ShadowEnemyPath, s.Enemies, pos, yaw);
            GenUtil.Set(go.GetComponent<ShadowEnemy>(), "leashRadius", leash);
        }

        private static void Hint(Scaffold s, Vector3 center, Vector3 size, string keys, string text, TutorialHint.DismissWhen dismiss,
            bool requireFire = false, bool requireBoth = false, float minDuration = 2.5f)
        {
            var go = new GameObject("Hint_" + (string.IsNullOrEmpty(keys) ? "Text" : keys.Replace(" ", "").Replace("/", "")));
            go.transform.SetParent(s.Tutorial, false);
            go.transform.position = center;
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var hint = go.AddComponent<TutorialHint>();
            GenUtil.Set(hint, "keys", keys);
            GenUtil.Set(hint, "text", text);
            GenUtil.Set(hint, "dismissWhen", (int)dismiss);
            GenUtil.Set(hint, "requireFire", requireFire);
            GenUtil.Set(hint, "requireBothSlots", requireBoth);
            GenUtil.Set(hint, "minDuration", minDuration);
        }

        private static void Draft(Scaffold s, Vector3 center, Vector3 size)
        {
            var go = new GameObject("WindDraft");
            go.transform.SetParent(s.Puzzle, false);
            go.transform.position = center;
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var wind = go.AddComponent<AudioSource>();
            wind.clip = AudioGenerator.Real("AMB_Wind.ogg", "AMB_Wind");
            wind.loop = true;
            wind.playOnAwake = true;
            wind.spatialBlend = 0.6f;
            wind.volume = 0.35f;
            var zone = go.AddComponent<WindDraftZone>();
            GenUtil.Set(zone, "windAudio", wind);

            // dust streaming out of the fissure
            var gust = Place(G.DustVolumePath, go.transform, center);
            gust.transform.localScale = size;
            var ps = gust.GetComponentInChildren<ParticleSystem>();
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-2.5f, -1.2f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.2f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            var em = ps.emission;
            em.rateOverTime = 60f;
        }

        private static void Exit(Scaffold s, Vector3 position, Vector3 size)
        {
            var go = new GameObject("LevelExit");
            go.transform.SetParent(s.Puzzle, false);
            go.transform.position = position;
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            go.AddComponent<LevelExit>();
        }

        private static void SetGuideStops(GameObject guide, params (Transform point, DialogueData dialogue)[] stops)
        {
            var so = new SerializedObject(guide.GetComponent<GuidingLight>());
            var p = so.FindProperty("stops");
            p.arraySize = stops.Length;
            for (int i = 0; i < stops.Length; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("point").objectReferenceValue = stops[i].point;
                e.FindPropertyRelative("dialogue").objectReferenceValue = stops[i].dialogue;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Label(Scaffold s, string text, Vector3 position, Vector3 euler)
        {
            var go = new GameObject("DevNote_Label");
            go.transform.SetParent(s.Environment, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            var tmp = go.AddComponent<TextMeshPro>();
            if (FontLibrary.Body != null) tmp.font = FontLibrary.Body;
            tmp.text = text;
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.8f, 0.6f, 0.55f);
            tmp.rectTransform.sizeDelta = new Vector2(10f, 4f);
        }

        /// <summary>Places the Meshy main character (if imported). With holdTorch it stands with a lit torch raised, looking at lookAt.</summary>
        private static GameObject PlaceCharacter(Transform parent, Vector3 position, float yaw, bool holdTorch, Vector3 lookAt)
        {
            string path = $"{CharacterImporter.CharacterPrefabDir}/PF_MainCharacter.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return null;
            var character = Place(path, parent, position, yaw);
            if (!holdTorch) return character;

            var handTarget = Sub("TorchHand", character.transform);
            handTarget.localPosition = new Vector3(0.32f, 1.42f, 0.28f);
            handTarget.localRotation = Quaternion.Euler(-10f, 0f, -12f);
            var look = Sub("LookTarget", character.transform);
            look.position = lookAt;

            var torch = Place(G.TorchPath, handTarget, handTarget.position);
            torch.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            torch.transform.localRotation = Quaternion.identity;
            var body = torch.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None; // interpolation would pin it in place while the bearer walks
            foreach (var c in torch.GetComponentsInChildren<Collider>()) c.enabled = false;
            torch.GetComponent<CarryableItem>().enabled = false;
            var flame = torch.GetComponent<FlammableItem>();
            GenUtil.Set(flame, "startLit", true);
            GenUtil.Set(flame, "infiniteBurn", true);
            GenUtil.Set(flame, "countsAsPlayerFire", false);

            var pose = character.AddComponent<TorchBearerPose>();
            GenUtil.Set(pose, "animator", character.GetComponentInChildren<Animator>());
            GenUtil.Set(pose, "controller", AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{CharacterImporter.AnimatorDir}/AC_MainCharacter.controller"));
            GenUtil.Set(pose, "handTarget", handTarget);
            GenUtil.Set(pose, "lookTarget", look);
            GenUtil.Set(pose, "gripEuler", G.RightGripEuler);
            return character;
        }
    }
}
