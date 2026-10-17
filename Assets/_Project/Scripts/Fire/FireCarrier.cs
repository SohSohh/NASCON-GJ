using System;
using NasconGJ.Core;
using NasconGJ.Interaction;
using NasconGJ.Items;
using NasconGJ.Player;
using UnityEngine;

namespace NasconGJ.Fire
{
    /// <summary>
    /// Player-side fire mechanics from the GDD:
    ///  - Hold TransferFire while looking at an unlit flammable (or with an unlit item in the other slot) to pass the flame on.
    ///  - Hold CoverFire to shield the flame with your hand. Limited time, then it burns you and is forced open.
    /// </summary>
    public class FireCarrier : MonoBehaviour
    {
        [Header("Transfer")]
        [SerializeField] private float transferTime = 1.2f;

        [Header("Cover")]
        [SerializeField] private float maxCoverTime = 4f;
        [SerializeField] private float coverRecoveryRate = 0.75f;
        [SerializeField] private float burnCooldown = 2.5f;
        [SerializeField] private float coveredMoveSpeedMultiplier = 0.7f;

        [Header("Cover hand visual")]
        [SerializeField] private Transform view;
        [SerializeField] private Transform coverHand;
        [SerializeField] private Vector3 coverHandHiddenLocalPos = new(0.2f, -0.75f, 0.35f);
        [SerializeField] private Vector3 coverHandOffsetFromFlame = new(-0.02f, 0.06f, -0.07f);
        [SerializeField] private float coverHandSpeed = 12f;

        /// <summary>Raised when the cover is held too long and the fire bites.</summary>
        public event Action HandBurned;

        public float TransferProgress { get; private set; }
        public FlammableItem TransferTarget { get; private set; }
        public bool IsCovering { get; private set; }
        public bool IsBurnCooldown => cooldownTimer > 0f;
        /// <summary>1 = full cover time available, 0 = exhausted.</summary>
        public float CoverMeter => 1f - coverUsed / maxCoverTime;

        private PlayerInputReader input;
        private PlayerInventory inventory;
        private PlayerInteractor interactor;
        private FirstPersonController controller;
        private FlammableItem coveredFlame;
        private float coverUsed;
        private float cooldownTimer;
        private bool transferNeedsRelease;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            inventory = GetComponent<PlayerInventory>();
            interactor = GetComponent<PlayerInteractor>();
            controller = GetComponent<FirstPersonController>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                SetCover(null, false);
                return;
            }

            FlammableItem flame = inventory.ActiveFlame;
            bool hasFire = flame != null && flame.IsBurning;

            UpdateCover(flame, hasFire);
            UpdateTransfer(flame, hasFire);
            UpdateCoverHand(flame);
        }

        private void UpdateCover(FlammableItem flame, bool hasFire)
        {
            bool wantsCover = input.CoverHeld && hasFire && cooldownTimer <= 0f;

            if (wantsCover)
            {
                coverUsed += Time.deltaTime;
                if (coverUsed >= maxCoverTime)
                {
                    wantsCover = false;
                    cooldownTimer = burnCooldown;
                    HandBurned?.Invoke();
                }
            }
            else
            {
                coverUsed = Mathf.Max(0f, coverUsed - Time.deltaTime * coverRecoveryRate);
                cooldownTimer -= Time.deltaTime;
            }

            SetCover(flame, wantsCover);
        }

        private void SetCover(FlammableItem flame, bool covering)
        {
            if (coveredFlame != null && (coveredFlame != flame || !covering)) coveredFlame.SetCovered(false);
            coveredFlame = covering ? flame : null;
            if (coveredFlame != null) coveredFlame.SetCovered(true);

            if (controller != null) controller.SpeedMultiplier = covering ? coveredMoveSpeedMultiplier : 1f;

            if (covering == IsCovering) return;
            IsCovering = covering;
            GameEvents.RaiseFireCovered(covering);
        }

        private void UpdateTransfer(FlammableItem flame, bool hasFire)
        {
            TransferTarget = hasFire ? FindTransferTarget(flame) : null;

            if (!input.TransferHeld) transferNeedsRelease = false;

            if (TransferTarget != null && input.TransferHeld && !transferNeedsRelease && !IsCovering)
            {
                TransferProgress += Time.deltaTime / transferTime;
                if (TransferProgress >= 1f)
                {
                    TransferTarget.Ignite();
                    GameEvents.RaiseFireTransferred(TransferTarget.gameObject);
                    TransferProgress = 0f;
                    transferNeedsRelease = true;
                }
            }
            else
            {
                TransferProgress = 0f;
            }
        }

        private FlammableItem FindTransferTarget(FlammableItem flame)
        {
            var looked = interactor != null ? interactor.LookedAtFlammable : null;
            if (looked != null && looked != flame && !looked.IsBurning) return looked;

            var other = inventory.OtherFlame;
            if (other != null && !other.IsBurning) return other;

            return null;
        }

        private void UpdateCoverHand(FlammableItem flame)
        {
            if (coverHand == null || view == null) return;

            Vector3 targetLocal = coverHandHiddenLocalPos;
            Quaternion targetRot = Quaternion.Euler(10f, -10f, 0f);
            if (IsCovering && flame != null)
            {
                Vector3 world = flame.Position + view.TransformVector(coverHandOffsetFromFlame);
                targetLocal = coverHand.parent.InverseTransformPoint(world);
                targetRot = Quaternion.Euler(-35f, -25f, 70f);
            }

            float t = Time.deltaTime * coverHandSpeed;
            coverHand.localPosition = Vector3.Lerp(coverHand.localPosition, targetLocal, t);
            coverHand.localRotation = Quaternion.Slerp(coverHand.localRotation, targetRot, t);
        }
    }
}
