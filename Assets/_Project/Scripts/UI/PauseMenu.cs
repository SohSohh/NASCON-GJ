using NasconGJ.Core;
using NasconGJ.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NasconGJ.UI
{
    /// <summary>Esc / Start toggles pause.</summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button quitButton;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            panel.SetActive(false);
            resumeButton.onClick.AddListener(() => SetPaused(false));
            restartButton.onClick.AddListener(() => { SetPaused(false); SceneLoader.ReloadCurrent(); });
            menuButton.onClick.AddListener(() => { SetPaused(false); SceneLoader.Load(SceneNames.MainMenu); });
            quitButton.onClick.AddListener(Quit);
        }

        private void Update()
        {
            bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                           (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (pressed) SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            var gm = GameManager.Instance;
            if (paused && gm != null && gm.State != GameState.Playing) return;

            IsPaused = paused;
            panel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            gm?.SetState(paused ? GameState.Paused : GameState.Playing);
            FirstPersonController.SetCursorLocked(!paused);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy() => Time.timeScale = 1f;
    }
}
