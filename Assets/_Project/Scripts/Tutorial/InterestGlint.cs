using NasconGJ.Items;
using UnityEngine;

namespace NasconGJ.Tutorial
{
    /// <summary>
    /// "Look here": the item's outline pulses in the guiding light's pale colour and a few motes rise off it
    /// (the dropped torch, the stick to light). Turn on/off from tutorial scripts.
    /// No light source on purpose: a point light this close to the floor blows out into a white disk.
    /// </summary>
    public class InterestGlint : MonoBehaviour
    {
        // the guiding light's colour, so it reads as "the guide points here", never as fire
        [SerializeField] private Color color = new(0.7f, 0.9f, 1f);
        [SerializeField] private float pulseSpeed = 2.4f;
        [SerializeField] private Material sparkMaterial;

        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");

        private ParticleSystem sparks;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private bool active;

        public void SetActive(bool on)
        {
            if (sparks == null) Build();
            active = on;
            if (on) sparks.Play();
            else
            {
                sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                var carry = GetComponent<CarryableItem>();
                if (carry != null) carry.RefreshHighlight();
            }
        }

        public void SetSparkMaterial(Material m) => sparkMaterial = m;

        private void Build()
        {
            renderers = GetComponentsInChildren<MeshRenderer>();
            block = new MaterialPropertyBlock();

            var go = new GameObject("Glint");
            go.transform.SetParent(transform, false);
            go.transform.position = transform.position + Vector3.up * 0.25f; // world up: items usually lie on their side
            sparks = go.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.startColor = color;
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = sparks.emission;
            em.rateOverTime = 7f;
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;
            var col = sparks.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (sparkMaterial != null) r.sharedMaterial = sparkMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            if (!active || renderers == null) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    r.GetPropertyBlock(block, i);
                    block.SetColor(RimColorId, color * (1.2f + pulse * 1.3f));
                    block.SetFloat(RimStrengthId, 0.6f + 0.4f * pulse);
                    r.SetPropertyBlock(block, i);
                }
            }
        }
    }
}
