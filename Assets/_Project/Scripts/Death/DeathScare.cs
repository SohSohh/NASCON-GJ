using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Death
{
    /// <summary>Screen-space death sequence; instantiated from Resources/DeathScare.</summary>
    public sealed class DeathScare : MonoBehaviour
    {
        [SerializeField] private Texture2D closedFace;
        [SerializeField] private Texture2D openFace;
        [SerializeField] private AudioClip jumpscare;
        [SerializeField, Min(0f)] private float stareDuration = 3f;
        [SerializeField, Min(0.01f)] private float flickerInterval = 0.03f;
        [SerializeField, Min(2)] private int flickerCount = 6;
        [SerializeField, Min(0.01f)] private float rushDuration = 0.55f;
        [SerializeField, Range(0.1f, 0.9f)] private float initialHeight = 0.28f;
        [SerializeField, Min(0f)] private float stareShake = 1.5f;
        [SerializeField, Min(0f)] private float rushShake = 5f;
        [SerializeField, Min(0f)] private float rushRotationShake = 12f;
        [SerializeField, Range(0f, 1f)] private float orangeFlickerAlpha = 0.2f;
        [SerializeField, Min(0f)] private float faceFadeDuration = 0.02f;
        [SerializeField, Min(0f)] private float blackHold = 0.5f;
        [SerializeField] private AudioClip impactHit;
        [Header("Impact frames (versions of the open face)")]
        [SerializeField] private Texture2D impactNegative;
        [SerializeField] private Texture2D impactRed;
        [SerializeField] private Texture2D impactHarsh;

        public string Phase { get; private set; }
        public bool IsConfigured => closedFace != null && openFace != null && jumpscare != null;
        private UnityEngine.UI.RawImage face;
        private UnityEngine.UI.RawImage vignette;
        private UnityEngine.UI.RawImage ghostRed, ghostCyan;
        private UnityEngine.UI.Image orangeFlash;
        private UnityEngine.UI.Image blackBackdrop;
        private RectTransform visuals;
        private RectTransform canvasRect;
        private AudioSource voice;
        private Texture2D vignetteTexture;
        private Texture2D featherTexture;
        private readonly List<(AudioSource source, bool muted)> mutedSources = new();

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            canvasRect = (RectTransform)transform;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var visualRoot = new GameObject("ScareVisuals", typeof(RectTransform));
            visualRoot.transform.SetParent(transform, false);
            visuals = (RectTransform)visualRoot.transform;
            visuals.anchorMin = Vector2.zero;
            visuals.anchorMax = Vector2.one;
            visuals.offsetMin = visuals.offsetMax = Vector2.zero;
            var backdrop = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            backdrop.transform.SetParent(visuals, false);
            var rect = (RectTransform)backdrop.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            blackBackdrop = backdrop.GetComponent<UnityEngine.UI.Image>();
            blackBackdrop.color = Color.black;
            var vignetteObject = new GameObject("Vignette", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            vignetteObject.transform.SetParent(visuals, false);
            var vignetteRect = (RectTransform)vignetteObject.transform;
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.offsetMin = vignetteRect.offsetMax = Vector2.zero;
            vignette = vignetteObject.GetComponent<UnityEngine.UI.RawImage>();
            vignette.raycastTarget = false;
            vignette.color = new Color(0f, 0f, 0f, 0.8f);
            vignetteTexture = CreateVignetteTexture();
            vignette.texture = vignetteTexture;
            var portrait = new GameObject("Face", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            portrait.transform.SetParent(visuals, false);
            face = portrait.GetComponent<UnityEngine.UI.RawImage>();
            face.raycastTarget = false;
            face.texture = closedFace;
            face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            // feathered edges: a soft black frame over the face (it rides on the face, so it grows with it)
            var featherObject = new GameObject("Feather", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            featherObject.transform.SetParent(portrait.transform, false);
            var featherRect = (RectTransform)featherObject.transform;
            featherRect.anchorMin = Vector2.zero;
            featherRect.anchorMax = Vector2.one;
            featherRect.offsetMin = featherRect.offsetMax = Vector2.zero;
            var feather = featherObject.GetComponent<UnityEngine.UI.RawImage>();
            feather.raycastTarget = false;
            feather.color = Color.black;
            featherTexture = CreateFeatherTexture();
            feather.texture = featherTexture;
            // colour-split ghosts behind the feather: the image tears into red and cyan on every hit
            ghostRed = Ghost("GhostRed", new Color(1f, 0.1f, 0.05f, 0f));
            ghostCyan = Ghost("GhostCyan", new Color(0.1f, 0.9f, 1f, 0f));
            var flashObject = new GameObject("OrangeFlash", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            flashObject.transform.SetParent(transform, false);
            var flashRect = (RectTransform)flashObject.transform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = flashRect.offsetMax = Vector2.zero;
            orangeFlash = flashObject.GetComponent<UnityEngine.UI.Image>();
            orangeFlash.color = new Color(1f, 0.22f, 0f, 0f);
            orangeFlash.raycastTarget = false;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            voice.ignoreListenerPause = true;
            voice.clip = jumpscare;
            voice.volume = 1f;
            jumpscare?.LoadAudioData();
        }

        public IEnumerator Play()
        {
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source == voice) continue;
                mutedSources.Add((source, source.mute));
                source.mute = true;
            }
            Canvas.ForceUpdateCanvases();
            float aspect = (float)closedFace.width / closedFace.height;
            float height = Mathf.Min(canvasRect.rect.height * initialHeight, canvasRect.rect.width * 0.8f / aspect);
            Vector2 initialSize = new Vector2(height * aspect, height);
            face.rectTransform.sizeDelta = initialSize;
            face.texture = closedFace;
            float stareElapsed = 0f;
            Phase = "Stare";
            while (stareElapsed < stareDuration)
            {
                stareElapsed += Time.unscaledDeltaTime;
                ApplyShake(stareShake);
                yield return null;
            }
            ResetShake();

            Phase = "Flicker";
            for (int i = 0; i < flickerCount; i++)
            {
                face.texture = i % 2 == 0 ? openFace : closedFace;
                orangeFlash.color = new Color(1f, 0.22f, 0f, i % 2 == 0 ? orangeFlickerAlpha : orangeFlickerAlpha * 0.2f);
                float flickerElapsed = 0f;
                while (flickerElapsed < flickerInterval)
                {
                    flickerElapsed += Time.unscaledDeltaTime;
                    ApplyShake(rushShake * 0.35f);
                    yield return null;
                }
            }

            face.texture = openFace;
            orangeFlash.color = new Color(1f, 0.22f, 0f, orangeFlickerAlpha * 0.35f);
            Phase = "Rush";
            // Audio and the first jump in size happen in the same frame.
            voice.Play();
            float soundStarted = Time.unscaledTime;
            float endHeight = Mathf.Max(canvasRect.rect.height, canvasRect.rect.width / aspect) * 1.3f;
            Vector2 endSize = new Vector2(endHeight * aspect, endHeight);
            // stop-motion: it comes at you in hard cuts, not a smooth zoom
            float[] steps = { 0.1f, 0.24f, 0.45f, 0.72f, 1f };
            float[] holds = { 0.24f, 0.2f, 0.2f, 0.18f, 0.18f };
            for (int i = 0; i < steps.Length; i++)
            {
                face.rectTransform.sizeDelta = Vector2.Lerp(initialSize, endSize, steps[i]);
                float hold = holds[i] * rushDuration, held = 0f;
                bool firstFrame = true;
                while (held < hold)
                {
                    held += Time.unscaledDeltaTime;
                    float kick = firstFrame ? 2.2f : 0.6f;
                    ApplyShake(rushShake * kick, rushRotationShake * kick);
                    Ghosts(firstFrame ? 22f : 8f, firstFrame ? 0.3f : 0.12f);
                    orangeFlash.color = new Color(1f, 0.22f, 0f, firstFrame ? orangeFlickerAlpha * 0.9f : orangeFlickerAlpha * 0.2f);
                    firstFrame = false;
                    yield return null;
                }
            }
            face.rectTransform.sizeDelta = endSize;
            ResetShake();

            Phase = "Impact";
            // hit-stop: the frame freezes on the hit, and the scream freezes with it
            Ghosts(0f, 0f);
            orangeFlash.color = Color.clear;
            voice.Pause();
            if (impactHit != null) voice.PlayOneShot(impactHit, 1f);
            yield return new WaitForSecondsRealtime(0.09f);
            voice.UnPause();
            Color bg = blackBackdrop.color;
            // impact frames, each a hard cut with its own kick; the scream stutters through them
            yield return ImpactFrame(Color.black, impactNegative, 1.04f, new Vector2(-24f, 10f), 9f, 0.055f, true);
            yield return ImpactFrame(Color.black, impactRed, 1.14f, new Vector2(30f, -14f), -7f, 0.05f, false);
            if (impactHit != null) voice.PlayOneShot(impactHit, 0.8f);
            yield return ImpactFrame(Color.black, impactHarsh, 1.22f, new Vector2(-12f, -26f), 5f, 0.055f, true);
            yield return ImpactFrame(Color.black, impactNegative, 1.5f, new Vector2(40f, 30f), -11f, 0.04f, false);
            yield return ImpactFrame(Color.white, null, 1.5f, Vector2.zero, 0f, 0.035f, true, hideFace: true);
            yield return ImpactFrame(Color.black, impactRed, 1.6f, new Vector2(-30f, 18f), 6f, 0.045f, false);
            // the after-shudder
            blackBackdrop.color = bg;
            face.texture = openFace;
            face.color = Color.white;
            for (int i = 0; i < 4; i++)
            {
                face.rectTransform.sizeDelta = endSize * (1.05f + i * 0.03f);
                face.rectTransform.anchoredPosition = Random.insideUnitCircle * (40f - i * 8f);
                face.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-6f, 6f));
                Ghosts(16f - i * 3f, 0.22f);
                yield return new WaitForSecondsRealtime(0.035f);
            }
            Ghosts(0f, 0f);
            ResetShake();
            face.enabled = false;
            orangeFlash.color = Color.clear;
            Phase = "Black";
            // Never reload halfway through the supplied sound.
            float soundRemaining = Mathf.Max(0f, jumpscare.length - (Time.unscaledTime - soundStarted));
            yield return new WaitForSecondsRealtime(Mathf.Max(blackHold, soundRemaining));
            Phase = "Complete";
        }

        private void OnDestroy()
        {
            foreach (var entry in mutedSources)
                if (entry.source != null) entry.source.mute = entry.muted;
            if (vignetteTexture != null) Destroy(vignetteTexture);
            if (featherTexture != null) Destroy(featherTexture);
        }

        private UnityEngine.UI.RawImage Ghost(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            go.transform.SetParent(face.transform, false);
            go.transform.SetSiblingIndex(0);                 // under the feather frame
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var img = go.GetComponent<UnityEngine.UI.RawImage>();
            img.raycastTarget = false;
            img.color = color;
            return img;
        }

        /// <summary>Red and cyan copies pulled apart by <paramref name="offset"/> pixels.</summary>
        private void Ghosts(float offset, float alpha)
        {
            foreach (var (g, sign) in new[] { (ghostRed, 1f), (ghostCyan, -1f) })
            {
                if (g == null) continue;
                g.texture = face.texture;
                g.rectTransform.anchoredPosition = new Vector2(offset * sign, offset * 0.3f * -sign);
                var c = g.color; c.a = alpha; g.color = c;
            }
        }

        /// <summary>One impact frame: a hard cut to another version of the face (negative, blood red, harsh), jolted, held briefly.</summary>
        private IEnumerator ImpactFrame(Color background, Texture2D image, float scale, Vector2 jolt, float roll, float hold, bool stutterAudio, bool hideFace = false)
        {
            blackBackdrop.color = background;
            vignette.enabled = background != Color.white;
            face.enabled = !hideFace;
            face.color = Color.white;
            face.texture = image != null ? image : openFace;
            Vector2 baseSize = face.rectTransform.sizeDelta;
            face.rectTransform.sizeDelta = baseSize * scale / Mathf.Max(0.01f, currentScale);
            currentScale = scale;
            face.rectTransform.anchoredPosition = jolt;
            face.rectTransform.localRotation = Quaternion.Euler(0f, 0f, roll);
            visuals.anchoredPosition = jolt * 0.5f;
            Ghosts(hideFace ? 0f : 18f, hideFace ? 0f : 0.22f);
            if (stutterAudio) voice.Pause();
            yield return new WaitForSecondsRealtime(hold * 0.5f);
            if (stutterAudio) voice.UnPause();
            yield return new WaitForSecondsRealtime(hold * 0.5f);
            vignette.enabled = true;
        }

        private float currentScale = 1f;

        private void ApplyShake(float strength, float rotationStrength = -1f)
        {
            float time = Time.unscaledTime;
            visuals.anchoredPosition = new Vector2(
                (Mathf.PerlinNoise(time * 37f, 0f) - 0.5f) * strength * 2f,
                (Mathf.PerlinNoise(0f, time * 41f) - 0.5f) * strength * 2f);
            face.rectTransform.anchoredPosition = new Vector2(
                (Mathf.PerlinNoise(time * 53f, 7f) - 0.5f) * strength * 1.2f,
                (Mathf.PerlinNoise(11f, time * 47f) - 0.5f) * strength * 1.2f);
            float rotation = rotationStrength >= 0f ? rotationStrength : strength * 0.35f;
            face.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                (Mathf.PerlinNoise(time * 29f, 13f) - 0.5f) * rotation);
        }

        private void ResetShake()
        {
            visuals.anchoredPosition = Vector2.zero;
            face.rectTransform.anchoredPosition = Vector2.zero;
            face.rectTransform.localRotation = Quaternion.identity;
        }

        /// <summary>Opaque at the border, clear inside: the image fades into the black with no hard rectangle.</summary>
        private static Texture2D CreateFeatherTexture()
        {
            const int size = 256;
            const float margin = 0.2f;   // how far in (fraction of each side) the fade reaches
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));      // distance to the nearest side
                float corner = Mathf.Max(0f, Vector2.Distance(new Vector2(Mathf.Clamp(u, margin, 1f - margin), Mathf.Clamp(v, margin, 1f - margin)), new Vector2(u, v)) / margin);
                float k = Mathf.Max(1f - edge / margin, corner);                             // rounded corners
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateVignetteTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.3f, radius));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            return texture;
        }
    }
}
