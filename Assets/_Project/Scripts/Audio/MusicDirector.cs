using NasconGJ.Entities;
using UnityEngine;

namespace NasconGJ.Audio
{
    /// <summary>Both music layers follow the same proximity trigger as shadow whisper and fuel drain.</summary>
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private AudioSource ambience;
        [SerializeField] private AudioSource presence;
        [SerializeField] private float ambienceVolume = 0.55f;
        [SerializeField] private float presenceVolume = 0.8f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

        private void OnEnable()
        {
            Prepare(ambience);
            Prepare(presence);
        }

        private static void Prepare(AudioSource source)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.Stop();
            source.clip?.LoadAudioData();
        }

        private void Update()
        {
            bool inRange = false;
            foreach (var shadow in ShadowCoordinator.Enemies)
            {
                if (shadow != null && shadow.IsWhisperAudible)
                {
                    inRange = true;
                    break;
                }
            }

            SetPlayback(ambience, inRange, ambienceVolume, Time.deltaTime);
            SetPlayback(presence, inRange, presenceVolume, Time.deltaTime);
        }

        private void SetPlayback(AudioSource source, bool active, float volume, float deltaTime, bool immediate = false)
        {
            if (source == null) return;

            float targetVolume = active ? volume : 0f;
            if (immediate || fadeDuration <= 0f)
                source.volume = targetVolume;
            else
                source.volume = Mathf.MoveTowards(source.volume, targetVolume,
                    Mathf.Max(volume, 0.01f) / fadeDuration * deltaTime);

            if (active && source.isActiveAndEnabled && source.clip != null)
            {
                if (!source.isPlaying) source.Play();
            }
            else if (!active && source.volume <= 0.001f && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void OnDisable()
        {
            SetPlayback(ambience, false, ambienceVolume, 0f, true);
            SetPlayback(presence, false, presenceVolume, 0f, true);
        }
    }
}
