using UnityEngine;

namespace NasconGJ.Fire
{
    /// <summary>
    /// Any light that matters to gameplay (held torch, dropped item, wall torch, guiding light).
    /// The shadow system queries these to decide where shadows are cast and what enemies avoid.
    /// </summary>
    public interface ILightSource
    {
        bool IsLit { get; }
        Vector3 Position { get; }
        float Radius { get; }
        Transform Transform { get; }
        bool IsCarried { get; }
    }

    /// <summary>
    /// Implement on objects that can hold the player's fire (torches, candles, sticks...).
    /// </summary>
    public interface IFlammable
    {
        bool IsBurning { get; }

        /// <summary>Seconds of fire left; 0 when extinguished.</summary>
        float RemainingBurnTime { get; }

        void Ignite();

        void Extinguish();
    }
}
