using NasconGJ.Fire;
using UnityEngine;

namespace NasconGJ.Audio
{
    /// <summary>Crackle loop that follows a flame: louder when burning bright, muffled when covered, silent when out.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class FlameAudio : MonoBehaviour
    {
        [SerializeField] private FlammableItem flame;
        [SerializeField] private float volume = 0.55f;
        [SerializeField] private AudioClip igniteClip;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (flame == null) flame = GetComponentInParent<FlammableItem>();
            source.loop = true;
            source.volume = 0f;
            source.time = Random.value * Mathf.Max(0.01f, source.clip != null ? source.clip.length - 0.1f : 0f);
        }

        private void OnEnable()
        {
            if (flame != null) flame.Ignited += OnIgnited;
        }

        private void OnDisable()
        {
            if (flame != null) flame.Ignited -= OnIgnited;
        }

        private void OnIgnited(FlammableItem _)
        {
            if (igniteClip != null) source.PlayOneShot(igniteClip, 0.8f);
        }

        private void Update()
        {
            if (flame == null) return;
            float shadowDamping = Mathf.Lerp(1f, 0.65f, flame.ShadowThreatLevel);
            float target = flame.IsLit ? volume * (flame.IsCovered ? 0.25f : 1f) * Mathf.Lerp(0.5f, 1f, flame.NormalizedFuel) * (1f + flame.Gust * 0.6f) * shadowDamping : 0f;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * 2f);
            if (source.volume > 0.001f && !source.isPlaying) source.Play();
            else if (source.volume <= 0.001f && source.isPlaying) source.Pause();
        }
    }
}
