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

        private ProceduralHumanoid rig;

        /// <summary>Walking speed in m/s for cutscenes (the legs walk in place; the mover moves the root).</summary>
        public float Speed { get; set; }
        private Vector3 handBase;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            rig = GetComponent<ProceduralHumanoid>() ?? gameObject.AddComponent<ProceduralHumanoid>();
            if (handTarget != null) handBase = handTarget.localPosition;
        }

        private void LateUpdate()
        {
            if (rig == null) return;
            float t = Time.time;
            if (handTarget != null)
                handTarget.localPosition = handBase + new Vector3(Mathf.Sin(t * 0.6f) * sway * 0.5f, Mathf.Sin(t * 1.1f) * sway, Mathf.Sin(t * 0.7f) * sway * 0.5f);
            rig.Speed = Speed;
            rig.LookTarget = lookTarget;
            rig.RightHandTarget = handTarget;
            rig.RightHandWeight = handTarget != null ? 1f : 0f;
            rig.Apply(Time.deltaTime);
        }
    }
}
