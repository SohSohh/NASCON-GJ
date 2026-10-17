using System;
using System.Collections;
using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>The kill: the shadow tears off the wall and fills the camera, maw first.</summary>
    public class ShadowLunge : MonoBehaviour
    {
        private static readonly int FrameId = Shader.PropertyToID("_Frame");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");

        public static void Play(Material material, Vector4 frame, Transform cameraTransform, Action onImpact)
        {
            var go = new GameObject("ShadowLunge");
            go.transform.SetParent(cameraTransform, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = BuildQuad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetVector(FrameId, frame);
            block.SetFloat(GlowId, 2.2f);
            renderer.SetPropertyBlock(block);
            go.AddComponent<ShadowLunge>().StartCoroutine(Run(go.transform, cameraTransform, onImpact));
        }

        private static IEnumerator Run(Transform quad, Transform cam, Action onImpact)
        {
            Vector3 camStart = cam.localPosition;
            const float duration = 0.38f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float e = k * k * k;
                quad.localPosition = new Vector3(Mathf.Sin(k * 40f) * 0.04f, Mathf.Lerp(-0.25f, -0.05f, e), Mathf.Lerp(3.2f, 0.32f, e));
                quad.localScale = new Vector3(1.9f, 2.5f, 1f) * Mathf.Lerp(0.8f, 1.6f, e);
                cam.localPosition = camStart + UnityEngine.Random.insideUnitSphere * (0.03f * k);
                yield return null;
            }
            cam.localPosition = camStart;
            onImpact?.Invoke();
        }

        private static Mesh BuildQuad()
        {
            var m = new Mesh { name = "LungeQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }
    }
}
