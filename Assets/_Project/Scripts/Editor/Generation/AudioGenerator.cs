using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Synthesises every placeholder sound in code (no external audio): tomb drone, wind, drips, stone footsteps,
    /// fire crackle, ignition, shadow whispers, heartbeat, lunge screech, UI ticks, guiding-light chime, menu drone.
    /// Writes 16-bit mono WAVs to Audio/Generated. Swap any file for a real recording with the same name later.
    /// </summary>
    public static class AudioGenerator
    {
        public const string Dir = GenUtil.Root + "/Audio/Generated";
        private const int Rate = 44100;

        public static string PathOf(string name) => $"{Dir}/{name}.wav";
        public static AudioClip Load(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(PathOf(name));

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(Dir);
            var rng = new System.Random(4242);

            Write("AMB_TombDrone", Loop(Drone(rng, 24f, 41f, 0.35f), 2f));
            Write("AMB_Wind", Loop(Wind(rng, 12f), 1.5f));
            Write("MUS_MenuDrone", Loop(MenuDrone(rng, 30f), 3f));
            for (int i = 0; i < 3; i++) Write($"SFX_Drip_{i + 1}", Drip(rng, 1500f + i * 350f));
            for (int i = 0; i < 4; i++) Write($"SFX_Step_{i + 1}", Footstep(rng));
            Write("SFX_FireCrackle", Loop(Crackle(rng, 8f), 0.5f));
            Write("SFX_Ignite", Ignite(rng));
            Write("SFX_ShadowWhisper", Loop(Whisper(rng, 10f), 1f));
            Write("SFX_Heartbeat", Heartbeat());
            Write("SFX_Lunge", Lunge(rng));
            Write("SFX_UI_Hover", Tick(rng));
            Write("SFX_UI_Select", Boom(rng));
            Write("SFX_GuideChime", Chime());
            Write("SFX_Thunder", Thunder(rng));
            Write("SFX_Collapse", Collapse(rng));
            Write("SFX_Impact", Impact(rng));
            AssetDatabase.Refresh();
            ConfigureImports();
        }

        // ------------------------------------------------------------ voices

        private static float[] Drone(System.Random rng, float seconds, float fundamental, float noiseAmount)
        {
            int n = (int)(seconds * Rate);
            var o = new float[n];
            float brown = 0f;
            var lp = new OnePole(180f);
            var band = new Biquad(420f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                brown = Mathf.Clamp(brown + (Rand(rng) * 0.02f), -1f, 1f) * 0.998f;
                float rumble = lp.Process(brown) * noiseAmount * 3f;
                float wind = band.Process(Rand(rng)) * 0.12f * (0.6f + 0.4f * Mathf.Sin(t * 0.31f));
                float tone = Mathf.Sin(2f * Mathf.PI * fundamental * t) * 0.12f * (1f + 0.3f * Mathf.Sin(t * 0.44f)) +
                             Mathf.Sin(2f * Mathf.PI * fundamental * 1.5f * t + Mathf.Sin(t * 0.2f)) * 0.05f;
                o[i] = rumble + wind + tone;
            }
            return Normalize(o, 0.6f);
        }

        private static float[] MenuDrone(System.Random rng, float seconds)
        {
            int n = (int)(seconds * Rate);
            var o = new float[n];
            var band = new Biquad(600f, 0.5f);
            float[] notes = { 55f, 82.4f, 110f, 164.8f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float trem = 0.5f + 0.5f * Mathf.Sin(t * (0.11f + k * 0.07f) * Mathf.PI * 2f + k);
                    s += Mathf.Sin(2f * Mathf.PI * notes[k] * t + 0.3f * Mathf.Sin(t * 0.5f + k)) * trem / (k + 1.5f);
                }
                s += band.Process(Rand(rng)) * 0.08f * (0.5f + 0.5f * Mathf.Sin(t * 0.17f));
                o[i] = s;
            }
            return Normalize(o, 0.45f);
        }

        private static float[] Wind(System.Random rng, float seconds)
        {
            int n = (int)(seconds * Rate);
            var o = new float[n];
            var a = new Biquad(500f, 1.6f);
            var b = new Biquad(900f, 2.5f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float gust = 0.35f + 0.65f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * 0.9f + Mathf.Sin(t * 0.37f) * 2f), 2f);
                a.SetFreq(380f + 260f * Mathf.Sin(t * 0.6f));
                b.SetFreq(820f + 300f * Mathf.Sin(t * 0.45f + 1f));
                float x = Rand(rng);
                o[i] = (a.Process(x) * 0.7f + b.Process(x) * 0.35f) * gust;
            }
            return Normalize(o, 0.6f);
        }

        /// <summary>A crack, then a long rolling rumble.</summary>
        private static float[] Thunder(System.Random rng)
        {
            int n = (int)(6f * Rate);
            var o = new float[n];
            var lp = new OnePole(220f);
            var lp2 = new OnePole(90f);
            var crackLp = new OnePole(2600f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float x = Rand(rng);
                float crack = crackLp.Process(x) * Mathf.Exp(-t * 9f) * (t < 0.02f ? t / 0.02f : 1f);
                float roll = 0.55f + 0.45f * Mathf.Sin(t * 5.3f + Mathf.Sin(t * 1.7f) * 2f);
                float rumbleEnv = Mathf.Min(1f, t * 3f) * Mathf.Exp(-t * 0.65f) * roll;
                o[i] = crack * 0.8f + lp.Process(x) * 2.2f * rumbleEnv + lp2.Process(x) * 3.5f * rumbleEnv;
            }
            return Normalize(o, 0.9f);
        }

        /// <summary>Stone giving way: a deep groan, cracks, then falling debris.</summary>
        private static float[] Collapse(System.Random rng)
        {
            int n = (int)(4.5f * Rate);
            var o = new float[n];
            var lp = new OnePole(120f);
            var mid = new Biquad(700f, 1.2f);
            float[] cracks = { 0.35f, 0.9f, 1.25f, 1.4f, 1.9f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float x = Rand(rng);
                float groan = lp.Process(x) * 3f * Mathf.Min(1f, t * 1.5f) * Mathf.Exp(-Mathf.Max(0f, t - 2.2f) * 1.5f);
                float c = 0f;
                foreach (float ct in cracks)
                    if (t >= ct) c += Mathf.Exp(-(t - ct) * 40f);
                float debris = t > 1.4f ? mid.Process(x) * 0.5f * (Rand(rng) > 0.6f ? 1f : 0.2f) * Mathf.Exp(-(t - 1.4f) * 1.1f) : 0f;
                o[i] = groan + x * c * 0.9f + debris;
            }
            return Normalize(o, 0.85f);
        }

        /// <summary>A heavy body hitting stone far below.</summary>
        private static float[] Impact(System.Random rng)
        {
            int n = (int)(1.6f * Rate);
            var o = new float[n];
            var lp = new OnePole(400f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float thump = Mathf.Sin(2f * Mathf.PI * (55f - t * 12f) * t) * Mathf.Exp(-t * 6f);
                float grit = lp.Process(Rand(rng)) * Mathf.Exp(-t * 14f) * 1.4f;
                o[i] = thump + grit;
            }
            return Normalize(o, 0.9f);
        }

        private static float[] Drip(System.Random rng, float freq)
        {
            int n = (int)(0.9f * Rate);
            var o = new float[n];
            foreach (var (delay, gain) in new[] { (0f, 1f), (0.13f, 0.35f), (0.29f, 0.15f) })
            {
                int start = (int)(delay * Rate);
                for (int i = 0; i + start < n && i < Rate / 4; i++)
                {
                    float t = i / (float)Rate;
                    float f = freq * (1f - Mathf.Min(t * 14f, 0.45f));
                    o[i + start] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 45f) * gain;
                }
            }
            return Normalize(o, 0.5f);
        }

        private static float[] Footstep(System.Random rng)
        {
            int n = (int)(0.28f * Rate);
            var o = new float[n];
            var lp = new OnePole(900f + (float)rng.NextDouble() * 700f);
            float thump = 70f + (float)rng.NextDouble() * 40f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * 32f);
                float grit = lp.Process(Rand(rng)) * env;
                float low = Mathf.Sin(2f * Mathf.PI * thump * t) * Mathf.Exp(-t * 45f) * 0.6f;
                float scuff = t > 0.05f ? Rand(rng) * Mathf.Exp(-(t - 0.05f) * 60f) * 0.15f : 0f;
                o[i] = grit + low + scuff;
            }
            return Normalize(o, 0.7f);
        }

        private static float[] Crackle(System.Random rng, float seconds)
        {
            int n = (int)(seconds * Rate);
            var o = new float[n];
            var lp = new OnePole(600f);
            for (int i = 0; i < n; i++) o[i] = lp.Process(Rand(rng)) * 0.5f;
            int pops = (int)(seconds * 22f);
            for (int p = 0; p < pops; p++)
            {
                int start = rng.Next(n);
                float amp = 0.3f + (float)rng.NextDouble() * 0.9f;
                int len = (int)((0.002f + rng.NextDouble() * 0.012f) * Rate);
                var hp = new OnePole(3000f);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float x = Rand(rng);
                    o[start + i] += (x - hp.Process(x)) * amp * Mathf.Exp(-i / (float)len * 5f);
                }
            }
            return Normalize(o, 0.55f);
        }

        private static float[] Ignite(System.Random rng)
        {
            int n = (int)(1.2f * Rate);
            var o = new float[n];
            var lp = new OnePole(200f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                lp.SetFreq(200f + 2400f * Mathf.Min(1f, t * 3f));
                float env = Mathf.Min(1f, t * 8f) * Mathf.Exp(-t * 3.5f);
                o[i] = lp.Process(Rand(rng)) * env + Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 8f) * 0.5f;
            }
            return Normalize(o, 0.6f);
        }

        private static float[] Whisper(System.Random rng, float seconds)
        {
            int n = (int)(seconds * Rate);
            var o = new float[n];
            var f1 = new Biquad(700f, 4f);
            var f2 = new Biquad(1250f, 5f);
            var f3 = new Biquad(2600f, 6f);
            var breath = new OnePole(400f);
            float syllable = 0f, target = 0f;
            int next = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                if (i >= next)
                {
                    target = rng.NextDouble() < 0.65 ? 0.4f + (float)rng.NextDouble() * 0.6f : 0f;
                    next = i + (int)((0.08f + rng.NextDouble() * 0.22f) * Rate);
                    f1.SetFreq(550f + (float)rng.NextDouble() * 400f);
                    f2.SetFreq(1000f + (float)rng.NextDouble() * 700f);
                }
                syllable += (target - syllable) * 0.0025f;
                float x = Rand(rng);
                float voice = (f1.Process(x) + f2.Process(x) * 0.7f + f3.Process(x) * 0.4f) * syllable;
                float b = breath.Process(Rand(rng)) * 0.25f * (0.5f + 0.5f * Mathf.Sin(t * 0.8f));
                o[i] = voice + b;
            }
            return Normalize(o, 0.5f);
        }

        private static float[] Heartbeat()
        {
            int n = (int)(1.0f * Rate);
            var o = new float[n];
            foreach (var (delay, gain) in new[] { (0f, 1f), (0.26f, 0.7f) })
            {
                int start = (int)(delay * Rate);
                for (int i = 0; i + start < n && i < Rate / 3; i++)
                {
                    float t = i / (float)Rate;
                    o[i + start] += (Mathf.Sin(2f * Mathf.PI * 52f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 78f * t)) * Mathf.Exp(-t * 18f) * gain * Mathf.Min(1f, t * 300f);
                }
            }
            return Normalize(o, 0.8f);
        }

        private static float[] Lunge(System.Random rng)
        {
            int n = (int)(1.3f * Rate);
            var o = new float[n];
            var band = new Biquad(3000f, 2f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                band.SetFreq(Mathf.Lerp(3200f, 380f, Mathf.Min(1f, t * 1.4f)));
                float env = Mathf.Min(1f, t * 60f) * Mathf.Exp(-t * 2.4f);
                float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 140f, Mathf.Min(1f, t * 1.2f)) * t);
                float s = band.Process(Rand(rng)) * 1.4f + tone * 0.5f;
                o[i] = (float)Math.Tanh(s * 2.2f) * env;
            }
            return Normalize(o, 0.9f);
        }

        private static float[] Tick(System.Random rng)
        {
            int n = (int)(0.12f * Rate);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                o[i] = (Mathf.Sin(2f * Mathf.PI * 1250f * t) * 0.6f + Rand(rng) * 0.15f) * Mathf.Exp(-t * 70f);
            }
            return Normalize(o, 0.35f);
        }

        private static float[] Boom(System.Random rng)
        {
            int n = (int)(1.8f * Rate);
            var o = new float[n];
            var lp = new OnePole(300f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float swell = lp.Process(Rand(rng)) * Mathf.Min(1f, t * 3f) * Mathf.Exp(-t * 2.2f);
                float low = Mathf.Sin(2f * Mathf.PI * 46f * t) * Mathf.Exp(-t * 2.8f) * Mathf.Min(1f, t * 80f);
                o[i] = swell * 0.8f + low;
            }
            return Normalize(o, 0.75f);
        }

        private static float[] Chime()
        {
            int n = (int)(2.6f * Rate);
            var o = new float[n];
            float[] partials = { 880f, 1324f, 2217f, 2960f };
            float[] decay = { 1.6f, 2.4f, 3.5f, 5f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 0; k < partials.Length; k++)
                    s += Mathf.Sin(2f * Mathf.PI * partials[k] * t + Mathf.Sin(t * 6f) * 0.05f) * Mathf.Exp(-t * decay[k]) / (k + 1f);
                o[i] = s * Mathf.Min(1f, t * 200f);
            }
            return Normalize(o, 0.45f);
        }

        // ------------------------------------------------------------ helpers

        private static float Rand(System.Random rng) => (float)rng.NextDouble() * 2f - 1f;

        private static float[] Normalize(float[] s, float peak)
        {
            float max = 1e-6f;
            foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < s.Length; i++) s[i] = s[i] / max * peak;
            return s;
        }

        /// <summary>Crossfades the tail into the head so the clip loops seamlessly.</summary>
        private static float[] Loop(float[] s, float fadeSeconds)
        {
            int f = Mathf.Min((int)(fadeSeconds * Rate), s.Length / 3);
            int n = s.Length - f;
            var o = new float[n];
            Array.Copy(s, o, n);
            for (int i = 0; i < f; i++)
            {
                float k = i / (float)f;
                o[i] = o[i] * Mathf.Sqrt(k) + s[n + i] * Mathf.Sqrt(1f - k);
            }
            return o;
        }

        private static void Write(string name, float[] samples)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PathOf(name)));
            using var fs = new FileStream(full, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int dataBytes = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataBytes);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(Rate);
            w.Write(Rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataBytes);
            foreach (float v in samples) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
        }

        private static void ConfigureImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool longClip = path.Contains("AMB_") || path.Contains("MUS_");
                var settings = importer.defaultSampleSettings;
                settings.loadType = longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        private class OnePole
        {
            private float a, y;
            public OnePole(float cutoff) => SetFreq(cutoff);
            public void SetFreq(float cutoff) => a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
            public float Process(float x) => y += a * (x - y);
        }

        /// <summary>RBJ band-pass biquad.</summary>
        private class Biquad
        {
            private float b0, b1, b2, a1, a2, x1, x2, y1, y2;
            private readonly float q;
            public Biquad(float freq, float q) { this.q = q; SetFreq(freq); }

            public void SetFreq(float freq)
            {
                float w = 2f * Mathf.PI * freq / Rate;
                float alpha = Mathf.Sin(w) / (2f * q);
                float a0 = 1f + alpha;
                b0 = alpha / a0;
                b1 = 0f;
                b2 = -alpha / a0;
                a1 = -2f * Mathf.Cos(w) / a0;
                a2 = (1f - alpha) / a0;
            }

            public float Process(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }
    }
}
