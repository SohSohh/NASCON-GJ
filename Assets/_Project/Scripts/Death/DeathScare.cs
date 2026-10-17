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

        public string Phase { get; private set; }
        public bool IsConfigured => closedFace != null && openFace != null && jumpscare != null;
        private UnityEngine.UI.RawImage face;
        private UnityEngine.UI.RawImage vignette;
        private UnityEngine.UI.Image orangeFlash;
        private UnityEngine.UI.Image blackBackdrop;
        private RectTransform visuals;
        private RectTransform canvasRect;
        private AudioSource voice;
        private Texture2D vignetteTexture;
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
            // Audio and the first increase in size happen in the same frame.
            voice.Play();
            float soundStarted = Time.unscaledTime;
            float endHeight = Mathf.Max(canvasRect.rect.height, canvasRect.rect.width / aspect) * 1.3f;
            Vector2 endSize = new Vector2(endHeight * aspect, endHeight);
            float elapsed = 0f;
            while (elapsed < rushDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rushDuration);
                float acceleration = 0.1f * t + 0.9f * t * t * t;
                face.rectTransform.sizeDelta = Vector2.Lerp(initialSize, endSize, acceleration);
                ApplyShake(rushShake * (1f - t * 0.5f), rushRotationShake * (1f - t * 0.35f));
                orangeFlash.color = new Color(1f, 0.22f, 0f,
                    orangeFlickerAlpha * (0.15f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 35f)) * 0.45f));
                yield return null;
            }
            face.rectTransform.sizeDelta = endSize;
            ResetShake();
            Phase = "Impact";
            Color faceColor = face.color;
            float fadeElapsed = 0f;
            while (fadeElapsed < faceFadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                Color fadedColor = faceColor;
                fadedColor.a = Mathf.Lerp(faceColor.a, 0f, Mathf.Clamp01(fadeElapsed / faceFadeDuration));
                face.color = fadedColor;
                yield return null;
            }
            faceColor.a = 0f;
            face.color = faceColor;
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
        }

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
