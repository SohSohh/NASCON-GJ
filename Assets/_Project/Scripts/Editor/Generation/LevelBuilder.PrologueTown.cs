using System.Collections.Generic;
using System.IO;
using NasconGJ.Core;
using NasconGJ.Cutscenes;
using NasconGJ.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using K = NasconGJ.EditorTools.Generation.EnvironmentKitGenerator;
using G = NasconGJ.EditorTools.Generation.GameplayPrefabGenerator;

namespace NasconGJ.EditorTools.Generation
{
    public static partial class LevelBuilder
    {
        // =====================================================================
        //  C01b — Prologue, present day (3D). Follows the 2D puppet backstory (C01_Prologue).
        //  Busy town → rooftop talk → the Void → the shockwave → the one left → the tomb → black → falling memories → L01.
        //  Four sets far apart so their lights never mix:
        //    town (0,0,0) · wasteland path (200,0,0) · tomb facade (200,0,32) · inside the tomb (400,0,0)
        //  All placeholder geometry is generated; swap models freely, the director only needs the references.
        // =====================================================================

        /// <summary>Rebuild only the 3D half of the prologue.</summary>
        public static void BuildPrologueTownOnly()
        {
            prefabCache.Clear();
            BuildPrologueTown();
        }

        private static void BuildPrologueTown()
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

            // painted sky dome (gradient + drifting clouds), follows the camera across all sets
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "SkyDome";
            Object.DestroyImmediate(dome.GetComponent<Collider>());
            dome.transform.SetParent(lighting, false);
            dome.transform.localScale = Vector3.one * 700f;
            var domeR = dome.GetComponent<Renderer>();
            domeR.sharedMaterial = MaterialLibrary.Get(MaterialLibrary.SkyDomeMat);
            domeR.shadowCastingMode = ShadowCastingMode.Off;
            domeR.receiveShadows = false;
            dome.AddComponent<SkyFollow>();

            // ---------------- 1. the town: bright and alive (the cartoon look comes from ComicPost: ink, flat colour, impact frames)
            Ground(town, new Vector3(0, 0, 4), new Vector3(220, 1, 160), MatId.Grass);
            Ground(town, new Vector3(0, 0.01f, -1f), new Vector3(150, 1, 7), MatId.StoneFloor);

            Color[] plasters =
            {
                new(1f, 0.93f, 0.78f), new(1f, 0.72f, 0.55f), new(0.66f, 0.84f, 0.98f),
                new(1f, 0.76f, 0.82f), new(0.8f, 0.94f, 0.66f), new(0.98f, 0.9f, 0.6f),
            };
            Color[] roofs = { new(0.9f, 0.3f, 0.3f), new(0.36f, 0.5f, 0.9f), new(0.95f, 0.55f, 0.2f), new(0.6f, 0.36f, 0.8f) };
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
            for (int i = 0; i < 10; i++) HouseAt(i % 3 == 1 ? K.HouseTall : i % 3 == 2 ? K.HouseWide : K.House, -22f + i * 6.4f, 6.1f, (i * 37 % 9) - 4f);
            for (int i = 0; i < 9; i++) HouseAt(i % 2 == 0 ? K.HouseTall : K.House, -19f + i * 6.8f, 13.6f, (i * 53 % 11) - 5f);
            for (int i = 0; i < 9; i++) HouseAt(i % 3 == 0 ? K.HouseWide : K.House, -20f + i * 7f, -8.6f, 180f + (i * 29 % 7) - 3f);

            // low hills behind the town, and ridges on the horizon at the end of the street (behind the Void)
            foreach (float x in new[] { -60f, -40f, -20f, 0f, 20f, 40f, 60f })
            {
                var cliff = Place(K.RockWall, town, new Vector3(x, 0, 44f), 0f);
                cliff.transform.localScale = new Vector3(5.2f, 3.2f, 5f);
            }
            foreach (var (x, z, s, yaw) in new[] { (132f, -45f, 7f, 10f), (135f, 18f, 7f, 70f), (130f, 50f, 7f, 140f), (138f, -20f, 7f, 200f), (120f, -60f, 7f, 30f), (125f, 60f, 7f, 300f) })
            {
                var ridge = Place(K.RockLarge, town, new Vector3(x, -2f, z), yaw);
                ridge.transform.localScale = new Vector3(s * 1.6f, s, s * 1.6f);
            }

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
                lamps.Add(MakeLight("LampLight", post.transform, LightType.Point, post.transform.TransformPoint(new Vector3(0.52f, 2.2f, 0f)), new Color(1f, 0.66f, 0.36f), 3f, 7f));
            }

            // the townsfolk: neighbours standing in pairs, chatting; their shadows will be left on the walls behind them
            string[] folk = { "Villager_Man", "Villager_Woman", "Villager_Elder" };
            var people = new (Vector3 pos, int variant, int partner)[]
            {
                (new Vector3(-6.4f, 0, -0.7f), 0, 1), (new Vector3(-5.3f, 0, -0.2f), 1, 0),     // the pair the camera passes
                (new Vector3(0.6f, 0, 0.5f), 2, 3), (new Vector3(1.7f, 0, 0.1f), 0, 2),
                (new Vector3(7.4f, 0, -0.9f), 1, 5), (new Vector3(8.4f, 0, -0.3f), 2, 4),
                (new Vector3(13.4f, 0, 0.6f), 0, -1),
            };
            var villagers = new List<Object>();
            var villagerGos = new List<GameObject>();
            var imprints = new List<Object>();
            var smokeMat = MaterialLibrary.Get(MaterialLibrary.ShadowSmokeMat);
            for (int i = 0; i < people.Length; i++)
            {
                var (pos, variant, _) = people[i];
                string meshyPath = $"{CharacterImporter.CharacterPrefabDir}/PF_{folk[variant]}.prefab";
                bool rigged = AssetDatabase.LoadAssetAtPath<GameObject>(meshyPath) != null;
                var v = PlaceAt(rigged ? meshyPath : G.VillagerPath(variant), town, pos, Quaternion.Euler(0, 180f, 0));
                if (rigged) v.AddComponent<NasconGJ.Player.ProceduralHumanoid>();
                var villager = v.GetComponent<Villager>() ?? v.AddComponent<Villager>();
                GenUtil.Set(villager, "strollOffset", Vector3.zero);
                GenUtil.Set(villager, "phase", i * 0.37f);
                GenUtil.Set(villager, "smokeMaterial", smokeMat);
                villagers.Add(villager);
                villagerGos.Add(v);

                // cast away from the Void (which is down the street at +X): the stain lands beside them on the wall
                var imprint = Place(G.TownsfolkFigurePath, town, new Vector3(pos.x - 1.3f, 0.02f, 4.15f), 180f);
                var f = imprint.GetComponent<ShadowFigure>();
                GenUtil.Set(f, "frame", i % 3);
                GenUtil.Set(f, "height", 1.75f);
                GenUtil.Set(f, "flip", i % 2 == 1);
                GenUtil.Set(f, "opacity", 0f);
                imprints.Add(f);
            }
            for (int i = 0; i < people.Length; i++)
            {
                int partner = people[i].partner;
                if (partner < 0) continue;
                GenUtil.Set(villagerGos[i].GetComponent<Villager>(), "talkTo", villagerGos[partner].transform);
                Vector3 to = villagerGos[partner].transform.position - villagerGos[i].transform.position;
                to.y = 0f;
                villagerGos[i].transform.rotation = Quaternion.LookRotation(to);
            }


            // ---------------- more life: the story's people, a market, strollers, kids, all procedural
            Villager Folk(string name, int variant, Vector3 pos, float yaw, Villager.Activity act, float scale = 1f, float stoop = 0f,
                Transform talk = null, Vector3 stroll = default, float speed = 1.05f)
            {
                string meshy = $"{CharacterImporter.CharacterPrefabDir}/PF_{folk[variant]}.prefab";
                bool rig = AssetDatabase.LoadAssetAtPath<GameObject>(meshy) != null;
                var go = PlaceAt(rig ? meshy : G.VillagerPath(variant), town, pos, Quaternion.Euler(0f, yaw, 0f));
                go.name = name;
                go.transform.localScale *= scale;
                if (rig && go.GetComponent<NasconGJ.Player.ProceduralHumanoid>() == null) go.AddComponent<NasconGJ.Player.ProceduralHumanoid>();
                var v = go.GetComponent<Villager>() ?? go.AddComponent<Villager>();
                GenUtil.Set(v, "activity", (int)act);
                GenUtil.Set(v, "stoop", stoop);
                GenUtil.Set(v, "strollOffset", stroll);
                GenUtil.Set(v, "strollSpeed", speed);
                GenUtil.Set(v, "phase", Mathf.Repeat(pos.x * 0.37f + pos.z * 0.71f, 1f));
                GenUtil.Set(v, "smokeMaterial", smokeMat);
                if (talk != null) GenUtil.Set(v, "talkTo", talk);
                villagers.Add(v);
                return v;
            }

            // the old man from the tale, older now, still telling it on his bench; kids gathered round
            Ground(town, new Vector3(-10.6f, 0.45f, 3.05f), new Vector3(1.7f, 0.45f, 0.5f), MatId.Wood);
            var elder = Folk("Elder_Storyteller", 2, new Vector3(-10.6f, 0f, 3.0f), 180f, Villager.Activity.Story, 1f, 0.85f);
            Folk("Kid_A", 0, new Vector3(-11.4f, 0f, 1.55f), 15f, Villager.Activity.HandsBehind, 0.7f, 0f, elder.transform);
            Folk("Kid_B", 1, new Vector3(-10.4f, 0f, 1.35f), 0f, Villager.Activity.HandsBehind, 0.66f, 0f, elder.transform);
            Folk("Kid_C", 1, new Vector3(-9.5f, 0f, 1.7f), -25f, Villager.Activity.HandsBehind, 0.72f, 0f, elder.transform);

            // a market stall: counter, awning, goods; the vendor sells to a customer
            Ground(town, new Vector3(2.6f, 0.95f, -5.2f), new Vector3(2.6f, 0.95f, 0.8f), MatId.Wood);
            foreach (var (gx, gz) in new[] { (1.8f, -5.25f), (2.6f, -5.15f), (3.4f, -5.25f) })
                Place(K.Crate, town, new Vector3(gx, 0.95f, gz), gx * 40f).transform.localScale = Vector3.one * 0.55f;
            Place(K.Bunting, town, new Vector3(2.6f, 2.5f, -5.6f), 0f).transform.localScale = new Vector3(0.35f, 1f, 1f);
            var vendor = Folk("Vendor", 0, new Vector3(2.6f, 0f, -5.95f), 0f, Villager.Activity.Sell);
            Folk("Customer", 1, new Vector3(2.4f, 0f, -4.1f), 180f, Villager.Activity.Chat, 1f, 0f, vendor.transform);

            // people going about their day along the street
            Folk("Stroller_A", 0, new Vector3(-19f, 0f, -1.6f), 90f, Villager.Activity.Idle, 1f, 0f, null, new Vector3(22f, 0f, 0f), 1.1f);
            Folk("Stroller_B", 1, new Vector3(10f, 0f, 0.9f), -90f, Villager.Activity.Idle, 1f, 0f, null, new Vector3(-20f, 0f, 0f), 0.95f);
            Folk("Stroller_C", 2, new Vector3(-4f, 0f, -2.6f), 90f, Villager.Activity.HandsBehind, 1f, 0.5f, null, new Vector3(9f, 0f, 0f), 0.7f);
            var carrier = Folk("Carrier", 0, new Vector3(16f, 0f, -0.8f), -90f, Villager.Activity.Carry, 1f, 0f, null, new Vector3(-14f, 0f, 0f), 0.9f);
            var box = Place(K.Crate, carrier.transform, carrier.transform.TransformPoint(new Vector3(0f, 0.86f, 0.36f)), 0f);
            box.transform.localScale = Vector3.one * 0.55f;
            box.transform.localRotation = Quaternion.identity;
            foreach (var c in box.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            Folk("Runner_A", 0, new Vector3(-6f, 0f, -0.2f), 90f, Villager.Activity.Idle, 0.68f, 0f, null, new Vector3(10f, 0f, 0.4f), 2.7f);
            Folk("Runner_B", 1, new Vector3(-7.2f, 0f, 0.3f), 90f, Villager.Activity.Idle, 0.64f, 0f, null, new Vector3(10.5f, 0f, -0.3f), 2.6f);
            Folk("Doorway_Pair_A", 2, new Vector3(9.4f, 0f, 3.2f), 160f, Villager.Activity.HandsBehind, 1f, 0.4f, null);
            var gossip = Folk("Gossip_A", 1, new Vector3(-16.2f, 0f, -4.6f), 30f, Villager.Activity.Chat);
            Folk("Gossip_B", 1, new Vector3(-15.3f, 0f, -3.9f), 210f, Villager.Activity.Chat, 1f, 0f, gossip.transform);
            GenUtil.Set(gossip, "talkTo", GameObject.Find("Gossip_B").transform);

            // ---------------- the backdrop: the town keeps going in every direction, trees, cliffs on the horizon
            for (int r = 0; r < 3; r++)
            {
                for (int i = 0; i < 19; i++)
                {
                    float x = -83f + i * 7.4f + (r % 2) * 3.4f;
                    HouseAt(new[] { K.House, K.HouseTall, K.HouseWide }[(i + r) % 3], x, 21f + r * 8f, (i * 41 % 13) - 6f);
                    HouseAt(new[] { K.HouseWide, K.House, K.HouseTall }[(i + r) % 3], x + 1.7f, -16.5f - r * 8f, 180f + (i * 23 % 11) - 5f);
                }
            }
            var trunkMat = MaterialLibrary.Get(MatId.Wood);
            var leaf = new[] { MaterialLibrary.Get(MatId.Grass), MaterialLibrary.Get(MatId.PaintGreen), MaterialLibrary.Get(MatId.Grass) };
            void Tree(Vector3 p, float s, int kind)
            {
                var t = Sub("Tree", town);
                t.position = p;
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(trunk.GetComponent<Collider>());
                trunk.transform.SetParent(t, false);
                trunk.transform.localPosition = new Vector3(0f, 1.1f * s, 0f);
                trunk.transform.localScale = new Vector3(0.28f, 1.1f, 0.28f) * s;
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                for (int k = 0; k < 3; k++)
                {
                    var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.DestroyImmediate(crown.GetComponent<Collider>());
                    crown.transform.SetParent(t, false);
                    float a = k * 2.1f + p.x;
                    crown.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.5f, 2.5f + k * 0.45f, Mathf.Sin(a) * 0.5f) * s;
                    crown.transform.localScale = Vector3.one * (1.9f - k * 0.35f) * s;
                    crown.GetComponent<Renderer>().sharedMaterial = leaf[Mathf.Abs(kind) % leaf.Length];
                }
            }
            for (int i = 0; i < 40; i++)
            {
                float a = i * 2.399f;
                float rad = 34f + (i * 37 % 29);
                var p = new Vector3(8f + Mathf.Cos(a) * rad * 1.5f, 0f, 4f + Mathf.Sin(a) * rad);
                if (Mathf.Abs(p.z) < 12f && p.x > -50f && p.x < 60f) continue; // keep the street clear
                Tree(p, 1f + (i % 5) * 0.25f, i);
            }
            foreach (var (tx, tz) in new[] { (-24f, 9.5f), (-25.5f, -11f), (21f, 9.8f), (24f, -12.5f), (-30f, 17f), (30f, 17.5f) })
                Tree(new Vector3(tx, 0f, tz), 1.2f, (int)tx);
            // the west end of the street (behind the rooftop two-shot): more houses, trees and a far ridge
            for (int i = 0; i < 6; i++)
            {
                HouseAt(new[] { K.HouseTall, K.House, K.HouseWide }[i % 3], -30f - i * 6.6f, 6.1f + (i % 2) * 0.6f, (i * 31 % 9) - 4f);
                HouseAt(new[] { K.House, K.HouseWide, K.HouseTall }[i % 3], -27f - i * 6.9f, -8.6f - (i % 2) * 0.5f, 180f + (i * 17 % 7) - 3f);
            }
            for (int i = 0; i < 10; i++) Tree(new Vector3(-36f - i * 5.5f, 0f, (i % 2 == 0 ? 1f : -1f) * (13f + (i * 7 % 5))), 1.1f + (i % 3) * 0.3f, i);
            foreach (var (x, z, s, yaw) in new[] { (-120f, 10f, 12f, 60f), (-110f, -35f, 10f, 140f), (-115f, 45f, 11f, 220f), (-70f, 60f, 10f, 30f), (-20f, 75f, 13f, 120f), (40f, 72f, 12f, 210f), (-80f, -50f, 10f, 80f), (0f, -70f, 12f, 160f), (60f, -65f, 11f, 250f) })
            {
                var ridge = Place(K.RockLarge, town, new Vector3(x, -3f, z), yaw);
                ridge.transform.localScale = new Vector3(s * 1.8f, s, s * 1.8f);
            }

            // the Void: at the far end of the street, so the street itself leads the eye to it
            var voidApparition = BuildVoidApparition(town, new Vector3(95f, -6f, -1f), 50f);
            var imprintLight = MakeLight("ImprintLight", town, LightType.Spot, new Vector3(-6f, 3.5f, -3.2f), new Color(1f, 0.5f, 0.38f), 0f, 16f);
            imprintLight.transform.rotation = Quaternion.LookRotation(new Vector3(-7f, 1.2f, 4.1f) - imprintLight.transform.position);
            imprintLight.spotAngle = 110f;
            var voidWash = MakeLight("VoidWash", town, LightType.Spot, new Vector3(40f, 14f, -1f), new Color(1f, 0.22f, 0.1f), 0f, 70f);
            voidWash.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 1f) - voidWash.transform.position);
            voidWash.spotAngle = 90f;
            voidWash.innerSpotAngle = 40f;
            voidWash.shadows = LightShadows.None;

            // ---------------- the rooftop: a flat terrace over the south row, looking up the street toward the Void
            var terrace = Sub("Rooftop", town);
            Ground(terrace, new Vector3(-6.2f, 5f, -9.6f), new Vector3(6.6f, 0.35f, 4.6f), MatId.Wood);
            Ground(terrace, new Vector3(-6.2f, 5.55f, -7.35f), new Vector3(6.6f, 0.55f, 0.25f), MatId.StoneDark);   // parapet
            Ground(terrace, new Vector3(-9.4f, 5.55f, -9.6f), new Vector3(0.25f, 0.55f, 4.6f), MatId.StoneDark);
            foreach (var (px, pz) in new[] { (-9.2f, -11.6f), (-3.2f, -11.6f), (-9.2f, -7.6f), (-3.2f, -7.6f) })
                Place(K.Pillar, terrace, new Vector3(px, 0.65f, pz), 0f);
            var roofPost = Place(K.LampPost, terrace, new Vector3(-9f, 5f, -11.7f), 90f);
            lamps.Add(MakeLight("RoofLamp", roofPost.transform, LightType.Point, roofPost.transform.TransformPoint(new Vector3(0.52f, 2.2f, 0f)), new Color(1f, 0.66f, 0.36f), 2.5f, 6f));
            Place(K.Crate, terrace, new Vector3(-4.1f, 5f, -11f), 20f);
            Place(K.Barrel, terrace, new Vector3(-3.6f, 5f, -10.2f), 0f);

            var roofBearer = Point("Roof_Bearer", terrace, new Vector3(-6.9f, 5f, -7.95f));
            roofBearer.rotation = Quaternion.LookRotation(new Vector3(1f, 0f, 0.35f));
            string girlPath = $"{CharacterImporter.CharacterPrefabDir}/PF_Sister.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(girlPath) == null) girlPath = $"{CharacterImporter.CharacterPrefabDir}/PF_Villager_Woman.prefab";
            bool girlRigged = AssetDatabase.LoadAssetAtPath<GameObject>(girlPath) != null;
            var girlGo = PlaceAt(girlRigged ? girlPath : G.VillagerPath(1), terrace, new Vector3(-5.65f, 5f, -7.85f), Quaternion.LookRotation(new Vector3(-0.6f, 0f, 0.8f)));
            girlGo.name = "Sister";
            if (girlRigged && girlGo.GetComponent<NasconGJ.Player.ProceduralHumanoid>() == null) girlGo.AddComponent<NasconGJ.Player.ProceduralHumanoid>();
            var girl = girlGo.GetComponent<Villager>() ?? girlGo.AddComponent<Villager>();
            GenUtil.Set(girl, "strollOffset", Vector3.zero);
            GenUtil.Set(girl, "smokeMaterial", smokeMat);

            var roofWideFrom = Mark("Roof_Wide_From", marks, new Vector3(-10.6f, 7.5f, -12.8f), new Vector3(2f, 5f, 2f));
            var roofWideTo = Mark("Roof_Wide_To", marks, new Vector3(-9.7f, 7.1f, -12f), new Vector3(2f, 5.3f, 2f));
            var roofGirl = Mark("Roof_Girl", marks, new Vector3(-7.45f, 6.55f, -9.45f), new Vector3(-5.65f, 6.4f, -7.85f)); // the town and sky behind her // over the bearer's left shoulder, torch out of frame
            var roofBearerCam = Mark("Roof_Bearer_Cam", marks, new Vector3(-4.8f, 6.6f, -8.8f), new Vector3(-6.9f, 6.4f, -7.95f));
            var roofLookUp = Mark("Roof_LookUp", marks, new Vector3(-6.2f, 5.6f, -5.9f), new Vector3(-6.3f, 7.1f, -8f));
            var roofVoid = Mark("Roof_Void", marks, new Vector3(-10f, 6.5f, -8.7f), new Vector3(70f, 22f, -1f)); // their backs in the foreground

            var craneFrom = Mark("Crane_From", marks, new Vector3(-12f, 24f, -30f), new Vector3(-3f, 3f, 6f)); // high over the roofs, then down onto the street
            var craneTo = Mark("Crane_To", marks, new Vector3(-9.5f, 3.4f, -4.4f), new Vector3(-4f, 1.7f, 2f));
            var lookUpFrom = Mark("LookUp_From", marks, new Vector3(-4.2f, 1.45f, -1.6f), new Vector3(-5.4f, 1.58f, -0.15f));
            var lookUpTo = Mark("LookUp_To", marks, new Vector3(-4.5f, 1.4f, -1.25f), new Vector3(-5.4f, 1.6f, -0.15f));
            var riseFrom = Mark("Rise_From", marks, new Vector3(-12f, 1.2f, -2.2f), new Vector3(40f, 8f, -0.8f));
            var riseTo = Mark("Rise_To", marks, new Vector3(-10.5f, 0.8f, -2.4f), new Vector3(70f, 22f, -0.8f));
            var floodFrom = Mark("Flood_From", marks, new Vector3(-10f, 1.7f, -2.6f), new Vector3(20f, 3.5f, -0.5f));
            var floodTo = Mark("Flood_To", marks, new Vector3(-10.8f, 1.6f, -2.7f), new Vector3(20f, 3.2f, -0.5f));
            var imprintFrom = Mark("Imprint_From", marks, new Vector3(-4.6f, 1.6f, -4.6f), new Vector3(-7.1f, 1.2f, 4.1f));
            var imprintTo = Mark("Imprint_To", marks, new Vector3(-5.6f, 1.45f, -1.9f), new Vector3(-7.1f, 1.15f, 4.1f));
            var darkFrom = Mark("Dark_From", marks, new Vector3(-40f, 12f, -3f), new Vector3(70f, 18f, -1f));
            var darkTo = Mark("Dark_To", marks, new Vector3(-34f, 9f, -2.5f), new Vector3(70f, 20f, -1f));

            // ---------------- 2. the wasteland path
            Ground(path, new Vector3(200, 0, 12), new Vector3(60, 1, 60), MatId.StoneDark);
            foreach (var (x, z, s, yaw) in new[] { (192.5f, 4f, 1.6f, 20f), (208.5f, 9f, 2f, 130f), (191.2f, 15f, 1.8f, 250f), (207f, 1f, 1.3f, 75f), (209f, 21f, 2.4f, 10f) })
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
            var music = Source("Music", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/MUS_TownTheme.ogg"), true, true, 0f);
            var dread = Source("Dread", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Music/MUS_Presence.mp3"), true, false, 0f);
            var sfx = Source("SFX", null, false, false, 1f);
            var townAmbience = Source("TownBirds", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/AMB_Town.ogg"), true, false, 0f);
            var townMurmur = Source("TownMurmur", null, true, false, 0f); // the murmur is baked into AMB_Town now

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
            GenUtil.Set(director, "lookUpFrom", lookUpFrom);
            GenUtil.Set(director, "lookUpTo", lookUpTo);
            GenUtil.Set(director, "floodFrom", floodFrom);
            GenUtil.Set(director, "floodTo", floodTo);
            GenUtil.Set(director, "imprintFrom", imprintFrom);
            GenUtil.Set(director, "imprintTo", imprintTo);
            GenUtil.Set(director, "imprintLight", imprintLight);
            GenUtil.SetArray(director, "villagers", villagers.ToArray());
            GenUtil.SetArray(director, "imprints", imprints.ToArray());
            GenUtil.SetArray(director, "lamps", lamps.ToArray());
            GenUtil.SetArray(director, "glowingWindows", windows.ToArray());
            GenUtil.Set(director, "voidApparition", voidApparition);
            GenUtil.Set(director, "voidWash", voidWash);
            GenUtil.Set(director, "bearer", actor);
            GenUtil.Set(director, "girl", girl);
            var voiceSrc = Source("Voice", null, false, false, 1f);
            GenUtil.Set(director, "voice", voiceSrc);
            var ducker = director.gameObject.AddComponent<VoiceDucker>();
            GenUtil.Set(ducker, "voice", voiceSrc);
            GenUtil.SetArray(director, "voiceLines", new Object[] { AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_Y01_OldTomb.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_Y02_Grandpa.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_R01_WellTalks.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_Y03_SoQuiet.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_P01_Emptied.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_P02_OneSoul.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_P03_ManyWalked.ogg"), AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Voice/VO_P04_NoneWalkedOut.ogg") });
            GenUtil.Set(director, "thunderSource", Source("ThunderRoll", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_ThunderVoid.ogg"), false, false, 1f));
            GenUtil.Set(director, "windAmbience", Source("VoidWind", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/AMB_VoidWind.ogg"), true, false, 0f));
            GenUtil.Set(director, "insideAmbience", Source("TombInside", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/AMB_TombInside.ogg"), true, false, 0f));
            GenUtil.Set(director, "padSource", Source("LonePad", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/MUS_LonePad.ogg"), true, false, 0f));
            GenUtil.Set(director, "fallClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_FallWhoosh.ogg"));
            GenUtil.Set(director, "cryClip", AudioGenerator.Real("SFX_Heartbeat.wav", "SFX_Heartbeat")); // a slow heartbeat in the black
            GenUtil.Set(director, "lowSource", Source("LowSFX", null, false, false, 1f));
            GenUtil.Set(director, "laughClip", AudioGenerator.Real("SFX_ChildrenLaugh.ogg", "SFX_Whoosh"));
            GenUtil.Set(director, "crackleClip", AudioGenerator.Real("SFX_FireCrackleShort.wav", "SFX_Collapse"));
            GenUtil.Set(director, "swishHeavyClip", AudioGenerator.Real("SFX_Swish_Heavy.wav", "SFX_Whoosh"));
            GenUtil.Set(director, "stormAmbience", Source("Storm", AudioGenerator.Real("AMB_RainThunder.ogg", "AMB_Wind"), true, false, 0f));
            GenUtil.Set(director, "followStart", new Vector3(-0.9f, 1.55f, 2.1f)); // torch side away from the lens: we see the face
            GenUtil.Set(director, "followEnd", new Vector3(-1.9f, 1.6f, 0.9f));
            if (actor != null) foreach (var l in actor.GetComponentsInChildren<Light>(true)) l.intensity *= 0.5f;
            GenUtil.Set(director, "roofBearer", roofBearer);
            GenUtil.Set(director, "roofWideFrom", roofWideFrom);
            GenUtil.Set(director, "roofWideTo", roofWideTo);
            GenUtil.Set(director, "roofGirl", roofGirl);
            GenUtil.Set(director, "roofBearerCam", roofBearerCam);
            GenUtil.Set(director, "roofLookUp", roofLookUp);
            GenUtil.Set(director, "roofVoid", roofVoid);
            if (actor != null) GenUtil.Set(girl, "talkTo", actor.transform);
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
            GenUtil.Set(director, "thunderClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_ThunderStrike.ogg"));
            GenUtil.Set(director, "collapseClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_Collapse.ogg"));
            GenUtil.Set(director, "impactClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_ImpactHeavy.ogg"));
            GenUtil.Set(director, "townAmbience", townAmbience);
            GenUtil.Set(director, "townMurmur", townMurmur);
            GenUtil.Set(director, "whooshClip", AudioGenerator.Real("SFX_Swish_Light.wav", "SFX_Whoosh"));
            GenUtil.Set(director, "riserClip", AudioGenerator.Real("SFX_Swish_Heavy.wav", "SFX_Riser"));
            GenUtil.Set(director, "boomClip", AudioGenerator.Real("SFX_DeepBoom.wav", "SFX_Boom"));
            GenUtil.Set(director, "ringClip", AudioGenerator.Real("SFX_Gong_2.ogg", "SFX_Ring"));
            GenUtil.Set(director, "heartbeatClip", AudioGenerator.Real("SFX_Heartbeat.wav", "SFX_Heartbeat"));

            // ---------------- the fall: memories flashing past on a glitching tape
            ComicLook.Apply(0.7f, 0.55f, 0f);
            Shader.SetGlobalFloat("_NasconBrushReduce", 0f);
            var memoryFall = BuildMemoryFall(ui);
            GenUtil.Set(director, "memoryFall", memoryFall);

            // sanity: no camera move may pass through geometry either
            foreach (var (from, to) in new[] { (craneFrom, craneTo), (roofWideFrom, roofWideTo), (floodFrom, floodTo), (imprintFrom, imprintTo),
                         (darkFrom, darkTo), (tombFrom, tombTo), (doorwayFrom, doorwayTo), (lookUpFrom, lookUpTo), (roofVoid, roofVoid), (roofGirl, roofGirl), (roofBearerCam, roofBearerCam), (roofLookUp, roofLookUp) })
            {
                Vector3 a = from.position, b = to.position;
                float len = Mathf.Max(0.01f, (b - a).magnitude);
                foreach (var hit in Physics.SphereCastAll(a, 0.2f, (b - a) / len, len))
                    if (!hit.collider.isTrigger) Debug.LogWarning($"[Gen] camera {from.name} -> {to.name} passes through {hit.collider.name} at {hit.point}");
                foreach (var c in Physics.OverlapSphere(a, 0.2f)) if (!c.isTrigger) Debug.LogWarning($"[Gen] camera {from.name} starts inside {c.name}");
            }

            // sanity: the bearer's walks must not cut through any collider (they're scripted, nothing stops them)
            Physics.SyncTransforms();
            foreach (var (label, path2) in new[] { ("walk", walkPath), ("tomb", tombPath), ("inside", insidePath) })
                for (int i = 0; i + 1 < path2.Length; i++)
                {
                    Vector3 a = path2[i].position + Vector3.up * 0.4f, b = path2[i + 1].position + Vector3.up * 0.4f;
                    foreach (var hit in Physics.CapsuleCastAll(a, a + Vector3.up * 1.0f, 0.3f, (b - a).normalized, (b - a).magnitude))
                        if (!hit.collider.isTrigger && hit.collider.gameObject.layer != LayerMask.NameToLayer(GameLayers.Ground))
                            Debug.LogWarning($"[Gen] {label} path {i}->{i + 1} passes through {hit.collider.name} ({hit.collider.transform.parent?.name}) at {hit.point}");
                }

            MaterialLibrary.BuildCinematicProfile();
            var filmGo = new GameObject("Volume_Cinematic");
            filmGo.transform.SetParent(systems, false);
            var film = filmGo.AddComponent<Volume>();
            film.isGlobal = true;
            film.priority = 30f;
            film.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MaterialLibrary.CinematicProfilePath);

            camGo.transform.SetPositionAndRotation(craneFrom.position, craneFrom.rotation);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scenes + "Cutscenes/" + SceneNames.PrologueTown + ".unity");
        }

        /// <summary>The Void as a sky-sized silhouette: a quad with the shadow-creature sheet, smoke pouring off it.</summary>
        private static VoidApparition BuildVoidApparition(Transform parent, Vector3 feet, float height)
        {
            var root = new GameObject("Void_Apparition");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, -90f, 0f)); // faces back down the street (-X)
            var head = Sub("Head", parent);
            var boltLight = MakeLight("VoidBoltLight", root.transform, LightType.Directional, Vector3.zero, new Color(0.75f, 0.7f, 1f), 0f, 10f);
            boltLight.transform.rotation = Quaternion.Euler(35f, 90f, 0f);
            boltLight.shadows = LightShadows.None;
            var app = root.AddComponent<VoidApparition>();
            GenUtil.Set(app, "vineMaterial", MaterialLibrary.Get(MaterialLibrary.VoidVineMat));
            GenUtil.Set(app, "heartMaterial", MaterialLibrary.Get(MaterialLibrary.ShadowHeartMat));
            GenUtil.Set(app, "boltMaterial", MaterialLibrary.Get(MaterialLibrary.LightningMat));
            GenUtil.Set(app, "height", height);
            GenUtil.Set(app, "hiddenDrop", height * 1.1f);
            GenUtil.Set(app, "head", head);
            GenUtil.Set(app, "boltLight", boltLight);
            return app;
        }

        private static string MemoryDir => GenUtil.TextureDir + "/Memories";

        /// <summary>Render the camera from a mark into a 960x540 PNG under Textures/Generated/Memories (used by the memory fall).</summary>
        private static Texture2D RenderStill(Camera cam, Transform mark, float fov, string name)
        {
            GenUtil.EnsureFolder(MemoryDir);
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            Vector3 p0 = cam.transform.position; Quaternion r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
            cam.transform.SetPositionAndRotation(mark.position, mark.rotation);
            cam.fieldOfView = fov;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = prev;
            cam.transform.SetPositionAndRotation(p0, r0);
            cam.fieldOfView = f0;
            rt.Release();
            Object.DestroyImmediate(rt);
            string path = $"{MemoryDir}/{name}.png";
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Full-screen overlay for the fall: black, a glitching memory image, a white flash, and the track (slightly distorted).</summary>
        private static MemoryFall BuildMemoryFall(Transform parent)
        {
            var root = new GameObject("MemoryFall", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            RectTransform Full(string name, System.Type type)
            {
                var go = new GameObject(name, typeof(RectTransform), type);
                go.transform.SetParent(root.transform, false);
                var r = (RectTransform)go.transform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
                return r;
            }
            Full("Black", typeof(Image)).GetComponent<Image>().color = Color.black;
            var img = Full("Memory", typeof(RawImage)).GetComponent<RawImage>();
            var glitch = MaterialLibrary.LoadOrCreate("M_MemoryGlitch", "NASCON/MemoryGlitch");
            glitch.SetColor("_Tint", new Color(0.86f, 0.84f, 0.82f, 1f)); // memories drained to grey; the blood is in the flash frames
            glitch.SetFloat("_Fade", 1f);
            glitch.SetFloat("_Noise", 0.65f);
            EditorUtility.SetDirty(glitch);
            img.material = glitch;
            img.raycastTarget = false;
            var flash = Full("Flash", typeof(Image)).GetComponent<Image>();
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;

            var track = root.AddComponent<AudioSource>();
            track.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Music/MUS_MemoryFall.mp3");
            track.playOnAwake = false;
            track.spatialBlend = 0f;
            root.AddComponent<AudioDistortionFilter>().distortionLevel = 0.42f; // torn, overdriven
            root.AddComponent<AudioLowPassFilter>().cutoffFrequency = 5200f;
            var echo = root.AddComponent<AudioEchoFilter>();
            echo.delay = 140f;
            echo.decayRatio = 0.35f;
            echo.wetMix = 0.4f;

            AudioSource Child(string n, AudioClip c, bool loop)
            {
                var go = new GameObject(n);
                go.transform.SetParent(root.transform, false);
                var a = go.AddComponent<AudioSource>();
                a.clip = c; a.loop = loop; a.playOnAwake = false; a.spatialBlend = 0f;
                return a;
            }
            var staticSrc = Child("TapeStatic", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_TapeStatic.ogg"), true);
            var voice = Child("Voice", null, false);
            voice.gameObject.AddComponent<AudioEchoFilter>().wetMix = 0.25f;
            var fall = root.AddComponent<MemoryFall>();
            GenUtil.Set(fall, "staticSource", staticSrc);
            GenUtil.Set(fall, "voice", voice);
            GenUtil.Set(fall, "screamClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/SFX_MemoryScream.ogg"));
            GenUtil.Set(fall, "group", root.GetComponent<CanvasGroup>());
            GenUtil.Set(fall, "image", img);
            GenUtil.Set(fall, "flash", flash);
            GenUtil.Set(fall, "track", track);
            var mems = new List<Object>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { MemoryDir }))
                mems.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid)));
            GenUtil.SetArray(fall, "memories", mems.ToArray());
            return fall;
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
