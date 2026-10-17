using System.Collections.Generic;
using NasconGJ.Core;
using NasconGJ.Cutscenes;
using NasconGJ.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using K = NasconGJ.EditorTools.Generation.EnvironmentKitGenerator;
using G = NasconGJ.EditorTools.Generation.GameplayPrefabGenerator;

namespace NasconGJ.EditorTools.Generation
{
    public static partial class LevelBuilder
    {
        // =====================================================================
        //  C01 — Prologue cutscene. Four sets far apart so their lights never mix:
        //    town (0,0,0) · wasteland path (200,0,0) · tomb facade (200,0,32) · inside the tomb (400,0,0)
        //  All placeholder geometry is generated; swap models freely, the director only needs the references.
        // =====================================================================

        /// <summary>Rebuild only the prologue (fast iteration on the cutscene).</summary>
        public static void BuildPrologueOnly()
        {
            prefabCache.Clear();
            BuildPrologue();
        }

        private static void BuildPrologue()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyRenderSettings();
            var systems = Group("SYSTEMS");
            var town = Group("SET_1_TOWN");
            var path = Group("SET_2_WASTELAND");
            var tomb = Group("SET_3_TOMB");
            var inside = Group("SET_4_INSIDE");
            var lighting = Group("LIGHTING");
            var marks = Group("CAMERA_MARKS");
            var audio = Group("AUDIO");
            var ui = Group("UI");

            // ---------------- rig
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(systems, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            var volumes = Sub("Volumes", lighting);
            AddVolume(volumes);
            var outdoorGo = new GameObject("Volume_Outdoor");
            outdoorGo.transform.SetParent(volumes, false);
            var outdoor = outdoorGo.AddComponent<Volume>();
            outdoor.isGlobal = true;
            outdoor.priority = 10f;
            outdoor.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MaterialLibrary.OutdoorProfilePath);

            var sun = MakeLight("Sky_Sun", lighting, LightType.Directional, Vector3.zero, new Color(1f, 0.55f, 0.32f), 0.9f, 10f);
            sun.transform.rotation = Quaternion.Euler(14f, 25f, 0f);
            var lightning = MakeLight("Lightning", lighting, LightType.Directional, Vector3.zero, new Color(0.72f, 0.78f, 1f), 0f, 10f);
            lightning.transform.rotation = Quaternion.Euler(32f, 15f, 0f);
            lightning.shadows = LightShadows.None;

            var canvas = PlaceAt(UIPrefabGenerator.CutscenePath, ui, Vector3.zero, Quaternion.identity);

            // ---------------- 1. the town: bright, painted, alive
            Ground(town, new Vector3(0, 0, 4), new Vector3(140, 1, 90), MatId.StoneDark);
            Ground(town, new Vector3(0, 0.01f, -1f), new Vector3(70, 1, 7), MatId.StoneFloor);

            Color[] plasters =
            {
                new(0.93f, 0.86f, 0.72f), new(0.86f, 0.62f, 0.38f), new(0.62f, 0.74f, 0.82f),
                new(0.9f, 0.72f, 0.66f), new(0.78f, 0.82f, 0.6f), new(0.95f, 0.9f, 0.82f),
            };
            Color[] roofs = { new(0.66f, 0.26f, 0.16f), new(0.34f, 0.36f, 0.44f), new(0.55f, 0.3f, 0.12f), new(0.42f, 0.2f, 0.18f) };
            var windows = new List<Renderer>();
            int houseIndex = 0;
            void HouseAt(string kit, float x, float z, float yaw)
            {
                var h = Place(kit, town, new Vector3(x, 0, z), yaw);
                windows.Add(h.GetComponent<Renderer>());
                var tint = h.AddComponent<SurfaceTint>();
                GenUtil.SetStrings(tint, "materialNames", new[] { "M_Plaster", "M_Roof" });
                GenUtil.SetColors(tint, "colors", new[] { plasters[houseIndex % plasters.Length], roofs[(houseIndex * 3 + 1) % roofs.Length] });
                houseIndex++;
            }
            HouseAt(K.House, -16f, 6f, 3f);
            HouseAt(K.HouseTall, -10.6f, 6f, -2f);
            HouseAt(K.HouseWide, -4.6f, 6.2f, 1f);
            HouseAt(K.House, 1.6f, 6f, -3f);
            HouseAt(K.HouseTall, 6.6f, 6f, 2f);
            HouseAt(K.HouseWide, 12.4f, 6.2f, -1f);
            HouseAt(K.House, 18.4f, 6f, 4f);
            HouseAt(K.HouseTall, -13.5f, 13.5f, 6f);
            HouseAt(K.House, -6.5f, 13f, -5f);
            HouseAt(K.HouseTall, 3.5f, 13.5f, 3f);
            HouseAt(K.House, 9.8f, 13f, -4f);
            HouseAt(K.HouseTall, 16f, 13.5f, 2f);
            HouseAt(K.House, -17f, -8.5f, 180f);
            HouseAt(K.HouseWide, -10f, -8.8f, 177f);
            HouseAt(K.HouseTall, 10f, -8.5f, 183f);
            HouseAt(K.House, 17f, -8.5f, 180f);

            // street life: pennants across the street, flower boxes, barrels and crates
            foreach (float x in new[] { -13.5f, -4f, 5f, 14f })
                Place(K.Bunting, town, new Vector3(x, 4.4f, -6.2f), -90f);
            foreach (float x in new[] { -15f, -9.4f, -2.8f, 3.6f, 8.4f, 14.2f })
                Place(K.FlowerBox, town, new Vector3(x, 0f, 3.75f), 0f);
            foreach (var (x, z, yaw) in new[] { (-8.2f, 3.5f, 10f), (-7.6f, 3.2f, 40f), (11f, 3.4f, 0f), (16.6f, -6.2f, 20f) })
                Place(K.Barrel, town, new Vector3(x, 0, z), yaw);
            foreach (var (x, z, yaw) in new[] { (4.3f, 3.5f, 12f), (4.95f, 3.35f, 50f), (-12f, -6f, 30f) })
                Place(K.Crate, town, new Vector3(x, 0, z), yaw);

            var lamps = new List<Light>();
            foreach (float x in new[] { -12.5f, -2.5f, 8.5f, 15.5f })
            {
                var post = Place(K.LampPost, town, new Vector3(x, 0, 2.7f), 180f);
                windows.Add(post.GetComponent<Renderer>());
                lamps.Add(MakeLight("LampLight", post.transform, LightType.Point, post.transform.TransformPoint(new Vector3(0.52f, 2.2f, 0f)), new Color(1f, 0.66f, 0.36f), 4f, 8f));
            }
            lamps.Add(MakeLight("PairFill", town, LightType.Point, new Vector3(-4.3f, 2.1f, -2.8f), new Color(1f, 0.72f, 0.45f), 3f, 6f)); // spill from a doorway across the street

            // the townsfolk (Meshy, rigged, animated by ProceduralHumanoid) and the shadows they'll leave on the walls
            string[] folk = { "Villager_Man", "Villager_Woman", "Villager_Elder" };
            var people = new (Vector3 pos, float yaw, int variant, Vector3 stroll)[]
            {
                (new Vector3(-14f, 0, 0.3f), 90f, 0, new Vector3(9f, 0, 0)),     // the one the camera walks beside
                (new Vector3(-6.3f, 0, -0.7f), 70f, 1, Vector3.zero),            // two neighbours talking
                (new Vector3(-5.3f, 0, -0.3f), -110f, 2, Vector3.zero),
                (new Vector3(2f, 0, 1.1f), -90f, 1, new Vector3(-5f, 0, 0)),
                (new Vector3(5.4f, 0, -1.8f), 200f, 0, Vector3.zero),
                (new Vector3(8.6f, 0, 0.5f), 90f, 2, new Vector3(5f, 0, 0)),
                (new Vector3(13.6f, 0, -0.6f), -60f, 0, new Vector3(0f, 0, -3f)),
            };
            var villagers = new List<Object>();
            var imprints = new List<Object>();
            var smokeMat = MaterialLibrary.Get(MaterialLibrary.ShadowSmokeMat);
            for (int i = 0; i < people.Length; i++)
            {
                var (pos, yaw, variant, stroll) = people[i];
                string meshyPath = $"{CharacterImporter.CharacterPrefabDir}/PF_{folk[variant]}.prefab";
                bool rigged = AssetDatabase.LoadAssetAtPath<GameObject>(meshyPath) != null;
                var v = PlaceAt(rigged ? meshyPath : G.VillagerPath(variant), town, pos, Quaternion.Euler(0, yaw, 0));
                if (rigged) v.AddComponent<NasconGJ.Player.ProceduralHumanoid>();
                var villager = v.GetComponent<Villager>() ?? v.AddComponent<Villager>();
                GenUtil.Set(villager, "strollOffset", stroll);
                GenUtil.Set(villager, "phase", i * 0.37f);
                GenUtil.Set(villager, "smokeMaterial", smokeMat);
                villagers.Add(villager);

                var imprint = Place(G.TownsfolkFigurePath, town, new Vector3(pos.x + stroll.x * 0.5f, 0.02f, 3.9f), 180f);
                var f = imprint.GetComponent<ShadowFigure>();
                GenUtil.Set(f, "frame", i % 3);
                GenUtil.Set(f, "height", 1.75f);
                GenUtil.Set(f, "flip", i % 2 == 1);
                GenUtil.Set(f, "opacity", 0f);
                imprints.Add(f);
            }

            // low hills behind the town; the Void rises from beyond them into the sky
            foreach (float x in new[] { -60f, -40f, -20f, 0f, 20f, 40f, 60f })
            {
                var cliff = Place(K.RockWall, town, new Vector3(x, 0, 44f), 0f);
                cliff.transform.localScale = new Vector3(5.2f, 3.2f, 5f);
            }
            foreach (var (x, z, s) in new[] { (-22f, 40.5f, 3f), (-6f, 41f, 2.6f), (12f, 40.5f, 3.4f), (28f, 41f, 2.8f) })
            {
                var rock = Place(K.RockLarge, town, new Vector3(x, 0, z), x * 7f);
                rock.transform.localScale = Vector3.one * s;
            }
            // at the far end of the street, so the street itself leads the eye to it
            var voidApparition = BuildVoidApparition(town, new Vector3(95f, 31f, -1f), 62f);
            var voidWash = MakeLight("VoidWash", town, LightType.Spot, new Vector3(40f, 14f, -1f), new Color(1f, 0.22f, 0.1f), 0f, 70f);
            voidWash.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 1f) - voidWash.transform.position);
            voidWash.spotAngle = 90f;
            voidWash.innerSpotAngle = 40f;
            voidWash.shadows = LightShadows.None;

            var craneFrom = Mark("Crane_From", marks, new Vector3(-11f, 15f, -18f), new Vector3(-2f, 3f, 6f));
            var craneTo = Mark("Crane_To", marks, new Vector3(-8.5f, 3.4f, -7.5f), new Vector3(-2f, 1.7f, 2f));
            // low behind the townsfolk, looking down the street to where it rises
            var riseFrom = Mark("Rise_From", marks, new Vector3(-12f, 1.2f, -2.2f), new Vector3(30f, 6f, -0.8f));
            var riseTo = Mark("Rise_To", marks, new Vector3(-10.5f, 0.9f, -2.4f), new Vector3(60f, 22f, -0.8f));
            var takenFrom = Mark("Taken_From", marks, new Vector3(-3.1f, 1.45f, -5.4f), new Vector3(-5.9f, 1.25f, 1f));
            var takenTo = Mark("Taken_To", marks, new Vector3(-3.9f, 1.5f, -4.3f), new Vector3(-5.9f, 1.35f, 1f));
            var darkFrom = Mark("Dark_From", marks, new Vector3(-30f, 14f, -3f), new Vector3(60f, 16f, -1f));
            var darkTo = Mark("Dark_To", marks, new Vector3(-24f, 10f, -2.5f), new Vector3(60f, 20f, -1f));

            // ---------------- 2. the wasteland path
            Ground(path, new Vector3(200, 0, 12), new Vector3(60, 1, 60), MatId.StoneDark);
            foreach (var (x, z, s, yaw) in new[] { (194.5f, 4f, 1.6f, 20f), (206.5f, 9f, 2f, 130f), (193.5f, 15f, 1.8f, 250f), (207f, 1f, 1.3f, 75f), (205f, 19f, 2.4f, 10f) })
            {
                var rock = Place(K.RockLarge, path, new Vector3(x, 0, z), yaw);
                rock.transform.localScale = Vector3.one * s;
            }
            foreach (var (x, z) in new[] { (197.6f, 2f), (202.8f, 6.5f), (197.2f, 12.5f), (203.2f, 16f), (196f, 20f) })
                Place(K.RockSmall, path, new Vector3(x, 0, z), x * 13f);
            Place(K.PillarBroken, path, new Vector3(197.8f, 0, 9.6f), 30f);
            Place(K.PillarBroken, path, new Vector3(203f, 0, 13.4f), -20f);
            Place(K.Obelisk, path, new Vector3(196.4f, 0, 18f), 0f, new Vector3(0, 15f, 7f));
            Place(K.RubbleB, path, new Vector3(202.4f, 0, 3.4f), 80f);
            Place(K.RubbleA, path, new Vector3(198.4f, 0, 15.6f), 10f);
            var mist = Place(G.DustVolumePath, path, new Vector3(200f, 1.5f, 10f));
            mist.transform.localScale = new Vector3(14f, 3f, 24f);
            var walkPath = new[]
            {
                Point("Walk_0", path, new Vector3(200f, 0f, -0.5f)),
                Point("Walk_1", path, new Vector3(200.4f, 0f, 8f)),
                Point("Walk_2", path, new Vector3(199.8f, 0f, 17f)),
            };

            // ---------------- 3. the tomb in the storm
            Ground(tomb, new Vector3(200, 0.005f, 36), new Vector3(60, 1, 40), MatId.StoneDark);
            foreach (float x in new[] { 192f, 208f })
            {
                var wall = Place(K.Wall, tomb, new Vector3(x, 0, 34f), 0f);
                wall.transform.localScale = new Vector3(2f, 2.2f, 2f);
            }
            var door = Place(K.WallDoorway, tomb, new Vector3(200f, 0, 34f), 0f);
            door.transform.localScale = new Vector3(2f, 2f, 2f);
            foreach (float x in new[] { 192f, 200f, 208f })
            {
                var upper = Place(K.Wall, tomb, new Vector3(x, x == 200f ? 8f : 8.8f, 34f), 0f);
                upper.transform.localScale = new Vector3(2f, 1.4f, 2f);
            }
            foreach (float x in new[] { 196.4f, 203.6f })
            {
                var pillar = Place(K.Pillar, tomb, new Vector3(x, 0, 32.4f), 0f);
                pillar.transform.localScale = new Vector3(1.6f, 2.3f, 1.6f);
            }
            foreach (float x in new[] { 192.5f, 207.5f })
            {
                var ob = Place(K.Obelisk, tomb, new Vector3(x, 0, 29.5f), x < 200f ? 8f : -8f);
                ob.transform.localScale = Vector3.one * 1.6f;
            }
            Place(G.BrazierUnlitPath, tomb, new Vector3(197.8f, 0, 31f));
            Place(G.BrazierUnlitPath, tomb, new Vector3(202.2f, 0, 31f));
            Place(K.RubbleB, tomb, new Vector3(195f, 0, 30.5f), 40f);
            Place(K.RockLarge, tomb, new Vector3(188f, 0, 30f), 60f);
            Place(K.RockLarge, tomb, new Vector3(212f, 0, 30.5f), 200f);
            // the dark beyond the door
            Place(K.Wall, tomb, new Vector3(200f, 0, 41f), 0f).transform.localScale = new Vector3(2f, 2f, 1f);
            var roof = Place(K.Ceiling, tomb, new Vector3(200f, 6.2f, 37.5f));
            roof.transform.localScale = new Vector3(2f, 1f, 2f);
            var tombPath = new[]
            {
                Point("Tomb_0", tomb, new Vector3(200f, 0f, 22f)),
                Point("Tomb_1", tomb, new Vector3(200f, 0f, 31f)),
                Point("Tomb_2", tomb, new Vector3(200f, 0f, 37f)),
            };
            var tombFrom = Mark("Tomb_From", marks, new Vector3(196.6f, 0.45f, 19f), new Vector3(200f, 6.5f, 33f));
            var tombTo = Mark("Tomb_To", marks, new Vector3(198.4f, 0.8f, 23.6f), new Vector3(200f, 4.8f, 33f));
            var doorwayFrom = Mark("Doorway_From", marks, new Vector3(200.4f, 1.7f, 37.4f), new Vector3(200f, 1.7f, 24f));
            var doorwayTo = Mark("Doorway_To", marks, new Vector3(200.2f, 1.45f, 36.6f), new Vector3(200f, 1.9f, 24f));

            // ---------------- 4. inside: a corridor whose floor gives way
            foreach (float z in new[] { -4f, 0f, 4f, 12f })
            {
                Place(K.Floor, inside, new Vector3(400f, 0, z));
                Place(K.Ceiling, inside, new Vector3(400f, 4.2f, z));
            }
            Place(K.Ceiling, inside, new Vector3(400f, 4.2f, 8f));
            foreach (float z in new[] { -4f, 0f, 4f, 8f, 12f })
            {
                Place(K.Wall, inside, new Vector3(398f, 0, z), 90f);
                Place(K.Wall, inside, new Vector3(402f, 0, z), 90f);
            }
            Place(K.Wall, inside, new Vector3(400f, 0, 14f), 0f);
            Place(K.Wall, inside, new Vector3(400f, 0, -6f), 0f);
            // the failing floor: loose slabs over a shaft
            var pieces = new List<Object>();
            foreach (float x in new[] { 399f, 401f })
            foreach (float z in new[] { 6.5f, 7.5f, 8.5f, 9.5f })
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "LooseSlab";
                slab.transform.SetParent(inside, false);
                slab.transform.position = new Vector3(x, -0.18f, z);
                slab.transform.localScale = new Vector3(1.96f, 0.36f, 0.96f);
                slab.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(MatId.StoneFloor);
                var body = slab.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.mass = 40f;
                pieces.Add(body);
            }
            for (int i = 0; i < 4; i++) // shaft walls going down into the dark
            {
                Place(K.Wall, inside, new Vector3(398f, -4f - i * 4f, 8f), 90f);
                Place(K.Wall, inside, new Vector3(402f, -4f - i * 4f, 8f), 90f);
                Place(K.Wall, inside, new Vector3(400f, -4f - i * 4f, 6f), 0f);
                Place(K.Wall, inside, new Vector3(400f, -4f - i * 4f, 10f), 0f);
            }
            var crackLight = MakeLight("CrackLight", inside, LightType.Spot, new Vector3(400.4f, 9f, 8.6f), Moon, 9f, 12f);
            crackLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            crackLight.spotAngle = 34f;
            var dustGo = Place(G.DustVolumePath, inside, new Vector3(400f, 0.4f, 8f));
            dustGo.transform.localScale = new Vector3(3.6f, 0.6f, 3.6f);
            var dust = dustGo.GetComponentInChildren<ParticleSystem>();
            var main = dust.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.6f);
            var emission = dust.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 140) });
            var insidePath = new[]
            {
                Point("Inside_0", inside, new Vector3(400f, 0f, -0.5f)),
                Point("Inside_1", inside, new Vector3(400.1f, 0f, 7.9f)),
            };

            // ---------------- the bearer
            var bearer = PlaceCharacter(path, walkPath[0].position, 0f, holdTorch: true, lookAt: walkPath[0].position + new Vector3(0f, 1.4f, 7f));
            CutsceneActor actor = null;
            if (bearer != null)
            {
                actor = bearer.AddComponent<CutsceneActor>();
                GenUtil.Set(actor, "pose", bearer.GetComponent<NasconGJ.Player.TorchBearerPose>());
            }

            // ---------------- audio
            AudioSource Source(string name, AudioClip clip, bool loop, bool play, float volume)
            {
                var go = Sub(name, audio).gameObject;
                var s = go.AddComponent<AudioSource>();
                s.clip = clip;
                s.loop = loop;
                s.playOnAwake = play;
                s.volume = volume;
                return s;
            }
            var music = Source("Music", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Music/MUS_MainAmbience.mp3"), true, true, 0f);
            var dread = Source("Dread", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Music/MUS_Presence.mp3"), true, false, 0f);
            var sfx = Source("SFX", null, false, false, 1f);

            // ---------------- director
            var director = new GameObject("PrologueDirector").AddComponent<PrologueDirector>();
            director.transform.SetParent(systems, false);
            GenUtil.Set(director, "cam", cam);
            GenUtil.Set(director, "ui", canvas.GetComponent<CutsceneUI>());
            GenUtil.Set(director, "sun", sun);
            GenUtil.Set(director, "outdoorVolume", outdoor);
            GenUtil.Set(director, "craneFrom", craneFrom);
            GenUtil.Set(director, "craneTo", craneTo);
            GenUtil.Set(director, "riseFrom", riseFrom);
            GenUtil.Set(director, "riseTo", riseTo);
            GenUtil.Set(director, "darkFrom", darkFrom);
            GenUtil.Set(director, "darkTo", darkTo);
            GenUtil.Set(director, "doorwayFrom", doorwayFrom);
            GenUtil.Set(director, "doorwayTo", doorwayTo);
            GenUtil.Set(director, "takenFrom", takenFrom);
            GenUtil.Set(director, "takenTo", takenTo);
            GenUtil.SetArray(director, "villagers", villagers.ToArray());
            GenUtil.SetArray(director, "imprints", imprints.ToArray());
            GenUtil.SetArray(director, "lamps", lamps.ToArray());
            GenUtil.SetArray(director, "glowingWindows", windows.ToArray());
            GenUtil.Set(director, "voidApparition", voidApparition);
            GenUtil.Set(director, "voidWash", voidWash);
            GenUtil.Set(director, "bearer", actor);
            GenUtil.SetArray(director, "walkPath", walkPath);
            GenUtil.Set(director, "tombFrom", tombFrom);
            GenUtil.Set(director, "tombTo", tombTo);
            GenUtil.SetArray(director, "tombPath", tombPath);
            GenUtil.Set(director, "lightning", lightning);
            GenUtil.SetArray(director, "insidePath", insidePath);
            GenUtil.SetArray(director, "collapsePieces", pieces.ToArray());
            GenUtil.Set(director, "collapseDust", dust);
            GenUtil.Set(director, "music", music);
            GenUtil.Set(director, "dread", dread);
            GenUtil.Set(director, "sfx", sfx);
            GenUtil.Set(director, "thunderClip", AudioGenerator.Load("SFX_Thunder"));
            GenUtil.Set(director, "collapseClip", AudioGenerator.Load("SFX_Collapse"));
            GenUtil.Set(director, "impactClip", AudioGenerator.Load("SFX_Impact"));

            camGo.transform.SetPositionAndRotation(craneFrom.position, craneFrom.rotation);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scenes + "Cutscenes/" + SceneNames.Prologue + ".unity");
        }

        /// <summary>The Void as a sky-sized silhouette: a quad with the shadow-creature sheet, smoke pouring off it.</summary>
        private static VoidApparition BuildVoidApparition(Transform parent, Vector3 center, float height)
        {
            var root = new GameObject("Void_Apparition");
            root.transform.SetParent(parent, false);
            root.transform.position = center;
            var quadGo = new GameObject("Silhouette");
            quadGo.transform.SetParent(root.transform, false);
            quadGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // face back down the street (-X)
            var quadMesh = GenUtil.SaveAsset(MeshBuilder.Quad("SM_VoidQuad"), $"{GenUtil.PropMeshDir}/SM_VoidQuad.asset");
            quadGo.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            var r = quadGo.AddComponent<MeshRenderer>();
            r.sharedMaterial = MaterialLibrary.Get(MaterialLibrary.VoidSkyMat);
            r.shadowCastingMode = ShadowCastingMode.Off;
            var head = Sub("Head", root.transform);
            head.localPosition = new Vector3(0f, height * 0.29f, 0f);

            // tar smoke boiling off its edges
            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(root.transform, false);
            var ps = smokeGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(8f, 16f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.02f;
            main.maxParticles = 300;
            main.playOnAwake = false;
            var em = ps.emission;
            em.rateOverTime = 0f; // the blob smoke buried the silhouette; keep the shape clean
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(4f, height * 0.7f, height * 0.4f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.25f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = G.Grad(new[] { Color.white, new Color(0.6f, 0.3f, 0.3f) }, new[] { 0f, 1f }, new[] { 0f, 0.85f, 0f }, new[] { 0f, 0.25f, 1f });
            var psr = smokeGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = MaterialLibrary.Get(MaterialLibrary.ShadowSmokeMat);
            psr.shadowCastingMode = ShadowCastingMode.Off;

            var app = root.AddComponent<VoidApparition>();
            GenUtil.Set(app, "quad", r);
            GenUtil.Set(app, "height", height);
            GenUtil.Set(app, "hiddenDrop", height * 0.9f);
            GenUtil.Set(app, "smoke", ps);
            GenUtil.Set(app, "head", head);
            return app;
        }

        /// <summary>A flat slab of ground (top surface at center.y).</summary>
        private static void Ground(Transform parent, Vector3 center, Vector3 size, MatId mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Ground";
            go.transform.SetParent(parent, false);
            go.transform.position = center - Vector3.up * size.y * 0.5f;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(mat);
            go.layer = LayerMask.NameToLayer(GameLayers.Ground);
        }

        /// <summary>Camera mark: a transform placed at <paramref name="position"/> looking at <paramref name="lookAt"/>.</summary>
        private static Transform Mark(string name, Transform parent, Vector3 position, Vector3 lookAt)
        {
            var t = Sub(name, parent);
            t.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
            return t;
        }
    }
}
