using UnityEngine;

namespace NasconGJ.Rendering
{
    /// <summary>
    /// Per-level tuning of the shared GBCamera palette pass. Sets shader globals while this level is loaded and clears
    /// them when it unloads, so the shared material asset (and every other level) is never touched.
    /// </summary>
    public class LevelPostLook : MonoBehaviour
    {
        [Tooltip("Blend the hard palette steps with a filtered lookup (softer band edges).")]
        [Range(0f, 1f)] [SerializeField] private float softenBands = 0.65f;
        [Tooltip("Pulls the brightest palette colours down toward the darks.")]
        [Range(0f, 0.6f)] [SerializeField] private float reduceContrast = 0.2f;
        [Tooltip("Lets a little of the unpaletted image through.")]
        [Range(0f, 1f)] [SerializeField] private float reducePalette = 0.15f;
        [Tooltip("Exposure change before the palette lookup (-0.15 = 15% darker).")]
        [Range(-0.6f, 0.6f)] [SerializeField] private float exposure = -0.15f;
        [Tooltip("Finer pixels (0.25 = 25% more vertical resolution).")]
        [Range(0f, 1.5f)] [SerializeField] private float finerPixels = 0.25f;

        private static readonly int LookId = Shader.PropertyToID("_GBLevelLook");
        private static readonly int PixelId = Shader.PropertyToID("_GBLevelPixelScale");

        private void OnEnable() => Apply();
        private void OnValidate() { if (isActiveAndEnabled) Apply(); }

        private void Apply()
        {
            Shader.SetGlobalVector(LookId, new Vector4(softenBands, reduceContrast, reducePalette, exposure));
            Shader.SetGlobalFloat(PixelId, finerPixels);
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(LookId, Vector4.zero);
            Shader.SetGlobalFloat(PixelId, 0f);
        }
    }
}
