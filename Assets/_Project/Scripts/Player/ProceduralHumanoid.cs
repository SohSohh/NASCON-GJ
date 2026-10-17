using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Player
{
    /// <summary>
    /// Fully procedural humanoid animation, no clips. Builds a pose every frame in Unity's humanoid
    /// muscle space (so it works on any humanoid rig) from a few layered signals:
    ///   idle (breathing, weight shift, arms relaxed), walk/run cycle (legs, knees, counter-swinging arm,
    ///   torso twist), crouch, look pitch,
    /// then solves analytic two-bone IK for the hands (right hand to the held item, left hand to shield the flame).
    /// Call <see cref="Apply"/> once per frame from LateUpdate (owner decides order).
    /// </summary>
    public class ProceduralHumanoid : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Header("Gait")]
        [SerializeField] private float strideFrequency = 1.7f;   // cycles per metre-per-second, scaled
        [SerializeField] private float legSwing = 0.55f;
        [SerializeField] private float kneeBend = 0.75f;
        [SerializeField] private float armSwing = 0.35f;

        [Header("Feet (planted, IK)")]
        [Tooltip("Plant the feet on the ground and step with two-bone IK (natural walking, no sliding).")]
        [SerializeField] private bool footIK = true;
        [SerializeField] private float stepHeight = 0.1f;
        [SerializeField] private LayerMask groundMask = ~(1 << 2); // everything but Ignore Raycast

        [Header("Idle")]
        [SerializeField] private float breathRate = 0.28f;
        [SerializeField] private float breathDepth = 0.08f;

        /// <summary>Horizontal speed in m/s.</summary>
        public float Speed { get; set; }
        /// <summary>0 = standing, 1 = deep crouch.</summary>
        public float Crouch { get; set; }
        /// <summary>Degrees, positive looks down (camera convention).</summary>
        public float LookPitch { get; set; }
        /// <summary>Optional world point the head/chest turn toward (overrides pitch).</summary>
        public Transform LookTarget { get; set; }

        public Transform RightHandTarget { get; set; }
        public float RightHandWeight { get; set; }
        public Transform LeftHandTarget { get; set; }
        public float LeftHandWeight { get; set; }

        public Animator Animator => animator;
        public Transform Bone(HumanBodyBones b) => animator != null ? animator.GetBoneTransform(b) : null;

        private HumanPoseHandler handler;
        private HumanPose pose;
        private Vector3 bindBodyPosition;
        private Quaternion bindBodyRotation;
        private readonly Dictionary<string, int> muscle = new();
        private float phase, breath, walkBlend, time;
        private Transform rUpper, rLower, rHand, lUpper, lLower, lHand, head, chest;
        private readonly Transform[] legUpper = new Transform[2], legLower = new Transform[2], legFoot = new Transform[2];
        private readonly Quaternion[] footRest = new Quaternion[2];
        private float ankleLocal, spreadLocal, gait, bob;
        private Vector3 lastPos;
        private bool hasLast;
        private struct Foot { public Vector3 planted, from, land; public bool swinging, init; }
        private readonly Foot[] feet = new Foot[2];

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                enabled = false;
                return;
            }
            animator.enabled = false; // we drive the skeleton ourselves
            handler = new HumanPoseHandler(animator.avatar, animator.transform);
            handler.GetHumanPose(ref pose);
            // Re-reading body position/rotation every frame drifts; pin them to the bind values instead.
            bindBodyPosition = pose.bodyPosition;
            bindBodyRotation = pose.bodyRotation;
            for (int i = 0; i < HumanTrait.MuscleCount; i++) muscle[HumanTrait.MuscleName[i]] = i;

            rUpper = Bone(HumanBodyBones.RightUpperArm);
            rLower = Bone(HumanBodyBones.RightLowerArm);
            rHand = Bone(HumanBodyBones.RightHand);
            lUpper = Bone(HumanBodyBones.LeftUpperArm);
            lLower = Bone(HumanBodyBones.LeftLowerArm);
            lHand = Bone(HumanBodyBones.LeftHand);
            head = Bone(HumanBodyBones.Head);
            chest = Bone(HumanBodyBones.Chest) ?? Bone(HumanBodyBones.Spine);
            time = Random.value * 10f;

            // legs: 0 = left, 1 = right. Ankle height and stance width measured from the bind pose.
            legUpper[0] = Bone(HumanBodyBones.LeftUpperLeg); legLower[0] = Bone(HumanBodyBones.LeftLowerLeg); legFoot[0] = Bone(HumanBodyBones.LeftFoot);
            legUpper[1] = Bone(HumanBodyBones.RightUpperLeg); legLower[1] = Bone(HumanBodyBones.RightLowerLeg); legFoot[1] = Bone(HumanBodyBones.RightFoot);
            if (legFoot[0] == null || legFoot[1] == null || legUpper[0] == null || legUpper[1] == null) footIK = false;
            else
            {
                Vector3 l = transform.InverseTransformPoint(legFoot[0].position), r = transform.InverseTransformPoint(legFoot[1].position);
                ankleLocal = Mathf.Max(0.02f, (l.y + r.y) * 0.5f);
                spreadLocal = Mathf.Clamp(Mathf.Abs(l.x - r.x) * 0.5f, 0.06f, 0.2f);
                for (int i = 0; i < 2; i++) footRest[i] = Quaternion.Inverse(transform.rotation) * legFoot[i].rotation;
            }
        }

        public void Apply(float dt)
        {
            if (handler == null) return;
            time += dt;

            float run = Mathf.InverseLerp(3.2f, 4.6f, Speed);
            walkBlend = Mathf.MoveTowards(walkBlend, Mathf.Clamp01(Speed / 1.2f), dt * 4f);
            phase += dt * Mathf.Lerp(0.9f, 1.25f, run) * strideFrequency * Mathf.Max(Speed, 0.01f) / 1.4f * Mathf.PI;
            breath = Mathf.Sin(time * breathRate * Mathf.PI * 2f);
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float w = walkBlend;
            float swing = legSwing * Mathf.Lerp(1f, 1.35f, run);

            var m = pose.muscles;
            for (int i = 0; i < m.Length; i++) m[i] = 0f;

            // ---- spine: breathing, lean into motion, crouch hunch, look pitch
            float pitch = Mathf.Clamp(LookPitch / 80f, -1f, 1f);
            Set(m, "Spine Front-Back", 0.06f + breath * breathDepth * 0.4f + w * 0.08f + run * 0.1f + Crouch * 0.35f + pitch * 0.15f);
            Set(m, "Chest Front-Back", breath * breathDepth + Crouch * 0.2f + pitch * 0.2f);
            Set(m, "UpperChest Front-Back", breath * breathDepth * 0.6f + pitch * 0.15f);
            Set(m, "Spine Twist Left-Right", s * 0.08f * w);
            Set(m, "Chest Twist Left-Right", -s * 0.1f * w);
            Set(m, "Neck Nod Down-Up", -pitch * 0.25f);
            Set(m, "Head Nod Down-Up", -pitch * 0.45f + breath * 0.02f);
            Set(m, "Spine Left-Right", Mathf.Sin(time * 0.37f) * 0.04f * (1f - w)); // idle weight shift

            // ---- legs: walk cycle (left/right in antiphase), soft knees when idle, crouch
            float idleKnee = 0.05f + Mathf.Sin(time * 0.37f) * 0.03f;
            for (int side = 0; side < 2; side++)
            {
                string L = side == 0 ? "Left" : "Right";
                float ls = side == 0 ? s : -s;
                float lc = side == 0 ? c : -c;
                float lift = Mathf.Max(0f, lc); // knee bends while the leg swings forward
                if (footIK)
                {
                    // a little bend so the knee plane is defined; the IK below places the feet
                    Set(m, $"{L} Upper Leg Front-Back", 0.1f);
                    Set(m, $"{L} Lower Leg Stretch", 0.75f);
                    continue;
                }
                Set(m, $"{L} Upper Leg Front-Back", ls * swing * w + Crouch * 0.85f);
                Set(m, $"{L} Upper Leg In-Out", 0.08f);
                Set(m, $"{L} Lower Leg Stretch", 1f - (idleKnee + lift * kneeBend * w + Crouch * 1.1f));
                Set(m, $"{L} Foot Up-Down", -ls * 0.25f * w + Crouch * 0.4f);
            }

            // ---- arms: relaxed, the left counter-swings when walking (the right usually holds something)
            for (int side = 0; side < 2; side++)
            {
                string A = side == 0 ? "Left" : "Right";
                float swingDir = side == 0 ? -s : s;
                Set(m, $"{A} Shoulder Down-Up", -0.35f + breath * 0.04f);
                Set(m, $"{A} Arm Down-Up", -0.85f);
                Set(m, $"{A} Arm Front-Back", 0.12f + swingDir * armSwing * w);
                Set(m, $"{A} Arm Twist In-Out", 0.1f);
                Set(m, $"{A} Forearm Stretch", 0.75f - w * 0.15f);
                Set(m, $"{A} Forearm Twist In-Out", 0.2f);
                Set(m, $"{A} Hand Down-Up", 0.05f);
            }

            pose.muscles = m;
            pose.bodyPosition = bindBodyPosition + Vector3.up * (footIK ? -Crouch * 0.35f - 0.03f - 0.02f * w + bob : -Crouch * 0.35f + Mathf.Abs(c) * 0.02f * w);
            pose.bodyRotation = bindBodyRotation;
            handler.SetHumanPose(ref pose);

            if (footIK) SolveLegs(dt);

            // ---- look at a world target (menus/cutscenes)
            if (LookTarget != null && head != null && chest != null)
            {
                RotateToward(chest, LookTarget.position, 0.25f);
                RotateToward(head, LookTarget.position, 0.6f);
            }

            // ---- hands
            if (RightHandTarget != null && RightHandWeight > 0.001f)
                TwoBoneIK(rUpper, rLower, rHand, RightHandTarget.position, transform.right * 0.6f - transform.up, RightHandWeight);
            if (LeftHandTarget != null && LeftHandWeight > 0.001f)
                TwoBoneIK(lUpper, lLower, lHand, LeftHandTarget.position, -transform.right * 0.6f - transform.up, LeftHandWeight);
        }

        /// <summary>
        /// Walk by planting feet: each foot stays locked to the ground in stance, then swings in an arc to a landing
        /// spot half a stride ahead of its hip. Cadence comes from the real ground speed, so the feet never slide.
        /// </summary>
        private void SolveLegs(float dt)
        {
            Vector3 pos = transform.position;
            Vector3 vel = hasLast && dt > 1e-5f ? (pos - lastPos) / dt : Vector3.zero;
            lastPos = pos;
            hasLast = true;
            vel.y = 0f;
            float speed = vel.magnitude;
            if (speed > 8f) // teleported
            {
                speed = 0f;
                vel = Vector3.zero;
                feet[0].init = feet[1].init = false;
            }

            float scale = transform.lossyScale.y;
            float ankle = ankleLocal * scale, spread = spreadLocal * scale;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 moveDir = speed > 0.05f ? vel / speed : fwd;
            float stride = Mathf.Clamp(0.3f + speed * 0.32f, 0.3f, 1.0f);
            bool walking = speed > 0.08f;
            if (walking) gait += speed * dt / (2f * stride);
            bob = walking ? -Mathf.Abs(Mathf.Cos(gait * Mathf.PI * 2f)) * Mathf.Clamp01(speed / 1.5f) * 0.035f : Mathf.MoveTowards(bob, 0f, dt * 0.2f);

            for (int i = 0; i < 2; i++)
            {
                Vector3 hip = pos + right * (i == 0 ? -spread : spread);
                Foot f = feet[i];
                if (!f.init) { f.planted = Ground(hip, pos.y); f.init = true; f.swinging = false; }

                Vector3 target;
                float p = Mathf.Repeat(gait + i * 0.5f, 1f);
                if (walking)
                {
                    f.land = Ground(hip + moveDir * stride * 0.5f, pos.y);
                    if (p >= 0.5f)
                    {
                        if (!f.swinging) { f.from = f.planted; f.swinging = true; }
                        float k = (p - 0.5f) * 2f;
                        target = Vector3.Lerp(f.from, f.land, Mathf.SmoothStep(0f, 1f, k)) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * stepHeight);
                    }
                    else
                    {
                        if (f.swinging) { f.planted = f.land; f.swinging = false; }
                        target = f.planted;
                    }
                }
                else
                {
                    // standing: settle each foot back under its hip with a small lift
                    Vector3 rest = Ground(hip, pos.y);
                    float d = Vector3.Distance(f.planted, rest);
                    if (d > 0.01f) f.planted = Vector3.MoveTowards(f.planted, rest, dt * 1.2f);
                    f.swinging = false;
                    target = f.planted + Vector3.up * (Mathf.Min(d, 0.12f) * 0.6f);
                }
                feet[i] = f;

                Vector3 pole = fwd + right * (i == 0 ? -0.15f : 0.15f);
                TwoBoneIK(legUpper[i], legLower[i], legFoot[i], target + Vector3.up * ankle, pole, 1f);
                float toe = f.swinging ? Mathf.Sin(p * Mathf.PI * 2f) * 12f : 0f;
                legFoot[i].rotation = Quaternion.AngleAxis(toe, right) * transform.rotation * footRest[i];
            }
        }

        private Vector3 Ground(Vector3 point, float fallbackY)
        {
            if (Physics.Raycast(point + Vector3.up * 0.6f, Vector3.down, out RaycastHit hit, 1.4f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return new Vector3(point.x, fallbackY, point.z);
        }

        private void Set(float[] m, string name, float value)
        {
            if (muscle.TryGetValue(name, out int i)) m[i] = Mathf.Clamp(value, -1f, 1f);
        }

        private static void RotateToward(Transform bone, Vector3 target, float weight)
        {
            Vector3 forward = bone.parent != null ? bone.parent.forward : bone.forward;
            Vector3 to = (target - bone.position).normalized;
            Quaternion delta = Quaternion.FromToRotation(forward, to);
            bone.rotation = Quaternion.Slerp(Quaternion.identity, delta, weight) * bone.rotation;
        }

        /// <summary>Analytic two-bone IK (law of cosines). <paramref name="pole"/> = direction the elbow should bend toward.</summary>
        public static void TwoBoneIK(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 pole, float weight)
        {
            if (upper == null || lower == null || end == null) return;
            Vector3 a = upper.position, b = lower.position, c = end.position;
            Vector3 t = Vector3.Lerp(c, target, Mathf.Clamp01(weight));
            float lab = (b - a).magnitude, lcb = (c - b).magnitude;
            float lat = Mathf.Clamp((t - a).magnitude, 0.01f, lab + lcb - 0.001f);

            float acAb0 = Angle(c - a, b - a);
            float baBc0 = Angle(a - b, c - b);
            float acAt0 = Angle(c - a, t - a);
            float acAb1 = Mathf.Acos(Mathf.Clamp((lcb * lcb - lab * lab - lat * lat) / (-2f * lab * lat), -1f, 1f));
            float baBc1 = Mathf.Acos(Mathf.Clamp((lat * lat - lab * lab - lcb * lcb) / (-2f * lab * lcb), -1f, 1f));

            Vector3 axis0 = Vector3.Cross(c - a, pole).normalized;
            if (axis0.sqrMagnitude < 1e-6f) axis0 = Vector3.Cross(c - a, Vector3.up).normalized;
            Vector3 axis1 = Vector3.Cross(c - a, t - a);
            axis1 = axis1.sqrMagnitude > 1e-8f ? axis1.normalized : axis0;

            lower.rotation = Quaternion.AngleAxis((baBc1 - baBc0) * Mathf.Rad2Deg, axis0) * lower.rotation;
            upper.rotation = Quaternion.AngleAxis(acAt0 * Mathf.Rad2Deg, axis1) * Quaternion.AngleAxis((acAb1 - acAb0) * Mathf.Rad2Deg, axis0) * upper.rotation;
        }

        private static float Angle(Vector3 x, Vector3 y) =>
            Mathf.Acos(Mathf.Clamp(Vector3.Dot(x.normalized, y.normalized), -1f, 1f));
    }
}
