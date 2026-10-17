using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// The living body of a shadow: a <see cref="VineCreature"/> (gaunt figure of black thorny vines with a smouldering
    /// heart) that clings splayed against the wall it crawls on, stands up out of the floor to face you, reaches for you
    /// while it hunts, and goes rigid when frozen by light. Black smoke peels off it.
    /// Driven every frame by <see cref="ShadowEnemy"/>; the flat sprite under it is its faint cast stain.
    /// States follow the team's AI: Dormant, Observe, Approach, Drain, Herd, Frozen (+ resume warning), Windup, Lunge.
    /// </summary>
    public class ShadowBody : MonoBehaviour
    {
        [SerializeField] private Material vineMaterial;
        [SerializeField] private Material heartMaterial;
        [SerializeField] private ParticleSystem wisps;
        [SerializeField] private int seed = 11;

        private VineCreature creature;
        private Transform rig;
        private bool hidden;

        private void Awake()
        {
            if (vineMaterial == null) { enabled = false; return; }
            var go = new GameObject("VineBody");
            go.transform.SetParent(transform, false);
            creature = go.AddComponent<VineCreature>();
            creature.Configure(1f, vineMaterial, heartMaterial, seed + Random.Range(0, 1000), 1.8f);
            creature.Build();
            rig = go.transform;
        }

        public void SetHidden(bool hide)
        {
            if (creature == null || hidden == hide) return;
            hidden = hide;
            creature.SetVisible(!hide);
            if (wisps != null) { var em = wisps.emission; em.enabled = !hide; }
        }

        /// <summary>Lay the body out on the surface. <paramref name="target"/> is what the reaching arm goes for.</summary>
        public void Pose(Vector3 basePoint, Vector3 normal, Vector3 up, float height, float facing, ShadowEnemy.State state,
            Vector3 target, float glowAmount, float dissolve, float dt, bool isResumeWarning = false)
        {
            if (creature == null || hidden) return;

            bool frozen = state == ShadowEnemy.State.Frozen;
            // frozen: locked rigid in place; during the resume warning it starts to stir again
            creature.TimeScale = frozen ? (isResumeWarning ? 0.35f : 0f) : 1f;

            bool onFloor = normal.y > 0.65f;
            Quaternion rot;
            if (onFloor)
            {
                Vector3 toward = Vector3.ProjectOnPlane(target - basePoint, Vector3.up);
                rot = Quaternion.LookRotation(toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.forward, Vector3.up);
            }
            else rot = Quaternion.LookRotation(normal, up);

            rig.SetPositionAndRotation(basePoint + normal * (onFloor ? 0f : 0.06f), rot);
            rig.localScale = new Vector3(height, height, onFloor ? height : height * 0.35f); // pressed flat against walls

            bool hunting = state == ShadowEnemy.State.Approach || state == ShadowEnemy.State.Drain || state == ShadowEnemy.State.Herd ||
                           state == ShadowEnemy.State.Windup || state == ShadowEnemy.State.Lunge;
            bool draining = state == ShadowEnemy.State.Drain;
            bool dormant = state == ShadowEnemy.State.Dormant;
            creature.Flatten = onFloor ? 0f : 0.85f;
            creature.Reach = state == ShadowEnemy.State.Windup || state == ShadowEnemy.State.Lunge ? 1f : draining ? 0.85f : hunting ? 0.6f : 0f;
            creature.Curl = dormant ? 0.6f : 0f;                                         // asleep: folded in on itself
            creature.Spread = state == ShadowEnemy.State.Windup ? 0.4f : state == ShadowEnemy.State.Herd ? 0.25f : 0f;
            creature.Twitch = frozen ? 0f : dormant ? 0.15f : hunting ? 0.9f : 0.5f;
            creature.ReachTarget = rig.InverseTransformPoint(target);

            float glow = frozen && !isResumeWarning ? glowAmount * 0.25f : state == ShadowEnemy.State.Herd ? glowAmount * 1.35f : glowAmount;
            creature.SetGlow(glow, dissolve);

            if (wisps != null)
            {
                wisps.transform.SetPositionAndRotation(basePoint + up * (height * 0.55f) + normal * 0.15f, rot);
                var shape = wisps.shape;
                shape.scale = new Vector3(height * 0.45f, height * 0.7f, 0.2f);
                var em = wisps.emission;
                em.rateOverTime = frozen ? (isResumeWarning ? 3f : 0f) : draining ? 20f : hunting ? 14f : dormant ? 2f : 6f;
            }
        }
    }
}
