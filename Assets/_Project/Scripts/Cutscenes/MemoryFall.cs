using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// The fall into the tomb, seen from inside the bearer's head: a hard black, then memories flash past like a
    /// chewed-up tape (torn rows, RGB split, roll, grain), faster and faster while they drift upward (we're falling),
    /// over a slightly distorted track. Ends on a white flash and black. Drive with <see cref="Play"/>.
    /// </summary>
    public class MemoryFall : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RawImage image;
        [SerializeField] private Image flash;
        [SerializeField] private Texture[] memories;
        [SerializeField] private AudioSource track;
        [SerializeField] private float trackStart;
        [SerializeField] private AudioSource staticSource;   // looped tape hiss under the flashes
        [SerializeField] private AudioSource voice;          // the scream, on its own clean-ish source
        [SerializeField] private AudioClip screamClip;
        [SerializeField] private float duration = 5.5f;
        [Range(0f, 1f)] [SerializeField] private float trackVolume = 0.9f;
        [Tooltip("Colour of the flash frames that cut in between memories.")]
        [SerializeField] private Color flashTint = new(0.55f, 0.02f, 0.02f, 1f);

        private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
        private static readonly int SplitId = Shader.PropertyToID("_Split");
        private static readonly int RollId = Shader.PropertyToID("_Roll");
        private Material mat;
        private readonly List<Texture> snaps = new();

        /// <summary>Grab what the camera sees right now as a memory (taken during the cutscene, so it's lit exactly as played).</summary>
        public void Snap(Camera cam)
        {
            if (cam == null) return;
            var rt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            snaps.Add(rt);
        }

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
            if (image != null) { mat = new Material(image.material); image.material = mat; }
        }

        public IEnumerator Play()
        {
            var pool = new List<Texture>(snaps);
            if (memories != null) pool.AddRange(memories);
            if (pool.Count == 0 || image == null) yield break;
            group.alpha = 1f;
            image.enabled = false;
            flash.color = new Color(1f, 1f, 1f, 0f);
            if (track != null) { track.time = Mathf.Min(trackStart, track.clip.length - 0.1f); track.volume = trackVolume; track.Play(); }
            if (staticSource != null) { staticSource.volume = 0.6f; staticSource.Play(); }

            var rt = image.rectTransform;
            float t = 0f;
            int last = -1;
            while (t < duration)
            {
                float k = t / duration;
                float hold = Mathf.Lerp(0.42f, 0.05f, k * k);                         // faster and faster
                if (Random.value < 0.18f + 0.2f * k)                                   // dropouts: black, or a blood-red frame
                {
                    image.enabled = false;
                    bool red = Random.value < 0.45f + 0.3f * k;
                    if (red) flash.color = flashTint;
                    yield return new WaitForSeconds(red ? 0.06f : 0.04f);
                    flash.color = new Color(1f, 1f, 1f, 0f);
                    t += 0.05f;
                }
                int pick = Random.Range(0, pool.Count);
                if (pick == last && pool.Count > 1) pick = (pick + 1) % pool.Count;
                last = pick;
                image.texture = pool[pick];
                image.enabled = true;
                float scale = Random.Range(1.02f, 1.25f + k * 0.3f);
                float rot = Random.Range(-3f, 3f) * (0.5f + k);
                Vector2 start = new(Random.Range(-20f, 20f), Random.Range(-60f, -20f));
                mat.SetFloat(GlitchId, Mathf.Lerp(0.15f, 0.7f, k) * Random.Range(0.6f, 1.1f));
                mat.SetFloat(SplitId, Mathf.Lerp(0.003f, 0.012f, k));
                for (float f = 0f; f < hold; f += Time.deltaTime)
                {
                    float u = f / hold;
                    rt.anchoredPosition = start + Vector2.up * (140f + 300f * k) * u; // the memory rises past us as we fall
                    rt.localScale = Vector3.one * (scale + u * 0.06f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, rot);
                    mat.SetFloat(RollId, Random.value < 0.08f ? Random.value : 0f);
                    if (track != null) track.pitch = 0.97f + Mathf.Sin(Time.time * 7f) * 0.02f; // tape wow
                    yield return null;
                }
                t += hold;
            }
            // a last red flare with a scream, then the dark at the bottom
            image.enabled = false;
            if (voice != null && screamClip != null) voice.PlayOneShot(screamClip, 1f);
            if (staticSource != null) StartCoroutine(FadeOut(staticSource, 0.8f));
            for (float f = 0f; f < 0.1f; f += Time.deltaTime) { flash.color = Color.Lerp(new Color(1f, 0.3f, 0.25f, 0f), new Color(1f, 0.3f, 0.25f, 1f), f / 0.1f); yield return null; }
            for (float f = 0f; f < 0.6f; f += Time.deltaTime) { flash.color = Color.Lerp(new Color(1f, 0.3f, 0.25f, 1f), new Color(0.2f, 0f, 0f, 0f), f / 0.6f); yield return null; }
            flash.color = new Color(1f, 1f, 1f, 0f);
            if (track != null) StartCoroutine(FadeOut(track, 1.2f));
        }

        private static IEnumerator FadeOut(AudioSource s, float d)
        {
            float from = s.volume;
            for (float t = 0f; t < d; t += Time.deltaTime) { s.volume = Mathf.Lerp(from, 0f, t / d); yield return null; }
            s.Stop();
        }
    }
}
