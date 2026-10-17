using NasconGJ.Core;
using UnityEngine;

namespace NasconGJ.Cutscenes.Scroll
{
    /// <summary>
    /// The story scroll is already painted, so the game's painterly brush filter and comic pass are switched off
    /// while it plays (and restored afterwards).
    /// </summary>
    public class ScrollLook : MonoBehaviour
    {
        private static readonly int BrushId = Shader.PropertyToID("_NasconBrushReduce");
        private static readonly int PaletteId = Shader.PropertyToID("_NasconPaletteReduce");
        private float brush, palette;

        private void OnEnable()
        {
            brush = Shader.GetGlobalFloat(BrushId);
            palette = Shader.GetGlobalFloat(PaletteId);
            Shader.SetGlobalFloat(BrushId, 99f);
            Shader.SetGlobalFloat(PaletteId, 1f);
            ComicLook.Apply(0f, 0f, 0f);
            ComicLook.SetVoid(Vector3.zero, 0f, 1f);
            ComicLook.SetImpact(0f);
            ComicLook.SetDrain(0f);
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(BrushId, brush);
            Shader.SetGlobalFloat(PaletteId, palette);
        }
    }
}
