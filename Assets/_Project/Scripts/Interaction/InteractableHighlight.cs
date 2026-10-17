using NasconGJ.Fire;
using NasconGJ.Items;
using UnityEngine;

namespace NasconGJ.Interaction
{
    /// <summary>Per-instance readability for pickups, dialogue and fire-transfer targets.</summary>
    [DisallowMultipleComponent]
    public sealed class InteractableHighlight : MonoBehaviour
    {
        private static readonly int HighlightId = Shader.PropertyToID("_InteractionHighlight");
        private Renderer[] renderers;
        private Material[][] materials;
        private MaterialPropertyBlock block;
        private IInteractable interactable;
        private CarryableItem carry;
        private FlammableItem flame;
        private PlayerInteractor player;

        public void SetPlayer(PlayerInteractor owner) => player = owner;

        public void Initialize(PlayerInteractor owner)
        {
            player = owner;
            interactable = GetComponent<IInteractable>();
            carry = GetComponent<CarryableItem>();
            flame = GetComponent<FlammableItem>();
            renderers = GetComponentsInChildren<Renderer>(true);
            materials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++) materials[i] = renderers[i].sharedMaterials;
            block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (player == null || !player.isActiveAndEnabled) { Apply(0); return; }
            bool available = (interactable is Behaviour behaviour && behaviour.isActiveAndEnabled && interactable.CanInteract(player.gameObject)) ||
                             (flame != null && flame.isActiveAndEnabled && !flame.IsBurning);
            if (carry != null && carry.IsHeld) available = false;
            bool focused = player.Current == interactable && interactable != null ||
                           flame != null && player.LookedAtFlammable == flame;
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 2.4f);
            Apply(available ? (focused ? 1.6f : 0.8f) * pulse : 0f);
        }

        private void OnDisable() => Apply(0f);

        private void Apply(float strength)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                for (int m = 0; m < materials[i].Length; m++)
                {
                    var material = materials[i][m];
                    if (material == null || !material.HasProperty(HighlightId)) continue;
                    r.GetPropertyBlock(block, m);
                    block.SetFloat(HighlightId, strength);
                    r.SetPropertyBlock(block, m);
                }
            }
        }
    }
}
