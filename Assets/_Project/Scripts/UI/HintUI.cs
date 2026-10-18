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
        /// <summary>Id of the hint on screen (world prompts use ids starting with "prompt:").</summary>
        public string CurrentId => currentId;

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
        public void Show(string keys, string label, float minDuration, Func<bool> dismissWhen = null, string id = null, bool quiet = false)
        {
            if (routine != null) StopCoroutine(routine);
            currentId = id;
            routine = StartCoroutine(Run(keys, label, minDuration, dismissWhen, id, quiet));
        }

        public void Hide(string id = null)
        {
            if (id != null && id != currentId) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeOut());
        }

        private IEnumerator Run(string keys, string label, float minDuration, Func<bool> dismissWhen, string id, bool quiet)
        {
            // if another hint is up, let it go first so the change is readable
            if (group.alpha > 0.05f) yield return FadeOut();
            while (DialogueUI.IsOpen) yield return null;
            if (dismissWhen != null && dismissWhen()) yield break;
            currentId = id;

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
            if (!quiet && chimeSource != null && chimeClip != null) chimeSource.PlayOneShot(chimeClip, 0.35f);

            yield return FadeIn();

            float shown = 0f;
            while (shown < minDuration || (dismissWhen != null && !dismissWhen()))
            {
                if (DialogueUI.IsOpen)
                {
                    group.alpha = 0f;
                    while (DialogueUI.IsOpen) yield return null;
                    if (dismissWhen != null && dismissWhen()) break;
                    yield return FadeIn();
                }
                shown += Time.deltaTime;
                if (hasKey) keyCap.localScale = Vector3.one * (1f + Mathf.Sin(shown * 4f) * 0.04f);
                yield return null;
            }
            keyCap.localScale = Vector3.one;
            yield return FadeOut();
        }

        private void LateUpdate()
        {
            // A guide can start speaking after our coroutine has run this frame.
            if (DialogueUI.IsOpen) group.alpha = 0f;
        }

        private IEnumerator FadeIn()
        {
            for (float t = 0f; t < fadeInTime; t += Time.deltaTime)
            {
                if (DialogueUI.IsOpen)
                {
                    group.alpha = 0f;
                    while (DialogueUI.IsOpen) yield return null;
                    t = 0f;
                }
                float k = Mathf.SmoothStep(0f, 1f, t / fadeInTime);
                group.alpha = k;
                card.anchoredPosition = new Vector2(0f, Mathf.Lerp(-20f, 0f, k));
                card.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
                yield return null;
            }
            group.alpha = 1f;
            card.localScale = Vector3.one;
            card.anchoredPosition = Vector2.zero;
        }

        private IEnumerator FadeOut()
        {
            float from = group.alpha;
            for (float t = 0f; t < fadeOutTime; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, 0f, t / fadeOutTime);
                yield return null;
            }
            group.alpha = 0f;
            currentId = null;
        }
    }
}
