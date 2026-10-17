using System.IO;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>Procedural textures: stone grit, soft particle, HUD ring, and the shadow-creature silhouette.</summary>
    public static class TextureGenerator
    {
        public static string StoneGritPath => GenUtil.TextureDir + "/T_StoneGrit.png";
        public static string SoftParticlePath => GenUtil.TextureDir + "/T_SoftParticle.png";
        public static string RingPath => GenUtil.TextureDir + "/T_Ring.png";
        public static string RingOutlinePath => GenUtil.TextureDir + "/T_RingOutline.png";
        public static string ShadowPath => GenUtil.TextureDir + "/T_ShadowSilhouette.png";

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(GenUtil.TextureDir);
            Save(StoneGrit(256), StoneGritPath, true, false, TextureWrapMode.Repeat);
            Save(SoftParticle(64), SoftParticlePath, true, false, TextureWrapMode.Clamp);
            Save(Ring(128, 0.09f), RingPath, true, true, TextureWrapMode.Clamp);
            Save(Ring(128, 0.2f), RingOutlinePath, true, true, TextureWrapMode.Clamp);
            Save(ShadowSilhouette(256, 512), ShadowPath, true, false, TextureWrapMode.Clamp);
            Save(RoundedRect(64, 16), RoundedPath, true, true, TextureWrapMode.Clamp, new Vector4(20, 20, 20, 20));
            Save(HorizontalShade(512), ShadePath, true, true, TextureWrapMode.Clamp);
            Save(Diamond(64), DiamondPath, true, true, TextureWrapMode.Clamp);
            Save(LineFade(512), LinePath, true, true, TextureWrapMode.Clamp);
        }

        public static string RoundedPath => GenUtil.TextureDir + "/T_UI_Rounded.png";
        public static string ShadePath => GenUtil.TextureDir + "/T_UI_Shade.png";
        public static string DiamondPath => GenUtil.TextureDir + "/T_UI_Diamond.png";
        public static string LinePath => GenUtil.TextureDir + "/T_UI_Line.png";

        public static Sprite GetSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // ---------------- UI sprites ----------------

        private static Texture2D RoundedRect(int size, float radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                float py = Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                float d = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Opaque on the left fading to clear on the right (eased), for text legibility over the menu scene.</summary>
        private static Texture2D HorizontalShade(int width)
        {
            var tex = new Texture2D(width, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float a = Mathf.Pow(1f - t, 1.6f);
                for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D Diamond(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs((x + 0.5f) / size * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                float d = (dx + dy * 0.8f) - 0.9f;
                float a = Mathf.Clamp01(-d * size * 0.5f);
                float core = Mathf.Clamp01(-(dx + dy * 0.8f - 0.45f) * size * 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a * 0.75f + core * 0.25f)));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D LineFade(int width)
        {
            var tex = new Texture2D(width, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 6f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - t) * 2.2f));
                for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        // ---------------- textures ----------------

        private static Texture2D StoneGrit(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float n = Fbm(u, v, 4, 5, 11);
                float blotch = Fbm(u, v, 2, 2, 31);
                float h = Hash(x, y, 7);
                float speck = h > 0.965f ? -0.16f : h < 0.02f ? 0.07f : 0f;
                float value = Mathf.Clamp01(0.8f + (n - 0.5f) * 0.32f + (blotch - 0.5f) * 0.18f + speck);
                tex.SetPixel(x, y, new Color(value, value * 0.98f, value * 0.95f, 1f));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D SoftParticle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a = a * a * (3f - 2f * a);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D Ring(int size, float halfThickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.78f) / halfThickness);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, a)));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Spindly horned humanoid with long clawed arms (matches the shadow concept sheets).
        /// Coordinates are in "height units": x in [0,0.5], y in [0,1].
        /// </summary>
        private static Texture2D ShadowSilhouette(int width, int height)
        {
            // (ax, ay, bx, by, radius) with x given in 0..1 of the width
            float[,] s =
            {
                // head + horns
                { 0.50f, 0.865f, 0.50f, 0.87f, 0.055f },
                { 0.46f, 0.90f, 0.36f, 0.985f, 0.010f },
                { 0.54f, 0.90f, 0.66f, 0.975f, 0.010f },
                { 0.36f, 0.985f, 0.33f, 0.96f, 0.006f },
                // neck, chest, waist
                { 0.50f, 0.82f, 0.51f, 0.77f, 0.025f },
                { 0.50f, 0.75f, 0.50f, 0.62f, 0.075f },
                { 0.50f, 0.62f, 0.51f, 0.50f, 0.045f },
                { 0.38f, 0.735f, 0.63f, 0.74f, 0.035f },
                // left arm hanging, long fingers
                { 0.38f, 0.735f, 0.24f, 0.56f, 0.024f },
                { 0.24f, 0.56f, 0.16f, 0.34f, 0.018f },
                { 0.16f, 0.34f, 0.10f, 0.23f, 0.006f },
                { 0.16f, 0.34f, 0.15f, 0.21f, 0.006f },
                { 0.16f, 0.34f, 0.21f, 0.23f, 0.006f },
                // right arm raised, reaching
                { 0.63f, 0.74f, 0.80f, 0.66f, 0.024f },
                { 0.80f, 0.66f, 0.90f, 0.80f, 0.018f },
                { 0.90f, 0.80f, 0.97f, 0.90f, 0.006f },
                { 0.90f, 0.80f, 0.99f, 0.84f, 0.006f },
                { 0.90f, 0.80f, 0.93f, 0.92f, 0.006f },
                // legs
                { 0.47f, 0.50f, 0.40f, 0.27f, 0.030f },
                { 0.40f, 0.27f, 0.37f, 0.02f, 0.020f },
                { 0.53f, 0.50f, 0.60f, 0.28f, 0.030f },
                { 0.60f, 0.28f, 0.66f, 0.02f, 0.020f },
            };

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int py = 0; py < height; py++)
            for (int px = 0; px < width; px++)
            {
                float x = (px + 0.5f) / width * 0.5f;
                float y = (py + 0.5f) / height;
                float d = float.MaxValue;
                for (int i = 0; i < s.GetLength(0); i++)
                {
                    var a = new Vector2(s[i, 0] * 0.5f, s[i, 1]);
                    var b = new Vector2(s[i, 2] * 0.5f, s[i, 3]);
                    d = Mathf.Min(d, SegmentDistance(new Vector2(x, y), a, b) - s[i, 4]);
                }

                // ragged smoky edges + drips
                float edgeNoise = (ValueNoise(x * 60f, y * 60f, 1000, 3) - 0.5f) * 0.018f;
                float drip = (ValueNoise(x * 90f, y * 6f, 1000, 9) - 0.6f) * 0.03f * Mathf.Clamp01((0.6f - y) * 3f);
                d += edgeNoise + Mathf.Min(0f, -drip);

                float core = 1f - Mathf.SmoothStep(-0.004f, 0.004f, d);
                float haze = (1f - Mathf.SmoothStep(0f, 0.035f, d)) * 0.35f * ValueNoise(x * 25f, y * 25f, 1000, 5);
                float alpha = Mathf.Clamp01(core + haze);
                tex.SetPixel(px, py, new Color(0f, 0f, 0f, alpha));
            }
            tex.Apply();
            return tex;
        }

        // ---------------- noise ----------------

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

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

        private static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int xa = Mod(x0, period), xb = Mod(x0 + 1, period), ya = Mod(y0, period), yb = Mod(y0 + 1, period);
            float a = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), fx);
            float b = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>Tileable fractal noise over u,v in [0,1).</summary>
        private static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int period = basePeriod;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(u * period, v * period, period, seed + i) * amp;
                norm += amp;
                amp *= 0.5f;
                period *= 2;
            }
            return sum / norm;
        }

        private static int Mod(int a, int m) => ((a % m) + m) % m;

        // ---------------- saving ----------------

        private static void Save(Texture2D tex, string assetPath, bool srgb, bool sprite, TextureWrapMode wrap, Vector4 border = default)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.sRGBTexture = srgb;
            importer.wrapMode = wrap;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = !sprite;
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
            }
            importer.SaveAndReimport();
        }
    }
}
