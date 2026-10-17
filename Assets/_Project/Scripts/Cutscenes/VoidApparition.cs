using System.Collections;
using NasconGJ.Entities;
using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// The Void as a colossal silhouette in the sky: a huge quad drawn with the shadow-creature sheet,
    /// rising from behind the hills, swaying through its idle frames with burning eyes, tar smoke
    /// pouring off it. Black against a red sky, so it reads from any distance.
    /// </summary>
    public class VoidApparition : MonoBehaviour
    {
        [SerializeField] private MeshRenderer quad;
        [SerializeField] private float height = 52f;
        [SerializeField] private float hiddenDrop = 46f;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private float idleFps = 1.6f;
        [Tooltip("Child at head height, for townsfolk to look up at.")]
        [SerializeField] private Transform head;

        private static readonly int FrameId = Shader.PropertyToID("_Frame");
        private static readonly int FlipId = Shader.PropertyToID("_Flip");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        private MaterialPropertyBlock block;
        private Vector3 shown;
        private float rise, opacity, time, glowBoost;

        public Transform Head => head;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            shown = transform.position;
            if (quad != null) quad.transform.localScale = new Vector3(height * ShadowSheet.Aspect, height, 1f);
            Apply();
            if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Rise(float duration) => StartCoroutine(RiseRoutine(duration));

        /// <summary>Eyes flare (when it takes the townsfolk).</summary>
        public void Flare() => glowBoost = 1.5f;

        private IEnumerator RiseRoutine(float duration)
        {
            if (smoke != null) smoke.Play();
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                rise = 1f - (1f - k) * (1f - k) * (1f - k); // fast then slowing, like something huge settling
                opacity = Mathf.Clamp01(k * 2.5f);
                yield return null;
            }
            rise = 1f;
            opacity = 1f;
        }

        private void Update()
        {
            time += Time.deltaTime;
            glowBoost = Mathf.MoveTowards(glowBoost, 0f, Time.deltaTime * 0.8f);
            Apply();
        }

        private void Apply()
        {
            if (quad == null) return;
            float sway = Mathf.Sin(time * 0.35f) * height * 0.015f;
            transform.position = shown + Vector3.down * (hiddenDrop * (1f - rise)) + Vector3.right * sway;
            int frame = ShadowSheet.Idle + Mathf.FloorToInt(time * idleFps) % 4;
            quad.GetPropertyBlock(block);
            block.SetVector(FrameId, ShadowSheet.CreatureFrame(frame));
            block.SetFloat(FlipId, 0f);
            block.SetFloat(GlowId, (1.4f + Mathf.PerlinNoise(time * 0.8f, 2f) * 0.6f + glowBoost) * opacity);
            block.SetFloat(DissolveId, 0f);
            block.SetFloat(OpacityId, opacity);
            quad.SetPropertyBlock(block);
        }
    }
}
