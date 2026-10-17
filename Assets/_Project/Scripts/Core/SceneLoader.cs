using System;
using UnityEngine.SceneManagement;

namespace NasconGJ.Core
{
    /// <summary>
    /// Thin wrapper around SceneManager so every transition goes through one place
    /// (fades, loading screens, analytics etc. can be added here later).
    /// </summary>
    public static class SceneLoader
    {
        public static event Action<string> SceneLoadStarted;

        public static void Load(string sceneName)
        {
            SceneLoadStarted?.Invoke(sceneName);
            SceneManager.LoadScene(sceneName);
        }

        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);

        /// <summary>Loads the next scene in SceneNames.StoryOrder (wraps to MainMenu at the end).</summary>
        public static void LoadNext()
        {
            string current = SceneManager.GetActiveScene().name;
            int index = Array.IndexOf(SceneNames.StoryOrder, current);
            int next = index < 0 || index + 1 >= SceneNames.StoryOrder.Length ? 0 : index + 1;
            Load(SceneNames.StoryOrder[next]);
        }
    }
}
