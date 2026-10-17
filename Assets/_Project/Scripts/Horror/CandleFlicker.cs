using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>A candle flame (with or without its own light): noisy flicker, and can be snuffed (light + flame mesh off).</summary>
    public class CandleFlicker : MonoBehaviour
    {
        [SerializeField] private Renderer[] flames;
        [SerializeField] private float amount = 0.35f;
        [SerializeField] private float speed = 9f;

        private Light lamp;
        private float baseIntensity, seed;
        public bool Lit { get; private set; } = true;

        private void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp != null ? lamp.intensity : 0f;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            if (!Lit || lamp == null) return;
            float n = Mathf.PerlinNoise(seed, Time.time * speed);
            lamp.intensity = baseIntensity * (1f - amount + amount * 2f * n);
        }

        public void Snuff(bool snuffed)
        {
            Lit = !snuffed;
            if (lamp != null) lamp.enabled = Lit;
            if (flames != null) foreach (var f in flames) if (f != null) f.enabled = Lit;
        }
    }
}
