using System;
using UnityEngine;

namespace NasconGJ.Core
{
    /// <summary>
    /// Global event bus. Systems owned by different devs talk through these events
    /// instead of holding direct references to each other.
    /// Raise with GameEvents.RaiseX(...), listen in OnEnable / unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // Fire
        public static event Action<GameObject> FireTransferred;   // new burning object
        public static event Action FireExtinguished;              // player has no flame left -> darkness
        public static event Action<bool> FireCovered;             // true = hands covering flame

        // Player
        public static event Action PlayerDied;
        public static event Action<Transform> CheckpointReached;

        // Guiding light / dialogue
        public static event Action<string> DialogueStarted;       // dialogue id
        public static event Action DialogueFinished;

        // Puzzles
        public static event Action<string> PuzzleSolved;          // puzzle id

        public static void RaiseFireTransferred(GameObject target) => FireTransferred?.Invoke(target);
        public static void RaiseFireExtinguished() => FireExtinguished?.Invoke();
        public static void RaiseFireCovered(bool covered) => FireCovered?.Invoke(covered);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseCheckpointReached(Transform point) => CheckpointReached?.Invoke(point);
        public static void RaiseDialogueStarted(string id) => DialogueStarted?.Invoke(id);
        public static void RaiseDialogueFinished() => DialogueFinished?.Invoke();
        public static void RaisePuzzleSolved(string id) => PuzzleSolved?.Invoke(id);

        // Static events survive domain-reload-disabled play mode; clear them on every play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            FireTransferred = null;
            FireExtinguished = null;
            FireCovered = null;
            PlayerDied = null;
            CheckpointReached = null;
            DialogueStarted = null;
            DialogueFinished = null;
            PuzzleSolved = null;
        }
    }
}
