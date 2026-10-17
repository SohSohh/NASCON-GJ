using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Fire
{
    /// <summary>
    /// Global list of active gameplay lights. Light sources Register in OnEnable and
    /// Unregister in OnDisable; shadow enemies / puzzles read Active.
    /// </summary>
    public static class LightSourceRegistry
    {
        private static readonly List<ILightSource> sources = new();

        public static IReadOnlyList<ILightSource> Active => sources;

        public static void Register(ILightSource source)
        {
            if (!sources.Contains(source)) sources.Add(source);
        }

        public static void Unregister(ILightSource source) => sources.Remove(source);

        /// <summary>True if the point is inside the radius of any lit source.</summary>
        public static bool IsPointLit(Vector3 point)
        {
            foreach (var s in sources)
            {
                if (s.IsLit && (s.Position - point).sqrMagnitude <= s.Radius * s.Radius) return true;
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => sources.Clear();
    }
}
