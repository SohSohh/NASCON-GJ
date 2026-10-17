using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Cutscenes.Scroll
{
    /// <summary>
    /// One painted panel of the scroll (a set of ink layers + puppets around one place).
    /// Drives the panel's time of day: towards night every layer's wash and ink sink into blue-black,
    /// the paper darkens, and accents (lit windows, lanterns) glow. Also paints layers on/off with the brush reveal.
    /// </summary>
    public class InkPanel : MonoBehaviour
    {
        [SerializeField] private Renderer paper;
        [SerializeField] private Color nightTint = new(0.16f, 0.15f, 0.24f, 1f);
        [SerializeField] private float nightAccentGlow = 2.2f;

        private static readonly int LightId = Shader.PropertyToID("_LightColor");
        private static readonly int DarkId = Shader.PropertyToID("_DarkColor");
        private static readonly int AtmosColorId = Shader.PropertyToID("_AtmosColor");
        private static readonly int GlowId = Shader.PropertyToID("_AccentGlow");
        private static readonly int AccentId = Shader.PropertyToID("_AccentColor");
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int DarknessId = Shader.PropertyToID("_Darkness");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int LampId = Shader.PropertyToID("_Lamp");

        private class Layer { public Material mat; public Color light, dark, atmos; public Color accent; public float glow, opacity; public bool lamp; public Renderer r; }
        private readonly List<Layer> layers = new();
        private Material paperMat;
        private float night, lampsOut;

        public float Night => night;

        private void Awake()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == paper || r.GetComponentInParent<InkPuppet>() != null || r is ParticleSystemRenderer) continue;
                var m = r.material;
                if (!m.HasProperty(LightId)) continue;
                layers.Add(new Layer { mat = m, r = r, light = m.GetColor(LightId), dark = m.GetColor(DarkId), atmos = m.GetColor(AtmosColorId), glow = m.GetFloat(GlowId), accent = m.GetColor(AccentId),
                    opacity = m.GetFloat(OpacityId), lamp = m.HasProperty(LampId) && m.GetFloat(LampId) > 0.5f });
            }
            if (paper != null) paperMat = paper.material;
        }

        /// <summary>0 = the day palette as painted, 1 = moonless night.</summary>
        public void SetNight(float k)
        {
            night = k;
            foreach (var l in layers)
            {
                if (l.r.name == "NightMoon") { l.mat.SetFloat(OpacityId, l.opacity * k); continue; } // the moon keeps its own light
                l.mat.SetColor(LightId, Color.Lerp(l.light, l.light * nightTint * 1.6f, k));
                l.mat.SetColor(DarkId, Color.Lerp(l.dark, l.dark * 0.5f, k));
                l.mat.SetColor(AtmosColorId, Color.Lerp(l.atmos, l.atmos * nightTint, k));
                // only lit windows and lanterns glow in the dark; moss, gold leaf and seals just sink into the night
                l.mat.SetFloat(GlowId, l.lamp ? Mathf.Lerp(l.glow, nightAccentGlow, k) * (1f - lampsOut) : l.glow);
                if (!l.lamp) l.mat.SetColor(AccentId, Color.Lerp(l.accent, l.accent * nightTint * 1.4f, k));
                // the sun sets and the moon rises in the same sky
                if (l.r.name == "Sun") l.mat.SetFloat(OpacityId, l.opacity * (1f - k));
                else if (l.r.name == "NightMoon") l.mat.SetFloat(OpacityId, l.opacity * k);
            }
            if (paperMat != null) paperMat.SetFloat(DarknessId, k * 0.92f);
        }

        /// <summary>Lit windows and lanterns die (0..1).</summary>
        public void SetLampsOut(float k)
        {
            lampsOut = k;
            SetNight(night);
        }

        public IEnumerator Blend(float toNight, float duration)
        {
            float from = night;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                SetNight(Mathf.Lerp(from, toNight, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            SetNight(toNight);
        }

        /// <summary>Brush-paint every layer on, back to front, like an artist laying down the washes.</summary>
        public IEnumerator PaintOn(float duration)
        {
            var ordered = new List<Layer>(layers);
            ordered.Sort((a, b) => b.r.transform.position.z.CompareTo(a.r.transform.position.z));
            foreach (var l in ordered) l.mat.SetFloat(RevealId, 0f);
            float step = duration / Mathf.Max(1, ordered.Count);
            for (int i = 0; i < ordered.Count; i++)
            {
                StartCoroutine(RevealOne(ordered[i].mat, step * 2.5f));
                yield return new WaitForSeconds(step);
            }
        }

        private static IEnumerator RevealOne(Material m, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                m.SetFloat(RevealId, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            m.SetFloat(RevealId, 1f);
        }

        public void SetLayerOpacity(string childName, float a)
        {
            var t = transform.Find(childName);
            if (t == null) return;
            foreach (var r in t.GetComponentsInChildren<Renderer>()) if (r.material.HasProperty(OpacityId)) r.material.SetFloat(OpacityId, a);
        }
    }
}
