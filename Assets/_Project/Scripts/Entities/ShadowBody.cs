using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// The living mass of a shadow creature: a cluster of wobbling tar blobs laid out on a rough skeleton
    /// (base, robe, chest, head with horn nubs, two clawed arms), half sunk into the surface it crawls on.
    /// Every blob drifts, swells and shrinks on its own, so the shape is never quite the same twice;
    /// arms reach for the player when it stalks, everything balls up and burns away when light hits it.
    /// Driven every frame by <see cref="ShadowEnemy"/>; the flat sprite underneath stays as its cast stain.
    /// </summary>
    public class ShadowBody : MonoBehaviour
    {
        [SerializeField] private Mesh globMesh;
        [SerializeField] private Material globMaterial;
        [SerializeField] private Material eyeMaterial;
        [SerializeField] private ParticleSystem drips;

        // x, y in units of creature height (x toward the facing side), radius in height units, limb id
        private static readonly (float x, float y, float r, int limb)[] Layout =
        {
            (-0.10f, 0.05f, 0.12f, 4), (0.10f, 0.05f, 0.11f, 4), (0.00f, 0.02f, 0.16f, 4),       // pooled base
            (0.00f, 0.19f, 0.14f, 0), (0.02f, 0.32f, 0.14f, 0), (0.03f, 0.46f, 0.13f, 0),       // robe / body
            (0.05f, 0.58f, 0.12f, 0),                                                         // chest
            (-0.09f, 0.65f, 0.075f, 0), (0.13f, 0.65f, 0.075f, 0),                            // shoulders
            (0.07f, 0.76f, 0.08f, 1), (0.03f, 0.86f, 0.03f, 1), (0.12f, 0.85f, 0.028f, 1),     // head + horn nubs
            (-0.17f, 0.50f, 0.05f, 2), (-0.21f, 0.37f, 0.045f, 2), (-0.22f, 0.29f, 0.028f, 2), // back arm
            (0.20f, 0.55f, 0.05f, 3), (0.28f, 0.47f, 0.045f, 3), (0.33f, 0.42f, 0.03f, 3),     // reaching arm
        };

        private Transform[] globs;
        private float[] phases;
        private Transform eyeL, eyeR;
        private Material glob, eye;
        private bool hidden;
        private float time, swell = 1f, reach;

        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        private void Awake()
        {
            if (globMesh == null || globMaterial == null) { enabled = false; return; }
            glob = new Material(globMaterial);
            eye = eyeMaterial != null ? new Material(eyeMaterial) : glob;
            globs = new Transform[Layout.Length];
            phases = new float[Layout.Length];
            for (int i = 0; i < Layout.Length; i++)
            {
                globs[i] = MakePart("Glob_" + i, glob);
                phases[i] = Random.value * 20f;
            }
            eyeL = MakePart("EyeL", eye);
            eyeR = MakePart("EyeR", eye);

            // drips are little tar droplets, not flat sprites
            if (drips != null)
            {
                var pr = drips.GetComponent<ParticleSystemRenderer>();
                pr.renderMode = ParticleSystemRenderMode.Mesh;
                pr.mesh = globMesh;
                pr.sharedMaterial = glob;
            }
        }

        private Transform MakePart(string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = globMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        public void SetHidden(bool hide)
        {
            if (hidden == hide || globs == null) return;
            hidden = hide;
            foreach (var g in globs) g.gameObject.SetActive(!hide);
            eyeL.gameObject.SetActive(!hide);
            eyeR.gameObject.SetActive(!hide);
            if (drips != null) { var em = drips.emission; em.enabled = !hide; }
        }

        /// <summary>Lay the body out on the surface. <paramref name="target"/> is what the reaching arm goes for.</summary>
        public void Pose(Vector3 basePoint, Vector3 normal, Vector3 up, float height, float facing, ShadowEnemy.State state,
            Vector3 target, float glowAmount, float dissolve, float dt)
        {
            if (globs == null || hidden) return;
            time += dt;
            if (normal.y > 0.65f)
            {
                // on the floor: rise up out of the puddle as a standing figure, facing the player
                Vector3 toward = Vector3.ProjectOnPlane(target - basePoint, Vector3.up);
                normal = toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
                if (normal.sqrMagnitude < 0.01f) normal = Vector3.forward;
                up = Vector3.up;
                facing = 1f;
            }
            Vector3 right = Vector3.Cross(normal, up) * facing;
            Quaternion rot = Quaternion.LookRotation(normal, up);

            bool hunting = state == ShadowEnemy.State.Stalk || state == ShadowEnemy.State.Windup;
            bool burning = state == ShadowEnemy.State.Recoil;
            swell = Mathf.MoveTowards(swell, state == ShadowEnemy.State.Windup ? 1.18f : burning ? 0.8f : 1f, dt * 1.5f);
            reach = Mathf.MoveTowards(reach, hunting ? 1f : 0f, dt * 2.2f);
            float jitterSpeed = burning ? 9f : hunting ? 1.6f : 0.7f;
            float jitter = burning ? 0.035f : 0.05f;

            // where the reaching hand wants to be, in the body's surface coordinates
            Vector3 toTarget = Vector3.ProjectOnPlane(target - basePoint, normal);
            Vector2 reachGoal = new Vector2(Vector3.Dot(toTarget, right), Vector3.Dot(toTarget, up)) / Mathf.Max(0.1f, height);
            reachGoal = Vector2.ClampMagnitude(reachGoal - new Vector2(0.13f, 0.65f), 0.32f) + new Vector2(0.13f, 0.65f);

            for (int i = 0; i < Layout.Length; i++)
            {
                var (x, y, r, limb) = Layout[i];
                Vector2 p = new Vector2(x, y);
                float ph = phases[i];

                if (limb == 3 && reach > 0f)
                {
                    // the front arm stretches toward the player, joints spread along the reach
                    float along = i - 15 + 1; // 1..3 from shoulder outward
                    p = Vector2.Lerp(p, Vector2.Lerp(new Vector2(0.13f, 0.65f), reachGoal, along / 3f), reach);
                }
                if (limb == 2) p.y += Mathf.Sin(time * 0.9f + ph) * 0.03f;  // back arm drags and sways
                if (burning) p = Vector2.Lerp(p, new Vector2(0.02f, 0.42f), 0.35f); // balls up away from the light

                // each blob wanders on its own: the silhouette is never quite still
                p.x += (Mathf.PerlinNoise(time * jitterSpeed * 0.5f + ph, 0.3f) - 0.5f) * jitter * 2f;
                p.y += (Mathf.PerlinNoise(0.7f, time * jitterSpeed * 0.5f + ph) - 0.5f) * jitter * 2f;
                float size = r * swell * (1f + Mathf.Sin(time * (1.1f + i * 0.07f) + ph) * 0.18f);
                if (limb == 4) size *= 1f + Mathf.Sin(time * 0.6f + ph) * 0.12f; // the pool breathes

                float d = size * 2f * height;
                var t = globs[i];
                t.position = basePoint + right * (p.x * height) + up * (p.y * height) + normal * (size * height * 0.35f);
                t.rotation = rot;
                t.localScale = new Vector3(d, d * (limb == 4 ? 0.7f : 1f), d * 0.55f);
            }

            // eyes on the head blob, toward the viewer side of the wall
            Vector3 head = globs[9].position;
            float eyeSize = height * (state == ShadowEnemy.State.Windup ? 0.03f : 0.022f) * (0.85f + 0.15f * Mathf.PerlinNoise(time * 3f, 1f));
            eyeL.position = head + right * (height * 0.012f) + up * (height * 0.004f) + normal * (height * 0.05f);
            eyeR.position = head + right * (height * 0.045f) + up * (height * 0.006f) + normal * (height * 0.045f);
            eyeL.localScale = eyeR.localScale = new Vector3(eyeSize, eyeSize * 0.55f, eyeSize * 0.5f);
            eyeL.rotation = eyeR.rotation = rot;

            glob.SetFloat(GlowId, glowAmount);
            glob.SetFloat(DissolveId, dissolve);
            if (eye != glob) eye.SetFloat(DissolveId, dissolve);

            if (drips != null)
            {
                drips.transform.SetPositionAndRotation(basePoint + up * (height * 0.45f) + normal * (height * 0.05f), rot);
                var shape = drips.shape;
                shape.scale = new Vector3(height * 0.45f, height * 0.6f, 0.1f);
                var em = drips.emission;
                em.rateOverTime = burning ? 30f : hunting ? 9f : 4f;
            }
        }

        private void OnDestroy()
        {
            if (glob != null) Destroy(glob);
            if (eye != null && eye != glob) Destroy(eye);
        }
    }
}
