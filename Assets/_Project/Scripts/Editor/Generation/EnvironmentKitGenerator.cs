using System;
using NasconGJ.Core;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Modular low-poly tomb kit on a 4 m grid. Every piece is built from slightly jittered stone
    /// blocks so it reads as hand-cut masonry under torchlight.
    ///
    /// Conventions: pivot at bottom-centre. Walls span X [-2, 2], Y [0, 4], thickness along Z.
    /// Floors/ceilings span 4x4 on XZ (floor top at y=0, ceiling underside at y=0).
    /// </summary>
    public static class EnvironmentKitGenerator
    {
        public const string Wall = "PF_Wall_4x4";
        public const string WallDoorway = "PF_Wall_Doorway_4x4";
        public const string Floor = "PF_Floor_4x4";
        public const string Ceiling = "PF_Ceiling_4x4";
        public const string Pillar = "PF_Pillar_4m";
        public const string PillarBroken = "PF_Pillar_Broken";
        public const string Stairs = "PF_Stairs_4w";
        public const string BlockLarge = "PF_Block_Large";
        public const string BlockSmall = "PF_Block_Small";
        public const string RubbleA = "PF_Rubble_A";
        public const string RubbleB = "PF_Rubble_B";
        public const string Altar = "PF_Altar";
        public const string Obelisk = "PF_Obelisk";
        public const string RockSmall = "PF_Rocks_Small";
        public const string RockLarge = "PF_Rocks_Large";
        public const string RockWall = "PF_RockWall_4x4";
        public const string SlabCollapsed = "PF_Slab_Collapsed";
        public const string Chain = "PF_Chain_6m";
        // prologue town (outdoors)
        public const string House = "PF_House";
        public const string HouseTall = "PF_House_Tall";
        public const string HouseWide = "PF_House_Wide";
        public const string LampPost = "PF_LampPost";
        public const string Bunting = "PF_Bunting_8m";
        public const string Barrel = "PF_Barrel";
        public const string Crate = "PF_Crate";
        public const string FlowerBox = "PF_FlowerBox";

        public static string PathOf(string prefabName) => $"{GenUtil.KitPrefabDir}/{prefabName}.prefab";

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(GenUtil.EnvMeshDir);
            GenUtil.EnsureFolder(GenUtil.KitPrefabDir);

            int wallLayer = LayerMask.NameToLayer(GameLayers.ShadowSurface);
            int groundLayer = LayerMask.NameToLayer(GameLayers.Ground);
            int blockerLayer = LayerMask.NameToLayer(GameLayers.LightBlocker);

            Make(Wall, 101, BuildWall, wallLayer, go => AddBox(go, new Vector3(0, 2, 0), new Vector3(4, 4, 0.6f)));
            Make(WallDoorway, 102, BuildDoorway, wallLayer, go =>
            {
                AddBox(go, new Vector3(-1.5f, 2f, 0), new Vector3(1f, 4f, 0.6f));
                AddBox(go, new Vector3(1.5f, 2f, 0), new Vector3(1f, 4f, 0.6f));
                AddBox(go, new Vector3(0, 3.5f, 0), new Vector3(2f, 1f, 0.6f));
            });
            Make(Floor, 103, BuildFloor, groundLayer, go => AddBox(go, new Vector3(0, -0.2f, 0), new Vector3(4, 0.4f, 4)));
            Make(Ceiling, 104, BuildCeiling, 0, go => AddBox(go, new Vector3(0, 0.2f, 0), new Vector3(4, 0.4f, 4)));
            Make(Pillar, 105, b => BuildPillar(b, 4f, false), blockerLayer, go => AddBox(go, new Vector3(0, 2, 0), new Vector3(0.9f, 4, 0.9f)));
            Make(PillarBroken, 106, b => BuildPillar(b, 2.2f, true), blockerLayer, go => AddBox(go, new Vector3(0, 1.2f, 0), new Vector3(0.9f, 2.4f, 0.9f)));
            Make(Stairs, 107, BuildStairs, groundLayer, go => go.AddComponent<MeshCollider>());
            Make(BlockLarge, 108, b => BuildBlock(b, 1.2f, 1.0f), blockerLayer, go => AddBox(go, new Vector3(0, 0.55f, 0), new Vector3(1.25f, 1.1f, 1.25f)));
            Make(BlockSmall, 109, b => BuildBlock(b, 0.6f, 0.55f), blockerLayer, go => AddBox(go, new Vector3(0, 0.3f, 0), new Vector3(0.62f, 0.6f, 0.62f)));
            Make(RubbleA, 110, b => BuildRubble(b, 9, 0.9f), 0, null);
            Make(RubbleB, 111, b => BuildRubble(b, 14, 1.4f), 0, null);
            Make(Altar, 112, BuildAltar, blockerLayer, go => AddBox(go, new Vector3(0, 0.55f, 0), new Vector3(2.4f, 1.1f, 1.6f)));
            Make(Obelisk, 113, BuildObelisk, blockerLayer, go => AddBox(go, new Vector3(0, 1.75f, 0), new Vector3(0.8f, 3.5f, 0.8f)));

            Make(RockSmall, 114, bb => BuildRocks(bb, 7, 0.9f, 0.25f, 0.8f), blockerLayer, go => go.AddComponent<MeshCollider>());
            Make(RockLarge, 115, bb => BuildRocks(bb, 13, 2.2f, 0.5f, 1.7f), blockerLayer, go => go.AddComponent<MeshCollider>());
            Make(RockWall, 116, BuildRockWall, wallLayer, go => go.AddComponent<MeshCollider>());
            Make(SlabCollapsed, 117, BuildCollapsedSlab, blockerLayer, go => go.AddComponent<MeshCollider>());
            Make(Chain, 118, BuildChain, 0, null);

            Make(House, 119, b => BuildHouse(b, 4.2f, 3.6f, 2.9f, false), wallLayer, go => AddBox(go, new Vector3(0, 1.5f, 0), new Vector3(4.2f, 3f, 3.6f)));
            Make(HouseTall, 120, b => BuildHouse(b, 3.8f, 3.6f, 5f, true), wallLayer, go => AddBox(go, new Vector3(0, 2.5f, 0), new Vector3(3.8f, 5f, 3.6f)));
            Make(HouseWide, 121, b => BuildHouse(b, 6.2f, 4f, 3.2f, false), wallLayer, go => AddBox(go, new Vector3(0, 1.6f, 0), new Vector3(6.2f, 3.2f, 4f)));
            Make(LampPost, 122, BuildLampPost, blockerLayer, go => AddBox(go, new Vector3(0, 1.4f, 0), new Vector3(0.25f, 2.8f, 0.25f)));
            Make(Bunting, 123, BuildBunting, 0, null);
            Make(Barrel, 124, BuildBarrel, blockerLayer, go => AddBox(go, new Vector3(0, 0.45f, 0), new Vector3(0.62f, 0.9f, 0.62f)));
            Make(Crate, 125, b => b.Box(MatId.WoodLight, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.6f, 0.6f)).Box(MatId.Wood, new Vector3(0, 0.3f, 0), new Vector3(0.64f, 0.08f, 0.64f)), blockerLayer, go => AddBox(go, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.6f, 0.6f)));
            Make(FlowerBox, 126, BuildFlowerBox, 0, null);

            AssetDatabase.SaveAssets();
        }

        // ---------------- piece assembly ----------------

        private static void Make(string prefabName, int seed, Action<MeshBuilder> build, int layer, Action<GameObject> colliders)
        {
            var builder = new MeshBuilder(seed);
            build(builder);
            string meshName = prefabName.Replace("PF_", "SM_");
            var mesh = GenUtil.SaveAsset(builder.Build(meshName, out var slots), $"{GenUtil.EnvMeshDir}/{meshName}.asset");

            var go = new GameObject(prefabName);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = MaterialLibrary.Get(slots);
            colliders?.Invoke(go);
            var mc = go.GetComponent<MeshCollider>();
            if (mc != null) mc.sharedMesh = mesh;
            GenUtil.SetLayerRecursive(go, layer);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                                       StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            GenUtil.SavePrefab(go, PathOf(prefabName));
        }

        private static void AddBox(GameObject go, Vector3 center, Vector3 size)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        // ---------------- shapes ----------------

        /// <summary>Timber-framed townhouse, front (door, windows) facing -Z, pitched roof with the ridge along X.</summary>
        private static void BuildHouse(MeshBuilder b, float w, float d, float h, bool tall)
        {
            float front = -d * 0.5f;
            b.Box(MatId.StoneDark, new Vector3(0, 0.25f, 0), new Vector3(w + 0.12f, 0.5f, d + 0.12f));         // stone footing
            b.Box(MatId.Plaster, new Vector3(0, 0.5f + (h - 0.5f) * 0.5f, 0), new Vector3(w, h - 0.5f, d));     // plaster walls
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Box(MatId.Wood, new Vector3(sx * (w * 0.5f - 0.02f), h * 0.5f, sz * (d * 0.5f - 0.02f)), new Vector3(0.18f, h, 0.18f));
            b.Box(MatId.Wood, new Vector3(0, h - 0.08f, front - 0.02f), new Vector3(w + 0.14f, 0.16f, 0.14f));   // top beam
            if (tall) b.Box(MatId.Wood, new Vector3(0, h * 0.5f, front - 0.02f), new Vector3(w + 0.14f, 0.14f, 0.12f)); // floor beam
            b.Box(MatId.Wood, new Vector3(0, 0.55f, front - 0.02f), new Vector3(w + 0.1f, 0.1f, 0.1f));          // sill beam

            // door with a little hood
            float doorX = -w * 0.22f;
            b.Box(MatId.Wood, new Vector3(doorX, 1.45f, front - 0.04f), new Vector3(0.95f, 1.9f, 0.08f));
            b.Box(MatId.Roof, new Vector3(doorX, 2.55f, front - 0.25f), new Vector3(1.3f, 0.08f, 0.5f), new Vector3(-18f, 0, 0));

            // warm windows with sills and shutters
            void Window(float x, float y, float z, float yaw)
            {
                var rot = Quaternion.Euler(0, yaw, 0);
                b.Box(MatId.Window, new Vector3(x, y, z) + rot * new Vector3(0, 0, -0.03f), new Vector3(0.62f, 0.62f, 0.06f), new Vector3(0, yaw, 0));
                b.Box(MatId.Wood, new Vector3(x, y - 0.36f, z) + rot * new Vector3(0, 0, -0.07f), new Vector3(0.8f, 0.08f, 0.12f), new Vector3(0, yaw, 0));
                b.Box(MatId.Wood, new Vector3(x, y, z) + rot * new Vector3(-0.44f, 0, -0.06f), new Vector3(0.24f, 0.7f, 0.05f), new Vector3(0, yaw, 0));
                b.Box(MatId.Wood, new Vector3(x, y, z) + rot * new Vector3(0.44f, 0, -0.06f), new Vector3(0.24f, 0.7f, 0.05f), new Vector3(0, yaw, 0));
            }
            Window(w * 0.22f, 1.6f, front, 0f);
            if (w > 5f) Window(w * 0.38f, 1.6f, front, 0f);
            if (tall)
            {
                Window(-w * 0.2f, 3.6f, front, 0f);
                Window(w * 0.22f, 3.6f, front, 0f);
            }
            Window(-w * 0.5f, 1.6f, 0f, 90f);
            Window(w * 0.5f, 1.6f, 0f, -90f);

            // roof + chimney
            float roofH = Mathf.Min(2.2f, d * 0.5f);
            b.Wedge(MatId.Roof, new Vector3(0, h, 0), new Vector3(w + 0.6f, roofH, d + 0.7f));
            b.Box(MatId.StoneDark, new Vector3(w * 0.28f, h + roofH * 0.7f, d * 0.18f), new Vector3(0.5f, roofH + 0.6f, 0.5f));
        }

        /// <summary>A string of pennants 8 m long along X, sagging in the middle, pivot at the left end.</summary>
        private static void BuildBunting(MeshBuilder b)
        {
            MatId[] colours = { MatId.PaintRed, MatId.PaintYellow, MatId.PaintBlue, MatId.Bandage, MatId.PaintGreen };
            const int flags = 16;
            for (int i = 0; i <= flags; i++)
            {
                float x = i * 8f / flags;
                float sag = -Mathf.Sin(x / 8f * Mathf.PI) * 0.7f;
                if (i < flags) b.Box(MatId.Wood, new Vector3(x + 0.25f, sag, 0), new Vector3(0.52f, 0.015f, 0.015f), new Vector3(0, 0, -Mathf.Cos((x + 0.25f) / 8f * Mathf.PI) * 15f));
                if (i == 0 || i == flags) continue;
                b.Wedge(colours[i % colours.Length], new Vector3(x, sag, 0), new Vector3(0.28f, 0.34f, 0.02f), new Vector3(0, 0, 180f));
            }
        }

        private static void BuildBarrel(MeshBuilder b)
        {
            b.Prism(MatId.WoodLight, 10, Vector3.zero, 0.29f, 0.9f);
            foreach (float y in new[] { 0.12f, 0.75f })
                b.Prism(MatId.Metal, 10, new Vector3(0, y, 0), 0.305f, 0.05f);
        }

        private static void BuildFlowerBox(MeshBuilder b)
        {
            b.Box(MatId.Wood, new Vector3(0, 0.1f, 0), new Vector3(0.8f, 0.2f, 0.25f));
            MatId[] petals = { MatId.PaintRed, MatId.PaintYellow, MatId.Bandage, MatId.PaintRed, MatId.PaintBlue };
            for (int i = 0; i < 5; i++)
            {
                float x = -0.32f + i * 0.16f;
                b.Box(MatId.PaintGreen, new Vector3(x, 0.26f, 0), new Vector3(0.14f, 0.14f, 0.16f), new Vector3(0, i * 23f, 8f));
                b.Box(petals[i], new Vector3(x + 0.02f, 0.36f, 0.01f), new Vector3(0.08f, 0.08f, 0.08f), new Vector3(20f, i * 40f, 15f));
            }
        }

        private static void BuildLampPost(MeshBuilder b)
        {
            b.Box(MatId.StoneDark, new Vector3(0, 0.15f, 0), new Vector3(0.4f, 0.3f, 0.4f));
            b.Prism(MatId.Wood, 6, new Vector3(0, 0.3f, 0), 0.07f, 2.5f);
            b.Box(MatId.Wood, new Vector3(0.28f, 2.7f, 0), new Vector3(0.62f, 0.08f, 0.08f));
            b.Box(MatId.Metal, new Vector3(0.52f, 2.42f, 0), new Vector3(0.24f, 0.05f, 0.24f));
            b.Box(MatId.Window, new Vector3(0.52f, 2.25f, 0), new Vector3(0.2f, 0.3f, 0.2f));
            b.Prism(MatId.Metal, 4, new Vector3(0.52f, 2.4f, 0), 0.17f, 0.16f, new Vector3(0, 45f, 0));
        }

        /// <summary>Courses of staggered stone blocks filling x0..x1 / y0..y1, over a recessed dark core.</summary>
        private static void BrickRun(MeshBuilder b, float x0, float x1, float y0, float y1, float thickness, float courseHeight = 0.5f)
        {
            if (x1 - x0 < 0.05f || y1 - y0 < 0.05f) return;
            b.Box(MatId.StoneDark, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0), new Vector3(x1 - x0 - 0.02f, y1 - y0 - 0.02f, thickness - 0.1f));

            int rows = Mathf.Max(1, Mathf.RoundToInt((y1 - y0) / courseHeight));
            float rh = (y1 - y0) / rows;
            for (int r = 0; r < rows; r++)
            {
                float y = y0 + r * rh;
                float cursor = x0;
                bool first = true;
                while (cursor < x1 - 0.01f)
                {
                    float len = b.Range(0.7f, 1.5f);
                    if (first && r % 2 == 1) len *= 0.5f;
                    first = false;
                    float end = Mathf.Min(x1, cursor + len);
                    if (x1 - end < 0.35f) end = x1;

                    var mat = b.Chance(0.14f) ? MatId.StoneDark : MatId.Stone;
                    float depth = thickness + b.Range(-0.04f, 0.06f);
                    b.Box(mat,
                        new Vector3((cursor + end) * 0.5f, y + rh * 0.5f + b.Range(-0.01f, 0.01f), b.Range(-0.02f, 0.02f)),
                        new Vector3(end - cursor - 0.045f, rh - 0.045f, depth),
                        new Vector3(b.Range(-0.4f, 0.4f), b.Range(-0.6f, 0.6f), b.Range(-0.8f, 0.8f)));
                    cursor = end;
                }
            }
        }

        private static void BuildWall(MeshBuilder b)
        {
            BrickRun(b, -2f, 2f, 0f, 4f, 0.6f);
            // skirting course at the base
            b.Box(MatId.StoneDark, new Vector3(0, 0.12f, 0), new Vector3(3.98f, 0.24f, 0.72f));
        }

        private static void BuildDoorway(MeshBuilder b)
        {
            BrickRun(b, -2f, -1f, 0f, 3f, 0.6f);
            BrickRun(b, 1f, 2f, 0f, 3f, 0.6f);
            BrickRun(b, -2f, 2f, 3.5f, 4f, 0.6f);
            b.Box(MatId.Stone, new Vector3(0, 3.25f, 0), new Vector3(2.7f, 0.5f, 0.78f), new Vector3(0, 0, b.Range(-0.6f, 0.6f)));
            // jambs
            b.Box(MatId.StoneDark, new Vector3(-1.15f, 1.5f, 0), new Vector3(0.3f, 3f, 0.76f));
            b.Box(MatId.StoneDark, new Vector3(1.15f, 1.5f, 0), new Vector3(0.3f, 3f, 0.76f));
            b.Box(MatId.StoneDark, new Vector3(-1.6f, 0.12f, 0), new Vector3(0.8f, 0.24f, 0.72f));
            b.Box(MatId.StoneDark, new Vector3(1.6f, 0.12f, 0), new Vector3(0.8f, 0.24f, 0.72f));
        }

        private static void BuildFloor(MeshBuilder b)
        {
            b.Box(MatId.StoneDark, new Vector3(0, -0.25f, 0), new Vector3(4f, 0.3f, 4f));
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                var mat = b.Chance(0.12f) ? MatId.StoneDark : MatId.StoneFloor;
                b.Box(mat,
                    new Vector3(-1.5f + i, -0.1f + b.Range(-0.012f, 0.012f), -1.5f + j),
                    new Vector3(0.965f, 0.2f, 0.965f),
                    new Vector3(b.Range(-0.5f, 0.5f), b.Range(-1f, 1f), b.Range(-0.5f, 0.5f)));
            }
        }

        private static void BuildCeiling(MeshBuilder b)
        {
            b.Box(MatId.StoneDark, new Vector3(0, 0.2f, 0), new Vector3(4f, 0.4f, 4f));
            b.Box(MatId.Stone, new Vector3(0, -0.12f, -1.2f), new Vector3(4f, 0.26f, 0.45f));
            b.Box(MatId.Stone, new Vector3(0, -0.12f, 1.2f), new Vector3(4f, 0.26f, 0.45f));
        }

        private static void BuildPillar(MeshBuilder b, float height, bool broken)
        {
            b.Box(MatId.StoneDark, new Vector3(0, 0.175f, 0), new Vector3(1.25f, 0.35f, 1.25f));
            b.Box(MatId.Stone, new Vector3(0, 0.45f, 0), new Vector3(1.0f, 0.2f, 1.0f));

            float top = broken ? height : height - 0.45f;
            float y = 0.55f;
            while (y < top - 0.05f)
            {
                float h = Mathf.Min(b.Range(0.5f, 0.75f), top - y);
                float w = 0.78f + b.Range(-0.03f, 0.03f);
                b.Box(b.Chance(0.2f) ? MatId.StoneDark : MatId.Stone, new Vector3(b.Range(-0.01f, 0.01f), y + h * 0.5f, 0),
                    new Vector3(w, h - 0.03f, w), new Vector3(0, b.Range(-3f, 3f), 0));
                y += h;
            }

            if (broken)
            {
                b.Box(MatId.Stone, new Vector3(0.08f, top + 0.12f, 0.05f), new Vector3(0.6f, 0.3f, 0.55f), new Vector3(12f, 20f, -9f));
                b.Box(MatId.Stone, new Vector3(0.7f, 0.15f, 0.5f), new Vector3(0.5f, 0.3f, 0.45f), new Vector3(8f, 35f, 14f));
            }
            else
            {
                b.Box(MatId.Stone, new Vector3(0, height - 0.32f, 0), new Vector3(1.05f, 0.25f, 1.05f));
                b.Box(MatId.StoneDark, new Vector3(0, height - 0.1f, 0), new Vector3(1.3f, 0.2f, 1.3f));
            }
        }

        private static void BuildStairs(MeshBuilder b)
        {
            const int steps = 8;
            const float rise = 0.25f, run = 0.4f;
            for (int i = 0; i < steps; i++)
            {
                float h = (i + 1) * rise;
                b.Box(MatId.StoneDark, new Vector3(0, h * 0.5f - 0.01f, i * run + run * 0.5f), new Vector3(3.9f, h - 0.02f, run));
                // tread slabs, a few per step
                float x = -2f;
                while (x < 1.99f)
                {
                    float w = Mathf.Min(b.Range(0.8f, 1.6f), 2f - x);
                    b.Box(MatId.Stone, new Vector3(x + w * 0.5f, h - 0.04f, i * run + run * 0.5f + 0.02f),
                        new Vector3(w - 0.04f, 0.1f, run + 0.02f), new Vector3(b.Range(-0.6f, 0.6f), 0, b.Range(-0.6f, 0.6f)));
                    x += w;
                }
            }
        }

        private static void BuildBlock(MeshBuilder b, float size, float height)
        {
            b.Box(MatId.Stone, new Vector3(0, height * 0.5f, 0), new Vector3(size, height, size), new Vector3(0, b.Range(-4f, 4f), 0));
            b.Box(MatId.StoneDark, new Vector3(0, height + 0.05f, 0), new Vector3(size * 1.06f, 0.1f, size * 1.06f), new Vector3(b.Range(-1f, 1f), b.Range(-6f, 6f), b.Range(-1f, 1f)));
            int chips = 3;
            for (int i = 0; i < chips; i++)
            {
                float s = b.Range(0.08f, 0.2f) * size;
                b.Box(MatId.StoneDark, new Vector3(b.Range(-0.7f, 0.7f) * size, s * 0.4f, b.Range(-0.7f, 0.7f) * size), Vector3.one * s,
                    new Vector3(b.Range(0, 360), b.Range(0, 360), b.Range(0, 360)));
            }
        }

        private static void BuildRubble(MeshBuilder b, int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                float s = b.Range(0.08f, 0.38f);
                var p = new Vector2(b.Range(-1f, 1f), b.Range(-1f, 1f)) * radius;
                b.Box(b.Chance(0.5f) ? MatId.Stone : MatId.StoneDark, new Vector3(p.x, s * 0.35f, p.y),
                    new Vector3(s, s * b.Range(0.5f, 0.9f), s * b.Range(0.7f, 1.2f)),
                    new Vector3(b.Range(-25f, 25f), b.Range(0, 360), b.Range(-25f, 25f)));
            }
        }

        private static void BuildAltar(MeshBuilder b)
        {
            b.Box(MatId.StoneDark, new Vector3(0, 0.15f, 0), new Vector3(2.4f, 0.3f, 1.6f));
            b.Box(MatId.Stone, new Vector3(0, 0.65f, 0), new Vector3(1.8f, 0.7f, 1.0f));
            b.Box(MatId.Stone, new Vector3(0, 1.06f, 0), new Vector3(2.05f, 0.14f, 1.2f), new Vector3(0, 0, 0.6f));
            b.Box(MatId.StoneDark, new Vector3(-0.75f, 0.65f, 0), new Vector3(0.25f, 0.72f, 1.05f));
            b.Box(MatId.StoneDark, new Vector3(0.75f, 0.65f, 0), new Vector3(0.25f, 0.72f, 1.05f));
        }

        private static void BuildRocks(MeshBuilder b, int count, float radius, float minSize, float maxSize)
        {
            for (int i = 0; i < count; i++)
            {
                float size = b.Range(minSize, maxSize);
                float a = b.Range(0f, Mathf.PI * 2f);
                float r = Mathf.Sqrt(b.Range(0f, 1f)) * radius;
                var p = new Vector3(Mathf.Cos(a) * r, size * b.Range(0.2f, 0.45f), Mathf.Sin(a) * r);
                b.Box(b.Chance(0.6f) ? MatId.StoneDark : MatId.Stone, p,
                    new Vector3(size, size * b.Range(0.55f, 0.95f), size * b.Range(0.7f, 1.15f)),
                    new Vector3(b.Range(-30f, 30f), b.Range(0f, 360f), b.Range(-30f, 30f)));
            }
        }

        /// <summary>Jagged rock face filling a 4x4 wall slot (pivot bottom-centre, faces -Z like walls). Breaks up brick rooms into caverns.</summary>
        private static void BuildRockWall(MeshBuilder b)
        {
            b.Box(MatId.StoneDark, new Vector3(0, 2f, 0.15f), new Vector3(3.9f, 4f, 0.5f));
            for (int i = 0; i < 18; i++)
            {
                float size = b.Range(0.6f, 1.6f);
                var p = new Vector3(b.Range(-1.9f, 1.9f), b.Range(0.2f, 3.8f), b.Range(-0.35f, 0.1f));
                b.Box(b.Chance(0.55f) ? MatId.StoneDark : MatId.Stone, p,
                    new Vector3(size, size * b.Range(0.6f, 1.1f), size * b.Range(0.4f, 0.8f)),
                    new Vector3(b.Range(-25f, 25f), b.Range(-25f, 25f), b.Range(0f, 360f)));
            }
        }

        private static void BuildCollapsedSlab(MeshBuilder b)
        {
            b.Box(MatId.Stone, new Vector3(0, 0.9f, 0), new Vector3(2.7f, 0.45f, 1.9f), new Vector3(-18f, 4f, 9f));
            b.Box(MatId.StoneDark, new Vector3(0.8f, 0.35f, 0.4f), new Vector3(1.1f, 0.7f, 0.9f), new Vector3(12f, 30f, -8f));
            b.Box(MatId.StoneDark, new Vector3(-0.9f, 0.3f, -0.3f), new Vector3(0.9f, 0.6f, 1f), new Vector3(-6f, -20f, 14f));
            BuildRocks(b, 6, 1.2f, 0.15f, 0.45f);
        }

        private static void BuildChain(MeshBuilder b)
        {
            const float link = 0.12f;
            for (int i = 0; i < 50; i++)
            {
                float y = -i * link * 0.85f;
                float rot = i % 2 == 0 ? 0f : 90f;
                var q = Quaternion.Euler(0f, rot, b.Range(-3f, 3f));
                Vector3 Side(float x, float yy) => new Vector3(0f, y, 0f) + q * new Vector3(x, yy, 0f);
                b.Box(MatId.Metal, Side(-0.028f, -link * 0.5f), new Vector3(0.016f, link, 0.016f), q.eulerAngles);
                b.Box(MatId.Metal, Side(0.028f, -link * 0.5f), new Vector3(0.016f, link, 0.016f), q.eulerAngles);
                b.Box(MatId.Metal, Side(0f, 0f), new Vector3(0.07f, 0.016f, 0.016f), q.eulerAngles);
                b.Box(MatId.Metal, Side(0f, -link), new Vector3(0.07f, 0.016f, 0.016f), q.eulerAngles);
            }
        }

        private static void BuildObelisk(MeshBuilder b)
        {
            b.Box(MatId.StoneDark, new Vector3(0, 0.2f, 0), new Vector3(1.0f, 0.4f, 1.0f));
            float y = 0.4f;
            float w = 0.7f;
            for (int i = 0; i < 4; i++)
            {
                float h = 0.7f;
                b.Box(MatId.Stone, new Vector3(0, y + h * 0.5f, 0), new Vector3(w, h - 0.03f, w), new Vector3(0, b.Range(-2f, 2f), 0));
                y += h;
                w *= 0.88f;
            }
            // pointed cap: a box rotated 45° and squashed reads as a low-poly pyramid tip
            b.Box(MatId.StoneDark, new Vector3(0, y + 0.15f, 0), new Vector3(w * 0.8f, 0.35f, w * 0.8f), new Vector3(0, 45f, 0));
            b.Box(MatId.StoneDark, new Vector3(0, y + 0.38f, 0), new Vector3(w * 0.45f, 0.25f, w * 0.45f), new Vector3(0, 45f, 0));
        }
    }
}
