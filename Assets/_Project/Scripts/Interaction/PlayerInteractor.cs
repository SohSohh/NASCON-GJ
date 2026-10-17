using NasconGJ.Core;
using NasconGJ.Fire;
using NasconGJ.Player;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Interaction
{
    /// <summary>Raycasts from the camera to find what the player is looking at and handles Interact presses.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private float range = 2.6f;

        public IInteractable Current { get; private set; }
        public FlammableItem LookedAtFlammable { get; private set; }

        private PlayerInputReader input;
        private int mask;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            mask = ~LayerMask.GetMask(GameLayers.Player, "Ignore Raycast", GameLayers.ShadowEnemy);
        }

        private void Update()
        {
            Current = null;
            LookedAtFlammable = null;

            if (Physics.Raycast(view.position, view.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Collide))
            {
                Current = hit.collider.GetComponentInParent<IInteractable>();
                LookedAtFlammable = hit.collider.GetComponentInParent<FlammableItem>();
                if (Current != null && !Current.CanInteract(gameObject)) Current = null;
            }

            if (Current != null && input.InteractPressed && !DialogueUI.IsOpen)
                Current.Interact(gameObject);
        }
    }
}
