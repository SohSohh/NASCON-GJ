using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// Cutscene overlay: letterbox bars, one short caption line at a time, the title card,
    /// and the "hold to skip" indicator. The black fader is a <see cref="UI.ScreenFader"/> on the same canvas.
    /// </summary>
    public class CutsceneUI : MonoBehaviour
    {
        [SerializeField] private RectTransform barTop;
        [SerializeField] private RectTransform barBottom;
        [SerializeField] private float barHeight = 118f;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private CanvasGroup captionGroup;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup skipGroup;
        [SerializeField] private Image skipFill;

        private Coroutine captionRoutine, barsRoutine;

        private void Awake()
        {
            captionGroup.alpha = 0f;
            titleGroup.alpha = 0f;
            skipGroup.alpha = 0f;
            SetBars(0f);
        }

        public void Letterbox(bool on, float duration = 1.2f)
        {
            if (barsRoutine != null) StopCoroutine(barsRoutine);
            barsRoutine = StartCoroutine(Bars(on ? 1f : 0f, duration));
        }

        /// <summary>Fades one line in, holds it, fades it out.</summary>
        public void Caption(string text, float hold)
        {
            if (captionRoutine != null) StopCoroutine(captionRoutine);
            caption.text = text;
            captionRoutine = StartCoroutine(Pulse(captionGroup, 1.1f, hold, 1.1f));
        }

        public void Title(float hold) => StartCoroutine(Pulse(titleGroup, 1.6f, hold, 1.8f));

        public void SetSkip(float progress, bool visible)
        {
            skipGroup.alpha = Mathf.MoveTowards(skipGroup.alpha, visible ? 1f : 0f, Time.deltaTime * 3f);
            skipFill.fillAmount = progress;
        }

        private IEnumerator Bars(float target, float duration)
        {
            float start = barTop.sizeDelta.y / barHeight;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                SetBars(Mathf.SmoothStep(start, target, t / duration));
                yield return null;
            }
            SetBars(target);
        }

        private void SetBars(float k)
        {
            barTop.sizeDelta = new Vector2(barTop.sizeDelta.x, barHeight * k);
            barBottom.sizeDelta = new Vector2(barBottom.sizeDelta.x, barHeight * k);
        }

        private static IEnumerator Pulse(CanvasGroup group, float fadeIn, float hold, float fadeOut)
        {
            float from = group.alpha;
            for (float t = 0f; t < fadeIn; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, 1f, Mathf.SmoothStep(0f, 1f, t / fadeIn));
                yield return null;
            }
            group.alpha = 1f;
            yield return new WaitForSeconds(hold);
            for (float t = 0f; t < fadeOut; t += Time.deltaTime)
            {
                group.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / fadeOut);
                yield return null;
            }
            group.alpha = 0f;
        }
    }
}
