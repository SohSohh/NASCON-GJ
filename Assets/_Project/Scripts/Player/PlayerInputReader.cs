using UnityEngine;
using UnityEngine.InputSystem;

namespace NasconGJ.Player
{
    /// <summary>
    /// Reads the "Player" map of InputSystem_Actions and exposes it as simple properties.
    /// Every player system reads input through this, so rebinding happens in one asset.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputActionMap map;
        private InputAction move, look, jump, sprint, crouch, interact, attack, transfer, cover, drop, previous, next;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool LookIsGamepad => look.activeControl != null && look.activeControl.device is Gamepad;

        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool SprintHeld => sprint.IsPressed();
        public bool CrouchHeld => crouch.IsPressed();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool TransferHeld => transfer.IsPressed();
        public bool CoverHeld => cover.IsPressed();
        public bool DropPressed => drop.WasPressedThisFrame();
        public bool PreviousPressed => previous.WasPressedThisFrame();
        public bool NextPressed => next.WasPressedThisFrame();

        /// <summary>Advance dialogue: Interact or Attack (left click).</summary>
        public bool AdvancePressed => interact.WasPressedThisFrame() || attack.WasPressedThisFrame();

        private void Awake()
        {
            // own copy: enabling the shared asset leaves stale state behind across editor reloads
            // ("Map index out of range" spam on the next Play)
            actions = Instantiate(actions);
            map = actions.FindActionMap("Player", true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            jump = map.FindAction("Jump", true);
            sprint = map.FindAction("Sprint", true);
            crouch = map.FindAction("Crouch", true);
            interact = map.FindAction("Interact", true);
            attack = map.FindAction("Attack", true);
            transfer = map.FindAction("TransferFire", true);
            cover = map.FindAction("CoverFire", true);
            drop = map.FindAction("Drop", true);
            previous = map.FindAction("Previous", true);
            next = map.FindAction("Next", true);
        }

        private void OnEnable() => map?.Enable();
        private void OnDisable() => map?.Disable();

        private void OnDestroy()
        {
            if (actions != null) Destroy(actions);
        }
    }
}
