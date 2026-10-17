using NasconGJ.Core;
using NasconGJ.Death;
using NasconGJ.Fire;
using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// A shadow that lives on surfaces (shadow behaviour board: moves on walls, wraps around geometry,
    /// transitions between surfaces, avoids direct light).
    ///
    /// It crawls across any wall, floor or pillar in <see cref="surfaceMask"/> within a leash of its start point,
    /// stalks the player's position, refuses to enter directly lit points, and recoils (with a contorted pose)
    /// when light reaches it. If it gets close while unlit it winds up, then lunges off the wall at the camera.
    ///
    /// Placement: put it a few cm in front of a surface with its blue axis pointing OUT of the surface,
    /// at the height its feet should be.
    /// </summary>
    public class ShadowEnemy : MonoBehaviour
    {
        public enum State { Lurk, Stalk, Recoil, Windup, Lunge }

        [Header("Body")]
        [SerializeField] private ShadowSurfaceSkin skin;
        [Tooltip("The 3D tar body (blobs, eyes, drips). The sprite becomes its faint cast stain.")]
        [SerializeField] private ShadowBody body;
        [SerializeField, Range(0f, 1f)] private float stainOpacity = 0.4f;
        [SerializeField] private float height = 3.2f;

        [Header("Surfaces")]
        [SerializeField] private LayerMask surfaceMask = ~0;
        [SerializeField] private float leashRadius = 9f;

        [Header("Movement")]
        [SerializeField] private float lurkSpeed = 0.35f;
        [SerializeField] private float stalkSpeed = 1.5f;
        [SerializeField] private float recoilSpeed = 3.4f;

        [Header("Senses")]
        [SerializeField] private float detectRange = 13f;
        [SerializeField] private float killRange = 1.6f;
        [SerializeField] private float windupTime = 0.55f;

        [Header("Light")]
        [Tooltip("TENTATIVE (GDD): shadows cast by props also block this enemy, not only direct light.")]
        [SerializeField] private bool blockedByCastShadows;
        [SerializeField] private LayerMask occluderMask = ~0;
        [SerializeField] private float recoilHold = 0.7f;

        [Header("FX")]
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private AudioSource whisper;
        [SerializeField] private AudioClip lungeClip;

        public State Current { get; private set; } = State.Lurk;
        public bool IsRecoiling => Current == State.Recoil;
        public Vector3 SurfacePoint => pos;

        private Vector3 pos, normal, home, homeNormal, moveDir;
        private Vector3 visualNormal, visualUp;
        private Vector3 lightPos;
        private float stateTime, animTime, litTimer, facing = 1f, wanderSeed, whisperVolume;
        private Transform player;
        private PlayerDeath death;

        private float Width => height * ShadowSheet.Aspect;
        private Vector3 Center => pos + visualUp * (height * 0.45f) + visualNormal * 0.05f;

        private void Start()
        {
            wanderSeed = Random.value * 100f;
            if (Physics.Raycast(transform.position + transform.forward * 0.6f, -transform.forward, out RaycastHit hit, 2f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point;
                normal = hit.normal;
            }
            else
            {
                pos = transform.position;
                normal = transform.forward;
            }
            home = pos;
            homeNormal = normal;
            visualNormal = normal;
            visualUp = ComputeUp(normal, Vector3.zero);
            if (skin != null) skin.SetSurfaceMask(surfaceMask);
            if (whisper != null) whisper.volume = 0f;
            FindPlayer();
        }

        private void FindPlayer()
        {
            var go = GameObject.FindWithTag(GameTags.Player);
            if (go == null) return;
            player = go.transform;
            death = go.GetComponent<PlayerDeath>();
        }

        private void Enter(State state)
        {
            Current = state;
            stateTime = 0f;
            if (state == State.Recoil && smoke != null) smoke.Emit(12);
        }

        private void Update()
        {
            if (player == null) FindPlayer();
            float dt = Time.deltaTime;
            stateTime += dt;
            animTime += dt;

            Vector3 chest = player != null ? player.position + Vector3.up * 1.1f : pos;
            float distance = player != null ? Vector3.Distance(chest, Center) : float.PositiveInfinity; // no player (menus): never hunts
            bool playerDead = death != null && death.IsDead;

            if (Current != State.Lunge)
            {
                if (IsBlocked(Center, out Vector3 lp))
                {
                    lightPos = lp;
                    litTimer = recoilHold;
                    if (Current != State.Recoil) Enter(State.Recoil);
                }
                else litTimer -= dt;
            }

            switch (Current)
            {
                case State.Lurk:
                    Wander();
                    if (!playerDead && player != null && distance < detectRange) Enter(State.Stalk);
                    break;
                case State.Stalk:
                    Move(chest - pos, stalkSpeed);
                    if (playerDead || distance > detectRange * 1.3f) Enter(State.Lurk);
                    else if (distance < killRange) Enter(State.Windup);
                    break;
                case State.Recoil:
                    Move(pos - lightPos, recoilSpeed);
                    if (litTimer <= 0f) Enter(!playerDead && distance < detectRange ? State.Stalk : State.Lurk);
                    break;
                case State.Windup:
                    if (stateTime >= windupTime)
                    {
                        if (!playerDead && death != null && distance < killRange * 1.5f) Lunge();
                        else Enter(State.Stalk);
                    }
                    break;
            }

            UpdateVisual(chest, dt);
            UpdateFx(dt);
        }

        // ------------------------------------------------------------ movement

        private void Wander()
        {
            float t = Time.time * 0.15f;
            Vector3 right = Vector3.Cross(normal, visualUp);
            Vector3 target = home + right * ((Mathf.PerlinNoise(wanderSeed, t) - 0.5f) * leashRadius) +
                             visualUp * ((Mathf.PerlinNoise(t, wanderSeed) - 0.5f) * 1.5f);
            if ((target - pos).sqrMagnitude > 0.04f) Move(target - pos, lurkSpeed);
        }

        private void Move(Vector3 direction, float speed)
        {
            float step = speed * Time.deltaTime;
            Vector3 dir = Vector3.ProjectOnPlane(direction, normal);
            if (dir.sqrMagnitude < 1e-5f) return;
            dir.Normalize();
            if (TryCrawl(dir, step)) return;
            if (TryCrawl(Quaternion.AngleAxis(55f, normal) * dir, step)) return;
            TryCrawl(Quaternion.AngleAxis(-55f, normal) * dir, step);
        }

        private bool TryCrawl(Vector3 dir, float distance)
        {
            Vector3 candidate, candidateNormal;
            if (Physics.Raycast(pos + normal * 0.08f, dir, out RaycastHit hit, distance + 0.12f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                candidate = hit.point;               // concave corner: climb onto the surface ahead
                candidateNormal = hit.normal;
            }
            else
            {
                Vector3 ahead = pos + dir * distance;
                if (Physics.Raycast(ahead + normal * 0.3f, -normal, out hit, 0.6f, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    candidate = hit.point;           // same surface
                    candidateNormal = hit.normal;
                }
                else if (Physics.Raycast(ahead - normal * 0.12f, -dir, out hit, distance + 0.3f, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    candidate = hit.point;           // convex edge: wrap around it
                    candidateNormal = hit.normal;
                }
                else return false;
            }

            if ((candidate - home).sqrMagnitude > leashRadius * leashRadius) return false;
            if (Vector3.Dot(candidateNormal, homeNormal) < -0.3f) return false; // never wrap round to the back of a wall
            if (Current != State.Recoil)
            {
                Vector3 up = ComputeUp(candidateNormal, dir);
                if (IsBlocked(candidate + up * (height * 0.45f) + candidateNormal * 0.05f, out _)) return false;
            }

            pos = candidate;
            normal = candidateNormal;
            moveDir = dir;
            return true;
        }

        private Vector3 ComputeUp(Vector3 surfaceNormal, Vector3 travel)
        {
            if (Mathf.Abs(surfaceNormal.y) < 0.65f) return Vector3.ProjectOnPlane(Vector3.up, surfaceNormal).normalized; // wall
            Vector3 flat = travel.sqrMagnitude > 1e-4f ? travel : player != null ? player.position - pos : Vector3.forward;
            flat = Vector3.ProjectOnPlane(flat, surfaceNormal);
            return flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal).normalized; // floor: lie toward target
        }

        /// <summary>True if the point is directly lit (or inside a prop's cast shadow, when that rule is on).</summary>
        private bool IsBlocked(Vector3 point, out Vector3 lightPosition)
        {
            foreach (var source in LightSourceRegistry.Active)
            {
                if (!source.IsLit) continue;
                Vector3 toLight = source.Position - point;
                float d = toLight.magnitude;
                if (d > source.Radius || d < 1e-3f) continue;
                bool occluded = Physics.Raycast(point, toLight / d, d - 0.3f, occluderMask, QueryTriggerInteraction.Ignore);
                if (!occluded || blockedByCastShadows)
                {
                    lightPosition = source.Position;
                    return true;
                }
            }
            lightPosition = default;
            return false;
        }

        // ------------------------------------------------------------ attack

        private void Lunge()
        {
            Enter(State.Lunge);
            var cam = Camera.main;
            if (skin != null) skin.Renderer.enabled = false;
            if (body != null) body.SetHidden(true);
            if (lungeClip != null && cam != null) AudioSource.PlayClipAtPoint(lungeClip, cam.transform.position, 1f);
            if (cam != null && skin != null)
                ShadowLunge.Play(skin.Renderer.sharedMaterial, ShadowSheet.CreatureFrame(ShadowSheet.Attack + 1), cam.transform,
                    () => { if (death != null) death.Kill("Taken by a shadow."); });
            else if (death != null) death.Kill("Taken by a shadow.");
        }

        // ------------------------------------------------------------ presentation

        private void UpdateVisual(Vector3 chest, float dt)
        {
            Vector3 targetUp = ComputeUp(normal, moveDir);
            visualNormal = Vector3.Slerp(visualNormal, normal, dt * 10f).normalized;
            visualUp = Vector3.Slerp(visualUp, Vector3.ProjectOnPlane(targetUp, visualNormal), dt * 6f).normalized;

            Vector3 right = Vector3.Cross(visualNormal, visualUp);
            float side = Current == State.Stalk || Current == State.Windup ? Vector3.Dot(chest - pos, right) : Vector3.Dot(moveDir, right);
            if (Mathf.Abs(side) > 0.15f) facing = Mathf.Sign(side);

            int frame;
            float glow, dissolve = 0f;
            switch (Current)
            {
                case State.Stalk: frame = ShadowSheet.Stalk + Mathf.FloorToInt(animTime * 7f) % 4; glow = 1.35f; break;
                case State.Recoil:
                    frame = ShadowSheet.Recoil + Mathf.FloorToInt(animTime * 10f) % 2;
                    glow = 0.35f + Mathf.PerlinNoise(animTime * 20f, 0f) * 0.4f;
                    dissolve = 0.22f + Mathf.Sin(animTime * 17f) * 0.08f;
                    break;
                case State.Windup: frame = ShadowSheet.Attack; glow = 1.4f + Mathf.Sin(stateTime * 30f) * 0.4f; break;
                case State.Lunge: frame = ShadowSheet.Attack + 1; glow = 2f; break;
                default: frame = ShadowSheet.Idle + Mathf.FloorToInt(animTime * 3.5f) % 4; glow = 1.1f; break;
            }

            transform.position = pos;
            if (skin != null && Current != State.Lunge)
            {
                skin.Conform(pos - visualUp * 0.04f, visualNormal, visualUp, Width, height);
                skin.SetLook(ShadowSheet.CreatureFrame(frame), facing < 0f, body != null ? 0f : glow, dissolve, body != null ? stainOpacity : 1f);
            }
            if (body != null && Current != State.Lunge)
                body.Pose(pos, visualNormal, visualUp, height, facing, Current, chest, glow, dissolve * 2.2f, dt);
        }

        private void UpdateFx(float dt)
        {
            if (smoke != null)
            {
                smoke.transform.SetPositionAndRotation(Center, Quaternion.LookRotation(visualNormal, visualUp));
                var emission = smoke.emission;
                emission.rateOverTime = Current switch
                {
                    State.Stalk => 10f,
                    State.Recoil => 26f,
                    State.Windup => 30f,
                    State.Lunge => 0f,
                    _ => 4f,
                };
            }
            if (whisper != null)
            {
                float target = Current switch
                {
                    State.Stalk => 0.55f,
                    State.Windup => 1f,
                    State.Recoil => 0.35f,
                    State.Lunge => 0f,
                    _ => 0.2f,
                };
                whisperVolume = Mathf.MoveTowards(whisperVolume, target, dt * 1.5f);
                whisper.volume = whisperVolume;
                whisper.pitch = Current == State.Recoil ? 1.35f : 1f;
                whisper.transform.position = Center;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.5f);
            Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, leashRadius);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(Application.isPlaying ? Center : transform.position + Vector3.up * height * 0.45f, killRange);
        }
    }
}
