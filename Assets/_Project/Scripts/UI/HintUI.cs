using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace NasconGJ.UI
{
    /// <summary>
    /// One contextual hint at a time: a key cap plus a few words ("F  hold to pass the flame").
    /// Eases in with a soft chime and a gentle pulse, and stays up until the player actually does the thing.
    /// </summary>
    public class HintUI : MonoBehaviour
    {
        public static HintUI Instance { get; private set; }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private RectTransform keyCap;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private AudioSource chimeSource;
        [SerializeField] private AudioClip chimeClip;
        [SerializeField] private float fadeInTime = 0.7f;
        [SerializeField] private float fadeOutTime = 0.6f;

        private Coroutine routine;
        private string currentId;

        public bool IsShowing => group.alpha > 0.01f;

        private void Awake()
        {
            Instance = this;
            group.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Show a hint for at least <paramref name="minDuration"/> seconds. With <paramref name="dismissWhen"/> it then stays
        /// until that returns true (no time limit); without it, it fades after minDuration.
        /// </summary>
        public void Show(string keys, string label, float minDuration, Func<bool> dismissWhen = null, string id = null)
        {
            if (routine != null) StopCoroutine(routine);
            currentId = id;
            routine = StartCoroutine(Run(keys, label, minDuration, dismissWhen));
        }

        public void Hide(string id = null)
        {
            if (id != null && id != currentId) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeOut());
        }

        private IEnumerator Run(string keys, string label, float minDuration, Func<bool> dismissWhen)
        {
            // if another hint is up, let it go first so the change is readable
            if (group.alpha > 0.05f) yield return FadeOut();

            bool hasKey = !string.IsNullOrEmpty(keys);
            keyCap.gameObject.SetActive(hasKey);
            keyText.text = keys;
            labelText.text = label;
            if (hasKey)
            {
                keyText.ForceMeshUpdate();
                float width = Mathf.Max(58f, keyText.preferredWidth + 34f);
                var layout = keyCap.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout != null) layout.preferredWidth = width;
                else keyCap.sizeDelta = new Vector2(width, keyCap.sizeDelta.y);
            }
            if (chimeSource != null && chimeClip != null) chimeSource.PlayOneShot(chimeClip, 0.35f);

            for (float t = 0f; t < fadeInTime; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / fadeInTime);
                group.alpha = k;
                card.anchoredPosition = new Vector2(0f, Mathf.Lerp(-20f, 0f, k));
                card.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
                yield return null;
            }
            group.alpha = 1f;
            card.localScale = Vector3.one;

            float shown = 0f;
            while (shown < minDuration || (dismissWhen != null && !dismissWhen()))
            {
                shown += Time.unscaledDeltaTime;
                // slow breathing pulse on the key cap so the eye goes there
                if (hasKey) keyCap.localScale = Vector3.one * (1f + Mathf.Sin(shown * 4f) * 0.04f);
                yield return null;
            }
            keyCap.localScale = Vector3.one;
            yield return FadeOut();
        }

        private IEnumerator FadeOut()
        {
            float from = group.alpha;
            for (float t = 0f; t < fadeOutTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, 0f, t / fadeOutTime);
                yield return null;
            }
            group.alpha = 0f;
            currentId = null;
        }
    }
}
