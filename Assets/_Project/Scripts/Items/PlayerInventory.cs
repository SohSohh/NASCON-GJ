using System;
using NasconGJ.Fire;
using NasconGJ.Player;
using UnityEngine;

namespace NasconGJ.Items
{
    /// <summary>
    /// Two item slots (CS:GO style). Only the active slot is visible; the other is holstered
    /// but keeps burning. Previous/Next select a slot, Drop leaves the active item on the ground.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public const int SlotCount = 2;

        [SerializeField] private Transform handSocket;
        [SerializeField] private Transform view;
        [SerializeField] private CarryableItem startingItemPrefab;
        [SerializeField] private bool igniteStartingItem = true;
        [SerializeField] private float dropForce = 1.5f;

        public event Action Changed;

        public int ActiveSlot { get; private set; }
        public CarryableItem ActiveItem => slots[ActiveSlot];
        public CarryableItem OtherItem => slots[1 - ActiveSlot];
        public FlammableItem ActiveFlame => ActiveItem != null ? ActiveItem.Flammable : null;
        public FlammableItem OtherFlame => OtherItem != null ? OtherItem.Flammable : null;

        private readonly CarryableItem[] slots = new CarryableItem[SlotCount];
        private PlayerInputReader input;

        public CarryableItem GetSlot(int index) => slots[index];

        private void Awake() => input = GetComponent<PlayerInputReader>();

        private void Start()
        {
            if (startingItemPrefab == null) return;
            var item = Instantiate(startingItemPrefab);
            PickUp(item);
            if (igniteStartingItem && item.Flammable != null) item.Flammable.Ignite();
        }

        private void Update()
        {
            if (input.PreviousPressed) Select(0);
            if (input.NextPressed) Select(1);
            if (input.DropPressed) DropActive();
        }

        public void PickUp(CarryableItem item)
        {
            int slot = slots[ActiveSlot] == null ? ActiveSlot : slots[1 - ActiveSlot] == null ? 1 - ActiveSlot : -1;
            if (slot < 0)
            {
                // both hands full: swap, leaving the current item where the new one was lying
                var old = slots[ActiveSlot];
                slots[ActiveSlot] = null;
                old.OnDropped(item.transform.position + Vector3.up * 0.05f, item.transform.rotation, Vector3.zero);
                IgnorePlayer(old);
                slot = ActiveSlot;
            }

            slots[slot] = item;
            item.OnPickedUp(handSocket);
            Select(slot);
        }

        /// <summary>A just-dropped item must never collide with (and balance on) the player's own body.</summary>
        private void IgnorePlayer(CarryableItem item)
        {
            var own = GetComponentsInChildren<Collider>();
            foreach (var c in item.GetComponentsInChildren<Collider>())
                foreach (var p in own)
                    Physics.IgnoreCollision(c, p, true);
        }

        public void Select(int slot)
        {
            ActiveSlot = Mathf.Clamp(slot, 0, SlotCount - 1);
            for (int i = 0; i < SlotCount; i++)
                if (slots[i] != null) slots[i].SetHolstered(i != ActiveSlot);
            Changed?.Invoke();
        }

        public void DropActive()
        {
            var item = ActiveItem;
            if (item == null) return;
            slots[ActiveSlot] = null;

            // set it down clear of the body (never inside it or through a wall), lying on its side
            Vector3 flatForward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            float reach = 0.85f;
            if (Physics.Raycast(view.position, flatForward, out RaycastHit wall, reach + 0.2f, ~0, QueryTriggerInteraction.Ignore) &&
                !wall.collider.transform.IsChildOf(transform))
                reach = Mathf.Max(0.35f, wall.distance - 0.25f);
            Vector3 position = view.position + flatForward * reach + Vector3.down * 0.9f;
            Quaternion rotation = Quaternion.LookRotation(flatForward) * Quaternion.Euler(0f, 0f, 90f);
            item.OnDropped(position, rotation, flatForward * dropForce * 0.5f);
            IgnorePlayer(item);

            // Auto-switch to the other slot if it holds something.
            if (OtherItem != null) Select(1 - ActiveSlot);
            else Changed?.Invoke();
        }
    }
}
