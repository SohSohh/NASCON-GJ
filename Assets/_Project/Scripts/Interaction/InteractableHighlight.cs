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
        private static readonly int OutlineId = Shader.PropertyToID("_InteractionOutline");
        private Renderer[] renderers;
        private Material[][] materials;
        private MaterialPropertyBlock block;
        private IInteractable interactable;
        private CarryableItem carry;
        private FlammableItem flame;
        private PlayerInteractor player;
        private float appliedStrength = -1f;
        private bool outlineEnabled;

        public void SetPlayer(PlayerInteractor owner) => player = owner;

        public void Initialize(PlayerInteractor owner)
        {
            player = owner;
            interactable = GetComponent<IInteractable>();
            carry = GetComponent<CarryableItem>();
            flame = GetComponent<FlammableItem>();
            renderers = GetComponentsInChildren<Renderer>(true);
            materials = new Material[renderers.Length][];
            block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                materials[i] = renderers[i].sharedMaterials;
            }
            SetOutline(carry == null || !carry.IsHeld);
        }

        private void LateUpdate()
        {
            SetOutline(carry == null || !carry.IsHeld);
            if (player == null || !player.isActiveAndEnabled) { Apply(0); return; }
            bool available = (interactable is Behaviour behaviour && behaviour.isActiveAndEnabled && interactable.CanInteract(player.gameObject)) ||
                             (flame != null && flame.isActiveAndEnabled && !flame.IsBurning);
            if (carry != null && carry.IsHeld) available = false;
            bool focused = player.Current == interactable && interactable != null ||
                           flame != null && player.LookedAtFlammable == flame;
            Apply(available ? (focused ? 1.6f : 0.8f) : 0f);
        }

        private void OnDisable() => Apply(0f);

        private void SetOutline(bool enabled)
        {
            if (renderers == null || outlineEnabled == enabled) return;
            outlineEnabled = enabled;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                for (int m = 0; m < materials[i].Length; m++)
                {
                    var material = materials[i][m];
                    if (material == null || !material.HasProperty(OutlineId)) continue;
                    renderer.GetPropertyBlock(block, m);
                    block.SetFloat(OutlineId, enabled ? 1f : 0f);
                    renderer.SetPropertyBlock(block, m);
                }
            }
        }

        private void Apply(float strength)
        {
            if (renderers == null || Mathf.Approximately(appliedStrength, strength)) return;
            appliedStrength = strength;
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
