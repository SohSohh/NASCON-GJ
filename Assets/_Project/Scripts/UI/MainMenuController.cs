using System.Collections;
using NasconGJ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NasconGJ.UI
{
    /// <summary>
    /// Main menu: staged intro (title, crystal divider, items), animated items (MenuButtonFX),
    /// Settings and Credits panels that slide in over the list, and a fade into the first level.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Intro")]
        [SerializeField] private CanvasGroup title;
        [SerializeField] private CanvasGroup footer;
        [SerializeField] private RectTransform divider;
        [SerializeField] private float dividerWidth = 520f;
        [SerializeField] private CanvasGroup[] items;

        [Header("Buttons")]
        [SerializeField] private Button beginButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button settingsBack;
        [SerializeField] private Button creditsBack;

        [Header("Panels")]
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private CanvasGroup settingsPanel;
        [SerializeField] private CanvasGroup creditsPanel;

        [Header("Settings")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityValue;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private Slider brushSlider;
        [SerializeField] private TMP_Text brushValue;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Audio")]
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioSource music;

        [SerializeField] private string firstScene = SceneNames.Prologue;

        private bool busy;

        private void Start()
        {
            GameManager.Instance?.SetState(GameState.Menu);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            MenuButtonFX.UISource = uiSource;
            MenuButtonFX.HoverClip = hoverClip;
            MenuButtonFX.SelectClip = selectClip;

            beginButton.onClick.AddListener(() => { if (!busy) StartCoroutine(Begin()); });
            settingsButton.onClick.AddListener(() => Open(settingsPanel, sensitivitySlider.gameObject));
            creditsButton.onClick.AddListener(() => Open(creditsPanel, creditsBack.gameObject));
            quitButton.onClick.AddListener(Quit);
            settingsBack.onClick.AddListener(() => Close(settingsPanel, settingsButton.gameObject));
            creditsBack.onClick.AddListener(() => Close(creditsPanel, creditsButton.gameObject));

            SetupSettings();
            Hide(settingsPanel);
            Hide(creditsPanel);
            StartCoroutine(Intro());
        }

        private void Update()
        {
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (!back || busy) return;
            if (settingsPanel.gameObject.activeSelf) Close(settingsPanel, settingsButton.gameObject);
            else if (creditsPanel.gameObject.activeSelf) Close(creditsPanel, creditsButton.gameObject);
        }

        // ------------------------------------------------------------ intro

        private IEnumerator Intro()
        {
            menuGroup.interactable = false;
            title.alpha = footer.alpha = 0f;
            foreach (var item in items) item.alpha = 0f;
            divider.sizeDelta = new Vector2(0f, divider.sizeDelta.y);

            yield return new WaitForSecondsRealtime(0.9f);
            StartCoroutine(FadeMove(title, 1f, 1.6f, new Vector2(0f, -24f)));
            yield return new WaitForSecondsRealtime(0.7f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 1.1f)
            {
                divider.sizeDelta = new Vector2(dividerWidth * Mathf.SmoothStep(0f, 1f, t), divider.sizeDelta.y);
                yield return null;
            }
            divider.sizeDelta = new Vector2(dividerWidth, divider.sizeDelta.y);
            yield return new WaitForSecondsRealtime(0.3f);
            foreach (var item in items)
            {
                StartCoroutine(FadeMove(item, 1f, 0.6f, new Vector2(-24f, 0f)));
                yield return new WaitForSecondsRealtime(0.12f);
            }
            StartCoroutine(FadeMove(footer, 1f, 1f, Vector2.zero));
            menuGroup.interactable = true;
            Select(beginButton.gameObject);
        }

        private static IEnumerator FadeMove(CanvasGroup group, float to, float duration, Vector2 fromOffset)
        {
            var rt = (RectTransform)group.transform;
            Vector2 end = rt.anchoredPosition;
            Vector2 start = end + fromOffset;
            float from = group.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                group.alpha = Mathf.Lerp(from, to, k);
                rt.anchoredPosition = Vector2.Lerp(start, end, k);
                yield return null;
            }
            group.alpha = to;
            rt.anchoredPosition = end;
        }

        // ------------------------------------------------------------ panels

        private void Open(CanvasGroup panel, GameObject firstSelected)
        {
            if (busy) return;
            StartCoroutine(Swap(menuGroup, 0.12f, panel, firstSelected));
        }

        private void Close(CanvasGroup panel, GameObject reselect)
        {
            if (busy) return;
            StartCoroutine(SwapBack(panel, reselect));
        }

        private IEnumerator Swap(CanvasGroup from, float fromAlpha, CanvasGroup panel, GameObject firstSelected)
        {
            busy = true;
            from.interactable = false;
            from.blocksRaycasts = false;
            StartCoroutine(FadeMove(from, fromAlpha, 0.3f, Vector2.zero));
            panel.gameObject.SetActive(true);
            yield return FadeMove(panel, 1f, 0.35f, new Vector2(60f, 0f));
            panel.interactable = true;
            panel.blocksRaycasts = true;
            Select(firstSelected);
            busy = false;
        }

        private IEnumerator SwapBack(CanvasGroup panel, GameObject reselect)
        {
            busy = true;
            panel.interactable = false;
            panel.blocksRaycasts = false;
            yield return FadeMove(panel, 0f, 0.25f, Vector2.zero);
            Hide(panel);
            yield return FadeMove(menuGroup, 1f, 0.3f, Vector2.zero);
            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
            Select(reselect);
            busy = false;
        }

        private static void Hide(CanvasGroup panel)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
        }

        private static void Select(GameObject go)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
        }

        // ------------------------------------------------------------ settings

        private void SetupSettings()
        {
            sensitivitySlider.minValue = 0.2f;
            sensitivitySlider.maxValue = 2.5f;
            sensitivitySlider.value = GameSettings.LookSensitivity;
            sensitivitySlider.onValueChanged.AddListener(v => { GameSettings.LookSensitivity = v; RefreshLabels(); });

            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = GameSettings.MasterVolume;
            volumeSlider.onValueChanged.AddListener(v => { GameSettings.MasterVolume = v; RefreshLabels(); });

            brushSlider.minValue = 0;
            brushSlider.maxValue = GameSettings.MaxBrushQuality;
            brushSlider.wholeNumbers = true;
            brushSlider.value = GameSettings.BrushQuality;
            brushSlider.onValueChanged.AddListener(v => { GameSettings.BrushQuality = Mathf.RoundToInt(v); RefreshLabels(); });

            fullscreenToggle.isOn = GameSettings.Fullscreen;
            fullscreenToggle.onValueChanged.AddListener(v => GameSettings.Fullscreen = v);
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            sensitivityValue.text = GameSettings.LookSensitivity.ToString("0.00") + "x";
            volumeValue.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
            brushValue.text = GameSettings.BrushQualityNames[GameSettings.BrushQuality];
        }

        // ------------------------------------------------------------ begin / quit

        private IEnumerator Begin()
        {
            busy = true;
            menuGroup.interactable = false;
            StartCoroutine(FadeMove(menuGroup, 0f, 1.2f, Vector2.zero));
            StartCoroutine(FadeMove(title, 0f, 1.4f, Vector2.zero));
            if (music != null) StartCoroutine(FadeAudio(music, 1.6f));
            yield return new WaitForSecondsRealtime(0.5f);
            if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeTo(1f, 1.4f);
            SceneLoader.Load(firstScene);
        }

        private static IEnumerator FadeAudio(AudioSource source, float duration)
        {
            float start = source.volume;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
