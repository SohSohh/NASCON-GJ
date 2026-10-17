using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// Represents an exit, safe light, objective, or fuel source that shadow enemies herd the player away from.
    /// </summary>
    public class HerdTarget : MonoBehaviour
    {
        public enum TargetType
        {
            Exit,
            SafeLight,
            Objective,
            FuelRefill
        }

        public static readonly List<HerdTarget> All = new();

        [SerializeField] private TargetType type = TargetType.Exit;
        [SerializeField] private float priority = 1f;
        [SerializeField] private float activeRadius = 35f;
        [SerializeField] private bool isActive = true;

        public TargetType Type => type;
        public float Priority => priority;
        public float ActiveRadius => activeRadius;
        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public Vector3 Position => transform.position;

        private NasconGJ.Fire.ILightSource cachedLight;
        private bool lightChecked;

        public NasconGJ.Fire.ILightSource LightSource
        {
            get
            {
                if (!lightChecked || (cachedLight as Object) == null)
                {
                    cachedLight = GetComponent<NasconGJ.Fire.ILightSource>() ?? GetComponentInChildren<NasconGJ.Fire.ILightSource>();
                    lightChecked = true;
                }
                return cachedLight;
            }
        }

        public bool IsLitLight => LightSource != null && LightSource.IsLit;
        public float EffectiveLightRadius => IsLitLight ? LightSource.Radius : 0f;

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        /// <summary>
        /// Finds the highest-priority active herd target within range of the given position.
        /// If the target is a lit light source and the observer is already inside its protected light radius,
        /// it is ignored because the player has already reached safety.
        /// </summary>
        public static HerdTarget FindPrimaryTarget(Vector3 observerPos)
        {
            HerdTarget best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < All.Count; i++)
            {
                var target = All[i];
                if (target == null || !target.isActive) continue;

                float dist = Vector3.Distance(observerPos, target.Position);
                if (dist > target.activeRadius) continue;

                // If this is a lit safe light and the player is already inside its illuminated sanctuary,
                // enemies cannot herd them away from inside the light.
                if (target.IsLitLight)
                {
                    float safeZone = target.EffectiveLightRadius * 0.65f;
                    if (dist < safeZone) continue;
                }

                // Higher priority, with closer targets preferred
                float score = (target.priority * 100f) - dist;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = target;
                }
            }

            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();
    }
}
