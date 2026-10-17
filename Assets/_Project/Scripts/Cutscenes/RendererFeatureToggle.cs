using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// Switches named full-screen renderer features off while this component is enabled (e.g. the gameplay GBCamera
    /// palette pass during the prologue cutscenes, which have their own look) and restores them afterwards.
    /// </summary>
    public class RendererFeatureToggle : MonoBehaviour
    {
        [SerializeField] private ScriptableRendererData[] renderers;
        [SerializeField] private string[] disableFeatures = { "GBCamera" };

        private readonly System.Collections.Generic.List<ScriptableRendererFeature> switched = new();

        private void OnEnable()
        {
            switched.Clear();
            if (renderers == null) return;
            foreach (var data in renderers)
            {
                if (data == null) continue;
                foreach (var f in data.rendererFeatures)
                {
                    if (f == null || !f.isActive || System.Array.IndexOf(disableFeatures, f.name) < 0) continue;
                    f.SetActive(false);
                    switched.Add(f);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var f in switched) if (f != null) f.SetActive(true);
            switched.Clear();
        }
    }
}
