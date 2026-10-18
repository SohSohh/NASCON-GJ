using System.Collections.Generic;
using NasconGJ.Core;
using NasconGJ.Items;
using NasconGJ.Puzzles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Joins the levels with identical twisting passages (see <see cref="LevelPassage"/>): one beyond each level's exit
    /// room, one behind each level's start. The level's wall there becomes a doorway (the original wall is only switched
    /// off). Placement is checked against the level so a passage never cuts through it.
    ///   Tiles (4 m), local +z = direction of travel:  A(0,0) B(0,1) C(1,1) D(2,1) E(2,2) F(2,3)
    ///   A's back edge is the doorway out of the old level; F's front edge the doorway into the new one.
    ///   The switch happens in C, from where neither doorway can be seen.
    /// </summary>
    public static class LevelPassageBuilder
    {
        private static readonly Vector2Int[] Path = { new(0, 0), new(0, 1), new(1, 1), new(2, 1), new(2, 2), new(2, 3) };
        private const float Tile = 4f, CeilingY = 2.7f;
        private const string ExitRoot = "--- PASSAGE OUT ---", ArrivalRoot = "--- PASSAGE IN ---";

        private static readonly (string scene, bool exit, bool arrival)[] Levels =
        {
            ("Levels/L01_TombEntrance", true, false),
            ("Levels/L02_NarrowPath", true, true),
            ("Mazes/M01_SimpleMaze", true, true),
            ("Levels/L03_MultiStory", true, true),
            ("Mazes/M02_ComplexMaze", true, true),
            ("Levels/L05_Crystal", false, true),   // leaves through the crystal (its own fade), not a passage
        };

        [MenuItem("NASCON/Levels/Build Level Passages")]
        public static void BuildAll()
        {
            Diag.Clear();
            var log = new System.Text.StringBuilder();
            foreach (var (scene, exit, arrival) in Levels)
            {
                var s = EditorSceneManager.OpenScene($"{GenUtil.Root}/Scenes/{scene}.unity");
                foreach (var name in new[] { ExitRoot, ArrivalRoot }) { var old = GameObject.Find(name); if (old != null) Restore(old); }
                Diag.Clear();
                if (exit) log.AppendLine($"{scene} out: {BuildExit()}   {Diag}");
                Diag.Clear();
                if (arrival) log.AppendLine($"{scene} in: {BuildArrival()}   {Diag}");
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
            }
            Debug.Log("Level passages:\n" + log);
        }

        // -------------------------------------------------------------------------------------------- the two ends

        private static string BuildExit()
        {
            var exit = Object.FindAnyObjectByType<LevelExit>();
            if (exit == null) return "no LevelExit";
            Vector3 from = exit.GetComponent<Collider>().bounds.center;
            if (!FindWall(from, null, true, out var wall, out var outward, out bool mirror)) return "no free wall around the exit";
            var root = Root(ExitRoot, wall, outward, mirror, entryDoor: true, exitDoor: false);
            var passage = root.gameObject.AddComponent<LevelPassage>();
            GenUtil.Set(passage, "role", (int)LevelPassage.Role.Exit);
            var so = new SerializedObject(exit);
            GenUtil.Set(passage, "nextScene", so.FindProperty("nextSceneOverride").stringValue);
            GenUtil.Set(passage, "levelExit", exit);
            GenUtil.Set(passage, "enterTrigger", Box(root, "Enter", TileCenter(0, mirror) + new Vector3(0f, 1.3f, 1f), new Vector3(3.6f, 2.6f, 2f)));
            GenUtil.Set(passage, "swapTrigger", Box(root, "Swap", TileCenter(2, mirror) + Vector3.up * 1.3f, new Vector3(3.6f, 2.6f, 3.6f)));
            var block = Box(root, "Blocker", new Vector3(0f, 1.5f, 0.6f), new Vector3(3f, 3f, 0.3f));
            block.isTrigger = false;
            GenUtil.Set(passage, "blocker", block);
            return $"through {wall.name} at {wall.position:F1}, heading {outward:F0}{(mirror ? ", bends left" : "")}";
        }

        private static string BuildArrival()
        {
            var spawnGo = GameObject.Find("PlayerSpawn");
            var player = GameObject.FindWithTag(GameTags.Player);
            Vector3 at = spawnGo != null ? spawnGo.transform.position : player.transform.position;
            if (!Physics.Raycast(at + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore) && player != null) at = player.transform.position;   // a spawn marker in the void: use where the player stands
            Vector3 facing = spawnGo != null ? spawnGo.transform.forward : player.transform.forward;
            if (!FindWall(at + Vector3.up, -facing, false, out var wall, out var outward, out bool mirror)) return "no free wall around the start";
            var root = Root(ArrivalRoot, wall, outward, mirror, entryDoor: false, exitDoor: true);
            var passage = root.gameObject.AddComponent<LevelPassage>();
            GenUtil.Set(passage, "role", (int)LevelPassage.Role.Arrival);
            GenUtil.SetArray(passage, "itemPrefabs", new Object[]
            {
                AssetDatabase.LoadAssetAtPath<CarryableItem>($"{GenUtil.Root}/Prefabs/Items/PF_Torch.prefab"),
                AssetDatabase.LoadAssetAtPath<CarryableItem>($"{GenUtil.Root}/Prefabs/Items/PF_Stick.prefab"),
                AssetDatabase.LoadAssetAtPath<CarryableItem>($"{GenUtil.Root}/Prefabs/Items/PF_Candle.prefab"),
            });
            var torch = new GameObject("CheckpointTorch").transform;
            torch.SetParent(root, false);
            torch.localPosition = TileCenter(5, mirror) + new Vector3(mirror ? 1.2f : -1.2f, 0.12f, -0.6f);
            torch.localRotation = Quaternion.Euler(0f, 35f, 90f);
            GenUtil.Set(passage, "freshTorchPoint", torch);
            return $"through {wall.name} at {wall.position:F1}, heading in {(-outward):F0}{(mirror ? ", bends left" : "")}";
        }

        // -------------------------------------------------------------------------------------------- placement

        /// <summary>
        /// Exit: the passage starts at a wall around the point and runs outward (+z local). Arrival: it ends at such a
        /// wall, running inward. Tries the preferred side first, then the others; the bend either way.
        /// </summary>
        private static bool FindWall(Vector3 from, Vector3? preferred, bool isExit, out Transform wall, out Vector3 outward, out bool mirror)
        {
            var dirs = new List<Vector3> { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            if (preferred.HasValue)
            {
                Vector3 p = Vector3.ProjectOnPlane(preferred.Value, Vector3.up);
                dirs.Sort((a, b) => Vector3.Dot(b, p).CompareTo(Vector3.Dot(a, p)));
            }
            foreach (var d in dirs)
            {
                if (!Physics.Raycast(from, d, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) { Diag.Append($"[{d}: nothing within 8 m] "); continue; }
                var piece = hit.collider.transform;
                while (piece.parent != null && !piece.name.StartsWith("PF_Wall")) piece = piece.parent;
                if (!piece.name.StartsWith("PF_Wall_4x4")) { Diag.Append($"[{d}: {hit.collider.name} is not a kit wall] "); continue; }   // only plain kit walls become doorways
                Vector3 n = -d;                                                // the wall's face toward us
                Vector3 baseCenter = piece.position;                           // kit walls: pivot at the bottom centre
                // put the doorway's plane where the wall is, on our side's axis
                Vector3 doorPoint = new Vector3(Mathf.Abs(d.x) > 0.5f ? hit.point.x : baseCenter.x, baseCenter.y, Mathf.Abs(d.z) > 0.5f ? hit.point.z : baseCenter.z);
                foreach (bool m in new[] { false, true })
                {
                    Quaternion rot = Quaternion.LookRotation(isExit ? d : -d);
                    Vector3 origin = isExit ? doorPoint : doorPoint - rot * EndEdge(m);
                    var blocker = Blocking(origin, rot, m, piece);
                    if (blocker != null) { Diag.Append($"[{d}{(m ? " left" : "")}: blocked by {blocker}] "); continue; }
                    wall = piece; outward = d; mirror = m;
                    passageOrigin = origin; passageRotation = rot;
                    return true;
                }
            }
            wall = null; outward = Vector3.zero; mirror = false;
            return false;
        }

        private static Vector3 passageOrigin;
        private static Quaternion passageRotation;

        public static readonly System.Text.StringBuilder Diag = new();

        /// <summary>Name of whatever is in the passage's way, or null when it's clear (the doorway's own wall row excluded).</summary>
        private static string Blocking(Vector3 origin, Quaternion rot, bool mirror, Transform ignore)
        {
            for (int i = 0; i < Path.Length; i++)
            {
                // keep 0.9 m clear of the doorway edge so the walls either side of the door don't count
                Vector3 local = TileCenter(i, mirror) + Vector3.up * 1.4f + (i == 0 ? new Vector3(0f, 0f, 0.5f) : Vector3.zero);
                Vector3 half = i == 0 ? new Vector3(1.5f, 1.0f, 1.1f) : new Vector3(1.6f, 1.0f, 1.6f);
                foreach (var col in Physics.OverlapBox(origin + rot * local, half, rot, ~0, QueryTriggerInteraction.Ignore))
                    if (!col.transform.IsChildOf(ignore)) return col.name;
            }
            return null;
        }

        private static Vector3 TileCenter(int i, bool mirror) => new((mirror ? -1 : 1) * Path[i].x * Tile, 0f, Tile * 0.5f + Path[i].y * Tile);
        private static Vector3 EndEdge(bool mirror) => TileCenter(Path.Length - 1, mirror) + new Vector3(0f, 0f, Tile * 0.5f);

        // -------------------------------------------------------------------------------------------- geometry

        private static Transform Root(string name, Transform levelWall, Vector3 outward, bool mirror, bool entryDoor, bool exitDoor)
        {
            var root = new GameObject(name).transform;
            root.SetPositionAndRotation(passageOrigin, passageRotation);

            // the level's own wall becomes a doorway (switched off, never deleted; remembered for a rebuild)
            levelWall.gameObject.SetActive(false);
            root.gameObject.AddComponent<PassageWallMemo>().wall = levelWall.gameObject;
            var door = Kit("PF_Wall_Doorway_4x4", root, levelWall.position, levelWall.rotation);
            door.name = "LevelDoorway";

            var path = new HashSet<Vector2Int>(Path);
            for (int i = 0; i < Path.Length; i++)
            {
                var t = Path[i];
                Vector3 c = TileCenter(i, mirror);
                Kit("PF_Floor_4x4", root, root.TransformPoint(c), root.rotation);
                Kit("PF_Ceiling_4x4", root, root.TransformPoint(c + Vector3.up * CeilingY), root.rotation);
                foreach (var (step, edge, yaw) in new[] { (new Vector2Int(0, -1), new Vector3(0, 0, -2), 0f), (new Vector2Int(0, 1), new Vector3(0, 0, 2), 0f), (new Vector2Int(-1, 0), new Vector3(-2, 0, 0), 90f), (new Vector2Int(1, 0), new Vector3(2, 0, 0), 90f) })
                {
                    if (path.Contains(t + step)) continue;
                    bool isEntry = i == 0 && step.y == -1, isExit = i == Path.Length - 1 && step.y == 1;
                    if ((isEntry && entryDoor) || (isExit && exitDoor)) continue;   // the level's doorway is there
                    Vector3 e = edge; if (mirror) e.x = -e.x;
                    Kit("PF_Wall_4x4", root, root.TransformPoint(c + e), root.rotation * Quaternion.Euler(0f, yaw, 0f));
                }
            }
            return root;
        }

        private static GameObject Kit(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{GenUtil.Root}/Prefabs/Environment/Kit/{name}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            go.isStatic = true;
            return go;
        }

        private static BoxCollider Box(Transform root, string name, Vector3 localCenter, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = localCenter;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            go.layer = 2;
            return box;
        }

        private static void Restore(GameObject root)
        {
            var memo = root.GetComponent<PassageWallMemo>();
            if (memo != null && memo.wall != null) memo.wall.SetActive(true);
            Object.DestroyImmediate(root);
        }
    }
}
