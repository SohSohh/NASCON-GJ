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
        private FireCarrier carrier;
        private int mask;

        private System.Collections.IEnumerator Start()
        {
            // Also covers scene-local objects and items spawned after the level has loaded.
            var interval = new WaitForSeconds(1f);
            while (true)
            {
                foreach (var behaviour in FindObjectsByType<MonoBehaviour>())
                {
                    if (behaviour is not IInteractable && behaviour is not FlammableItem) continue;
                    if (behaviour.TryGetComponent<InteractableHighlight>(out var existing))
                    {
                        existing.SetPlayer(this);
                        continue;
                    }
                    behaviour.gameObject.AddComponent<InteractableHighlight>().Initialize(this);
                }
                yield return interval;
            }
        }

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            carrier = GetComponent<FireCarrier>();
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

            // small things (a stick on a ledge) are hard to hit with a thin ray: if it found nothing usable, look a little wider
            if (Current == null && LookedAtFlammable == null &&
                Physics.SphereCast(view.position, 0.14f, view.forward, out RaycastHit wide, range, mask, QueryTriggerInteraction.Ignore))
            {
                var near = wide.collider.GetComponentInParent<IInteractable>();
                if (near != null && near.CanInteract(gameObject)) Current = near;
                LookedAtFlammable = wide.collider.GetComponentInParent<FlammableItem>();
            }

            // looking at an unlit item while carrying fire, F is "hold to light it" (the ring fills), not "grab it"
            bool lightingIt = carrier != null && LookedAtFlammable != null && carrier.TransferTarget == LookedAtFlammable;
            if (Current != null && input.InteractPressed && !DialogueUI.IsOpen && !lightingIt)
                Current.Interact(gameObject);
        }
    }
}
