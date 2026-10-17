using NasconGJ.Fire;
using NasconGJ.Items;
using UnityEngine;
using UnityEngine.Rendering;

namespace NasconGJ.Player
{
    /// <summary>
    /// Full-body first person driven by <see cref="ProceduralHumanoid"/> (no animation clips):
    /// the legs walk with your real speed, the torso breathes and leans with the view, the right hand is
    /// IK'd onto the held item (which is then glued into the palm), the left hand rises to shield the flame,
    /// and the head + neck are collapsed so the camera sits behind the eyes. Look down and you see yourself.
    /// </summary>
    public class FirstPersonBody : MonoBehaviour
    {
        [SerializeField] private Animator bodyAnimator;
        [SerializeField] private Transform view;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandCoverTarget;
        [Tooltip("Unused by the procedural rig; kept so old prefabs keep their reference.")]
        [SerializeField] private RuntimeAnimatorController bodyController;

        [Header("Fit")]
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float backOffset = 0.12f;
        [Tooltip("Solve backOffset from arm length so the hand can just reach the hold point.")]
        [SerializeField] private bool autoFitReach = true;
        [Tooltip("Body sits this much lower than the eyes, so arms rise from below the frame instead of crossing it.")]
        [SerializeField] private float dropBelowEyes = 0.14f;
        [SerializeField] private Material firstPersonMaterial;

        [Header("Hands")]
        [Tooltip("How far past the wrist (along the forearm) the palm centre sits, in metres.")]
        [SerializeField] private float palmDistance = 0.07f;
        [Tooltip("Height of the grip above the held item's pivot (matches CarryableItem hold offsets).")]
        [SerializeField] private float gripHeight = 0.12f;
        [Tooltip("0 = keep the item's hold rotation, 1 = turn the handle to run through the fist (across the forearm).")]
        [SerializeField, Range(0f, 1f)] private float gripAlign = 0.85f;

        [Header("Walls")]
        [Tooltip("Pull the hand (and the torch) in toward the chest when a wall is closer than the arm reaches.")]
        [SerializeField] private float wallClearance = 0.14f;
        [SerializeField] private Vector3 tuckedOffset = new(0.07f, -0.2f, -0.2f);

        // kept for prefab compatibility (were IK rotations for the Animator version)
        [SerializeField, HideInInspector] private Vector3 rightGripEuler;
        [SerializeField, HideInInspector] private Vector3 rightGripOffset;
        [SerializeField, HideInInspector] private Vector3 leftCoverEuler;

        private FirstPersonController controller;
        private FireCarrier carrier;
        private PlayerInventory inventory;
        private ProceduralHumanoid rig;
        private Transform bodyRoot, hidden, rightHand, rightForearm;
        private float bodyBaseY, coverWeight, holdWeight, retract;
        private Vector3 socketBase;
        private bool socketInit;
        private int wallMask;

        private void Awake()
        {
            controller = GetComponent<FirstPersonController>();
            wallMask = ~LayerMask.GetMask("Player", "Ignore Raycast", "UI");
            carrier = GetComponent<FireCarrier>();
            inventory = GetComponent<PlayerInventory>();
            if (bodyAnimator == null) return;

            bodyRoot = bodyAnimator.transform.parent != null && bodyAnimator.transform.parent != transform ? bodyAnimator.transform.parent : bodyAnimator.transform;
            rightHand = bodyAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            rightForearm = bodyAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            hidden = bodyAnimator.GetBoneTransform(HumanBodyBones.Neck) ?? bodyAnimator.GetBoneTransform(HumanBodyBones.Head);
            FitToEyeHeight();

            rig = bodyRoot.GetComponent<ProceduralHumanoid>() ?? bodyRoot.gameObject.AddComponent<ProceduralHumanoid>();

            foreach (var r in bodyAnimator.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off; // the torch sits against the body; self-shadow would black out the arm
                if (firstPersonMaterial != null)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = firstPersonMaterial;
                    r.sharedMaterials = mats;
                }
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }
        }

        private void FitToEyeHeight()
        {
            var head = bodyAnimator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null) return;
            float headY = transform.InverseTransformPoint(head.position).y;
            if (headY > 0.1f) bodyRoot.localScale *= (eyeHeight - dropBelowEyes) / headY;
            bodyRoot.localPosition = new Vector3(0f, bodyRoot.localPosition.y, 0f);
            bodyBaseY = bodyRoot.localPosition.y;
            if (autoFitReach) backOffset = SolveBackOffset();
            bodyRoot.localPosition = new Vector3(0f, bodyBaseY, -backOffset);
        }

        /// <summary>Slide the body back until the right hand reaches the hold point at ~88% arm extension (bind pose, standing).</summary>
        private float SolveBackOffset()
        {
            var upper = bodyAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (upper == null || rightForearm == null || rightHand == null || rightHandTarget == null || view == null) return backOffset;
            float armLength = Vector3.Distance(upper.position, rightForearm.position) + Vector3.Distance(rightForearm.position, rightHand.position);
            Vector3 shoulder = transform.InverseTransformPoint(upper.position);
            Vector3 target = new Vector3(0f, eyeHeight, 0f) + view.InverseTransformPoint(rightHandTarget.position);
            float reach = armLength * 0.88f;
            float dx = target.x - shoulder.x, dy = target.y - shoulder.y;
            float dzSq = reach * reach - dx * dx - dy * dy;
            if (dzSq <= 0f) return 0f;
            return Mathf.Clamp(shoulder.z - (target.z - Mathf.Sqrt(dzSq)), 0f, 0.45f);
        }

        private void LateUpdate()
        {
            if (rig == null || !rig.enabled) return;
            float dt = Time.deltaTime;

            // no separate crouch pose needed: sink the whole body with the camera so the shoulders stay under the eyes
            if (controller != null)
            {
                float sink = controller.CameraRoot.localPosition.y - eyeHeight;
                bodyRoot.localPosition = new Vector3(0f, bodyBaseY + Mathf.Min(0f, sink), -backOffset);
            }

            coverWeight = Mathf.MoveTowards(coverWeight, carrier != null && carrier.IsCovering ? 1f : 0f, dt * 6f);
            holdWeight = Mathf.MoveTowards(holdWeight, inventory != null && inventory.ActiveItem != null ? 1f : 0f, dt * 5f);

            RetractFromWalls(dt);
            rig.Speed = controller != null ? controller.HorizontalSpeed : 0f;
            rig.LookPitch = view != null ? NormalizePitch(view.parent != null ? view.parent.localEulerAngles.x : view.localEulerAngles.x) : 0f;
            rig.RightHandTarget = rightHandTarget;
            rig.RightHandWeight = holdWeight;
            rig.LeftHandTarget = leftHandCoverTarget;
            rig.LeftHandWeight = coverWeight;
            rig.Apply(dt);

            if (hidden != null) hidden.localScale = Vector3.one * 0.001f;
            GlueItemToHand();
        }

        /// <summary>Hide the body for scripted camera moves that would put the camera inside it (lying on the floor).</summary>
        public void SetBodyVisible(bool visible)
        {
            if (bodyAnimator == null) return;
            foreach (var r in bodyAnimator.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        }

        private static float NormalizePitch(float x) => x > 180f ? x - 360f : x;

        /// <summary>
        /// The hold point sits ahead of the capsule, so up against a wall the arm and torch would sink into it.
        /// Cast along the arm; when something is closer than the reach, tuck the hand (and the item) in toward the chest.
        /// </summary>
        private void RetractFromWalls(float dt)
        {
            if (rightHandTarget == null || view == null) return;
            if (!socketInit) { socketBase = rightHandTarget.localPosition; socketInit = true; }
            Vector3 dir = view.TransformDirection(new Vector3(socketBase.x * 0.5f, socketBase.y * 0.3f, socketBase.z)).normalized;
            float reach = socketBase.magnitude + 0.2f; // hand plus the torch head in front of it
            float want = 0f;
            if (Physics.SphereCast(view.position, 0.09f, dir, out var hit, reach + wallClearance, wallMask, QueryTriggerInteraction.Ignore))
                want = Mathf.Clamp01((reach + wallClearance - hit.distance) / reach);
            // also feel straight ahead, so a wall met head-on (not along the arm) tucks the hand too
            if (Physics.SphereCast(view.position, 0.12f, view.forward, out var front, reach, wallMask, QueryTriggerInteraction.Ignore))
                want = Mathf.Max(want, Mathf.Clamp01((reach - front.distance) / reach));
            retract = Mathf.MoveTowards(retract, want, dt * (want > retract ? 6f : 2.5f)); // snap in, ease back out
            float k = Mathf.SmoothStep(0f, 1f, retract);
            rightHandTarget.localPosition = socketBase + tuckedOffset * k;
        }

        /// <summary>
        /// The rig has no finger bones, so instead of trusting the IK we move the held item into the palm and turn it
        /// so the handle runs through the closed fist (across the forearm, mostly upright), never through the wrist.
        /// </summary>
        private void GlueItemToHand()
        {
            var item = inventory != null ? inventory.ActiveItem : null;
            if (item == null || rightHand == null || rightForearm == null || holdWeight < 0.01f) return;
            if (rightHandTarget == null || Vector3.Distance(rightHand.position, rightHandTarget.position) > 0.4f) return;
            Vector3 along = (rightHand.position - rightForearm.position).normalized;
            Vector3 viewUp = view != null ? view.up : Vector3.up;
            Vector3 up = Vector3.ProjectOnPlane(viewUp, along);
            up = up.sqrMagnitude < 1e-4f ? viewUp : Vector3.Slerp(up.normalized, Vector3.up, 0.35f).normalized;
            Vector3 fwd = Vector3.ProjectOnPlane(view != null ? view.forward : transform.forward, up);
            Quaternion grip = Quaternion.LookRotation(fwd.sqrMagnitude < 1e-4f ? transform.forward : fwd, up);
            Quaternion hold = item.transform.parent.rotation * Quaternion.Euler(item.HoldRotationOffset);
            item.transform.rotation = Quaternion.Slerp(hold, grip, gripAlign * holdWeight);

            Vector3 palm = rightHand.position + along * palmDistance * bodyRoot.lossyScale.y;
            Vector3 target = palm - item.transform.up * gripHeight;
            item.transform.position = Vector3.Lerp(item.transform.parent.TransformPoint(item.HoldPositionOffset), target, holdWeight);
        }
    }
}
