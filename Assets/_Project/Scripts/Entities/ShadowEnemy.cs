using NasconGJ.Core;
using NasconGJ.Death;
using NasconGJ.Fire;
using NasconGJ.Items;
using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// A darkness predator that crawls across surfaces (walls, floors, ceilings) to drain the player's carried flame.
    /// Pursues detected players until contact; gameplay light freezes movement and attacks.
    /// Resumes after a short darkness grace period with telegraphed visual/audio cues.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class ShadowEnemy : MonoBehaviour
    {
        public enum State
        {
            Dormant,
            Observe,
            Approach,
            Drain,
            Herd,
            Frozen,
            Windup,
            Lunge
        }

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
        [SerializeField] private float crawlSpeed = 1.5f;
        [Tooltip("Movement speed multiplier when traversing low/weak light (default 60%).")]
        [SerializeField] private float weakLightSpeedMultiplier = 0.6f;

        [Header("Senses & Distances")]
        [SerializeField] private float detectRange = 13f;
        [Tooltip("Distance in world units from the shadow center to the player. Controls ambience, presence, whisper, and accelerated fuel drain together.")]
        [InspectorName("Shadow Effect Range")]
        [SerializeField, Min(0f)] private float drainRadius = 7f;
        [SerializeField] private float fullDrainDistance = 1.75f;
        [Tooltip("Instant-kill distance to the player's chest. Frozen shadows cannot kill.")]
        [SerializeField, Min(0f)] private float killRange = 1.6f;

        [Header("Light Rules")]
        [Tooltip("Continuous light exposure (0..1) above which the enemy freezes.")]
        [SerializeField] private float freezeThreshold = 0.70f;
        [Tooltip("Grace period in seconds after losing direct light before resuming motion.")]
        [SerializeField] private float resumeDelay = 0.75f;
        [SerializeField] private LayerMask occluderMask = ~0;
        [Tooltip("If true, shadows cast by props also repel/block, not only direct light.")]
        [SerializeField] private bool blockedByCastShadows;

        [Header("Fuel Drain")]
        [Tooltip("Max burn rate multiplier added to carried flame from this enemy (+50% = 0.5).")]
        [SerializeField] private float maxDrainMultiplier = 0.5f;

        [Header("FX & Audio")]
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private AudioSource whisper;
        [SerializeField, Min(0f)] private float whisperFadeDuration = 0.35f;
        [SerializeField] private AudioClip lungeClip;

        public State Current { get; private set; } = State.Dormant;
        public bool IsFrozen => Current == State.Frozen;
        public bool IsResumeWarning => Current == State.Frozen && resumeTimer > 0f && resumeTimer < resumeDelay;
        public Vector3 SurfacePoint => pos;
        public float CurrentLightExposure { get; private set; }

        public bool IsWithinShadowEffectRange => player != null && drainRadius > 0f &&
            (Center - player.position).sqrMagnitude <= drainRadius * drainRadius;
        // The proximity warning stays audible even when torchlight freezes movement.
        private float WhisperTargetVolume => Current == State.Windup ? 1f : 0.9f;
        public bool IsWhisperAudible => isActiveAndEnabled &&
            (death == null || !death.IsDead) && IsWithinShadowEffectRange &&
            whisper != null && whisper.isActiveAndEnabled && !whisper.mute &&
            whisper.clip != null && Current != State.Lunge;
        private Vector3 pos, normal, home, homeNormal, moveDir;
        private Vector3 visualNormal, visualUp;
        private Vector3 lightPos;
        private float stateTime, animTime, resumeTimer, facing = 1f, wanderSeed, whisperVolume;
        private Transform player;
        private PlayerDeath death;
        private bool hasDetectedPlayer;

        private float Width => height * ShadowSheet.Aspect;
        public Vector3 Center => pos + visualUp * (height * 0.45f) + visualNormal * 0.05f;

        private void OnEnable()
        {
            ShadowCoordinator.Register(this);
        }

        private void OnDisable()
        {
            ShadowCoordinator.Unregister(this);
            if (whisper != null) whisper.Stop();
        }

        private void Start()
        {
            wanderSeed = Random.value * 100f;
            if (Physics.Raycast(transform.position + transform.forward * 0.6f, -transform.forward, out RaycastHit hit, 2f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point;
                normal = hit.normal;
            }
            else if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
                         out hit, 3f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                // Floor-placed prefabs have no wall behind their forward-facing transform.
                pos = hit.point;
                normal = hit.normal;
            }
            else
            {
                pos = transform.position;
                normal = Vector3.up;
            }
            home = pos;
            homeNormal = normal;
            visualNormal = normal;
            visualUp = ComputeUp(normal, Vector3.zero);
            if (skin != null) skin.SetSurfaceMask(surfaceMask);
            if (whisper != null)
            {
                whisper.volume = 0f;
                // Player warning: distance attenuation must not hide active drain.
                whisper.spatialBlend = 0f;
                whisper.loop = true;
                whisper.playOnAwake = false;
                whisper.clip?.LoadAudioData();
                whisper.Stop();
            }
            FindPlayer();
        }

        private void FindPlayer()
        {
            var go = GameObject.FindWithTag(GameTags.Player);
            if (go == null) return;
            player = go.transform;
            death = go.GetComponentInChildren<PlayerDeath>() ?? go.GetComponentInParent<PlayerDeath>();
        }

        public void Awaken()
        {
            hasDetectedPlayer = true;
            if (Current == State.Dormant) Enter(State.Approach);
        }

        private void Enter(State state)
        {
            if (Current == state) return;
            Current = state;
            stateTime = 0f;
            if (state == State.Frozen && smoke != null) smoke.Emit(8);
        }

        private void Update()
        {
            if (player == null) FindPlayer();
            float dt = Time.deltaTime;
            stateTime += dt;
            if (Current != State.Frozen) animTime += dt;

            Vector3 chest = player != null ? player.position + Vector3.up * 1.1f : pos;
            float distance = player != null ? Vector3.Distance(chest, Center) : float.PositiveInfinity;
            bool playerDead = death != null && death.IsDead;

            // Light always wins over pursuit and contact, including after a movement step.
            UpdateLightFreeze(dt);
            if (!playerDead && player != null && Current != State.Lunge)
            {
                if (distance <= detectRange) hasDetectedPlayer = true;
                if (!IsFrozen)
                {
                    if (hasDetectedPlayer)
                    {
                        Enter(distance <= drainRadius ? State.Drain : State.Approach);
                        if (!TryKillPlayer(chest))
                        {
                            // Seek the player's feet so wall shadows descend onto the floor.
                            Move(player.position - pos, crawlSpeed);
                            UpdateLightFreeze(0f);
                            TryKillPlayer(chest);
                        }
                    }
                    else Wander(lurkSpeed * 0.5f);
                }
            }

            UpdateVisual(chest, dt);
            UpdateFx(dt);
        }

        private void UpdateLightFreeze(float dt)
        {
            float feet = SampleLightExposure(pos + normal * 0.12f, out Vector3 feetLight);
            float bodyExposure = SampleLightExposure(Center, out Vector3 bodyLight);
            float surfaceExposure = SamplePointFullExposure(pos, normal);
            CurrentLightExposure = Mathf.Max(surfaceExposure, Mathf.Max(feet, bodyExposure));
            bool lit = CurrentLightExposure > 0f &&
                       CurrentLightExposure >= freezeThreshold;
            if (Current == State.Lunge) return;
            if (lit)
            {
                lightPos = feet > bodyExposure ? feetLight : bodyLight;
                resumeTimer = 0f;
                moveDir = Vector3.zero;
                Enter(State.Frozen);
            }
            else if (IsFrozen)
            {
                resumeTimer += dt;
                if (resumeTimer >= resumeDelay)
                {
                    resumeTimer = 0f;
                    Enter(hasDetectedPlayer ? State.Approach : State.Dormant);
                }
            }
        }

        private bool TryKillPlayer(Vector3 chest)
        {
            if (IsFrozen || Current == State.Lunge || death == null || death.IsDead ||
                (Center - chest).sqrMagnitude > killRange * killRange) return false;
            // Contact cannot pass through a wall or other solid level geometry.
            Vector3 toPlayer = chest - Center;
            foreach (var hit in Physics.RaycastAll(Center, toPlayer.normalized, toPlayer.magnitude,
                         occluderMask, QueryTriggerInteraction.Ignore))
            {
                // A player's own capsule or carried item is contact, not a wall.
                if (hit.transform.IsChildOf(player) || hit.transform.IsChildOf(transform)) continue;
                return false;
            }
            Lunge();
            return true;
        }
        // ------------------------------------------------------------ movement & light

        private void Wander(float speed)
        {
            float t = Time.time * 0.15f;
            Vector3 right = Vector3.Cross(normal, visualUp);
            Vector3 target = home + right * ((Mathf.PerlinNoise(wanderSeed, t) - 0.5f) * leashRadius) +
                             visualUp * ((Mathf.PerlinNoise(t, wanderSeed) - 0.5f) * 1.5f);
            if ((target - pos).sqrMagnitude > 0.04f) Move(target - pos, speed);
        }

        private void Move(Vector3 direction, float speed)
        {
            // Low light slows movement speed to 60%
            if (CurrentLightExposure > 0.25f && CurrentLightExposure < freezeThreshold)
            {
                speed *= weakLightSpeedMultiplier;
            }

            if (IsFrozen) return;
            float step = Mathf.Min(speed * Time.deltaTime, direction.magnitude);
            Vector3 dir = Vector3.ProjectOnPlane(direction, normal);
            if (dir.sqrMagnitude < 1e-5f && hasDetectedPlayer)
                dir = Vector3.ProjectOnPlane(Vector3.down, normal);
            if (dir.sqrMagnitude < 1e-5f) return;
            dir.Normalize();

            Vector3 dirL = Quaternion.AngleAxis(55f, normal) * dir;
            Vector3 dirR = Quaternion.AngleAxis(-55f, normal) * dir;

            // Prioritize crawl directions with lowest light exposure
            TryCrawlDarkest(dir, dirL, dirR, step);
        }

        private void TryCrawlDarkest(Vector3 d1, Vector3 d2, Vector3 d3, float step)
        {
            // Test viability and sample light for candidates
            (Vector3 dir, float exposure, bool valid)[] candidates =
            {
                EvaluateCrawlCandidate(d1, step),
                EvaluateCrawlCandidate(d2, step),
                EvaluateCrawlCandidate(d3, step)
            };

            // Find valid candidate with minimal light exposure
            int bestIndex = -1;
            float lowestExp = float.PositiveInfinity;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!candidates[i].valid) continue;
                if (candidates[i].exposure < lowestExp)
                {
                    lowestExp = candidates[i].exposure;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                TryCrawl(candidates[bestIndex].dir, step);
            }
        }

        private (Vector3 dir, float exposure, bool valid) EvaluateCrawlCandidate(Vector3 dir, float distance)
        {
            Vector3 ahead = pos + dir * distance;
            if (Physics.Raycast(pos + normal * 0.08f, dir, out RaycastHit hit, distance + 0.12f, surfaceMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(ahead + normal * 0.3f, -normal, out hit, 0.6f, surfaceMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(ahead - normal * 0.12f, -dir, out hit, distance + 0.3f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                if (!hasDetectedPlayer && (hit.point - home).sqrMagnitude > leashRadius * leashRadius) return (dir, 1f, false);
                if (!hasDetectedPlayer && Vector3.Dot(hit.normal, homeNormal) < -0.3f) return (dir, 1f, false);

                float exp = SamplePointFullExposure(hit.point, hit.normal);
                // Avoid crawling directly into freeze-level or near-freeze direct light
                float crawlLimit = freezeThreshold - 0.18f;
                if (!hasDetectedPlayer && exp >= crawlLimit) return (dir, exp, false);
                return (dir, exp, true);
            }
            return (dir, 1f, false);
        }

        private bool TryCrawl(Vector3 dir, float distance)
        {
            Vector3 candidate, candidateNormal;
            if (Physics.Raycast(pos + normal * 0.08f, dir, out RaycastHit hit, distance + 0.12f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                candidate = hit.point;
                candidateNormal = hit.normal;
            }
            else
            {
                Vector3 ahead = pos + dir * distance;
                if (Physics.Raycast(ahead + normal * 0.3f, -normal, out hit, 0.6f, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    candidate = hit.point;
                    candidateNormal = hit.normal;
                }
                else if (Physics.Raycast(ahead - normal * 0.12f, -dir, out hit, distance + 0.3f, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    candidate = hit.point;
                    candidateNormal = hit.normal;
                }
                else return false;
            }

            if (!hasDetectedPlayer && (candidate - home).sqrMagnitude > leashRadius * leashRadius) return false;
            if (!hasDetectedPlayer && Vector3.Dot(candidateNormal, homeNormal) < -0.3f) return false;

            pos = candidate;
            normal = candidateNormal;
            moveDir = dir;
            return true;
        }

        private Vector3 ComputeUp(Vector3 surfaceNormal, Vector3 travel)
        {
            if (Mathf.Abs(surfaceNormal.y) < 0.65f) return Vector3.ProjectOnPlane(Vector3.up, surfaceNormal).normalized;
            Vector3 flat = travel.sqrMagnitude > 1e-4f ? travel : player != null ? player.position - pos : Vector3.forward;
            flat = Vector3.ProjectOnPlane(flat, surfaceNormal);
            return flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal).normalized;
        }

        /// <summary>
        /// Samples continuous light exposure (0..1) across all active gameplay light sources.
        /// </summary>
        public float SampleLightExposure(Vector3 point, out Vector3 dominantLightPos)
        {
            float totalExposure = 0f;
            dominantLightPos = default;
            float maxExposure = 0f;

            var activeSources = LightSourceRegistry.Active;
            for (int i = 0; i < activeSources.Count; i++)
            {
                var source = activeSources[i];
                if (source == null || !source.IsLit) continue;

                Vector3 toLight = source.Position - point;
                float d = toLight.magnitude;
                if (source.Radius <= 0f || d >= source.Radius) continue;
                if (d < 1e-3f) { dominantLightPos = source.Position; return 1f; }

                bool occluded = Physics.Raycast(point, toLight / d, d - 0.25f, occluderMask, QueryTriggerInteraction.Ignore);
                if (occluded && !blockedByCastShadows) continue;

                float exposure = 1f - Mathf.Clamp01(d / source.Radius);
                totalExposure = Mathf.Clamp01(totalExposure + exposure);

                if (exposure > maxExposure)
                {
                    maxExposure = exposure;
                    dominantLightPos = source.Position;
                }
            }

            return totalExposure;
        }

        /// <summary>
        /// Samples full exposure across feet and body center, matching the freeze-evaluation logic.
        /// </summary>
        public float SamplePointFullExposure(Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            float expFeet = SampleLightExposure(surfacePoint + surfaceNormal * 0.12f, out _);
            float expBody = SampleLightExposure(surfacePoint + surfaceNormal * 0.65f, out _);
            return Mathf.Max(expFeet, expBody);
        }

        // ------------------------------------------------------------ fuel drain model

        /// <summary>
        /// Calculates the fuel drain multiplier contributed by this enemy to the carried flame.
        /// </summary>
        public float CalculateDrainMultiplier(Vector3 playerChest)
        {
            if (Current == State.Frozen || Current == State.Dormant || Current == State.Observe) return 0f;

            float dist = Vector3.Distance(Center, playerChest);
            if (dist >= drainRadius) return 0f;

            float proximity = 1f - Mathf.Clamp01((dist - fullDrainDistance) / (drainRadius - fullDrainDistance));
            float stateWeight = Current switch
            {
                State.Approach => 0.25f,
                State.Drain => 1.0f,
                State.Herd => 0.75f,
                _ => 0f
            };

            return maxDrainMultiplier * proximity * stateWeight;
        }

        /// <summary>
        /// Proximity threat level used for flame flickering and crackle reduction.
        /// </summary>
        public float CalculateThreatLevel(Vector3 playerChest)
        {
            if (Current == State.Frozen || Current == State.Dormant) return 0f;
            float dist = Vector3.Distance(Center, playerChest);
            if (dist >= detectRange) return 0f;

            float proximity = 1f - Mathf.Clamp01(dist / detectRange);
            return Current switch
            {
                State.Drain => proximity * 1.0f,
                State.Herd => proximity * 0.8f,
                State.Approach => proximity * 0.6f,
                State.Observe => proximity * 0.25f,
                _ => 0f
            };
        }

        // ------------------------------------------------------------ attack

        private void Lunge()
        {
            if (IsFrozen || death == null || death.IsDead) return;
            Enter(State.Lunge);
            death.Kill("Taken by a shadow.");
            var cam = Camera.main;
            if (skin != null) skin.Renderer.enabled = false;
            if (body != null) body.SetHidden(true);
            if (lungeClip != null && cam != null && IsWithinShadowEffectRange)
                AudioSource.PlayClipAtPoint(lungeClip, cam.transform.position, 1f);
            if (cam != null && skin != null)
                ShadowLunge.Play(skin.Renderer.sharedMaterial, ShadowSheet.CreatureFrame(ShadowSheet.Attack + 1), cam.transform,
                    null);

        }

        // ------------------------------------------------------------ presentation

        private void UpdateVisual(Vector3 chest, float dt)
        {
            Vector3 targetUp = ComputeUp(normal, moveDir);
            visualNormal = Vector3.Slerp(visualNormal, normal, dt * 10f).normalized;
            visualUp = Vector3.Slerp(visualUp, Vector3.ProjectOnPlane(targetUp, visualNormal), dt * 6f).normalized;

            Vector3 right = Vector3.Cross(visualNormal, visualUp);
            float side = (Current == State.Approach || Current == State.Drain || Current == State.Windup)
                ? Vector3.Dot(chest - pos, right)
                : Vector3.Dot(moveDir, right);
            if (Mathf.Abs(side) > 0.15f) facing = Mathf.Sign(side);

            int frame;
            float glow = 1.1f, dissolve = 0f;
            switch (Current)
            {
                case State.Observe:
                case State.Approach:
                    frame = ShadowSheet.Stalk + Mathf.FloorToInt(animTime * 6f) % 4;
                    glow = 1.25f;
                    break;
                case State.Drain:
                    frame = ShadowSheet.Stalk + Mathf.FloorToInt(animTime * 8f) % 4;
                    glow = 1.4f;
                    break;
                case State.Herd:
                    frame = ShadowSheet.Stalk + Mathf.FloorToInt(animTime * 7f) % 4;
                    glow = 1.5f; // Enhanced readability on side routes
                    break;
                case State.Frozen:
                    frame = ShadowSheet.Idle;
                    glow = IsResumeWarning ? 0.9f : 0.35f;
                    dissolve = IsResumeWarning ? 0.05f : 0.18f;
                    break;
                case State.Windup:
                    frame = ShadowSheet.Attack;
                    glow = 1.4f + Mathf.Sin(stateTime * 30f) * 0.4f;
                    break;
                case State.Lunge:
                    frame = ShadowSheet.Attack + 1;
                    glow = 2f;
                    break;
                default: // Dormant
                    frame = ShadowSheet.Idle + Mathf.FloorToInt(animTime * 2f) % 4;
                    glow = 0.9f;
                    break;
            }

            transform.position = pos;
            if (skin != null && Current != State.Lunge)
            {
                skin.Conform(pos - visualUp * 0.04f, visualNormal, visualUp, Width, height);
                skin.SetLook(ShadowSheet.CreatureFrame(frame), facing < 0f, body != null ? 0f : glow, dissolve, body != null ? stainOpacity : 1f);
            }
            if (body != null && Current != State.Lunge)
            {
                body.Pose(pos, visualNormal, visualUp, height, facing, Current, chest, glow, dissolve * 2.2f, dt, IsResumeWarning);
            }
        }

        private void UpdateFx(float dt)
        {
            if (smoke != null)
            {
                smoke.transform.SetPositionAndRotation(Center, Quaternion.LookRotation(visualNormal, visualUp));
                var emission = smoke.emission;
                emission.rateOverTime = Current switch
                {
                    State.Frozen => IsResumeWarning ? 3f : 0f,
                    State.Drain => 18f,
                    State.Herd => 12f,
                    State.Approach => 8f,
                    State.Observe => 4f,
                    State.Windup => 30f,
                    State.Lunge => 0f,
                    _ => 1f // Dormant
                };
            }

            if (whisper != null)
            {
                bool whisperAudible = IsWhisperAudible;
                float targetVolume = whisperAudible ? WhisperTargetVolume : 0f;
                if (whisperFadeDuration <= 0f)
                    whisperVolume = targetVolume;
                else
                    whisperVolume = Mathf.MoveTowards(whisperVolume, targetVolume,
                        Mathf.Max(WhisperTargetVolume, 0.01f) / whisperFadeDuration * dt);

                whisper.volume = whisperVolume;
                whisper.pitch = Current == State.Frozen ? 0.75f : 1f;
                whisper.transform.position = Center;
                if (whisperAudible && whisperVolume > 0.001f && !whisper.isPlaying)
                {
                    whisper.Play();
                }
                else if (whisperVolume <= 0.001f && whisper.isPlaying) whisper.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.5f);
            Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, leashRadius);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(Application.isPlaying ? Center : transform.position + Vector3.up * height * 0.45f, drainRadius);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(Application.isPlaying ? Center : transform.position + Vector3.up * height * 0.45f, fullDrainDistance);
        }
    }
}
