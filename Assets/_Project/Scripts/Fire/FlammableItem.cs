using System;
using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Fire
{
    /// <summary>
    /// Anything that can burn: held torches, sticks, candles, braziers.
    /// Burns for a fixed time (or forever), flickers, dims when covered, and registers
    /// itself as a gameplay light so shadows react to it.
    /// </summary>
    public class FlammableItem : MonoBehaviour, IFlammable, ILightSource
    {
        public static readonly List<FlammableItem> All = new();

        [SerializeField] private string displayName = "Torch";

        [Header("Burn")]
        [SerializeField] private float maxBurnTime = 90f;
        [SerializeField] private bool infiniteBurn;
        [SerializeField] private bool startLit;
        [Tooltip("Fuel left when lit at start (e.g. a torch dropped during the fall).")]
        [Range(0.05f, 1f)] [SerializeField] private float startFuelFraction = 1f;
        [Tooltip("Carryable fires count toward the player's survival (no carryable fire left = darkness).")]
        [SerializeField] private bool countsAsPlayerFire = true;

        [Header("Visuals")]
        [SerializeField] private Light flameLight;
        [SerializeField] private ParticleSystem[] flameParticles;
        [SerializeField] private float flickerAmount = 0.18f;
        [SerializeField] private float flickerSpeed = 8f;
        [Tooltip("Below this fuel fraction the flame shrinks and flickers harder.")]
        [SerializeField] private float lowFuelThreshold = 0.25f;

        [Header("Gameplay light")]
        [Tooltip("Fraction of the Light's range that counts as 'lit' for shadows.")]
        [SerializeField] private float gameplayRadiusFactor = 0.6f;
        [SerializeField] private float coveredIntensity = 0.12f;

        public event Action<FlammableItem> Ignited;
        /// <summary>0..1 extra flicker from wind this frame (set by WindDraftZone).</summary>
        public float Gust => gust;
        public event Action<FlammableItem> Extinguished;

        public string DisplayName => displayName;
        public bool IsBurning { get; private set; }
        public bool IsCovered { get; private set; }
        public bool IsHolstered { get; private set; }
        public bool InfiniteBurn => infiniteBurn;
        /// <summary>Light brightness multiplier (an item lying on the floor burns dimmer than one held up high).</summary>
        public float IntensityScale { get; set; } = 1f;
        /// <summary>Tutorial safety net: fuel never drops below this fraction while set (0 = off).</summary>
        public float MinFuelFraction { get; set; }
        public bool CountsAsPlayerFire => countsAsPlayerFire;
        public float MaxBurnTime => maxBurnTime;
        public float RemainingBurnTime { get; private set; }
        public float NormalizedFuel => infiniteBurn ? 1f : Mathf.Clamp01(RemainingBurnTime / maxBurnTime);

        // ILightSource
        public bool IsLit => IsBurning && !IsHolstered;
        public Vector3 Position => flameLight != null ? flameLight.transform.position : transform.position;
        public float Radius => flameLight != null ? flameLight.range * currentRangeScale * gameplayRadiusFactor : 0f;
        public Transform Transform => transform;

        private float baseIntensity;
        private float baseRange;
        private float[] baseEmission;
        private float coverBlend = 1f;
        private float currentRangeScale = 1f;
        private float noiseSeed;
        private float gust;

        private void Awake()
        {
            noiseSeed = UnityEngine.Random.value * 100f;
            if (flameLight != null)
            {
                baseIntensity = flameLight.intensity;
                baseRange = flameLight.range;
            }
            baseEmission = new float[flameParticles?.Length ?? 0];
            for (int i = 0; i < baseEmission.Length; i++)
                baseEmission[i] = flameParticles[i].emission.rateOverTimeMultiplier;
        }

        private void OnEnable()
        {
            All.Add(this);
            LightSourceRegistry.Register(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
            LightSourceRegistry.Unregister(this);
        }

        private void Start()
        {
            // Something may have ignited us before Start (e.g. the player's starting torch).
            if (startLit)
            {
                Ignite();
                RemainingBurnTime = maxBurnTime * startFuelFraction;
            }
            else if (!IsBurning) ApplyVisuals(false);
        }

        public void Ignite()
        {
            if (IsBurning) return;
            IsBurning = true;
            RemainingBurnTime = maxBurnTime;
            ApplyVisuals(true);
            Ignited?.Invoke(this);
        }

        public void Extinguish()
        {
            if (!IsBurning) return;
            IsBurning = false;
            IsCovered = false;
            RemainingBurnTime = 0f;
            ApplyVisuals(false);
            Extinguished?.Invoke(this);
        }

        public void SetCovered(bool covered) => IsCovered = covered && IsBurning;

        /// <summary>Set remaining fuel directly (0..1), e.g. a scripted "your torch is dying" beat.</summary>
        public void SetFuel(float fraction)
        {
            if (IsBurning && !infiniteBurn) RemainingBurnTime = maxBurnTime * Mathf.Clamp01(fraction);
        }

        /// <summary>Burn extra fuel (wind, RUSH, ...).</summary>
        public void Drain(float seconds)
        {
            if (!IsBurning || infiniteBurn) return;
            RemainingBurnTime -= seconds;
            if (MinFuelFraction > 0f) RemainingBurnTime = Mathf.Max(RemainingBurnTime, maxBurnTime * MinFuelFraction);
            if (RemainingBurnTime <= 0f) Extinguish();
        }

        /// <summary>Wind strength hitting the flame this frame (0..1). Decays on its own.</summary>
        public void SetGust(float strength) => gust = Mathf.Max(gust, Mathf.Clamp01(strength));

        /// <summary>Hidden in the second inventory slot: keeps burning but gives no light.</summary>
        public void SetHolstered(bool holstered)
        {
            IsHolstered = holstered;
            ApplyVisuals(IsBurning);
        }

        private void ApplyVisuals(bool on)
        {
            bool visible = on && !IsHolstered;
            if (flameLight != null) flameLight.enabled = visible;
            if (flameParticles == null) return;
            foreach (var ps in flameParticles)
            {
                if (ps == null) continue;
                if (visible && !ps.isPlaying) ps.Play(true);
                else if (!visible) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Update()
        {
            if (!IsBurning) return;

            if (!infiniteBurn)
            {
                RemainingBurnTime -= Time.deltaTime;
                if (MinFuelFraction > 0f) RemainingBurnTime = Mathf.Max(RemainingBurnTime, maxBurnTime * MinFuelFraction);
                if (RemainingBurnTime <= 0f)
                {
                    Extinguish();
                    return;
                }
            }

            coverBlend = Mathf.MoveTowards(coverBlend, IsCovered ? coveredIntensity : 1f, Time.deltaTime * 6f);

            float fuel = NormalizedFuel;
            float lowFuel = fuel < lowFuelThreshold ? fuel / lowFuelThreshold : 1f; // 1 -> 0 as it dies
            float flickerStrength = flickerAmount * (lowFuel < 1f ? 2.5f - 1.5f * lowFuel : 1f) + gust * 0.7f;
            gust = Mathf.MoveTowards(gust, 0f, Time.deltaTime * 2f);
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed) * 2f - 1f;

            currentRangeScale = Mathf.Lerp(0.35f, 1f, coverBlend) * Mathf.Lerp(0.55f, 1f, lowFuel);

            if (flameLight != null)
            {
                flameLight.intensity = baseIntensity * IntensityScale * coverBlend * Mathf.Lerp(0.5f, 1f, lowFuel) * (1f + noise * flickerStrength);
                flameLight.range = baseRange * currentRangeScale;
            }

            for (int i = 0; i < baseEmission.Length; i++)
            {
                var emission = flameParticles[i].emission;
                emission.rateOverTimeMultiplier = baseEmission[i] * Mathf.Max(0.15f, coverBlend * Mathf.Lerp(0.4f, 1f, lowFuel));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();
    }
}
