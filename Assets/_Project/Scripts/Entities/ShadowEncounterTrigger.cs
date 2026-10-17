using NasconGJ.Core;
using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// Trigger volume that awakens one or more dormant shadow enemies when the player crosses it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShadowEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private ShadowEnemy[] enemiesToAwaken;
        [SerializeField] private bool triggerOnce = true;

        private bool triggered;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered && triggerOnce) return;
            if (!other.CompareTag(GameTags.Player)) return;

            triggered = true;
            if (enemiesToAwaken != null)
            {
                foreach (var enemy in enemiesToAwaken)
                {
                    if (enemy != null) enemy.Awaken();
                }
            }
        }
    }
}
