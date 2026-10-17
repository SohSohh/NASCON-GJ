using System;
using NasconGJ.Core;
using UnityEngine;

namespace NasconGJ.Player
{
    /// <summary>
    /// CharacterController-based first-person movement: walk / sprint / crouch / jump,
    /// mouse + gamepad look, head bob and footstep events.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float gamepadSensitivity = 150f;
        [SerializeField] private float pitchLimit = 85f;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.8f;
        [SerializeField] private float sprintSpeed = 4.6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float jumpHeight = 0.9f;
        [SerializeField] private float gravity = -20f;

        [Header("Crouch")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [SerializeField] private float standEyeHeight = 1.6f;
        [SerializeField] private float crouchEyeHeight = 0.95f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Head bob / footsteps")]
        [SerializeField] private float bobFrequency = 2.2f;
        [SerializeField] private float bobAmplitude = 0.035f;
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioClip[] footstepClips;

        /// <summary>Raised on every footstep. Audio, AI hearing etc. can hook in here.</summary>
        public event Action Footstep;

        public bool MovementLocked { get; set; }

        /// <summary>While true, something else (an intro, a cutscene) drives the camera root; look and head bob are paused.</summary>
        public bool ExternalCameraControl { get; set; }

        public Transform CameraRoot => cameraRoot;
        public bool IsGrounded => controller.isGrounded;
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public float HorizontalSpeed { get; private set; }

        /// <summary>External speed scale (e.g. slowed while covering fire). 1 = normal.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        private CharacterController controller;
        private PlayerInputReader input;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float pitch;
        private float bobTimer;
        private int lastStepIndex;
        private float eyeHeight;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            eyeHeight = standEyeHeight;
        }

        private void Start()
        {
            GameManager.Instance?.SetState(GameState.Playing);
            SetCursorLocked(true);
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused) return;

            if (!MovementLocked && !ExternalCameraControl) HandleLook();
            HandleMovement();
            if (!ExternalCameraControl) HandleCrouchAndBob();
        }

        private void HandleLook()
        {
            Vector2 delta = input.Look;
            delta *= (input.LookIsGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity) * GameSettings.LookSensitivity;

            transform.Rotate(Vector3.up, delta.x);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        /// <summary>Snap the view to level (used when an intro hands control back).</summary>
        public void ResetPitch() => SetPitch(0f);

        public void SetPitch(float degrees)
        {
            pitch = Mathf.Clamp(degrees, -pitchLimit, pitchLimit);
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Vector2 move = MovementLocked ? Vector2.zero : Vector2.ClampMagnitude(input.Move, 1f);

            IsCrouching = UpdateCrouchState(!MovementLocked && input.CrouchHeld);
            IsSprinting = !IsCrouching && input.SprintHeld && move.y > 0.1f;

            float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 target = (transform.right * move.x + transform.forward * move.y) * speed * SpeedMultiplier;
            planarVelocity = Vector3.MoveTowards(planarVelocity, target, acceleration * speed * Time.deltaTime);

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (!MovementLocked && !IsCrouching && input.JumpPressed)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            HorizontalSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        }

        private bool UpdateCrouchState(bool wantsCrouch)
        {
            if (!wantsCrouch && IsCrouching)
            {
                // Stay crouched if something is above our head.
                Vector3 origin = transform.position + Vector3.up * (crouchHeight - controller.radius);
                float distance = standHeight - crouchHeight + 0.05f;
                if (Physics.SphereCast(origin, controller.radius * 0.9f, Vector3.up, out _, distance,
                        ~LayerMask.GetMask(GameLayers.Player), QueryTriggerInteraction.Ignore))
                    return true;
            }

            float height = wantsCrouch ? crouchHeight : standHeight;
            controller.height = Mathf.MoveTowards(controller.height, height, crouchTransitionSpeed * Time.deltaTime);
            controller.center = Vector3.up * controller.height * 0.5f;
            return wantsCrouch;
        }

        private void HandleCrouchAndBob()
        {
            float targetEye = IsCrouching ? crouchEyeHeight : standEyeHeight;
            eyeHeight = Mathf.Lerp(eyeHeight, targetEye, crouchTransitionSpeed * Time.deltaTime);

            float bobOffset = 0f;
            if (controller.isGrounded && HorizontalSpeed > 0.2f)
            {
                bobTimer += Time.deltaTime * bobFrequency * HorizontalSpeed;
                bobOffset = -Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmplitude;

                int stepIndex = Mathf.FloorToInt(bobTimer / Mathf.PI);
                if (stepIndex != lastStepIndex)
                {
                    lastStepIndex = stepIndex;
                    PlayFootstep();
                }
            }

            cameraRoot.localPosition = new Vector3(0f, eyeHeight + bobOffset, 0f);
        }

        private void PlayFootstep()
        {
            Footstep?.Invoke();
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;
            footstepSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            footstepSource.PlayOneShot(footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)],
                IsCrouching ? 0.4f : IsSprinting ? 1f : 0.7f);
        }
    }
}
