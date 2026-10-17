using UnityEngine;

namespace NasconGJ.Core
{
    /// <summary>
    /// Per-scene settings for the comic finishing pass (NASCON/ComicPost): ink outline, toon colour and halftone.
    /// Also clears the one-shot effects (Void flood, impact frame) so they never leak between scenes.
    /// Cutscenes drive the same globals over time through the static setters.
    /// </summary>
    public class ComicLook : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] private float ink = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float toon = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float halftone = 0.35f;

        private static readonly int InkId = Shader.PropertyToID("_NasconInk");
        private static readonly int ToonId = Shader.PropertyToID("_NasconToon");
        private static readonly int HalftoneId = Shader.PropertyToID("_NasconHalftone");
        private static readonly int VoidId = Shader.PropertyToID("_NasconVoid");
        private static readonly int VoidSoftId = Shader.PropertyToID("_NasconVoidSoft");
        private static readonly int ImpactId = Shader.PropertyToID("_NasconImpact");
        private static readonly int DrainId = Shader.PropertyToID("_NasconDrain");
        private static readonly int VoidDarkId = Shader.PropertyToID("_NasconVoidDark");

        private void OnEnable()
        {
            Apply(ink, toon, halftone);
            SetVoid(Vector3.zero, 0f, 1f);
            SetImpact(0f);
            SetDrain(0f);
        }

        private void OnDisable()
        {
            SetVoid(Vector3.zero, 0f, 1f);
            SetImpact(0f);
        }

        public static void Apply(float inkAmount, float toonAmount, float halftoneAmount)
        {
            Shader.SetGlobalFloat(InkId, inkAmount);
            Shader.SetGlobalFloat(ToonId, toonAmount);
            Shader.SetGlobalFloat(HalftoneId, halftoneAmount);
        }

        /// <summary>World-space darkness flood around <paramref name="origin"/> (radius 0 = off).</summary>
        public static void SetVoid(Vector3 origin, float radius, float softness)
        {
            Shader.SetGlobalVector(VoidId, new Vector4(origin.x, origin.y, origin.z, radius));
            Shader.SetGlobalFloat(VoidSoftId, softness);
        }

        public static void SetImpact(float amount) => Shader.SetGlobalFloat(ImpactId, amount);

        public static void SetDrain(float amount) => Shader.SetGlobalFloat(DrainId, amount);

        /// <summary>How dark the world stays after the Void's wave has passed (0 = untouched, 1 = black).</summary>
        public static void SetVoidDark(float amount) => Shader.SetGlobalFloat(VoidDarkId, amount);
    }
}
