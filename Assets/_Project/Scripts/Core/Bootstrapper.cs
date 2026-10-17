using UnityEngine;

namespace NasconGJ.Core
{
    /// <summary>Lives in the Boot scene. Does one-time setup, then hands off to the main menu.</summary>
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private string firstScene = SceneNames.MainMenu;

        private void Start() => SceneLoader.Load(firstScene);
    }
}
