using System;
using UnityEngine;

namespace NasconGJ.Core
{
    /// <summary>Player-facing settings, persisted in PlayerPrefs and applied on boot.</summary>
    public static class GameSettings
    {
        private const string SensitivityKey = "nascon.lookSensitivity";
        private const string VolumeKey = "nascon.masterVolume";
        private const string BrushKey = "nascon.brushQuality";
        private static readonly int BrushReduceId = Shader.PropertyToID("_NasconBrushReduce");

        public const int MaxBrushQuality = 3;
        public static readonly string[] BrushQualityNames = { "Off", "Low", "Medium", "High" };

        public static event Action Changed;

        public static float LookSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
            set { PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.1f, 3f)); Changed?.Invoke(); }
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.9f);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); Apply(); }
        }

        /// <summary>Painterly brush-stroke strength (full-screen Kuwahara radius). 0 = off, 3 = full.</summary>
        public static int BrushQuality
        {
            get => PlayerPrefs.GetInt(BrushKey, MaxBrushQuality);
            set { PlayerPrefs.SetInt(BrushKey, Mathf.Clamp(value, 0, MaxBrushQuality)); Apply(); }
        }

        public static bool Fullscreen
        {
            get => Screen.fullScreen;
            set => Screen.fullScreen = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            Shader.SetGlobalFloat(BrushReduceId, MaxBrushQuality - BrushQuality);
            Changed?.Invoke();
        }
    }
}
