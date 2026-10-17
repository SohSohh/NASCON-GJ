using NasconGJ.Core;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>Fades a looping bed in while the player is inside this trigger and out when they leave.</summary>
    [RequireComponent(typeof(Collider))]
    public class AmbienceZone : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private float volume = 0.6f;
        [SerializeField] private float fadeIn = 4f, fadeOut = 3f;

        private bool inside;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2; // Ignore Raycast: the pickup/look ray goes straight through
            if (source != null) { source.volume = 0f; source.loop = true; }
        }

        private void OnTriggerEnter(Collider other) { if (other.CompareTag(GameTags.Player)) inside = true; }
        private void OnTriggerExit(Collider other) { if (other.CompareTag(GameTags.Player)) inside = false; }

        private void Update()
        {
            if (source == null) return;
            float target = inside ? volume : 0f;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * volume / (inside ? fadeIn : fadeOut));
            if (source.volume > 0.001f && !source.isPlaying) source.Play();
            else if (source.volume <= 0.001f && source.isPlaying && !inside) source.Stop();
        }
    }
}
