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

        // kept for prefab compatibility (were IK rotations for the Animator version)
        [SerializeField, HideInInspector] private Vector3 rightGripEuler;
        [SerializeField, HideInInspector] private Vector3 rightGripOffset;
        [SerializeField, HideInInspector] private Vector3 leftCoverEuler;

        private FirstPersonController controller;
        private FireCarrier carrier;
        private PlayerInventory inventory;
        private ProceduralHumanoid rig;
        private Transform bodyRoot, hidden, rightHand, rightForearm;
        private float bodyBaseY, coverWeight, holdWeight;

        private void Awake()
        {
            controller = GetComponent<FirstPersonController>();
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

        /// <summary>The rig has no finger bones, so instead of trusting the IK we move the held item so its grip sits in the palm.</summary>
        private void GlueItemToHand()
        {
            var item = inventory != null ? inventory.ActiveItem : null;
            if (item == null || rightHand == null || rightForearm == null || holdWeight < 0.5f) return;
            if (rightHandTarget == null || Vector3.Distance(rightHand.position, rightHandTarget.position) > 0.12f) return;
            Vector3 along = (rightHand.position - rightForearm.position).normalized;
            Vector3 palm = rightHand.position + along * palmDistance * bodyRoot.lossyScale.y;
            Vector3 target = palm - item.transform.up * gripHeight;
            item.transform.position = Vector3.Lerp(item.transform.parent.TransformPoint(item.HoldPositionOffset), target, holdWeight);
        }
    }
}
