using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>Keeps the painted sky dome centred on the camera, so every outdoor set shares one sky.</summary>
    public class SkyFollow : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position;
        }
    }
}
