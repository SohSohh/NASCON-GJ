using System;
using System.Collections;
using NasconGJ.Core;
using NasconGJ.Guiding;
using NasconGJ.Player;
using TMPro;
using UnityEngine;

namespace NasconGJ.UI
{
    /// <summary>Typewriter dialogue box. Interact / left click finishes the line, then advances.</summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text continueHint;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private AudioSource blipSource;
        [SerializeField] private AudioClip blipClip;
        [Tooltip("Played once when each line starts (the guiding light's voice).")]
        [SerializeField] private AudioClip lineClip;
        [SerializeField] private float fadeSpeed = 6f;
        [Tooltip("Lines move on by themselves after this many seconds (0 = wait for input).")]
        [SerializeField] private float autoAdvanceDelay = 3.2f;

        private bool open;
        private PlayerInputReader input;

        private void Awake()
        {
            Instance = this;
            if (group != null) group.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(DialogueData data, Action onFinished)
        {
            if (input == null) input = FindAnyObjectByType<PlayerInputReader>();
            StopAllCoroutines();
            StartCoroutine(Run(data, onFinished));
        }

        private bool Advance => input != null && input.AdvancePressed;

        private IEnumerator Run(DialogueData data, Action onFinished)
        {
            open = true;
            GameEvents.RaiseDialogueStarted(data.id);
            if (speakerText != null) speakerText.text = data.speaker;
            yield return null; // don't consume the press that opened us

            foreach (string line in data.lines)
            {
                bodyText.text = line;
                bodyText.maxVisibleCharacters = 0;
                bodyText.ForceMeshUpdate();
                continueHint.enabled = false;
                if (blipSource != null && lineClip != null) blipSource.PlayOneShot(lineClip, 0.6f);

                int total = bodyText.textInfo.characterCount;
                float visible = 0f;
                while (visible < total)
                {
                    group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.deltaTime * fadeSpeed);
                    if (Advance) visible = total;
                    else visible += Time.deltaTime * data.charactersPerSecond;

                    int shown = Mathf.Min(total, Mathf.FloorToInt(visible));
                    if (shown != bodyText.maxVisibleCharacters)
                    {
                        bodyText.maxVisibleCharacters = shown;
                        if (blipSource != null && blipClip != null && shown % 2 == 0) blipSource.PlayOneShot(blipClip, 0.5f);
                    }
                    yield return null;
                }

                continueHint.enabled = true;
                yield return null;
                float waited = 0f;
                while (!Advance && (autoAdvanceDelay <= 0f || waited < autoAdvanceDelay + line.Length * 0.02f))
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }

            open = false;
            while (group.alpha > 0f)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.deltaTime * fadeSpeed);
                yield return null;
            }

            GameEvents.RaiseDialogueFinished();
            onFinished?.Invoke();
        }
    }
}
