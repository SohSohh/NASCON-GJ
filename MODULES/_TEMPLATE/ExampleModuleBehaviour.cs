using NasconGJ.Core;
using UnityEngine;

// Namespace: NasconGJ.<Area>, matching the Scripts/ folder this module will be promoted into.
namespace NasconGJ.Modules.Template
{
    /// <summary>Copy this folder, rename it, delete this file. Folders starting with "_" are never imported.</summary>
    public class ExampleModuleBehaviour : MonoBehaviour
    {
        [SerializeField] private string puzzleId = "example";

        private void OnEnable() => GameEvents.PuzzleSolved += OnPuzzleSolved;
        private void OnDisable() => GameEvents.PuzzleSolved -= OnPuzzleSolved;

        private void OnPuzzleSolved(string id)
        {
            if (id == puzzleId) Debug.Log($"[{name}] puzzle {id} solved");
        }
    }
}
