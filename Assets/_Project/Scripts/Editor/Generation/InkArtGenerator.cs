using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Paints the 2D art for the story-scroll prologue: ink-wash landscapes, a Tsushima-style village,
    /// pines, pampas grass, the tomb, and jointed shadow-puppet figures. Everything is drawn in code
    /// (signed-distance shapes + brush noise) into mask textures read by NASCON/InkLayer:
    ///   R = coverage (the paper cut-out),  G = tone (0 pale wash .. 1 solid ink),
    ///   B = accent (lit windows, gold, teal pine needles, cut-out filigree), A = R.
    /// Coordinates inside a painter: y in 0..1 (bottom to top), x in 0..aspect.
    /// </summary>
    public static class InkArtGenerator
    {
        public static string Dir => GenUtil.TextureDir + "/Ink";
        public static string PathOf(string name) => $"{Dir}/{name}.png";

        // landscape + set pieces
        public const string MountainsFar = "T_Ink_MountainsFar";
        public const string MountainsMid = "T_Ink_MountainsMid";
        public const string Hills = "T_Ink_Hills";
        public const string MistBand = "T_Ink_Mist";
        public const string Pine = "T_Ink_Pine";
        public const string Grass = "T_Ink_Grass";
        public const string HouseRow = "T_Ink_HouseRow";
        public const string HouseRowBack = "T_Ink_HouseRowBack";
        public const string Wall = "T_Ink_Wall";
        public const string Well = "T_Ink_Well";
        public const string Stall = "T_Ink_Stall";
        public const string Lantern = "T_Ink_Lantern";
        public const string Torii = "T_Ink_Torii";
        public const string Tomb = "T_Ink_Tomb";
        public const string FigureRow = "T_Ink_FigureRow";
        public const string Oven = "T_Ink_Oven";
        public const string Fire = "T_Ink_Fire";
        public const string Logs = "T_Ink_Logs";
        public const string Moon = "T_Ink_Moon";
        public const string Leaf = "T_Ink_Leaf";
        public const string Seal = "T_Ink_Seal";
        public const string Paper = "T_Ink_Paper";
        public const string VoidBody = "T_Ink_VoidBody";
        public const string VoidArm = "T_Ink_VoidArm";
        public const string Ground = "T_Ink_Ground";
        public const string Calligraphy = "T_Ink_Calligraphy";
        public const string CloudScroll = "T_Ink_CloudScroll";

        /// <summary>Puppet kinds; each has a body and an arm texture: T_Puppet_{Kind}_Body / _Arm.</summary>
        public static readonly string[] PuppetKinds = { "Man", "Woman", "Child", "Elder", "Vendor", "Bearer" };
        public static string PuppetBody(string kind) => $"T_Puppet_{kind}_Body";
        public static string PuppetArm(string kind) => $"T_Puppet_{kind}_Arm";

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(Dir);
            Paint(MountainsFar, 2048, 512, MountainsFarP);
            Paint(MountainsMid, 2048, 640, MountainsMidP);
            Paint(Hills, 2048, 384, HillsP);
            Paint(MistBand, 2048, 256, MistP);
            Paint(Pine, 1024, 1024, PineP);
            Paint(Grass, 2048, 384, GrassP);
            Paint(HouseRow, 2048, 640, (x, y) => HouseRowP(x, y, 11, false));
            Paint(HouseRowBack, 2048, 512, (x, y) => HouseRowP(x, y, 23, true));
            Paint(Wall, 1024, 512, WallP);
            Paint(Well, 512, 512, WellP);
            Paint(Stall, 768, 512, StallP);
            Paint(Lantern, 256, 512, LanternP);
            Paint(Torii, 1024, 768, ToriiP);
            Paint(Tomb, 1536, 1024, TombP);
            Paint(FigureRow, 2048, 512, FigureRowP);
            Paint(Oven, 512, 512, OvenP);
            Paint(Fire, 256, 512, FireP);
            Paint(Logs, 512, 192, LogsP);
            Paint(Moon, 512, 512, MoonP);
            Paint(Leaf, 128, 128, LeafP);
            Paint(Seal, 256, 256, SealP);
            Paint(Paper, 1024, 1024, PaperP, wrap: true);
            Paint(VoidBody, 1024, 1536, VoidBodyP);
            Paint(VoidArm, 256, 1024, VoidArmP);
            Paint(Ground, 2048, 256, GroundP);
            Paint(Calligraphy, 256, 1024, CalligraphyP);
            Paint(CloudScroll, 1024, 384, CloudScrollP);
            foreach (var kind in PuppetKinds)
            {
                string k = kind;
                Paint(PuppetBody(k), 512, 1024, (x, y) => PuppetBodyP(k, x, y));
                Paint(PuppetArm(k), 128, 512, (x, y) => PuppetArmP(k, x, y));
            }
            AssetDatabase.SaveAssets();
        }

        // =====================================================================
        //  landscape
        // =====================================================================

        private static Color MountainsFarP(float x, float y)
        {
            float ridge = 0.55f + 0.22f * Fbm(x * 0.9f, 3.1f, 4, 101) + 0.12f * Mathf.Pow(Fbm(x * 2.3f, 7f, 3, 103), 2f);
            float peaks = 0f;
            foreach (var (px, h, w) in new[] { (0.6f, 0.3f, 0.35f), (1.7f, 0.38f, 0.28f), (2.6f, 0.25f, 0.4f), (3.4f, 0.33f, 0.3f) })
                peaks = Mathf.Max(peaks, h * Mathf.Exp(-Mathf.Pow((x - px) / w, 2f)));
            ridge = Mathf.Min(0.97f, ridge + peaks);
            float edge = (Fbm(x * 30f, y * 30f, 2, 105) - 0.5f) * 0.012f;
            float cov = 1f - Smooth(-0.004f, 0.004f, y - ridge + edge);
            float mist = Smooth(0.02f, 0.45f, y);                                   // dissolves into mist below
            float tone = Smooth(ridge - 0.38f, ridge, y) * 0.75f + Fbm(x * 9f, y * 2f, 3, 107) * 0.25f;
            return Px(cov * mist * SideFade(x, 4f), tone, 0f);
        }

        private static Color MountainsMidP(float x, float y)
        {
            // karst towers: tall rounded peaks with vertical dry-brush strokes and moss on top
            float h = 0f;
            int idx = 0;
            for (int i = 0; i < 9; i++)
            {
                float cx = 0.2f + i * 0.42f + (Hash(i, 1, 201) - 0.5f) * 0.2f;
                float ph = 0.45f + Hash(i, 2, 201) * 0.45f;
                float w = 0.12f + Hash(i, 3, 201) * 0.12f;
                float bump = ph * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((x - cx) / w, 2f)), 0.55f);
                if (bump > h) { h = bump; idx = i; }
            }
            h = Mathf.Max(h, 0.18f + 0.05f * Fbm(x * 3f, 1f, 3, 203));
            float edge = (Fbm(x * 25f, y * 25f, 3, 205) - 0.5f) * 0.02f;
            float cov = 1f - Smooth(-0.004f, 0.004f, y - h + edge);
            float strokes = Fbm(x * 40f, y * 3f + idx, 3, 207);
            float tone = Mathf.Clamp01(0.35f + 0.5f * Smooth(h - 0.25f, h, y) + (strokes - 0.5f) * 0.6f);
            float moss = cov * Smooth(h - 0.06f, h - 0.01f, y) * (Fbm(x * 30f, y * 30f, 2, 209) > 0.5f ? 1f : 0f);
            float mist = Smooth(0.0f, 0.3f, y);
            return Px(cov * mist * SideFade(x, 3.2f), tone, moss);
        }

        private static Color HillsP(float x, float y)
        {
            float h = 0.42f + 0.38f * Fbm(x * 1.6f, 2f, 4, 301);
            float edge = (Fbm(x * 40f, y * 40f, 2, 303) - 0.5f) * 0.015f;
            float cov = 1f - Smooth(-0.004f, 0.004f, y - h + edge);
            float tone = Mathf.Clamp01(0.3f + 0.55f * Smooth(h - 0.18f, h, y) + (Fbm(x * 6f, y * 3f, 3, 305) - 0.5f) * 0.3f);
            return Px(cov * SideFade(x, 2048f / 384f), tone, 0f);
        }

        private static Color MistP(float x, float y)
        {
            float band = Mathf.Exp(-Mathf.Pow((y - 0.5f) / 0.22f, 2f));
            float clouds = Smooth(0.35f, 0.75f, Fbm(x * 2.2f, y * 1.5f, 5, 401));
            return Px(band * clouds * SideFade(x, 8f), 0f, 0f);
        }

        private static Color PineP(float x, float y)
        {
            // gnarled trunk leaning out from the right, branches with starburst needle clusters (teal accent)
            var q = new Vector2(x, y);
            float trunk = 1e3f;
            Vector2 a = new(0.85f, 0f), b = new(0.62f, 0.35f), c = new(0.7f, 0.62f), d = new(0.45f, 0.86f);
            trunk = Mathf.Min(trunk, RoundCone(q, a, 0.05f, b, 0.04f));
            trunk = Mathf.Min(trunk, RoundCone(q, b, 0.04f, c, 0.03f));
            trunk = Mathf.Min(trunk, RoundCone(q, c, 0.03f, d, 0.018f));
            var branches = new List<(Vector2, Vector2)>
            {
                (b, new Vector2(0.28f, 0.42f)), (c, new Vector2(0.95f, 0.72f)), (c, new Vector2(0.32f, 0.66f)),
                (d, new Vector2(0.2f, 0.9f)), (Vector2.Lerp(b, c, 0.5f), new Vector2(0.98f, 0.5f)),
            };
            foreach (var (s, e) in branches) trunk = Mathf.Min(trunk, RoundCone(q, s, 0.016f, e, 0.006f));
            trunk += (Fbm(x * 60f, y * 60f, 2, 501) - 0.5f) * 0.008f;
            float trunkCov = 1f - Smooth(-0.002f, 0.002f, trunk);

            float needles = 0f;
            int n = 0;
            foreach (var (s, e) in branches)
            {
                for (int k = 0; k < 6; k++, n++)
                {
                    Vector2 cpos = Vector2.Lerp(s, e, 0.35f + k * 0.13f) + new Vector2((Hash(n, 1, 503) - 0.5f) * 0.06f, (Hash(n, 2, 503) - 0.3f) * 0.06f);
                    float r = 0.045f + Hash(n, 3, 503) * 0.03f;
                    Vector2 dv = q - cpos;
                    float dist = dv.magnitude;
                    if (dist > r) continue;
                    float ang = Mathf.Atan2(dv.y, dv.x);
                    float spokes = Mathf.Abs(Mathf.Sin(ang * (9f + Hash(n, 4, 503) * 4f) + Hash(n, 5, 503) * 6f));
                    float spoke = Smooth(0.82f, 0.95f, spokes) * (1f - Smooth(r * 0.7f, r, dist)) + (1f - Smooth(0f, r * 0.18f, dist));
                    needles = Mathf.Max(needles, spoke);
                }
            }
            float cov = Mathf.Max(trunkCov, needles);
            return Px(cov, trunkCov > needles ? 1f : 0.6f, needles > trunkCov ? 1f : 0f);
        }

        private static Color GrassP(float x, float y)
        {
            // pampas: thin curved blades from the bottom, golden feather plumes on the tips (accent)
            float cov = 0f, plume = 0f;
            int count = 260;
            for (int i = 0; i < count; i++)
            {
                float bx = i / (float)count * 5.4f + (Hash(i, 1, 601) - 0.5f) * 0.05f;
                if (Mathf.Abs(x - bx) > 0.35f) continue;
                float h = 0.3f + Hash(i, 2, 601) * 0.55f;
                float lean = (Hash(i, 3, 601) - 0.4f) * 0.25f;
                if (y > h + 0.08f) continue;
                float t = Mathf.Clamp01(y / h);
                float cx = bx + lean * t * t;
                float w = Mathf.Lerp(0.004f, 0.001f, t);
                float blade = 1f - Smooth(w * 0.6f, w, Mathf.Abs(x - cx));
                cov = Mathf.Max(cov, blade * (y < h ? 1f : 0f));
                if (Hash(i, 4, 601) > 0.45f)
                {
                    float tipX = bx + lean;
                    float e = Ellipse(new Vector2(x, y), new Vector2(tipX + lean * 0.15f, h + 0.02f), new Vector2(0.008f, 0.045f));
                    float feather = (1f - Smooth(-0.004f, 0.004f, e + (Fbm(x * 80f, y * 40f, 2, 603) - 0.5f) * 0.02f));
                    plume = Mathf.Max(plume, feather);
                }
            }
            return Px(Mathf.Max(cov, plume * 0.85f) * SideFade(x, 2048f / 384f), 0.7f, plume);
        }

        // =====================================================================
        //  village (Tsushima-style: low timber houses, deep curved tiled roofs, paper windows)
        // =====================================================================

        private static Color HouseRowP(float x, float y, int seed, bool back)
        {
            float cov = 0f, tone = 0f, accent = 0f;
            float cursor = 0.05f;
            int i = 0;
            float total = 2048f / (back ? 512f : 640f);
            while (cursor < total - 0.1f)
            {
                float w = 0.45f + Hash(i, 1, seed) * 0.45f;
                float wallH = (back ? 0.32f : 0.3f) + Hash(i, 2, seed) * 0.12f;
                float roofH = 0.16f + Hash(i, 3, seed) * 0.1f;
                bool twoStorey = Hash(i, 4, seed) > 0.7f;
                if (twoStorey) wallH += 0.18f;
                float x0 = cursor, x1 = cursor + w, mid = (x0 + x1) * 0.5f;
                if (x > x0 - 0.12f && x < x1 + 0.12f)
                {
                    // wall + posts
                    bool inWall = x > x0 && x < x1 && y < wallH;
                    if (inWall)
                    {
                        cov = 1f;
                        float post = Mathf.Min(Mathf.Abs(x - x0 - 0.015f), Mathf.Abs(x - x1 + 0.015f));
                        float beam = Mathf.Abs(y - wallH * 0.48f);
                        tone = post < 0.012f || beam < 0.008f ? 0.9f : 0.28f + Fbm(x * 30f, y * 30f, 2, seed) * 0.15f;
                        // shoji windows (glow accent)
                        int panes = Mathf.Max(1, Mathf.RoundToInt(w / 0.2f));
                        for (int p = 0; p < panes; p++)
                        {
                            float wx = Mathf.Lerp(x0 + 0.05f, x1 - 0.05f, (p + 0.5f) / panes);
                            float wy = twoStorey ? wallH * 0.72f : wallH * 0.6f;
                            if (Mathf.Abs(x - wx) < 0.045f && Mathf.Abs(y - wy) < 0.055f)
                            {
                                bool lattice = Frac((x - wx) * 45f) < 0.12f || Frac((y - wy) * 45f) < 0.12f;
                                accent = lattice ? 0f : 1f;
                                tone = lattice ? 0.9f : 0.15f;
                            }
                        }
                        // sliding door on single-storey houses
                        if (!twoStorey && Mathf.Abs(x - (x0 + w * 0.3f)) < 0.05f && y < wallH * 0.75f) tone = 0.8f;
                    }
                    // roof: deep eaves, upturned corners, ridge
                    float u = (x - mid) / (w * 0.5f + 0.1f);
                    if (Mathf.Abs(u) < 1f)
                    {
                        float eave = wallH - 0.015f + 0.035f * Mathf.Pow(Mathf.Abs(u), 6f);      // corners flick up
                        float top = wallH + roofH * (1f - Mathf.Pow(Mathf.Abs(u), 1.6f) * 0.85f);
                        float ridge = top + 0.02f * (1f - Mathf.Abs(u));
                        if (y > eave && y < ridge)
                        {
                            cov = 1f;
                            float tiles = Frac((x - mid) * 60f) < 0.18f ? 0.08f : 0f;
                            tone = Mathf.Clamp01(0.85f + tiles + (y > top ? 0.1f : 0f) - Smooth(eave, eave + 0.03f, y) * 0f);
                            accent = 0f;
                        }
                    }
                    // a hanging lantern on some eaves
                    if (!back && Hash(i, 5, seed) > 0.55f)
                    {
                        float lx = x0 + w * 0.82f, ly = wallH - 0.07f;
                        float e = Ellipse(new Vector2(x, y), new Vector2(lx, ly), new Vector2(0.025f, 0.035f));
                        if (e < 0f) { cov = 1f; tone = 0.2f; accent = 1f; }
                        if (Mathf.Abs(x - lx) < 0.002f && y > ly && y < wallH) { cov = 1f; tone = 1f; }
                    }
                }
                cursor = x1 + 0.08f + Hash(i, 6, seed) * 0.15f;
                i++;
            }
            float edge = (Fbm(x * 50f, y * 50f, 2, seed + 7) - 0.5f) * 0.6f;
            return Px(Mathf.Clamp01(cov), Mathf.Clamp01(tone + edge * 0.1f), accent);
        }

        private static Color WallP(float x, float y)
        {
            // a close house wall (plaster between timber posts), for shadows to fall on
            float post = Mathf.Min(Frac(x * 1.0f), 1f - Frac(x * 1.0f));
            bool timber = post < 0.03f || Mathf.Abs(y - 0.12f) < 0.02f || y > 0.93f;
            float tone = timber ? 0.85f : 0.1f + Fbm(x * 12f, y * 12f, 3, 701) * 0.12f;
            return Px(1f, tone, 0f);
        }

        private static Color WellP(float x, float y)
        {
            var q = new Vector2(x, y);
            float d = Box(q, new Vector2(0.5f, 0.17f), new Vector2(0.28f, 0.15f));            // stone ring
            d = Mathf.Min(d, Box(q, new Vector2(0.5f, 0.82f), new Vector2(0.36f, 0.04f)));     // little roof
            d = Mathf.Min(d, Box(q, new Vector2(0.24f, 0.5f), new Vector2(0.02f, 0.32f)));
            d = Mathf.Min(d, Box(q, new Vector2(0.76f, 0.5f), new Vector2(0.02f, 0.32f)));
            d = Mathf.Min(d, Box(q, new Vector2(0.5f, 0.62f), new Vector2(0.24f, 0.012f)));    // crossbar
            d = Mathf.Min(d, Capsule(q, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.42f), 0.004f)); // rope
            d = Mathf.Min(d, Box(q, new Vector2(0.5f, 0.39f), new Vector2(0.04f, 0.035f)));    // bucket
            float roofCurve = Box(q, new Vector2(0.5f, 0.87f + 0.03f * Mathf.Pow(Mathf.Abs(x - 0.5f) / 0.4f, 4f)), new Vector2(0.4f, 0.025f));
            d = Mathf.Min(d, roofCurve);
            float cov = 1f - Smooth(-0.003f, 0.003f, d + Noise(x, y, 711) * 0.006f);
            float stones = Frac(x * 14f + (Frac(y * 9f) < 0.5f ? 0.5f : 0f)) < 0.08f || Frac(y * 9f) < 0.1f ? 0.8f : 0.45f;
            return Px(cov, y < 0.32f ? stones : 0.9f, 0f);
        }

        private static Color StallP(float x, float y)
        {
            // market stall: striped awning on poles, a counter piled with goods (round fruit, baskets)
            var q = new Vector2(x, y);
            float aspect = 768f / 512f;
            float cov = 0f, tone = 0f, accent = 0f;
            float awningY = 0.78f + 0.04f * Mathf.Sin((x / aspect) * Mathf.PI);
            if (y > awningY - 0.1f && y < awningY && x > 0.05f && x < aspect - 0.05f)
            {
                cov = 1f;
                bool stripe = Frac(x * 6f) < 0.5f;
                tone = stripe ? 0.75f : 0.3f;
                accent = stripe ? 0f : 0.6f;
                // scalloped hem
                float sc = Mathf.Abs(Frac(x * 12f) - 0.5f);
                if (y < awningY - 0.1f + 0.03f * (1f - sc * 2f)) cov = 0f;
            }
            foreach (float px in new[] { 0.12f, aspect - 0.12f })
                if (Mathf.Abs(x - px) < 0.015f && y < awningY) { cov = 1f; tone = 0.95f; }
            if (Box(q, new Vector2(aspect * 0.5f, 0.22f), new Vector2(aspect * 0.42f, 0.2f)) < 0f) { cov = 1f; tone = 0.7f; accent = 0f; }
            for (int i = 0; i < 9; i++)
            {
                Vector2 c = new(0.25f + i * 0.13f, 0.47f + (i % 2) * 0.04f);
                if ((q - c).magnitude < 0.05f) { cov = 1f; tone = 0.35f; accent = 1f; }
            }
            float edge = Noise(x, y, 721);
            return Px(cov, Mathf.Clamp01(tone + edge * 0.1f), accent);
        }

        private static Color LanternP(float x, float y)
        {
            var q = new Vector2(x, y);
            float body = Ellipse(q, new Vector2(0.25f, 0.45f), new Vector2(0.19f, 0.27f));
            float caps = Mathf.Min(Box(q, new Vector2(0.25f, 0.72f), new Vector2(0.1f, 0.025f)), Box(q, new Vector2(0.25f, 0.18f), new Vector2(0.1f, 0.025f)));
            float cord = Box(q, new Vector2(0.25f, 0.86f), new Vector2(0.006f, 0.14f));
            float tassel = Box(q, new Vector2(0.25f, 0.1f), new Vector2(0.012f, 0.06f));
            float d = Mathf.Min(Mathf.Min(body, caps), Mathf.Min(cord, tassel));
            float cov = 1f - Smooth(-0.003f, 0.003f, d);
            bool ribs = body < 0f && Frac(y * 22f) < 0.12f;
            bool paper = body < 0f && !ribs;
            return Px(cov, paper ? 0.15f : 0.95f, paper ? 1f : 0f);
        }

        private static Color ToriiP(float x, float y)
        {
            var q = new Vector2(x, y);
            float aspect = 1024f / 768f;
            float cx = aspect * 0.5f;
            float d = Box(q, new Vector2(cx - 0.42f, 0.4f), new Vector2(0.035f, 0.4f));
            d = Mathf.Min(d, Box(q, new Vector2(cx + 0.42f, 0.4f), new Vector2(0.035f, 0.4f)));
            d = Mathf.Min(d, Box(q, new Vector2(cx, 0.68f), new Vector2(0.55f, 0.025f)));                         // nuki
            float kasagiY = 0.84f + 0.05f * Mathf.Pow(Mathf.Abs(x - cx) / 0.62f, 3f);
            d = Mathf.Min(d, Box(q, new Vector2(cx, kasagiY), new Vector2(0.62f, 0.035f)));                        // kasagi, curved up
            d = Mathf.Min(d, Box(q, new Vector2(cx, 0.76f), new Vector2(0.03f, 0.06f)));                            // gakuzuka
            float cov = 1f - Smooth(-0.003f, 0.003f, d + Noise(x, y, 731) * 0.005f);
            return Px(cov, 0.4f, cov);   // accent = vermilion
        }

        private static Color TombP(float x, float y)
        {
            // a cliff with a colossal carved gate: stepped stone frame, a black maw, guardian pillars
            var q = new Vector2(x, y);
            float aspect = 1536f / 1024f;
            float cx = aspect * 0.5f;
            float cliffTop = 0.86f + 0.1f * Fbm(x * 2f, 1f, 4, 801) - 0.95f * Mathf.Pow(Mathf.Abs(x - cx) / cx, 2.2f);
            float cliff = y - cliffTop + (Fbm(x * 30f, y * 30f, 2, 803) - 0.5f) * 0.02f;
            float cov = 1f - Smooth(-0.003f, 0.003f, cliff);
            float tone = 0.55f + (Fbm(x * 10f, y * 25f, 3, 805) - 0.5f) * 0.5f;
            float accent = 0f;
            // stepped frame
            for (int s = 0; s < 3; s++)
            {
                float fw = 0.34f - s * 0.06f, fh = 0.62f - s * 0.07f;
                float frame = Box(q, new Vector2(cx, fh * 0.5f), new Vector2(fw, fh * 0.5f));
                if (frame < 0f && frame > -0.02f) tone = 0.95f;
                else if (frame < 0f) tone = 0.45f + s * 0.1f;
            }
            // archway maw
            float maw = Box(q, new Vector2(cx, 0.2f), new Vector2(0.14f, 0.2f));
            maw = Mathf.Min(maw, (q - new Vector2(cx, 0.4f)).magnitude - 0.14f);
            if (maw < 0f) { tone = 1f; accent = 0f; }
            // carved lintel glyphs (faint accent)
            if (Mathf.Abs(y - 0.6f) < 0.02f && Mathf.Abs(x - cx) < 0.2f && Frac(x * 30f) < 0.5f) accent = 0.4f;
            // guardian pillars
            foreach (float px in new[] { cx - 0.42f, cx + 0.42f })
            {
                float p = Box(q, new Vector2(px, 0.25f), new Vector2(0.05f, 0.25f));
                p = Mathf.Min(p, (q - new Vector2(px, 0.55f)).magnitude - 0.06f);
                if (p < 0f) { cov = 1f; tone = 0.8f; }
            }
            // stairs
            if (y < 0.06f && Mathf.Abs(x - cx) < 0.3f + (0.06f - y) * 2f) { cov = 1f; tone = Frac(y * 50f) < 0.3f ? 0.9f : 0.5f; }
            return Px(cov, Mathf.Clamp01(tone), accent);
        }

        private static Color FigureRowP(float x, float y)
        {
            // past wanderers pressed into the stone: a long frieze of silhouettes, each different
            float cov = 0f;
            for (int i = 0; i < 18; i++)
            {
                float cx = 0.15f + i * 0.22f + (Hash(i, 1, 901) - 0.5f) * 0.05f;
                if (Mathf.Abs(x - cx) > 0.15f) continue;
                float s = 0.8f + Hash(i, 2, 901) * 0.2f;
                float d = HumanSil(new Vector2(x, y), cx, s, Hash(i, 3, 901), Hash(i, 4, 901) > 0.5f);
                cov = Mathf.Max(cov, 1f - Smooth(-0.003f, 0.003f, d + Noise(x, y, 903) * 0.006f));
            }
            return Px(cov, 1f, 0f);
        }

        private static float HumanSil(Vector2 q, float cx, float s, float pose, bool torch)
        {
            float lean = (pose - 0.5f) * 0.05f;
            Vector2 feet = new(cx, 0.05f);
            float d = RoundCone(q, feet + new Vector2(0f, 0.02f) * s, 0.06f * s, feet + new Vector2(lean, 0.5f) * s, 0.04f * s);
            d = Mathf.Min(d, (q - (feet + new Vector2(lean * 1.2f, 0.62f) * s)).magnitude - 0.045f * s);
            Vector2 sh = feet + new Vector2(lean, 0.5f) * s;
            Vector2 hand = sh + new Vector2(0.09f + pose * 0.05f, -0.05f + pose * 0.2f) * s;
            d = Mathf.Min(d, Capsule(q, sh, hand, 0.012f * s));
            if (torch) d = Mathf.Min(d, Capsule(q, hand, hand + new Vector2(0.01f, 0.14f) * s, 0.008f * s));
            return d;
        }

        private static Color OvenP(float x, float y)
        {
            var q = new Vector2(x, y);
            float d = Box(q, new Vector2(0.5f, 0.3f), new Vector2(0.38f, 0.3f));
            d = Mathf.Min(d, (q - new Vector2(0.5f, 0.6f)).magnitude - 0.3f);
            d = Mathf.Min(d, Box(q, new Vector2(0.72f, 0.82f), new Vector2(0.05f, 0.18f)));     // chimney
            float mouth = (q - new Vector2(0.5f, 0.36f)).magnitude - 0.14f;
            mouth = Mathf.Min(mouth, Box(q, new Vector2(0.5f, 0.24f), new Vector2(0.14f, 0.12f)));
            float cov = 1f - Smooth(-0.003f, 0.003f, d + Noise(x, y, 951) * 0.006f);
            bool glow = mouth < 0f;
            return Px(cov, glow ? 0.1f : 0.75f, glow ? 1f : 0f);
        }

        private static Color FireP(float x, float y)
        {
            // flame shape mask (the shader animates it); G = inner hot core
            float cx = 0.25f;
            float w = 0.2f * Mathf.Pow(Mathf.Max(0f, 1f - y), 0.7f) * Smooth(0f, 0.12f, y);
            float body = 1f - Smooth(w * 0.7f, w, Mathf.Abs(x - cx));
            float core = 1f - Smooth(w * 0.2f, w * 0.55f, Mathf.Abs(x - cx));
            core *= 1f - Smooth(0.2f, 0.55f, y);
            return Px(body, core, 0f);
        }

        private static Color LogsP(float x, float y)
        {
            var q = new Vector2(x, y);
            float aspect = 512f / 192f;
            float d = Capsule(q, new Vector2(0.4f, 0.2f), new Vector2(aspect - 0.6f, 0.45f), 0.09f);
            d = Mathf.Min(d, Capsule(q, new Vector2(0.6f, 0.45f), new Vector2(aspect - 0.4f, 0.2f), 0.09f));
            d = Mathf.Min(d, Capsule(q, new Vector2(0.3f, 0.12f), new Vector2(aspect - 0.3f, 0.12f), 0.07f));
            float cov = 1f - Smooth(-0.003f, 0.003f, d);
            float ember = Fbm(x * 12f, y * 12f, 2, 961) > 0.62f ? 1f : 0f;
            return Px(cov, 0.85f, ember * cov);
        }

        private static Color MoonP(float x, float y)
        {
            float r = (new Vector2(x, y) - new Vector2(0.5f, 0.5f)).magnitude;
            float disc = 1f - Smooth(0.36f, 0.38f, r + (Fbm(x * 8f, y * 8f, 3, 971) - 0.5f) * 0.02f);
            float halo = Mathf.Exp(-Mathf.Pow((r - 0.38f) / 0.08f, 2f)) * 0.4f * (r > 0.38f ? 1f : 0f);
            return Px(Mathf.Max(disc, halo), Fbm(x * 6f, y * 6f, 3, 973) * 0.25f, disc);
        }

        private static Color LeafP(float x, float y)
        {
            // five-lobed maple leaf
            var q = new Vector2(x, y) - new Vector2(0.5f, 0.45f);
            float ang = Mathf.Atan2(q.y, q.x);
            float r = q.magnitude;
            float lobes = 0.28f + 0.14f * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2.5f + 0.3f)), 3f);
            if (q.y < -0.15f) lobes *= 0.6f;
            float cov = 1f - Smooth(lobes - 0.02f, lobes, r);
            float stem = Mathf.Abs(q.x) < 0.012f && q.y < 0f && q.y > -0.4f ? 1f : 0f;
            return Px(Mathf.Max(cov, stem), 0.3f + r, 1f);
        }

        private static Color SealP(float x, float y)
        {
            // square red chop: thick worn border, abstract carved strokes inside (negative space)
            var q = new Vector2(x, y);
            float outer = Box(q, new Vector2(0.5f, 0.5f), new Vector2(0.42f, 0.42f));
            float inner = Box(q, new Vector2(0.5f, 0.5f), new Vector2(0.34f, 0.34f));
            float worn = Fbm(x * 25f, y * 25f, 3, 981);
            float cov = outer < 0f ? 1f : 0f;
            bool carved = false;
            if (inner < 0f)
            {
                // a few chunky strokes carved out of the red
                carved |= Mathf.Abs(x - 0.5f) < 0.025f && y > 0.24f && y < 0.76f;
                carved |= Mathf.Abs(y - 0.62f) < 0.025f && x > 0.27f && x < 0.73f;
                carved |= Mathf.Abs(y - 0.38f) < 0.025f && x > 0.3f && x < 0.5f;
                carved |= Mathf.Abs(x - 0.32f) < 0.025f && y > 0.38f && y < 0.62f;
                carved |= Mathf.Abs(y - 0.3f + (x - 0.55f) * 0.6f) < 0.022f && x > 0.55f && x < 0.75f;
                carved |= inner > -0.02f;
            }
            if (carved) cov = 0f;
            if (worn > 0.72f) cov *= 0.2f;
            return Px(cov, 0.2f, cov);
        }

        private static Color PaperP(float x, float y)
        {
            // fibrous rice paper, tileable: long thin fibres + mottling
            float fib = 0f;
            for (int i = 0; i < 3; i++)
            {
                float a = Hash(i, 1, 991) * 3.14f;
                float u = x * Mathf.Cos(a) + y * Mathf.Sin(a);
                float v = -x * Mathf.Sin(a) + y * Mathf.Cos(a);
                fib += Smooth(0.7f, 0.95f, VN(u * 4f, v * 160f, 993 + i));
            }
            float mottle = Fbm(x * 6f, y * 6f, 4, 995);
            float g = Mathf.Clamp01(0.75f + mottle * 0.2f - fib * 0.12f);
            return new Color(g, g, g, 1f);
        }

        // =====================================================================
        //  the Void (2D): a towering hunched shape in a ragged cloak that dissolves into smoke
        // =====================================================================

        private static Color VoidBodyP(float x, float y)
        {
            // 1024 x 1536 -> aspect 0.667. A tall wraith in a torn cloak, leaning forward (+x). The cloak billows back
            // (-x), its hem shreds into points and smoke, a deep hood droops forward with nothing inside it.
            var q = new Vector2(x, y);
            float spine = 0.28f + 0.11f * Smooth(0.15f, 0.92f, y) + 0.015f * Mathf.Sin(y * 7f);
            float low = (Fbm(y * 3f, 1.7f, 3, 1001) - 0.5f) * 0.07f;                                   // slow billows
            float streak = (Fbm(y * 22f, x * 1.5f, 2, 1002) - 0.5f) * 0.025f;                            // fabric streaks
            float u = q.x - spine;
            // half-width profile: wide torn hem, narrow waist, hunched shoulders, thin neck
            float wFront = Mathf.Lerp(0.2f, 0.085f, Smooth(0.05f, 0.6f, y));
            float wBack = Mathf.Lerp(0.3f, 0.11f, Smooth(0.0f, 0.62f, y)) + 0.05f * Smooth(0.25f, 0.0f, y);
            float w = (u > 0f ? wFront : wBack) + low + streak;
            float shoulders = Ellipse(q, new Vector2(spine - 0.015f, 0.66f), new Vector2(0.135f, 0.05f));
            float body = Mathf.Max(Mathf.Abs(u) - w, y - 0.66f);
            float d = Mathf.Min(body, shoulders);
            Vector2 neck = new(spine + 0.03f, 0.7f);
            d = Mathf.Min(d, RoundCone(q, new Vector2(spine, 0.66f), 0.05f, neck + new Vector2(0.03f, 0.05f), 0.04f));
            // hood: a drooping teardrop with a long point trailing back over the shoulders
            Vector2 hood = neck + new Vector2(0.08f, 0.1f);
            d = Mathf.Min(d, RoundCone(q, neck + new Vector2(0.01f, 0.03f), 0.06f, hood, 0.07f));
            d = Mathf.Min(d, RoundCone(q, hood + new Vector2(-0.02f, 0.02f), 0.05f, hood + new Vector2(-0.09f, 0.05f), 0.006f));
            d = Mathf.Min(d, RoundCone(q, hood, 0.065f, hood + new Vector2(0.06f, -0.04f), 0.03f));    // the cowl hangs over the void of a face
            // tattered hem: long ragged points hanging below the cloak
            float hemLine = 0.13f + 0.1f * Mathf.Pow(Mathf.Abs(Mathf.Sin(x * 26f + Fbm(x * 4f, 2f, 2, 1004) * 6f)), 3f) + (Fbm(x * 9f, 3f, 2, 1006) - 0.5f) * 0.06f;
            if (y < 0.2f) d = Mathf.Max(d, hemLine - y - 0.1f);
            // trailing tendrils
            for (int i = 0; i < 8; i++)
            {
                float sy = 0.12f + Hash(i, 1, 1007) * 0.45f;
                float side = i % 3 == 0 ? 1f : -1f;
                float sx = spine + side * (side > 0f ? Mathf.Lerp(0.18f, 0.08f, sy / 0.66f) : Mathf.Lerp(0.27f, 0.1f, sy / 0.66f));
                Vector2 p0 = new(sx, sy);
                Vector2 p1 = p0 + new Vector2(side * (0.06f + Hash(i, 2, 1007) * 0.08f), 0.02f + (Hash(i, 3, 1007) - 0.4f) * 0.08f);
                Vector2 p2 = p1 + new Vector2(side * 0.05f, 0.05f + Hash(i, 4, 1007) * 0.05f);
                d = Mathf.Min(d, RoundCone(q, p0, 0.016f, p1, 0.008f));
                d = Mathf.Min(d, RoundCone(q, p1, 0.008f, p2, 0.0008f));
            }
            d += (Fbm(x * 45f, y * 45f, 3, 1003) - 0.5f) * 0.012f;
            float cov = 1f - Smooth(-0.003f, 0.006f, d);
            float wisps = (1f - Smooth(0f, 0.07f, d)) * Smooth(0.55f, 0.85f, Fbm(x * 10f, y * 16f, 4, 1005)) * 0.55f;
            float tone = 0.88f + Fbm(x * 8f, y * 8f, 3, 1011) * 0.12f;
            return Px(Mathf.Max(cov, wisps), tone, 0f);
        }

        private static Color VoidArmP(float x, float y)
        {
            // 256 x 1024 (aspect 0.25); pivot at the top centre. A torn sleeve to the elbow, then a long bony forearm,
            // a knuckled hand and five hooked claws. The whole limb curves a little, never ruler-straight.
            var q = new Vector2(x, y);
            float cx = 0.125f + (Fbm(y * 2.5f, 4.2f, 2, 1013) - 0.5f) * 0.04f + 0.012f * Mathf.Sin(y * 5f);
            // sleeve: a rounded cone from the shoulder pin that flares and tears into points around the elbow
            float sleeve = RoundCone(q, new Vector2(cx, 0.955f), 0.03f, new Vector2(cx - 0.005f, 0.56f), 0.08f) + (Fbm(y * 18f, 1f, 2, 1015) - 0.5f) * 0.012f;
            float sleeveBottom = 0.5f + 0.08f * Mathf.Pow(Mathf.Abs(Mathf.Sin((x - 0.125f) * 120f)), 2f);
            float d = Mathf.Max(sleeve, sleeveBottom - y);
            Vector2 elbow = new(cx, 0.56f), wrist = new(cx + 0.01f, 0.2f);
            d = Mathf.Min(d, RoundCone(q, elbow, 0.02f, wrist, 0.011f));                                       // forearm
            d = Mathf.Min(d, (q - (wrist + new Vector2(0f, 0.01f))).magnitude - 0.017f);                       // wrist knob
            d = Mathf.Min(d, Ellipse(q, wrist - new Vector2(0f, 0.035f), new Vector2(0.028f, 0.035f)));        // hand
            for (int f = 0; f < 5; f++)
            {
                float spread = (f - 2f) * 0.022f;
                Vector2 k = wrist + new Vector2(spread * 0.6f, -0.06f);
                Vector2 m = k + new Vector2(spread * 0.7f, -0.08f + Mathf.Abs(f - 2f) * 0.015f);
                Vector2 t = m + new Vector2(spread * 0.2f + 0.018f, -0.06f + Mathf.Abs(f - 2f) * 0.012f);      // claws hook forward
                d = Mathf.Min(d, RoundCone(q, k, 0.008f, m, 0.0055f));
                d = Mathf.Min(d, RoundCone(q, m, 0.0055f, t, 0.0005f));
            }
            d += (Fbm(x * 60f, y * 60f, 2, 1011) - 0.5f) * 0.008f;
            float cov = 1f - Smooth(-0.002f, 0.006f, d);
            return Px(cov, 0.95f, 0f);
        }

        // =====================================================================
        //  ground, calligraphy, auspicious clouds
        // =====================================================================

        private static Color GroundP(float x, float y)
        {
            // a broad earth band: soft top edge with tufts, ink-dark toward the bottom
            float top = 0.8f + 0.1f * (Fbm(x * 2.5f, 1f, 3, 1301) - 0.5f) + (Fbm(x * 40f, 2f, 2, 1303) - 0.5f) * 0.03f;
            float cov = 1f - Smooth(-0.004f, 0.004f, y - top);
            float rim = Smooth(top - 0.08f, top, y) * 0.35f;                                                // pigment pooled along the top
            float tone = Mathf.Clamp01(0.3f + (top - y) * 0.55f + rim + (Fbm(x * 5f, y * 2.5f, 3, 1305) - 0.5f) * 0.3f);
            return Px(cov * SideFade(x, 8f), tone, 0f);
        }

        private static Color CalligraphyP(float x, float y)
        {
            // a column of pseudo-hanzi in a loose running hand: each glyph is a cluster of tapered strokes
            var q = new Vector2(x, y);
            float d = 1e3f;
            const int glyphs = 5;
            for (int g = 0; g < glyphs; g++)
            {
                float gy = 1f - (g + 0.5f) / glyphs;
                if (Mathf.Abs(y - gy) > 0.12f) continue;
                Vector2 c = new(0.125f, gy);
                int strokes = 4 + (int)(Hash(g, 0, 1401) * 4f);
                for (int s = 0; s < strokes; s++)
                {
                    float kind = Hash(g, s * 3 + 1, 1401);
                    Vector2 o = c + new Vector2((Hash(g, s * 3 + 2, 1401) - 0.5f) * 0.12f, (Hash(g, s * 3 + 3, 1401) - 0.5f) * 0.12f);
                    Vector2 dir = kind < 0.35f ? new Vector2(1f, 0.12f) : kind < 0.65f ? new Vector2(0.05f, -1f) : kind < 0.85f ? new Vector2(0.8f, -0.7f) : new Vector2(-0.6f, -0.8f);
                    float len = kind > 0.92f ? 0.015f : 0.04f + Hash(g, s, 1403) * 0.05f;
                    Vector2 a = o - dir.normalized * len * 0.5f, b = o + dir.normalized * len * 0.5f;
                    d = Mathf.Min(d, RoundCone(q, a, 0.012f, b, 0.004f));
                }
            }
            d += (Fbm(x * 90f, y * 90f, 2, 1405) - 0.5f) * 0.004f;
            float cov = 1f - Smooth(-0.002f, 0.002f, d);
            float dry = Smooth(0.62f, 0.75f, Fbm(x * 20f, y * 200f, 2, 1407)); // dry-brush streaks
            return Px(cov * (1f - dry * 0.6f), 1f, 0f);
        }

        private static Color CloudScrollP(float x, float y)
        {
            // xiangyun: auspicious clouds, a flat-bottomed row of round billows, each with a curling gold spiral
            var q = new Vector2(x, y);
            float aspect = 1024f / 384f;
            float d = Capsule(q, new Vector2(aspect * 0.12f, 0.17f), new Vector2(aspect * 0.9f, 0.17f), 0.045f);
            float accent = 0f;
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n;
                Vector2 c = new(aspect * (0.12f + t * 0.76f), 0.3f + 0.25f * Mathf.Sin(t * Mathf.PI) + (Hash(i, 1, 1501) - 0.5f) * 0.1f);
                float r = 0.13f + 0.08f * Mathf.Sin(t * Mathf.PI) + Hash(i, 2, 1501) * 0.04f;
                float dc = (q - c).magnitude - r;
                d = Mathf.Min(d, dc);
                // spiral: distance to an Archimedean curl inside the billow
                Vector2 v = q - c;
                float ang = Mathf.Atan2(v.y, v.x) + (i % 2 == 0 ? 0f : Mathf.PI);
                float rr = v.magnitude / r;
                float turn = Frac((rr * 2.2f - ang / (2f * Mathf.PI)));
                float line = Mathf.Abs(turn - 0.5f);
                if (rr < 0.84f && rr > 0.08f && line < 0.12f) accent = Mathf.Max(accent, 1f);
            }
            float cov = 1f - Smooth(-0.003f, 0.003f, d + Noise(x, y, 1503) * 0.006f);
            float rim = cov * Smooth(-0.03f, -0.016f, d); // gold outline
            accent = Mathf.Max(accent * cov, rim);
            return Px(cov * Smooth(0.03f, 0.12f, y), 0.15f, accent);
        }

        // =====================================================================
        //  shadow puppets (皮影): profile figures facing +x, dyed-leather robes with gold-lit cut-out bands,
        //  ink-black heads and hands. Arms are separate plates pinned at the shoulder.
        //  Mask: tone 1 = black leather, ~0.3 = dyed colour, accent = cut-outs the light shines through.
        // =====================================================================

        /// <summary>Body: feet at y≈0.02, head top near y≈0.97. Shoulder pivot for the arm per kind.</summary>
        public static float ShoulderY(string kind) => kind switch
        {
            "Child" => 0.56f,
            "Elder" => 0.5f,
            _ => 0.73f,
        };

        private static Color PuppetBodyP(string kind, float x, float y)
        {
            var q = new Vector2(x, y);
            float cx = 0.24f; // 512 wide / 1024 tall -> aspect 0.5
            float black = 1e3f, robe = 1e3f;
            float headY = 0.875f, headR = 0.052f, hemY = 0.08f, obiY = 0.5f, collarY = 0.73f;
            Vector2 head;
            switch (kind)
            {
                case "Child":
                    headY = 0.7f; headR = 0.07f; hemY = 0.2f; obiY = 0.38f; collarY = 0.57f;
                    head = new Vector2(cx + 0.01f, headY);
                    robe = RoundCone(q, new Vector2(cx, 0.24f), 0.1f, new Vector2(cx, 0.55f), 0.07f);
                    robe = Mathf.Max(robe, hemY - 0.01f - y + Mathf.Abs(Mathf.Sin(x * 60f)) * 0.008f);
                    black = Capsule(q, new Vector2(cx - 0.035f, 0.03f), new Vector2(cx - 0.03f, 0.22f), 0.022f);                 // legs mid-stride
                    black = Mathf.Min(black, Capsule(q, new Vector2(cx + 0.05f, 0.03f), new Vector2(cx + 0.03f, 0.22f), 0.022f));
                    black = Mathf.Min(black, Ellipse(q, new Vector2(cx - 0.025f, 0.025f), new Vector2(0.035f, 0.015f)));
                    black = Mathf.Min(black, Ellipse(q, new Vector2(cx + 0.065f, 0.025f), new Vector2(0.035f, 0.015f)));
                    black = Mathf.Min(black, Ellipse(q, new Vector2(cx - 0.03f, headY + 0.075f), new Vector2(0.032f, 0.024f))); // tuft
                    black = Mathf.Min(black, Ellipse(q, new Vector2(cx + 0.045f, headY + 0.055f), new Vector2(0.025f, 0.02f)));
                    break;
                case "Elder":
                    // seated cross-legged, a long white beard, bent back, staff planted beside him
                    headY = 0.66f; headR = 0.06f; hemY = 0.04f; obiY = 0.3f; collarY = 0.53f;
                    head = new Vector2(cx + 0.06f, headY);
                    robe = Ellipse(q, new Vector2(cx, 0.1f), new Vector2(0.21f, 0.09f));
                    robe = Mathf.Min(robe, RoundCone(q, new Vector2(cx - 0.02f, 0.16f), 0.12f, new Vector2(cx + 0.03f, 0.52f), 0.075f));
                    black = Ellipse(q, new Vector2(cx + 0.03f, headY + 0.06f), new Vector2(0.04f, 0.025f));                      // topknot
                    black = Mathf.Min(black, Capsule(q, new Vector2(cx - 0.18f, 0.02f), new Vector2(cx - 0.15f, 0.88f), 0.01f));   // staff
                    black = Mathf.Min(black, Ellipse(q, new Vector2(cx - 0.15f, 0.88f), new Vector2(0.022f, 0.03f)));              // gourd knot on the staff
                    break;
                default:
                    head = new Vector2(cx + 0.008f, headY);
                    float hemHalf = kind == "Woman" ? 0.15f : kind == "Bearer" ? 0.16f : 0.13f;
                    // robe: flares to the hem; the woman's trails back, the bearer's travelling cloak streams behind
                    robe = RoundCone(q, new Vector2(cx - (kind == "Woman" ? 0.02f : 0f), 0.14f), hemHalf, new Vector2(cx, 0.72f), 0.085f);
                    if (kind == "Woman") robe = Mathf.Min(robe, RoundCone(q, new Vector2(cx - 0.12f, 0.06f), 0.05f, new Vector2(cx - 0.02f, 0.3f), 0.08f));
                    if (kind == "Bearer") robe = Mathf.Min(robe, RoundCone(q, new Vector2(cx - 0.17f, 0.18f), 0.05f, new Vector2(cx - 0.02f, 0.76f), 0.07f));
                    robe = Mathf.Max(robe, hemY - y + Mathf.Abs(Mathf.Sin(x * 55f)) * 0.01f);
                    black = Capsule(q, new Vector2(cx - 0.035f, 0.025f), new Vector2(cx - 0.03f, hemY + 0.02f), 0.02f);              // feet, a step apart
                    black = Mathf.Min(black, Capsule(q, new Vector2(cx + 0.05f, 0.025f), new Vector2(cx + 0.035f, hemY + 0.02f), 0.02f));
                    black = Mathf.Min(black, Ellipse(q, new Vector2(cx - 0.02f, 0.022f), new Vector2(0.035f, 0.014f)));
                    black = Mathf.Min(black, Ellipse(q, new Vector2(cx + 0.065f, 0.022f), new Vector2(0.035f, 0.014f)));
                    black = Mathf.Min(black, Capsule(q, new Vector2(cx + 0.005f, collarY - 0.02f), new Vector2(cx + 0.01f, headY - headR * 0.8f), 0.022f)); // neck
                    if (kind == "Man") black = Mathf.Min(black, Ellipse(q, new Vector2(cx - 0.012f, headY + 0.06f), new Vector2(0.02f, 0.026f)));      // topknot
                    if (kind == "Woman")
                    {
                        black = Mathf.Min(black, (q - new Vector2(cx - 0.045f, headY + 0.03f)).magnitude - 0.042f);                                    // bun
                        black = Mathf.Min(black, Capsule(q, new Vector2(cx - 0.1f, headY + 0.06f), new Vector2(cx + 0.0f, headY + 0.04f), 0.004f));     // hairpin
                    }
                    if (kind == "Vendor")
                    {
                        // kasa: a wide shallow conical straw hat
                        float hy = headY + 0.03f;
                        float hat = Mathf.Max(Mathf.Max(Mathf.Abs(x - cx) - 0.15f, y - hy - (0.15f - Mathf.Abs(x - cx)) * 0.42f), hy - 0.012f - y);
                        black = Mathf.Min(black, hat);
                    }
                    if (kind == "Bearer")
                    {
                        // a hood over the head, and a scarf tail flying back
                        black = Mathf.Min(black, Ellipse(q, new Vector2(cx - 0.012f, headY + 0.012f), new Vector2(0.068f, 0.07f)));
                        black = Mathf.Min(black, RoundCone(q, new Vector2(cx - 0.02f, collarY + 0.02f), 0.02f, new Vector2(cx - 0.2f, collarY - 0.04f), 0.004f));
                    }
                    break;
            }
            // head in profile: skull + nose + chin (facing +x)
            float hd = (q - head).magnitude - headR;
            hd = Mathf.Min(hd, RoundCone(q, head + new Vector2(headR * 0.7f, headR * 0.1f), headR * 0.25f, head + new Vector2(headR * 1.15f, -headR * 0.15f), headR * 0.06f));
            hd = Mathf.Min(hd, Ellipse(q, head + new Vector2(headR * 0.45f, -headR * 0.75f), new Vector2(headR * 0.45f, headR * 0.3f)));
            if (kind == "Elder") hd = Mathf.Min(hd, RoundCone(q, head + new Vector2(headR * 0.75f, -headR * 0.8f), headR * 0.6f, head + new Vector2(headR * 1.6f, -headR * 4.5f), 0.004f)); // beard
            black = Mathf.Min(black, hd);

            float edgeN = (Fbm(x * 70f, y * 70f, 2, 1101) - 0.5f) * 0.004f;
            float covBlack = 1f - Smooth(-0.002f, 0.002f, black + edgeN);
            float covRobe = 1f - Smooth(-0.002f, 0.002f, robe + edgeN);
            float cov = Mathf.Max(covBlack, covRobe);

            // robe ornament: obi band, collar line, and a hem band of cut-out cloud scallops (gold light)
            float tone = covBlack > 0.5f ? 1f : 0.28f + Fbm(x * 30f, y * 30f, 2, 1103) * 0.1f;
            float accent = 0f;
            if (covBlack < 0.5f && covRobe > 0.5f)
            {
                bool inner = robe < -0.012f;
                if (Mathf.Abs(y - obiY) < 0.025f) tone = 0.92f;                                                     // obi
                if (Mathf.Abs(y - obiY) < 0.008f && inner) accent = 1f;                                              // gold thread in the obi
                float collar = Mathf.Abs((y - collarY) + (x - cx - 0.02f) * 1.6f);
                if (collar < 0.012f && y > obiY && y < collarY + 0.02f) tone = 0.95f;                               // crossed collar
                float band = y - hemY;
                if (inner && band > 0.015f && band < 0.07f)
                {
                    tone = 0.92f;
                    float u = Frac(x * 28f);
                    float arc = (new Vector2(u - 0.5f, (band - 0.02f) * 28f)).magnitude;                              // scallop row
                    if (arc > 0.22f && arc < 0.38f) accent = 1f;
                }
                // cloth folds: a few fine lines fanning out toward the hem
                float spread = 1f + Mathf.Max(0f, obiY - y) * 2.5f;
                float fold = Mathf.Abs(Frac((x - cx) / (0.05f * spread) + 0.5f) - 0.5f) * 0.05f * spread;
                if (inner && fold < 0.0035f && band > 0.08f && Mathf.Abs(y - obiY) > 0.03f && y < collarY - 0.02f) tone = 0.6f;
            }
            float eye = (q - (head + new Vector2(headR * 0.45f, headR * 0.15f))).magnitude < headR * 0.15f ? 1f : 0f;
            return Px(cov * (1f - eye), tone, accent);
        }

        private static Color PuppetArmP(string kind, float x, float y)
        {
            // 128x512 -> aspect 0.25; pivot at top centre (0.125, 0.97). A wide kimono sleeve hanging from the
            // shoulder pin (dyed, gold cut-out cuff), the forearm and hand in black leather below it.
            var q = new Vector2(x, y);
            float cx = 0.125f;
            float len = kind == "Child" ? 0.85f : 1f;
            float top = 0.97f, cuffY = top - 0.5f * len, wrist = top - 0.86f * len;
            float w = kind == "Child" ? 0.05f : 0.056f;
            float sleeve = RoundCone(q, new Vector2(cx, top - 0.02f), w, new Vector2(cx - 0.01f, cuffY + 0.03f), w * 1.7f);
            sleeve = Mathf.Max(sleeve, cuffY - y);
            float arm = RoundCone(q, new Vector2(cx + 0.005f, cuffY + 0.04f), w * 0.36f, new Vector2(cx + 0.01f, wrist), w * 0.26f);
            arm = Mathf.Min(arm, Ellipse(q, new Vector2(cx + 0.012f, wrist - 0.04f), new Vector2(w * 0.42f, 0.045f)));       // hand
            arm = Mathf.Min(arm, RoundCone(q, new Vector2(cx + 0.02f, wrist - 0.02f), w * 0.15f, new Vector2(cx + 0.035f, wrist - 0.06f), w * 0.08f)); // thumb
            float n = (Fbm(x * 70f, y * 70f, 2, 1201) - 0.5f) * 0.003f;
            float covS = 1f - Smooth(-0.002f, 0.002f, sleeve + n);
            float covA = 1f - Smooth(-0.002f, 0.002f, arm + n);
            float cuff = covS > 0.5f && y - cuffY < 0.05f ? 1f : 0f;
            float tone = covS > 0.5f ? (cuff > 0f ? 0.92f : 0.3f) : 1f;
            float accent = cuff > 0f && sleeve < -0.008f && Frac(x * 40f) < 0.4f && y - cuffY > 0.012f && y - cuffY < 0.035f ? 1f : 0f;
            return Px(Mathf.Max(covS, covA), tone, accent);
        }

        // =====================================================================
        //  plumbing
        // =====================================================================

        private delegate Color Painter(float x, float y);

        private static void Paint(string name, int w, int h, Painter painter, bool wrap = false)
        {
            var px = new Color[w * h];
            Parallel.For(0, h, py =>
            {
                for (int x = 0; x < w; x++)
                    px[py * w + x] = painter((x + 0.5f) / h, (py + 0.5f) / h);
            });
            string path = PathOf(name);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = false;
            importer.wrapMode = wrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        public static Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(PathOf(name));

        private static Color Px(float cov, float tone, float accent)
        {
            cov = Mathf.Clamp01(cov);
            return new Color(cov, Mathf.Clamp01(tone), Mathf.Clamp01(accent), cov);
        }

        private static float Box(Vector2 p, Vector2 c, Vector2 half)
        {
            Vector2 d = new(Mathf.Abs(p.x - c.x) - half.x, Mathf.Abs(p.y - c.y) - half.y);
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

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

        private static float Smooth(float a, float b, float x)
        {
            if (Math.Abs(b - a) < 1e-7f) return x < a ? 0f : 1f;
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        /// <summary>Fade a wide band out at its left and right ends so its edges never show.</summary>
        private static float SideFade(float x, float aspect) => Smooth(0f, 0.18f * aspect, x) * Smooth(aspect, aspect * 0.82f, x);

        private static float Noise(float x, float y, int seed) => Fbm(x * 40f, y * 40f, 2, seed) - 0.5f;

        private static float Fbm(float x, float y, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += VN(x, y, seed + o * 31) * amp;
                norm += amp;
                x *= 2.03f; y *= 2.03f; amp *= 0.5f;
            }
            return sum / norm;
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
