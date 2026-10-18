using NasconGJ.Core;
using NasconGJ.Entities;
using NasconGJ.Horror;
using NasconGJ.Items;
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
        // ------------------------------------------------------------------------------------------------------------
        //  The escape. In L05 the chase starts the moment you lift the crystal: the hallway is built onto the crystal
        //  room's north doorway (z 16) and runs 100 m along +z to white light. Rubble, broken pillars, slabs that drop
        //  from the ceiling. All of it sits under one "--- CHASE ---" root; the room's old exit is switched off.
        //  L06 gets the same hallway behind a small shrine and starts the chase on its own (a quick test level).
        // ------------------------------------------------------------------------------------------------------------
        private const string CrystalScenePath = GenUtil.Root + "/Scenes/Levels/L05_Crystal.unity";
        private const string EscapeScenePath = GenUtil.Root + "/Scenes/Levels/L06_Escape.unity";
        private const float ChaseLength = 100f;
        private const float L05DoorZ = 16f;

        // (x, z from the hallway start, kit piece or "slab")
        private static readonly (float x, float z, string piece)[] ChaseObstacles =
        {
            (-1.0f, 12f, K.BlockLarge),
            ( 0.9f, 18f, K.PillarBroken),
            (-0.8f, 24f, "slab"),
            ( 1.0f, 30f, K.BlockLarge), (-1.25f, 31f, K.BlockSmall),
            (-0.9f, 38f, K.PillarBroken),
            ( 0.8f, 44f, "slab"),
            ( 0.0f, 50f, K.BlockLarge),
            (-0.8f, 57f, "slab"), (0.8f, 61f, "slab"),
            ( 1.0f, 67f, K.PillarBroken), (-1.3f, 66f, K.BlockSmall),
            (-1.0f, 74f, K.BlockLarge),
            ( 0.9f, 80f, "slab"),
            (-0.9f, 86f, K.PillarBroken),
            ( 1.1f, 92f, K.BlockSmall),
        };

        [MenuItem("NASCON/Levels/Build Chase (L05 crystal + L06)")]
        public static void BuildChase()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            BuildChaseInL05();
            BuildChaseInL06();
            EditorSceneManager.OpenScene(CrystalScenePath);
        }

        private static void BuildChaseInL05()
        {
            var scene = EditorSceneManager.OpenScene(CrystalScenePath);
            var old = GameObject.Find("--- CHASE ---");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("--- CHASE ---").transform;

            ChaseHallway(root, L05DoorZ, out var slabs, out var endLights);

            // the crystal no longer fades out to another level: the escape starts right here
            var crystal = Object.FindAnyObjectByType<CrystalPickup>();
            if (crystal != null) GenUtil.Set(crystal, "continueInScene", true);
            // the room's old exit trigger at the doorway would load the next level: off
            var exits = new System.Collections.Generic.List<GameObject>();
            foreach (var e in Object.FindObjectsByType<Puzzles.LevelExit>(FindObjectsInactive.Include)) { e.gameObject.SetActive(false); }
            // the room's own shadows give way to the thing that comes out of the walls
            var shadows = GameObject.Find("Shadows");
            if (shadows != null) exits.Add(shadows);

            // the doorway out of the crystal room is walled up: that wall comes down when the dark comes
            var sealing = new System.Collections.Generic.List<GameObject>();
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))
            {
                var t = mf.transform;
                if (t.IsChildOf(root) || !t.name.StartsWith(K.Wall) || t.name.StartsWith(K.WallDoorway)) continue;
                if ((t.position - new Vector3(0f, 0f, L05DoorZ)).sqrMagnitude < 0.05f) sealing.Add(t.gameObject);
            }
            // and whatever stands in front of the doorway (the obelisk) comes down with it
            foreach (var c in Physics.OverlapBox(new Vector3(0f, 1f, L05DoorZ - 1.2f), new Vector3(1.1f, 0.8f, 1.0f)))
            {
                var go = PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject) ?? c.gameObject;
                if (go.transform.IsChildOf(root) || go.name.StartsWith(K.Wall) || go.name.StartsWith(K.WallDoorway) || go.name.StartsWith(K.Floor)) continue;
                if (go.GetComponentInChildren<CrystalPickup>() != null || go.CompareTag(GameTags.Player)) continue;
                if (!sealing.Contains(go)) sealing.Add(go);
            }
            var rubble = new System.Collections.Generic.List<GameObject>();
            var rubbleRoot = new GameObject("CollapsedWall").transform; rubbleRoot.SetParent(root, false);
            rubble.Add(Place(K.RubbleB, rubbleRoot, new Vector3(-1.4f, 0f, L05DoorZ - 0.9f), 20f));
            rubble.Add(Place(K.RubbleA, rubbleRoot, new Vector3(1.5f, 0f, L05DoorZ - 0.7f), 140f));
            rubble.Add(Place(K.BlockSmall, rubbleRoot, new Vector3(-2.3f, 0f, L05DoorZ - 1.2f), 33f));
            rubble.Add(Place(K.RubbleB, rubbleRoot, new Vector3(0.6f, 0f, L05DoorZ + 1.4f), 260f));
            foreach (var g in rubble) g.SetActive(false);

            var retry = new GameObject("ChaseRetryPoint").transform;
            retry.SetParent(root, false);
            retry.SetPositionAndRotation(crystal != null ? new Vector3(crystal.transform.position.x, 0.05f, crystal.transform.position.z - 1.4f) : new Vector3(0f, 0.05f, 9f), Quaternion.identity);

            var chase = ChaseDirectorObject(root, slabs, endLights);
            GenUtil.Set(chase, "waitForCrystal", true);
            GenUtil.Set(chase, "retryPoint", retry);
            GenUtil.SetArray(chase, "disableOnBegin", exits.ToArray());
            GenUtil.SetArray(chase, "collapseOnBegin", sealing.ToArray());
            GenUtil.SetArray(chase, "rubbleOnBegin", rubble.ToArray());
            GenUtil.Set(chase, "hallStartZ", L05DoorZ);
            GenUtil.Set(chase, "lightZ", L05DoorZ + ChaseLength - 3f);
            GenUtil.Set(chase, "entityBehind", 7.5f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Chase] L05: hallway z {L05DoorZ}..{L05DoorZ + ChaseLength}, {slabs.Length} slabs, {sealing.Count} sealing wall(s), crystal {(crystal != null ? "hooked" : "MISSING")}");
        }

        private static void BuildChaseInL06()
        {
            var scene = EditorSceneManager.OpenScene(EscapeScenePath);
            foreach (var n in new[] { "--- CHASE ---", "Kit", "Braziers", "ExitGlow", "Puzzle" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }
            foreach (var n in new[] { "Braziers_Torches", "Items" })
            {
                var holder = GameObject.Find(n);
                if (holder != null) for (int i = holder.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(holder.transform.GetChild(i).gameObject);
            }
            foreach (var label in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include)) Object.DestroyImmediate(label.gameObject);

            var root = new GameObject("--- CHASE ---").transform;
            var geo = new GameObject("Shrine").transform; geo.SetParent(root, false);
            // the shrine where the crystal lay (z -12..0), open onto the hallway
            for (float z = -10f; z <= -2f; z += 4f)
            {
                Place(K.Floor, geo, new Vector3(0f, 0f, z));
                Place(K.Ceiling, geo, new Vector3(0f, 4f, z));
                Place(K.Wall, geo, new Vector3(-2f, 0f, z), 90f);
                Place(K.Wall, geo, new Vector3(2f, 0f, z), 90f);
            }
            Place(K.Wall, geo, new Vector3(0f, 0f, -12f));
            Place(K.Altar, geo, new Vector3(0f, 0f, -10.2f));
            MakeLight("ShrineGlow", geo, LightType.Point, new Vector3(0f, 2.6f, -6f), new Color(0.8f, 0.88f, 1f), 4.5f, 10f);

            ChaseHallway(root, 0f, out var slabs, out var endLights);
            var chase = ChaseDirectorObject(root, slabs, endLights);
            GenUtil.Set(chase, "waitForCrystal", false);
            GenUtil.Set(chase, "hallStartZ", 0f);
            GenUtil.Set(chase, "lightZ", ChaseLength - 3f);
            GenUtil.Set(chase, "entityBehind", 6f);

            var player = GameObject.FindWithTag(GameTags.Player);
            if (player != null)
            {
                player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -1.5f), Quaternion.identity);
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null) GenUtil.Set(inv, "igniteStartingItem", true);
            }
            var spawn = GameObject.Find("PlayerSpawn");
            if (spawn != null) spawn.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -1.5f), Quaternion.identity);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The 100 m hallway from <paramref name="z0"/> along +z, its obstacles, its lights and the white room.</summary>
        private static void ChaseHallway(Transform root, float z0, out Rigidbody[] slabArray, out Light[] endLights)
        {
            var geo = new GameObject("Geometry").transform; geo.SetParent(root, false);
            var props = new GameObject("Obstacles").transform; props.SetParent(root, false);
            var lights = new GameObject("Lights").transform; lights.SetParent(root, false);

            for (float z = z0 + 2f; z <= z0 + ChaseLength + 6f; z += 4f)
            {
                Place(K.Floor, geo, new Vector3(0f, 0f, z));
                Place(K.Ceiling, geo, new Vector3(0f, 4f, z));
                Place(K.Wall, geo, new Vector3(-2f, 0f, z), 90f);
                Place(K.Wall, geo, new Vector3(2f, 0f, z), 90f);
            }
            Place(K.Wall, geo, new Vector3(0f, 0f, z0 + ChaseLength + 8f));

            var rng = new System.Random(606);
            for (float z = 4f; z < ChaseLength - 4f; z += 5.5f)
                Place(rng.NextDouble() < 0.5 ? K.RubbleA : K.RubbleB, geo, new Vector3((float)(rng.NextDouble() * 2.4 - 1.2), 0f, z0 + z + (float)rng.NextDouble() * 2f), (float)rng.NextDouble() * 360f);

            var stone = Prefab(K.BlockLarge).GetComponent<Renderer>().sharedMaterials[0];
            var slabs = new System.Collections.Generic.List<Rigidbody>();
            foreach (var (x, z, piece) in ChaseObstacles)
            {
                if (piece != "slab") { Place(piece, props, new Vector3(x, 0f, z0 + z), (float)rng.NextDouble() * 360f).isStatic = true; continue; }
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "FallingSlab";
                slab.transform.SetParent(props, false);
                slab.transform.position = new Vector3(x, 3.45f, z0 + z);
                slab.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 30f - 15f, 0f);
                slab.transform.localScale = new Vector3(1.5f, 0.55f, 1.2f);
                slab.GetComponent<Renderer>().sharedMaterial = stone;
                var rb = slab.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.mass = 400f;
                slabs.Add(rb);
            }
            slabArray = slabs.ToArray();

            for (float z = 4f; z < ChaseLength - 6f; z += 8f)
                MakeLight("Sconce", lights, LightType.Point, new Vector3(((int)(z / 8f) % 2 == 0 ? -1.3f : 1.3f), 2.8f, z0 + z), new Color(1f, 0.6f, 0.35f), 3.2f, 8f);
            endLights = new[]
            {
                MakeLight("WhiteLight_A", lights, LightType.Point, new Vector3(0f, 2.4f, z0 + ChaseLength + 1f), Color.white, 6f, 18f),
                MakeLight("WhiteLight_B", lights, LightType.Point, new Vector3(0f, 2.4f, z0 + ChaseLength + 6f), Color.white, 9f, 24f),
            };
            var white = GameObject.CreatePrimitive(PrimitiveType.Quad);
            white.name = "TheLight";
            Object.DestroyImmediate(white.GetComponent<Collider>());
            white.transform.SetParent(lights, false);
            white.transform.position = new Vector3(0f, 1.9f, z0 + ChaseLength + 7.6f);
            white.transform.localScale = new Vector3(3.4f, 3.8f, 1f);
            var whiteR = white.GetComponent<Renderer>();
            whiteR.sharedMaterial = WhiteLightMaterial();
            whiteR.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static ChaseDirector ChaseDirectorObject(Transform root, Rigidbody[] slabs, Light[] endLights)
        {
            var dir = new GameObject("ChaseDirector");
            dir.transform.SetParent(root, false);
            var chase = dir.AddComponent<ChaseDirector>();
            var sfx = dir.AddComponent<AudioSource>(); sfx.playOnAwake = false; sfx.spatialBlend = 0f;
            AudioSource Loop(string clip, string name)
            {
                var go = new GameObject(name); go.transform.SetParent(dir.transform, false);
                var a = go.AddComponent<AudioSource>();
                a.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/" + clip);
                a.loop = true; a.playOnAwake = false; a.spatialBlend = 0f; a.volume = 0f;
                return a;
            }
            AudioClip Clip(string f) => AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Designed/" + f);
            var creature = GenUtil.LoadPrefab(G.ShadowEnemyPath).GetComponentInChildren<ShadowSurfaceSkin>(true).GetComponent<MeshRenderer>().sharedMaterial;
            GenUtil.Set(chase, "corridorX", 0f);
            GenUtil.Set(chase, "massMaterial", ChaseMaterial("M_ShadowMass", "NASCON/ShadowMass"));
            GenUtil.Set(chase, "ashMaterial", ChaseMaterial("M_ChaseAsh", "NASCON/AshParticle"));
            GenUtil.Set(chase, "lungeMaterial", creature);
            GenUtil.Set(chase, "torchPrefab", AssetDatabase.LoadAssetAtPath<CarryableItem>(G.TorchPath));
            GenUtil.SetArray(chase, "slabs", slabs);
            GenUtil.SetArray(chase, "endLights", endLights);
            GenUtil.Set(chase, "sfx", sfx);
            GenUtil.Set(chase, "windLoop", Loop("AMB_VoidWind.ogg", "Wind"));
            GenUtil.Set(chase, "whisperLoop", Loop("SFX_Whisper.ogg", "Whispers"));
            GenUtil.Set(chase, "collapseClip", Clip("SFX_Collapse.ogg"));
            GenUtil.Set(chase, "screechClip", Clip("SFX_Screech.ogg"));
            GenUtil.Set(chase, "dreadClip", Clip("SFX_DreadHit.ogg"));
            GenUtil.Set(chase, "heartbeatClip", Clip("SFX_Heartbeat.ogg"));
            GenUtil.Set(chase, "impactClip", Clip("SFX_ImpactHeavy.ogg"));
            return chase;
        }

        private static Material ChaseMaterial(string name, string shader)
        {
            string path = GenUtil.Root + "/Art/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material WhiteLightMaterial()
        {
            const string path = GenUtil.Root + "/Art/Materials/M_ChaseWhiteLight.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.white };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
