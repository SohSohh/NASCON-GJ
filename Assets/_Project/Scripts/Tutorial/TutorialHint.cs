using System;
using NasconGJ.Core;
using NasconGJ.Fire;
using NasconGJ.Items;
using NasconGJ.Player;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Tutorial
{
    /// <summary>
    /// Trigger volume that shows one short key hint when the player walks in, and keeps it up until the
    /// player actually does the thing (or leaves). Designed so the space teaches and the text only names the key.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialHint : MonoBehaviour
    {
        public enum DismissWhen { Timer, LeaveArea, FireTransferred, Covering, Crouching, HoldingItem }

        [SerializeField] private string keys = "F";
        [SerializeField] private string text = "hold to pass the flame";
        [SerializeField] private float minDuration = 2.5f;
        [SerializeField] private DismissWhen dismissWhen = DismissWhen.Timer;
        [Tooltip("Only show if the player is carrying something in both hands.")]
        [SerializeField] private bool requireBothSlots;
        [Tooltip("Only show if the player is holding a burning item.")]
        [SerializeField] private bool requireFire;

        private bool shown;
        private bool transferred;
        private bool inside;
        private string id;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            id = System.Guid.NewGuid().ToString();
        }

        private void OnEnable() => GameEvents.FireTransferred += OnTransferred;
        private void OnDisable() => GameEvents.FireTransferred -= OnTransferred;
        private void OnTransferred(GameObject _) => transferred = true;

        private void OnTriggerEnter(Collider other)
        {
            if (shown || !other.CompareTag(GameTags.Player) || HintUI.Instance == null) return;
            var inventory = other.GetComponent<PlayerInventory>();
            if (requireBothSlots && (inventory == null || inventory.GetSlot(0) == null || inventory.GetSlot(1) == null)) return;
            if (requireFire && (inventory == null || inventory.ActiveFlame == null || !inventory.ActiveFlame.IsBurning)) return;

            shown = true;
            inside = true;
            transferred = false;
            HintUI.Instance.Show(keys, text, minDuration, BuildCondition(other.gameObject), id);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            inside = false;
            if (dismissWhen == DismissWhen.LeaveArea && HintUI.Instance != null) HintUI.Instance.Hide(id);
        }

        private Func<bool> BuildCondition(GameObject player)
        {
            var carrier = player.GetComponent<FireCarrier>();
            var controller = player.GetComponent<FirstPersonController>();
            var inventory = player.GetComponent<PlayerInventory>();
            return dismissWhen switch
            {
                DismissWhen.LeaveArea => () => !inside,
                DismissWhen.FireTransferred => () => transferred,
                DismissWhen.Covering => () => carrier != null && carrier.IsCovering,
                DismissWhen.Crouching => () => controller != null && controller.IsCrouching,
                DismissWhen.HoldingItem => () => inventory != null && inventory.ActiveItem != null,
                _ => null,
            };
        }
    }
}
