using UnityEngine;

namespace NasconGJ.Core
{
    public enum GameState
    {
        Menu,
        Cutscene,
        Playing,
        Paused,
        Dead,
    }

    /// <summary>
    /// Persistent singleton. Auto-created before the first scene loads, so any scene can be
    /// played directly from the editor without going through Boot.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => GameEvents.PlayerDied += HandlePlayerDied;
        private void OnDisable() => GameEvents.PlayerDied -= HandlePlayerDied;

        public void SetState(GameState state) => State = state;

        private void HandlePlayerDied() => SetState(GameState.Dead);
    }
}
