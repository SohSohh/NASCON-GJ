using UnityEngine;

namespace NasconGJ.Core
{
    /// <summary>Remembers the level wall a passage replaced with a doorway (so a rebuild can put it back).</summary>
    public class PassageWallMemo : MonoBehaviour
    {
        public GameObject wall;
    }
}
