using System.Collections.Generic;
using System.IO;
using NasconGJ.Entities;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Paints the shadow creature sprite sheet procedurally from signed-distance shapes, following
    /// DOCUMENTATION/CHARACTER SHEETS/shadow reference.png: tall, spindly, robe-like body dripping into a
    /// puddle, long clawed fingers, horned crown, broken glowing halo, glowing maw when attacking.
    ///
    /// Output channels: R body, G glow (halo/eyes/maw/burning rim), B inner detail streaks.
    /// Coordinates are in "height units": y 0..1 bottom to top, x in [-Aspect/2, Aspect/2], facing +x.
    /// </summary>
    public static class ShadowSheetGenerator
    {
        public static string CreaturePath => GenUtil.TextureDir + "/T_ShadowCreatureSheet.png";
        public static string TownsfolkPath => GenUtil.TextureDir + "/T_ShadowTownsfolk.png";

        private const int FW = ShadowSheet.FrameWidth;
        private const int FH = ShadowSheet.FrameHeight;

        private struct Pose
        {
            public float Lean, Squash, Halo, Curl;
            public Vector2 LElbow, LWrist, RElbow, RWrist;
            public float LFingerDir, RFingerDir;
            public bool Maw;
        }

        // ---------------------------------------------------------------- entry

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(GenUtil.TextureDir);
            var poses = BuildPoses();
            var sheet = new Color[FW * ShadowSheet.Cols * FH * ShadowSheet.Rows];
            for (int i = 0; i < poses.Count; i++)
                PaintFrame(sheet, FW * ShadowSheet.Cols, i % ShadowSheet.Cols, ShadowSheet.Rows - 1 - i / ShadowSheet.Cols, (x, y) => Creature(poses[i], x, y, i));
            Save(sheet, FW * ShadowSheet.Cols, FH * ShadowSheet.Rows, CreaturePath);

            var town = new Color[FW * ShadowSheet.TownsfolkCount * FH];
            for (int i = 0; i < ShadowSheet.TownsfolkCount; i++)
            {
                int kind = i;
                PaintFrame(town, FW * ShadowSheet.TownsfolkCount, i, 0, (x, y) => Townsfolk(kind, x, y));
            }
            Save(town, FW * ShadowSheet.TownsfolkCount, FH, TownsfolkPath);
        }

        private static List<Pose> BuildPoses()
        {
            var list = new List<Pose>();
            for (int i = 0; i < 4; i++) // lurk / idle sway
            {
                float t = i / 4f * Mathf.PI * 2f, s = Mathf.Sin(t);
                float lean = 0.015f * s;
                Shoulders(lean, 1f, out var ls, out var rs);
                list.Add(new Pose
                {
                    Lean = lean, Squash = 1f, Halo = 1f, Curl = 22f,
                    LElbow = ls + new Vector2(-0.035f, -0.15f + 0.01f * s), LWrist = ls + new Vector2(-0.05f, -0.31f),
                    RElbow = rs + new Vector2(0.035f, -0.15f - 0.01f * s), RWrist = rs + new Vector2(0.055f, -0.30f),
                    LFingerDir = -100f, RFingerDir = -78f,
                });
            }
            for (int i = 0; i < 4; i++) // stalk: lean in, reach, alternating claws
            {
                float t = i / 4f * Mathf.PI * 2f, s = Mathf.Sin(t);
                float lean = 0.07f;
                Shoulders(lean, 1f, out var ls, out var rs);
                list.Add(new Pose
                {
                    Lean = lean, Squash = 1f, Halo = 1f, Curl = 30f,
                    RElbow = rs + new Vector2(0.07f, -0.06f + 0.03f * s), RWrist = rs + new Vector2(0.13f, -0.11f + 0.05f * s),
                    LElbow = ls + new Vector2(0.03f, -0.15f), LWrist = ls + new Vector2(0.07f, -0.26f - 0.03f * s),
                    RFingerDir = -25f + 15f * s, LFingerDir = -60f,
                });
            }
            for (int i = 0; i < 2; i++) // recoil: pulled back, shielding
            {
                float sq = 0.86f;
                float lean = -0.04f;
                Shoulders(lean, sq, out var ls, out var rs);
                list.Add(new Pose
                {
                    Lean = lean, Squash = sq, Halo = i == 0 ? 0.45f : 0.15f, Curl = 55f,
                    RElbow = rs + new Vector2(0.05f, 0.08f), RWrist = rs + new Vector2(-0.005f, 0.18f),
                    LElbow = ls + new Vector2(-0.06f, 0.07f), LWrist = ls + new Vector2(-0.02f, 0.17f),
                    RFingerDir = 110f, LFingerDir = 75f,
                });
            }
            for (int i = 0; i < 2; i++) // attack: windup, lunge with maw open
            {
                float lean = i == 0 ? 0.1f : 0.14f;
                Shoulders(lean, 1f, out var ls, out var rs);
                list.Add(new Pose
                {
                    Lean = lean, Squash = 1f, Halo = 1.3f, Curl = 18f, Maw = true,
                    RElbow = rs + new Vector2(0.07f, 0.02f), RWrist = rs + new Vector2(0.12f, -0.02f + i * 0.03f),
                    LElbow = ls + new Vector2(0.03f, 0.08f), LWrist = ls + new Vector2(0.08f, 0.12f),
                    RFingerDir = 5f, LFingerDir = 35f,
                });
            }
            return list;
        }

        private static void Shoulders(float lean, float squash, out Vector2 l, out Vector2 r)
        {
            var c = new Vector2(lean * 0.7f, 0.67f * squash);
            l = c + new Vector2(-0.075f, 0f);
            r = c + new Vector2(0.075f, 0f);
        }

        // ---------------------------------------------------------------- creature

        private static Color Creature(Pose p, float x, float y, int frame)
        {
            var q = new Vector2(x, y);
            float sq = p.Squash;
            Shoulders(p.Lean, sq, out var ls, out var rs);
            var chest = new Vector2(p.Lean * 0.7f, 0.67f * sq);
            var hip = new Vector2(p.Lean * 0.35f, 0.47f * sq);
            var head = new Vector2(p.Lean, 0.79f * sq);

            float d = RoundCone(q, new Vector2(0f, 0.035f), 0.13f, hip, 0.042f);       // robe
            d = Mathf.Min(d, RoundCone(q, hip, 0.04f, chest, 0.06f));                    // torso
            d = Mathf.Min(d, Capsule(q, ls, rs, 0.027f));                                // shoulders
            d = Mathf.Min(d, Capsule(q, chest, new Vector2(p.Lean * 0.9f, 0.745f * sq), 0.017f));
            d = Mathf.Min(d, Capsule(q, head - new Vector2(0, 0.022f), head + new Vector2(0.005f, 0.026f), p.Maw ? 0.042f : 0.037f));

            // draped cape off the back shoulder + tattered strips at the hem (reference: robe-like, dripping)
            d = Mathf.Min(d, RoundCone(q, ls + new Vector2(-0.005f, -0.01f), 0.03f, new Vector2(-0.085f + p.Lean * 0.2f, 0.3f * sq), 0.055f));
            d = Mathf.Min(d, RoundCone(q, new Vector2(-0.085f + p.Lean * 0.2f, 0.3f * sq), 0.055f, new Vector2(-0.12f, 0.05f), 0.035f));
            for (int k = 0; k < 7; k++)
            {
                float sx = -0.15f + k * 0.05f + (Hash(k, frame, 61) - 0.5f) * 0.02f;
                float top = 0.13f + Hash(k, frame, 67) * 0.08f;
                float bottom = -0.01f - Hash(k, frame, 71) * 0.01f;
                d = Mathf.Min(d, RoundCone(q, new Vector2(sx * 0.9f, top), 0.016f, new Vector2(sx * 1.05f + (Hash(k, 3, 73) - 0.5f) * 0.03f, bottom), 0.003f));
            }

            // crown of horns
            d = Mathf.Min(d, RoundCone(q, head + new Vector2(0f, 0.04f), 0.011f, head + new Vector2(0.006f, 0.15f), 0.0015f));
            d = Mathf.Min(d, RoundCone(q, head + new Vector2(-0.024f, 0.03f), 0.009f, head + new Vector2(-0.075f, 0.1f), 0.0015f));
            d = Mathf.Min(d, RoundCone(q, head + new Vector2(0.024f, 0.03f), 0.009f, head + new Vector2(0.07f, 0.095f), 0.0015f));
            d = Mathf.Min(d, RoundCone(q, head + new Vector2(-0.012f, 0.045f), 0.006f, head + new Vector2(-0.032f, 0.115f), 0.0015f));
            d = Mathf.Min(d, RoundCone(q, head + new Vector2(0.012f, 0.045f), 0.006f, head + new Vector2(0.03f, 0.115f), 0.0015f));

            // arms + clawed hands
            d = Mathf.Min(d, Arm(q, ls, p.LElbow, p.LWrist, p.LFingerDir, p.Curl, -1f));
            d = Mathf.Min(d, Arm(q, rs, p.RElbow, p.RWrist, p.RFingerDir, p.Curl, 1f));

            // drips hanging from forearms
            d = Mathf.Min(d, Drips(q, p.LElbow, p.LWrist, 11 + frame));
            d = Mathf.Min(d, Drips(q, p.RElbow, p.RWrist, 23 + frame));

            // puddle at the base
            float puddle = Ellipse(q, new Vector2(0.01f, 0.012f), new Vector2(0.25f, 0.016f));
            puddle = Mathf.Min(puddle, Ellipse(q, new Vector2(-0.14f, 0.008f), new Vector2(0.07f, 0.008f)));
            puddle = Mathf.Min(puddle, Ellipse(q, new Vector2(0.17f, 0.01f), new Vector2(0.05f, 0.007f)));
            d = Mathf.Min(d, puddle);

            // ragged smoky edges, extra ragged robe hem
            float fine = (VN(x * 70f, y * 70f + frame * 3.1f, 9) - 0.5f) * 0.011f;
            float hem = y < 0.47f ? (VN(x * 22f, y * 8f + frame * 0.7f, 13) - 0.5f) * 0.045f * (1f - y / 0.47f) : 0f;
            float tear = y < 0.47f ? Mathf.Max(0f, VN(x * 60f, 3f, 17) - 0.62f) * 0.12f * (1f - y / 0.47f) : 0f;
            d += fine + hem + tear;

            float core = 1f - Smooth(-0.003f, 0.003f, d);
            float haze = (1f - Smooth(0f, 0.03f, d)) * 0.25f * VN(x * 30f, y * 30f + frame, 21);
            float body = Mathf.Clamp01(Mathf.Max(core, haze));

            // inner streaks + lighter band just inside the silhouette
            float streak = Smooth(0.55f, 0.85f, VN(x * 48f, y * 4.5f + frame * 0.3f, 31)) * 0.75f;
            float innerEdge = (1f - Smooth(0f, 0.014f, -d)) * 0.45f;
            float detail = core * Mathf.Clamp01(streak + innerEdge);

            // glow: faint smouldering edge patches, eyes, maw (no halo: in play it read as a ring on the head)
            float rim = (1f - Smooth(0f, 0.005f, Mathf.Abs(d + 0.002f))) * (VN(x * 38f, y * 38f, 41) > 0.6f ? 0.14f : 0.02f);
            float halo = 0f;
            float eyes = 1f - Smooth(-0.001f, 0.002f, Mathf.Min(
                Ellipse(q, head + new Vector2(0.016f, 0.004f), new Vector2(0.007f, 0.0028f)),
                Ellipse(q, head + new Vector2(-0.003f, 0.004f), new Vector2(0.006f, 0.0026f))));
            float maw = 0f;
            if (p.Maw)
            {
                var m = head + new Vector2(0.014f, -0.012f);
                float mouth = 1f - Smooth(-0.002f, 0.002f, Ellipse(q, m, new Vector2(0.034f, 0.03f)));
                float zig = Mathf.Abs(Frac((x - m.x) * 42f) - 0.5f) * 2f;
                float dy = y - m.y;
                float teeth = (dy > 0.002f + zig * 0.02f || dy < -0.002f - zig * 0.02f) ? 1f : 0.15f;
                maw = mouth * teeth;
                eyes = 0f;
            }
            float glow = Mathf.Clamp01(rim * core + halo + eyes + maw);

            return new Color(body, glow, detail, Mathf.Max(body, glow));
        }

        private static float Arm(Vector2 q, Vector2 shoulder, Vector2 elbow, Vector2 wrist, float fingerDir, float curl, float side)
        {
            float d = Capsule(q, shoulder, elbow, 0.016f);
            d = Mathf.Min(d, RoundCone(q, elbow, 0.013f, wrist, 0.009f));
            Vector2 palmDir = Dir(fingerDir);
            Vector2 palm = wrist + palmDir * 0.018f;
            d = Mathf.Min(d, Capsule(q, wrist, palm, 0.011f));
            for (int k = 0; k < 4; k++)
            {
                float a = fingerDir + (k - 1.5f) * 15f;
                Vector2 p0 = palm + Dir(a + 90f) * (k - 1.5f) * 0.004f;
                Vector2 p1 = p0 + Dir(a) * 0.045f;
                Vector2 p2 = p1 + Dir(a - curl * side * 0.6f) * 0.038f;
                Vector2 p3 = p2 + Dir(a - curl * side * 1.3f) * 0.026f;
                d = Mathf.Min(d, RoundCone(q, p0, 0.0045f, p1, 0.0038f));
                d = Mathf.Min(d, RoundCone(q, p1, 0.0038f, p2, 0.003f));
                d = Mathf.Min(d, RoundCone(q, p2, 0.003f, p3, 0.0008f));
            }
            return d;
        }

        private static float Drips(Vector2 q, Vector2 a, Vector2 b, int seed)
        {
            float d = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                float t = 0.25f + i * 0.3f;
                Vector2 top = Vector2.Lerp(a, b, t);
                float len = 0.02f + Hash(i, seed, 3) * 0.05f;
                d = Mathf.Min(d, RoundCone(q, top, 0.005f, top - new Vector2(0, len), 0.0015f));
                d = Mathf.Min(d, (q - (top - new Vector2(0, len + 0.012f + Hash(i, seed, 5) * 0.015f))).magnitude - 0.0045f);
            }
            return d;
        }

        // ---------------------------------------------------------------- townsfolk

        private static Color Townsfolk(int kind, float x, float y)
        {
            var q = new Vector2(x, y);
            float d;
            switch (kind)
            {
                case 0: // long coat, wide-brim hat
                    d = RoundCone(q, new Vector2(0, 0.02f), 0.1f, new Vector2(0, 0.62f), 0.075f);
                    d = Mathf.Min(d, Capsule(q, new Vector2(-0.09f, 0.635f), new Vector2(0.09f, 0.635f), 0.036f));
                    d = Mathf.Min(d, RoundCone(q, new Vector2(-0.1f, 0.62f), 0.028f, new Vector2(-0.118f, 0.36f), 0.022f));
                    d = Mathf.Min(d, RoundCone(q, new Vector2(0.1f, 0.62f), 0.028f, new Vector2(0.118f, 0.36f), 0.022f));
                    d = Mathf.Min(d, Capsule(q, new Vector2(0, 0.66f), new Vector2(0, 0.74f), 0.026f));
                    d = Mathf.Min(d, Capsule(q, new Vector2(0, 0.745f), new Vector2(0, 0.8f), 0.043f));
                    d = Mathf.Min(d, Ellipse(q, new Vector2(0, 0.835f), new Vector2(0.13f, 0.013f)));
                    d = Mathf.Min(d, Capsule(q, new Vector2(0, 0.85f), new Vector2(0, 0.895f), 0.045f));
                    break;
                case 1: // hooded robe
                    d = RoundCone(q, new Vector2(0, 0.02f), 0.12f, new Vector2(0, 0.6f), 0.07f);
                    d = Mathf.Min(d, Capsule(q, new Vector2(-0.08f, 0.62f), new Vector2(0.08f, 0.62f), 0.042f));
                    d = Mathf.Min(d, RoundCone(q, new Vector2(0, 0.66f), 0.066f, new Vector2(0.01f, 0.81f), 0.052f));
                    d = Mathf.Min(d, RoundCone(q, new Vector2(0.01f, 0.81f), 0.052f, new Vector2(-0.018f, 0.885f), 0.006f));
                    break;
                default: // hunched elder with cane
                    d = RoundCone(q, new Vector2(0, 0.02f), 0.11f, new Vector2(0.04f, 0.56f), 0.06f);
                    d = Mathf.Min(d, Capsule(q, new Vector2(0.06f, 0.62f), new Vector2(0.09f, 0.7f), 0.045f));
                    d = Mathf.Min(d, Capsule(q, new Vector2(0.06f, 0.55f), new Vector2(0.14f, 0.5f), 0.016f));
                    d = Mathf.Min(d, Capsule(q, new Vector2(0.15f, 0.52f), new Vector2(0.17f, 0.02f), 0.007f));
                    break;
            }
            d += (VN(x * 60f, y * 60f, 71 + kind) - 0.5f) * 0.01f;
            d += y < 0.3f ? (VN(x * 25f, y * 10f, 79) - 0.5f) * 0.025f * (1f - y / 0.3f) : 0f;
            float body = 1f - Smooth(-0.003f, 0.003f, d);
            return new Color(body, 0f, 0f, body);
        }

        // ---------------------------------------------------------------- painting

        private delegate Color Painter(float x, float y);

        private static void PaintFrame(Color[] pixels, int sheetWidth, int col, int row, Painter painter)
        {
            for (int py = 0; py < FH; py++)
            for (int px = 0; px < FW; px++)
            {
                float x = ((px + 0.5f) / FW - 0.5f) * ShadowSheet.Aspect;
                float y = (py + 0.5f) / FH;
                pixels[(row * FH + py) * sheetWidth + col * FW + px] = painter(x, y);
            }
        }

        private static void Save(Color[] pixels, int width, int height, string assetPath)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.sRGBTexture = false; // channels are masks, not colour
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------- SDF helpers

        private static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r) => SegDist(p, a, b, out _) - r;

        private static float RoundCone(Vector2 p, Vector2 a, float ra, Vector2 b, float rb)
        {
            float d = SegDist(p, a, b, out float t);
            return d - Mathf.Lerp(ra, rb, t);
        }

        private static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
        {
            Vector2 d = p - c;
            float k0 = new Vector2(d.x / r.x, d.y / r.y).magnitude;
            float k1 = new Vector2(d.x / (r.x * r.x), d.y / (r.y * r.y)).magnitude;
            return k1 > 1e-6f ? k0 * (k0 - 1f) / k1 : -Mathf.Min(r.x, r.y);
        }

        private static float SegDist(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            Vector2 ab = b - a;
            t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-7f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        private static Vector2 Dir(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        private static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
        private static float Frac(float v) => v - Mathf.Floor(v);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65535f;
            }
        }

        private static float VN(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(x0, y0, seed), Hash(x0 + 1, y0, seed), fx);
            float b = Mathf.Lerp(Hash(x0, y0 + 1, seed), Hash(x0 + 1, y0 + 1, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }
    }
}
