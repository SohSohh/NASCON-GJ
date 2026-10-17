using System.Collections;
using NasconGJ.Core;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Puzzles
{
    /// <summary>Trigger at the end of a level. Fades out and loads the next scene in story order (or an override).</summary>
    [RequireComponent(typeof(Collider))]
    public class LevelExit : MonoBehaviour
    {
        [Tooltip("Leave empty to use SceneNames.StoryOrder.")]
        [SerializeField] private string nextSceneOverride = "";
        [Tooltip("Optional: only opens after this puzzle id is solved.")]
        [SerializeField] private string requiredPuzzleId = "";

        private bool unlocked;
        private bool used;

        private void OnEnable() => GameEvents.PuzzleSolved += OnPuzzleSolved;
        private void OnDisable() => GameEvents.PuzzleSolved -= OnPuzzleSolved;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            unlocked = string.IsNullOrEmpty(requiredPuzzleId);
            if (GetComponent<Entities.HerdTarget>() == null)
                gameObject.AddComponent<Entities.HerdTarget>();
        }

        private void OnPuzzleSolved(string id)
        {
            if (id == requiredPuzzleId) unlocked = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (used || !unlocked || !other.CompareTag(GameTags.Player)) return;
            used = true;
            StartCoroutine(Exit());
        }

        private IEnumerator Exit()
        {
            if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeTo(1f, 1.2f);
            if (string.IsNullOrEmpty(nextSceneOverride)) SceneLoader.LoadNext();
            else SceneLoader.Load(nextSceneOverride);
        }
    }
}
