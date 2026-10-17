using System;
using NasconGJ.Core;
using UnityEngine;

namespace NasconGJ.Tutorial
{
    /// <summary>Trigger volume that tells a tutorial director when the player is inside.</summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialZone : MonoBehaviour
    {
        public event Action Entered;
        public event Action Exited;
        public bool PlayerInside { get; private set; }
        public bool EverEntered { get; private set; }

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            PlayerInside = true;
            EverEntered = true;
            Entered?.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            PlayerInside = false;
            Exited?.Invoke();
        }
    }
}
