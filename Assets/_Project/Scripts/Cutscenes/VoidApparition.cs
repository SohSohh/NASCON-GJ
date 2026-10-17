using System.Collections;
using NasconGJ.Entities;
using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// The Void in the prologue: a colossal <see cref="VineCreature"/> (gaunt figure of black thorny vines, burning heart)
    /// hanging in the sky. It rises from behind the town, slowly spreads its arms, twitches its head, and calls down
    /// jagged lightning around itself (thunderbolt beats for the director).
    /// </summary>
    public class VoidApparition : MonoBehaviour
    {
        [SerializeField] private Material vineMaterial;
        [SerializeField] private Material heartMaterial;
        [SerializeField] private Material boltMaterial;
        [SerializeField] private float height = 44f;
        [SerializeField] private float hiddenDrop = 50f;
        [Tooltip("Child at head height, for townsfolk to look up at.")]
        [SerializeField] private Transform head;
        [SerializeField] private Light boltLight;

        private VineCreature creature;
        private Vector3 shown;
        private float rise, time;
        private LineRenderer[] bolts;

        public Transform Head => head;

        private void Awake()
        {
            shown = transform.position;
            var go = new GameObject("Body");
            go.transform.SetParent(transform, false);
            creature = go.AddComponent<VineCreature>();
            creature.Configure(height, vineMaterial, heartMaterial, 31, 2.4f); // thick enough to survive the painterly brush filter at 130 m
            creature.Build();
            creature.Float = 1f;
            creature.Twitch = 0.35f;
            creature.SetGlow(1.2f, 0f);

            bolts = new LineRenderer[4];
            for (int i = 0; i < bolts.Length; i++)
            {
                var b = new GameObject("Bolt_" + i).AddComponent<LineRenderer>();
                b.transform.SetParent(transform, false);
                b.useWorldSpace = true;
                b.sharedMaterial = boltMaterial;
                b.widthMultiplier = 0.6f;
                b.numCapVertices = 2;
                b.enabled = false;
                bolts[i] = b;
            }
            if (boltLight != null) boltLight.intensity = 0f;
            Apply();
        }

        public void Rise(float duration) => StartCoroutine(RiseRoutine(duration));

        /// <summary>Slowly raise the arms out to the sides (0..1).</summary>
        public void SpreadArms(float amount) { if (creature != null) creature.Spread = amount; }

        /// <summary>The heart and rim flare (when it takes the townsfolk).</summary>
        public void Flare() => StartCoroutine(FlareRoutine());

        /// <summary>Jagged lightning cracks down around it for a moment.</summary>
        public void Strike() => StartCoroutine(StrikeRoutine());

        private IEnumerator RiseRoutine(float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                rise = 1f - (1f - k) * (1f - k) * (1f - k);
                yield return null;
            }
            rise = 1f;
        }

        private IEnumerator FlareRoutine()
        {
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                creature.SetGlow(1.2f + Mathf.Sin(t / 2f * Mathf.PI) * 3f, 0f);
                yield return null;
            }
            creature.SetGlow(1.2f, 0f);
        }

        private IEnumerator StrikeRoutine()
        {
            float[] pulses = { 1f, 0.2f, 0.9f, 0.35f, 0f };
            Vector3 body = shown + Vector3.up * height * 0.5f;
            foreach (var b in bolts)
            {
                b.enabled = true;
                Vector3 from = body + new Vector3(Random.Range(-1f, 1f) * height * 0.6f, height * Random.Range(0.4f, 0.9f), Random.Range(-1f, 1f) * height * 0.2f);
                Vector3 to = new Vector3(from.x + Random.Range(-1f, 1f) * height * 0.4f, 0f, from.z + Random.Range(-1f, 1f) * height * 0.3f);
                const int n = 14;
                b.positionCount = n;
                for (int i = 0; i < n; i++)
                {
                    float k = i / (n - 1f);
                    Vector3 p = Vector3.Lerp(from, to, k);
                    if (i > 0 && i < n - 1) p += new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), Random.Range(-1f, 1f)) * height * 0.06f;
                    b.SetPosition(i, p);
                }
                b.widthMultiplier = height * Random.Range(0.012f, 0.025f);
            }
            foreach (float p in pulses)
            {
                foreach (var b in bolts) b.enabled = p > 0.15f;
                if (boltLight != null) boltLight.intensity = p * 8f;
                creature.SetGlow(1.2f + p * 2f, 0f);
                yield return new WaitForSeconds(0.07f);
            }
            foreach (var b in bolts) b.enabled = false;
            if (boltLight != null) boltLight.intensity = 0f;
            creature.SetGlow(1.2f, 0f);
        }

        private void Update()
        {
            time += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            float bob = Mathf.Sin(time * 0.4f) * height * 0.01f;
            transform.position = shown + Vector3.down * (hiddenDrop * (1f - rise)) + Vector3.up * bob;
            if (head != null && creature != null) head.position = transform.position + Vector3.up * height * 0.85f;
        }
    }
}
