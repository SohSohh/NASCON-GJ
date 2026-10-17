using System.Collections;
using NasconGJ.Core;
using NasconGJ.Fire;
using NasconGJ.Player;
using UnityEngine;

namespace NasconGJ.Death
{
    /// <summary>
    /// GDD death rules: when no player fire is left anywhere, the player is in darkness.
    /// Death is not instant. An unseen seeker spawns at a random distance and walks toward the player,
    /// who can only track it by sound. When it arrives the player dies and the level reloads.
    /// Anything else (shadow enemies, RUSH) can kill directly through Kill().
    /// </summary>
    public class PlayerDeath : MonoBehaviour
    {
        [Header("Darkness seeker")]
        [SerializeField] private float seekerMinDistance = 14f;
        [SerializeField] private float seekerMaxDistance = 26f;
        [SerializeField] private float seekerSpeed = 1.3f;
        [SerializeField] private float seekerKillDistance = 1.2f;
        [Tooltip("3D looping source that follows the seeker (whispers, breathing).")]
        [SerializeField] private AudioSource seekerAudio;
        [Tooltip("Looping heartbeat that speeds up as the seeker closes in.")]
        [SerializeField] private AudioSource heartbeat;

        [Header("Death")]
        [SerializeField] private float reloadDelay = 3.5f;
        [SerializeField] private float fireCheckInterval = 0.25f;

        public bool InDarkness { get; private set; }
        public bool IsDead { get; private set; }
        public string DeathReason { get; private set; }
        public Vector3 SeekerPosition { get; private set; }

        private float checkTimer;

        private void Update()
        {
            if (IsDead) return;

            if (!InDarkness)
            {
                checkTimer -= Time.deltaTime;
                if (checkTimer > 0f) return;
                checkTimer = fireCheckInterval;
                if (!AnyPlayerFireBurning()) EnterDarkness();
                return;
            }

            Vector3 target = transform.position;
            SeekerPosition = Vector3.MoveTowards(SeekerPosition, new Vector3(target.x, SeekerPosition.y, target.z), seekerSpeed * Time.deltaTime);
            if (seekerAudio != null) seekerAudio.transform.position = SeekerPosition;
            if (heartbeat != null)
            {
                float closeness = 1f - Mathf.Clamp01(Vector3.Distance(SeekerPosition, target) / seekerMaxDistance);
                heartbeat.pitch = Mathf.Lerp(0.85f, 1.7f, closeness);
                heartbeat.volume = Mathf.Lerp(0.35f, 1f, closeness);
            }

            Vector3 flat = SeekerPosition - target;
            flat.y = 0f;
            if (flat.magnitude <= seekerKillDistance) Kill("The darkness found you.");
        }

        public static bool AnyPlayerFireBurning()
        {
            foreach (var fire in FlammableItem.All)
                if (fire.CountsAsPlayerFire && fire.IsBurning) return true;
            return false;
        }

        private void EnterDarkness()
        {
            InDarkness = true;
            Vector2 dir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(seekerMinDistance, seekerMaxDistance);
            SeekerPosition = transform.position + new Vector3(dir.x, 1f, dir.y) * distance;
            if (seekerAudio != null)
            {
                seekerAudio.transform.position = SeekerPosition;
                if (seekerAudio.clip != null) seekerAudio.Play();
            }
            if (heartbeat != null && heartbeat.clip != null) heartbeat.Play();
            GameEvents.RaiseFireExtinguished();
        }

        public void Kill(string reason)
        {
            if (IsDead) return;
            IsDead = true;
            DeathReason = reason;

            var controller = GetComponent<FirstPersonController>();
            if (controller != null) controller.MovementLocked = true;
            if (seekerAudio != null) seekerAudio.Stop();
            if (heartbeat != null) heartbeat.Stop();

            GameEvents.RaisePlayerDied();
            StartCoroutine(ReloadAfterDelay());
        }

        private IEnumerator ReloadAfterDelay()
        {
            var prefab = Resources.Load<DeathScare>("DeathScare");
            if (prefab != null && prefab.IsConfigured)
            {
                var scare = Instantiate(prefab);
                yield return scare.Play();
            }
            else
            {
                Debug.LogError("DeathScare prefab or its images/audio are missing.");
                yield return new WaitForSecondsRealtime(reloadDelay);
            }
            SceneLoader.ReloadCurrent();
        }
    }
}
