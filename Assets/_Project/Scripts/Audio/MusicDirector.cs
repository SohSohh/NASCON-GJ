using NasconGJ.Death;
using NasconGJ.Entities;
using UnityEngine;

namespace NasconGJ.Audio
{
    /// <summary>
    /// Two-layer adaptive music from DOCUMENTATION/Music And Audio:
    ///   ambience (main_ambience) always under the level,
    ///   presence (presence) swells when a shadow is hunting nearby or your flame is gone, and the ambience ducks under it.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private AudioSource ambience;
        [SerializeField] private AudioSource presence;
        [SerializeField] private float ambienceVolume = 0.55f;
        [SerializeField] private float presenceVolume = 0.8f;
        [SerializeField] private float threatRange = 12f;
        [SerializeField] private float riseSpeed = 0.6f;
        [SerializeField] private float fallSpeed = 0.25f;

        private ShadowEnemy[] shadows;
        private PlayerDeath death;
        private Transform listener;
        private float threat, scanTimer;

        private void Start()
        {
            if (ambience != null) { ambience.loop = true; ambience.volume = ambienceVolume; if (!ambience.isPlaying) ambience.Play(); }
            if (presence != null) { presence.loop = true; presence.volume = 0f; if (!presence.isPlaying) presence.Play(); }
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 1f;
                shadows = FindObjectsByType<ShadowEnemy>();
                if (death == null) death = FindAnyObjectByType<PlayerDeath>();
                if (listener == null && Camera.main != null) listener = Camera.main.transform;
            }

            float target = 0f;
            if (death != null && death.InDarkness) target = 1f;
            if (listener != null && shadows != null)
            {
                foreach (var s in shadows)
                {
                    if (s == null) continue;
                    bool hunting = s.Current == ShadowEnemy.State.Approach || s.Current == ShadowEnemy.State.Drain || s.Current == ShadowEnemy.State.Herd || s.Current == ShadowEnemy.State.Windup || s.Current == ShadowEnemy.State.Lunge;
                    float closeness = 1f - Mathf.Clamp01(Vector3.Distance(s.SurfacePoint, listener.position) / threatRange);
                    target = Mathf.Max(target, closeness * (hunting ? 1f : 0.35f));
                }
            }

            threat = Mathf.MoveTowards(threat, target, Time.deltaTime * (target > threat ? riseSpeed : fallSpeed));
            if (presence != null) presence.volume = threat * presenceVolume;
            if (ambience != null) ambience.volume = ambienceVolume * (1f - threat * 0.6f);
        }
    }
}
