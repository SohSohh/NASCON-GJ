using NasconGJ.Core;
using NasconGJ.Items;
using UnityEngine;

namespace NasconGJ.Fire
{
    /// <summary>
    /// A draft blowing through a crack. Inside it an uncovered flame gutters and burns down fast;
    /// covering it with your hand (CoverFire) protects it. Teaches the cover mechanic before RUSH.
    /// With <see cref="extinguishUncovered"/> it blows the flame out entirely after a grace period.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WindDraftZone : MonoBehaviour
    {
        [SerializeField] private float fuelDrainPerSecond = 5f;
        [SerializeField] private bool extinguishUncovered;
        [SerializeField] private float extinguishGrace = 1.4f;
        [SerializeField] private AudioSource windAudio;

        private PlayerInventory inventory;
        private FireCarrier carrier;
        private float uncoveredTime;
        private bool inside;

        private void Awake() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            inventory = other.GetComponent<PlayerInventory>();
            carrier = other.GetComponent<FireCarrier>();
            inside = true;
            uncoveredTime = 0f;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(GameTags.Player)) inside = false;
        }

        private void Update()
        {
            if (windAudio != null)
                windAudio.volume = Mathf.MoveTowards(windAudio.volume, inside ? 0.9f : 0.35f, Time.deltaTime);

            if (!inside || inventory == null) return;
            var flame = inventory.ActiveFlame;
            if (flame == null || !flame.IsBurning) return;

            bool covered = carrier != null && carrier.IsCovering;
            flame.SetGust(covered ? 0.15f : 1f);
            if (covered)
            {
                uncoveredTime = 0f;
                return;
            }

            uncoveredTime += Time.deltaTime;
            flame.Drain(fuelDrainPerSecond * Time.deltaTime);
            if (extinguishUncovered && uncoveredTime > extinguishGrace) flame.Extinguish();
        }
    }
}
