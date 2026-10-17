using System.Collections.Generic;
using NasconGJ.Core;
using NasconGJ.Death;
using NasconGJ.Fire;
using NasconGJ.Items;
using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// Coordinates all active shadow enemies in the scene.
    /// Manages aggregate fuel drain on the player's active carried flame (capped at +150%),
    /// while each shadow independently pursues the player.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ShadowCoordinator : MonoBehaviour
    {
        private static ShadowCoordinator instance;
        public static ShadowCoordinator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<ShadowCoordinator>();
                    if (instance == null)
                    {
                        var go = new GameObject("ShadowCoordinator");
                        instance = go.AddComponent<ShadowCoordinator>();
                    }
                }
                return instance;
            }
        }

        private static readonly List<ShadowEnemy> enemies = new();
        public static IReadOnlyList<ShadowEnemy> Enemies => enemies;

        [Header("Drain Limits")]
        [Tooltip("Max stacked carried-flame burn rate multiplier from all enemies combined (+150% = 1.5).")]
        [SerializeField] private float maxStackedDrain = 5f;
        private float drainLook;

        // Change this value to tune how fast the held flame burns inside a Shadow's effect range.
        public const float TorchBurnRateInShadowRange = 5f;

        private Transform player;
        private PlayerInventory inventory;
        private PlayerDeath death;

        public static void Register(ShadowEnemy enemy)
        {
            if (!enemies.Contains(enemy)) enemies.Add(enemy);
            _ = Instance; // ensure coordinator is awake
        }

        public static void Unregister(ShadowEnemy enemy)
        {
            enemies.Remove(enemy);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            FindPlayer();
        }

        private void FindPlayer()
        {
            var go = GameObject.FindWithTag(GameTags.Player);
            if (go == null) return;
            player = go.transform;
            inventory = go.GetComponent<PlayerInventory>();
            death = go.GetComponent<PlayerDeath>();
        }

        private void Update()
        {
            if (player == null || inventory == null)
            {
                FindPlayer();
                if (player == null || inventory == null) return;
            }

            Vector3 chest = player.position + Vector3.up * 1.1f;
            bool playerDead = death != null && death.IsDead;

            UpdateDrain(chest, playerDead);
        }

        private void UpdateDrain(Vector3 chest, bool playerDead)
        {
            FlammableItem activeFlame = inventory.ActiveFlame;

            if (playerDead || activeFlame == null || !activeFlame.IsBurning)
            {
                // Reset drain if player has no active burning item
                if (activeFlame != null)
                {
                    activeFlame.BurnRateMultiplier = 1f;
                    activeFlame.ShadowThreatLevel = 0f;
                }
                if (drainLook > 0f) { drainLook = 0f; ComicLook.SetDrain(0f); }
                return;
            }

            float totalDrainMultiplier = 0f;
            float maxThreat = 0f;
            bool shadowInTorchRange = false;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;

                bool shadowAudible = enemy.IsWhisperAudible;
                shadowInTorchRange |= shadowAudible;
                if (shadowAudible)
                    totalDrainMultiplier += enemy.CalculateDrainMultiplier(chest);

                float threat = enemy.CalculateThreatLevel(chest);
                if (threat > maxThreat) maxThreat = threat;
            }

            // Cap stacked drain (e.g. +150% max burn rate)
            float clampedDrain = Mathf.Min(totalDrainMultiplier, maxStackedDrain);
            float burnRate = 1f + clampedDrain;
            if (shadowInTorchRange) burnRate = Mathf.Max(burnRate, TorchBurnRateInShadowRange);
            activeFlame.BurnRateMultiplier = burnRate;
            activeFlame.ShadowThreatLevel = maxThreat;
            // you can see it feeding: the frame darkens and pulls in while the flame is being drunk
            drainLook = Mathf.MoveTowards(drainLook, Mathf.Clamp01(clampedDrain / 3f) * 0.6f, Time.deltaTime * 1.5f);
            ComicLook.SetDrain(drainLook);

            // Ensure other/holstered items in inventory are unaffected
            FlammableItem otherFlame = inventory.OtherFlame;
            if (otherFlame != null)
            {
                otherFlame.BurnRateMultiplier = 1f;
                otherFlame.ShadowThreatLevel = 0f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            enemies.Clear();
        }
    }
}
