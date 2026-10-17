using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// Non-hostile shadow decoration: the townsfolk left frozen on the walls, or the giant Void relief.
    /// Drapes itself on the surface it faces. Optionally sways through the idle frames.
    /// Place it like a ShadowEnemy: blue axis pointing out of the surface, pivot at the feet.
    /// </summary>
    public class ShadowFigure : MonoBehaviour
    {
        public enum Sheet { Townsfolk, Creature }

        [SerializeField] private ShadowSurfaceSkin skin;
        [SerializeField] private Sheet sheet = Sheet.Townsfolk;
        [SerializeField] private int frame;
        [SerializeField] private float height = 1.75f;
        [SerializeField] private bool flip;
        [SerializeField] private float glow;
        [SerializeField] private float opacity = 0.8f;
        [SerializeField] private bool animateIdle;
        [SerializeField] private float idleFps = 1.2f;
        [SerializeField] private LayerMask surfaceMask = ~0;

        private Vector3 basePoint, normal, up;

        private void Start()
        {
            if (Physics.Raycast(transform.position + transform.forward * 0.6f, -transform.forward, out RaycastHit hit, 2f, surfaceMask, QueryTriggerInteraction.Ignore))
                normal = hit.normal;
            else normal = transform.forward;
            basePoint = transform.position;
            up = Vector3.ProjectOnPlane(transform.up, normal).normalized;
            skin.SetSurfaceMask(surfaceMask);
            float aspect = ShadowSheet.Aspect;
            skin.Conform(basePoint, normal, up, height * aspect, height);
            Apply(0f);
        }

        private void Update()
        {
            if (animateIdle) Apply(Time.time);
        }

        /// <summary>Change opacity and glow at runtime (cutscenes fade figures in).</summary>
        public void SetLook(float newOpacity, float newGlow)
        {
            opacity = newOpacity;
            glow = newGlow;
            if (basePoint != Vector3.zero || normal != Vector3.zero) Apply(Time.time);
        }

        private void Apply(float time)
        {
            Vector4 rect = sheet == Sheet.Townsfolk
                ? ShadowSheet.TownsfolkFrame(frame)
                : ShadowSheet.CreatureFrame(animateIdle ? ShadowSheet.Idle + Mathf.FloorToInt(time * idleFps) % 4 : frame);
            float g = glow * (animateIdle ? 0.85f + Mathf.PerlinNoise(time * 0.7f, 3f) * 0.3f : 1f);
            skin.SetLook(rect, flip, g, 0f, opacity);
        }
    }
}
