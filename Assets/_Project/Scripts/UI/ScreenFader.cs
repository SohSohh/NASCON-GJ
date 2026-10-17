using System.Collections;
using UnityEngine;

namespace NasconGJ.UI
{
    /// <summary>Full-screen black overlay. Fades in from black on scene start.</summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private float fadeInOnStart = 1.5f;

        private Coroutine routine;

        private void Awake()
        {
            Instance = this;
            group.alpha = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start() => FadeTo(0f, fadeInOnStart);

        public Coroutine FadeTo(float alpha, float duration)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Fade(alpha, duration));
            return routine;
        }

        private IEnumerator Fade(float target, float duration)
        {
            float start = group.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
