using NasconGJ.Fire;
using NasconGJ.Interaction;
using UnityEngine;
using UnityEngine.Rendering;

namespace NasconGJ.Items
{
    /// <summary>An item the player can pick up, hold in one of two slots, and drop as a standalone light.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarryableItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string itemName = "Torch";
        [SerializeField] private Vector3 holdPositionOffset;
        [SerializeField] private Vector3 holdRotationOffset;

        [Header("Readability")]
        [Tooltip("Warm rim on the item while it lies in the world, so it reads against dark floors.")]
        [SerializeField] private Color highlightColor = new(1f, 0.72f, 0.45f, 1f);
        [Range(0f, 1f)] [SerializeField] private float highlightStrength = 0.75f;
        [Tooltip("Flame brightness while the item lies in the world (held = 1). A flame right on the floor otherwise blows out.")]
        [Range(0.1f, 1f)] [SerializeField] private float groundLightScale = 0.4f;

        public string ItemName => itemName;
        public Vector3 HoldPositionOffset => holdPositionOffset;
        public bool IsHeld { get; private set; }
        public FlammableItem Flammable { get; private set; }

        public string Prompt => $"Pick up {itemName}";

        private Rigidbody body;
        private Collider[] colliders;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            Flammable = GetComponent<FlammableItem>();
        }

        private void Start()
        {
            SetHighlight(!IsHeld);
            if (Flammable != null) Flammable.IntensityScale = IsHeld ? 1f : groundLightScale;
        }

        /// <summary>Re-apply the resting highlight (after something else, e.g. a tutorial glint, changed it).</summary>
        public void RefreshHighlight() => SetHighlight(!IsHeld);

        /// <summary>Rim light on every mesh material (per material index so other per-material blocks survive).</summary>
        private void SetHighlight(bool on)
        {
            block ??= new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r is not MeshRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].HasProperty(RimColorId)) continue;
                    r.GetPropertyBlock(block, i);
                    block.SetColor(RimColorId, on ? highlightColor : mats[i].GetColor(RimColorId));
                    block.SetFloat(RimStrengthId, on ? highlightStrength : mats[i].GetFloat(RimStrengthId));
                    r.SetPropertyBlock(block, i);
                }
            }
        }

        public bool CanInteract(GameObject interactor) => !IsHeld;

        public void Interact(GameObject interactor)
        {
            var inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory != null) inventory.PickUp(this);
        }

        public void OnPickedUp(Transform socket)
        {
            IsHeld = true;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            foreach (var c in colliders) c.enabled = false;
            foreach (var r in renderers) r.shadowCastingMode = ShadowCastingMode.Off;
            SetHighlight(false);
            if (Flammable != null) Flammable.IntensityScale = 1f;

            transform.SetParent(socket, false);
            transform.localPosition = holdPositionOffset;
            transform.localRotation = Quaternion.Euler(holdRotationOffset);
        }

        public void OnDropped(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            IsHeld = false;
            SetHolstered(false);
            Flammable?.SetCovered(false);

            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);
            foreach (var c in colliders) c.enabled = true;
            foreach (var r in renderers) r.shadowCastingMode = ShadowCastingMode.On;
            SetHighlight(true);
            if (Flammable != null) Flammable.IntensityScale = groundLightScale;

            body.position = position; // move the physics body too, or it snaps back to where it last simulated
            body.rotation = rotation;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = velocity;
        }

        public void SetHolstered(bool holstered)
        {
            foreach (var r in renderers) r.enabled = !holstered;
            Flammable?.SetHolstered(holstered);
        }
    }
}
