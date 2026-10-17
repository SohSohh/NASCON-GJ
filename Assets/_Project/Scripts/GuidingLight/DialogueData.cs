using UnityEngine;

namespace NasconGJ.Guiding
{
    /// <summary>A sequence of lines shown by the guiding light (Undertale-style typewriter).</summary>
    [CreateAssetMenu(menuName = "NASCON/Dialogue", fileName = "SO_Dialogue_New")]
    public class DialogueData : ScriptableObject
    {
        public string id = "dialogue";
        [Tooltip("Shown above the text, e.g. THE GUIDING LIGHT. Empty = no name.")]
        public string speaker = "";
        [TextArea(2, 4)] public string[] lines = { "..." };
        public float charactersPerSecond = 38f;
    }
}
