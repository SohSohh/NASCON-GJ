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
        private bool hinted;               // the RMB card, once per time you step into the wind

        private BoxCollider box;

        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

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

            // decide by position every frame, not by trigger events (a missed enter or exit left the flame
            // draining all over the level, or not at all)
            if (inventory == null)
            {
                var player = GameObject.FindWithTag(GameTags.Player);
                if (player != null) { inventory = player.GetComponent<PlayerInventory>(); carrier = player.GetComponent<FireCarrier>(); }
            }
            bool wasInside = inside;
            inside = inventory != null && box.bounds.Contains(inventory.transform.position + Vector3.up);
            if (inside && !wasInside) { uncoveredTime = 0f; hinted = false; }
            if (!inside) return;
            var flame = inventory.ActiveFlame;
            if (flame == null || !flame.IsBurning) return;

            bool covered = carrier != null && carrier.IsCovering;
            if (!covered && !hinted && UI.HintUI.Instance != null)
            {
                // always say how to save the flame here, even if the tutorial lesson was skipped past
                hinted = true;
                UI.HintUI.Instance.Show("RMB", "Hold to shield the flame from the wind", 1.2f,
                    () => !inside || (carrier != null && carrier.IsCovering), "draft-rmb");
            }
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
