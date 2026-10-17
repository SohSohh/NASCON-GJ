using System.Collections;
using UnityEngine;

namespace NasconGJ.Cutscenes.Scroll
{
    /// <summary>A quad glued in front of the camera that floods the frame with ink (or paper) and drains it away.</summary>
    public class InkTransitionOverlay : MonoBehaviour
    {
        [SerializeField] private Renderer quad;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int DirId = Shader.PropertyToID("_Dir");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Material mat;

        private void Awake()
        {
            mat = quad.material;
            mat.SetFloat(ProgressId, 0f);
        }

        public void SetCovered(float k, Color color)
        {
            mat.SetColor(ColorId, color);
            mat.SetFloat(ProgressId, k);
        }

        /// <summary>Ink blooms out from a screen point until the frame is covered.</summary>
        public IEnumerator Drop(Vector2 center, float duration, Color color, bool cover = true)
        {
            mat.SetFloat(ModeId, 0f);
            mat.SetVector(CenterId, center);
            mat.SetColor(ColorId, color);
            yield return Run(cover ? 0f : 1f, cover ? 1f : 0f, duration);
        }

        /// <summary>A dry-brush stroke sweeps across the frame.</summary>
        public IEnumerator Sweep(Vector2 dir, float duration, Color color, bool cover = true)
        {
            mat.SetFloat(ModeId, 1f);
            mat.SetVector(DirId, dir);
            mat.SetColor(ColorId, color);
            yield return Run(cover ? 0f : 1f, cover ? 1f : 0f, duration);
        }

        private IEnumerator Run(float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                mat.SetFloat(ProgressId, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            mat.SetFloat(ProgressId, to);
        }
    }
}
