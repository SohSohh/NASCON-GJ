using UnityEngine;

namespace NasconGJ.Player
{
    /// <summary>
    /// "Holding the torch up" for non-playable uses of the character (main menu, cutscenes), fully procedural:
    /// breathing idle from <see cref="ProceduralHumanoid"/>, right hand IK'd to a target that holds a lit torch,
    /// head and chest turned toward a look target, with a slow sway so it never looks frozen.
    /// </summary>
    public class TorchBearerPose : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("Unused by the procedural rig; kept so old prefabs keep their reference.")]
        [SerializeField] private RuntimeAnimatorController controller;
        [SerializeField] private Transform handTarget;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private Vector3 gripEuler;
        [SerializeField] private float sway = 0.015f;
        [Tooltip("Held item; snapped so its grip sits in the palm after the arm is posed (no hand through the handle).")]
        [SerializeField] private Transform heldItem;
        [SerializeField] private float gripHeight = 0.12f;
        [SerializeField] private float palmDistance = 0.075f;

        private ProceduralHumanoid rig;
        private Transform carried, leftGrip, rightGrip;
        private float raiseBlend;

        /// <summary>
        /// Something carried in both hands (the crystal): cradled at the chest, or lifted overhead as
        /// <see cref="Raise"/> goes 0 → 1. Null = the usual one-handed torch hold.
        /// </summary>
        public Transform Carried
        {
            get => carried;
            set
            {
                carried = value;
                if (carried == null || leftGrip != null) return;
                leftGrip = new GameObject("LeftGrip").transform;
                rightGrip = new GameObject("RightGrip").transform;
                leftGrip.SetParent(transform, false);
                rightGrip.SetParent(transform, false);
            }
        }

        /// <summary>0 = cradled at the chest, 1 = held high above the head with both arms.</summary>
        public float Raise { get; set; }

        /// <summary>Walking speed in m/s for cutscenes (the legs walk in place; the mover moves the root).</summary>
        public float Speed { get; set; }

        /// <summary>What the head and chest turn toward (cutscenes retarget it shot by shot).</summary>
        public Transform LookTarget { get => lookTarget; set => lookTarget = value; }

        /// <summary>False = hands free (the torch is hidden and the arm eases down). Cutscenes toggle it.</summary>
        public bool Holding
        {
            get => holding;
            set
            {
                holding = value;
                if (heldItem == null) return;
                heldItem.gameObject.SetActive(value);
                if (!value) return;
                foreach (var ps in heldItem.GetComponentsInChildren<ParticleSystem>(true)) ps.Play(true);  // relight the flame
                foreach (var l in heldItem.GetComponentsInChildren<Light>(true)) l.enabled = true;
                foreach (var a in heldItem.GetComponentsInChildren<AudioSource>(true)) if (a.loop && !a.isPlaying) a.Play();
            }
        }
        private bool holding = true;
        private float holdBlend = 1f;
        private Vector3 handBase;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            rig = GetComponent<ProceduralHumanoid>() ?? gameObject.AddComponent<ProceduralHumanoid>();
            if (handTarget != null) handBase = handTarget.localPosition;
            if (heldItem == null && handTarget != null)
            {
                var item = handTarget.GetComponentInChildren<Items.CarryableItem>(true);
                if (item != null) heldItem = item.transform;
            }
        }

        private void LateUpdate()
        {
            if (rig == null) return;
            float t = Time.time;
            if (handTarget != null)
                handTarget.localPosition = handBase + new Vector3(Mathf.Sin(t * 0.6f) * sway * 0.5f, Mathf.Sin(t * 1.1f) * sway, Mathf.Sin(t * 0.7f) * sway * 0.5f);
            rig.Speed = Speed;
            rig.LookTarget = lookTarget;
            if (carried != null)
            {
                // both hands on it: at the chest while walking, then lifted high (the head follows it up)
                raiseBlend = Mathf.MoveTowards(raiseBlend, Raise, Time.deltaTime * 1.6f);
                float k = Mathf.SmoothStep(0f, 1f, raiseBlend);
                Vector3 local = Vector3.Lerp(new Vector3(0f, 1.12f, 0.36f), new Vector3(0f, 1.9f, 0.14f), k);    // metres, on the body (raised: within arm's reach)
                Vector3 bob = new Vector3(0f, Mathf.Sin(t * 1.3f) * 0.012f + Mathf.Sin(t * 5.5f) * 0.004f * Mathf.Min(Speed, 1f), 0f);
                carried.position = transform.position + transform.rotation * local + bob;
                carried.Rotate(Vector3.up, 25f * Time.deltaTime, Space.World);
                float spread = Mathf.Lerp(0.13f, 0.11f, k);
                Vector3 grip = carried.position - Vector3.up * 0.05f;   // hands round its middle, not under it
                leftGrip.position = grip - transform.right * spread;
                rightGrip.position = grip + transform.right * spread;
                rig.RightHandTarget = rightGrip;
                rig.LeftHandTarget = leftGrip;
                rig.RightHandWeight = 1f;
                rig.LeftHandWeight = 1f;
                if (k > 0.3f && lookTarget == null) rig.LookTarget = carried;
                rig.Apply(Time.deltaTime);
                return;
            }
            rig.RightHandTarget = handTarget;
            rig.LeftHandWeight = 0f;
            holdBlend = Mathf.MoveTowards(holdBlend, holding ? 1f : 0f, Time.deltaTime * 2f);
            rig.RightHandWeight = handTarget != null ? holdBlend : 0f;
            rig.Apply(Time.deltaTime);
            GlueHeldItem();
        }

        private void GlueHeldItem()
        {
            if (heldItem == null || animator == null || !holding) return;
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var fore = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (hand == null || fore == null) return;
            Vector3 along = (hand.position - fore.position).normalized;
            float scale = transform.lossyScale.y;
            Vector3 palm = hand.position + along * palmDistance * scale;
            // handle runs through the closed fist: roughly perpendicular to the forearm, mostly upright
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, along).normalized;
            if (up.sqrMagnitude < 0.01f) up = Vector3.up;
            up = Vector3.Slerp(up, Vector3.up, 0.5f).normalized;
            heldItem.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, up), up);
            heldItem.position = palm - up * gripHeight * scale;
        }
    }
}
