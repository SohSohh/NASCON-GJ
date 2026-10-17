using System.Collections;
using TMPro;
using UnityEngine;

namespace NasconGJ.UI
{
    /// <summary>
    /// The one line that always says what to do next ("Take the torch"). Top-left, quiet, never in the way;
    /// animates and chimes when it changes so a new goal is noticed.
    /// </summary>
    public class ObjectiveUI : MonoBehaviour
    {
        public static ObjectiveUI Instance { get; private set; }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text text;
        [SerializeField] private AudioSource chimeSource;
        [SerializeField] private AudioClip chimeClip;

        private Coroutine routine;
        private Vector2 restingPosition;
        public string Current { get; private set; } = "";

        private void Awake()
        {
            Instance = this;
            group.alpha = 0f;
            restingPosition = root.anchoredPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Set(string objective)
        {
            if (Instance == null || objective == Instance.Current) return;
            Instance.Current = objective;
            if (Instance.routine != null) Instance.StopCoroutine(Instance.routine);
            Instance.routine = Instance.StartCoroutine(Instance.Change(objective));
        }

        private IEnumerator Change(string objective)
        {
            for (float a = group.alpha; a > 0f; a -= Time.unscaledDeltaTime * 3f)
            {
                group.alpha = a;
                yield return null;
            }
            text.text = objective;
            group.alpha = 0f;
            root.anchoredPosition = restingPosition;
            if (string.IsNullOrEmpty(objective)) yield break;
            if (chimeSource != null && chimeClip != null) chimeSource.PlayOneShot(chimeClip, 0.25f);
            Vector2 basePos = restingPosition;
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.8f);
                group.alpha = k;
                root.anchoredPosition = basePos + new Vector2(Mathf.Lerp(-30f, 0f, k), 0f);
                yield return null;
            }
            root.anchoredPosition = basePos;
            group.alpha = 1f;
        }
    }
}
