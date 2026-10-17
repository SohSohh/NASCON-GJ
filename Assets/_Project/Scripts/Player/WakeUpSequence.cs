using System.Collections;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Player
{
    /// <summary>
    /// Level-start intro: the player comes to on the floor after the fall, looking up the shaft,
    /// then slowly gets up. Control is handed back when they're standing.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController))]
    public class WakeUpSequence : MonoBehaviour
    {
        [SerializeField] private float lieStill = 2.2f;
        [SerializeField] private float getUp = 3.4f;
        [SerializeField] private Vector3 lyingPosition = new(0.15f, 0.32f, -0.2f);
        [SerializeField] private Vector3 lyingEuler = new(-62f, 25f, 32f);
        [Tooltip("When standing up, turn to face this (e.g. the torch you dropped).")]
        [SerializeField] private Transform faceOnWake;
        [SerializeField] private float facePitch = 38f;
        [SerializeField] private string controlsHintKeys = "";
        [SerializeField] private string controlsHintText = "";
        [Tooltip("Played as the eyes open: ringing ears, a slow heartbeat.")]
        [SerializeField] private AudioClip wakeClip;

        public bool Finished { get; private set; }

        private IEnumerator Start()
        {
            var controller = GetComponent<FirstPersonController>();
            Transform root = controller.CameraRoot;
            controller.ExternalCameraControl = true;
            controller.MovementLocked = true;
            var body = GetComponent<FirstPersonBody>();
            if (body != null) body.SetBodyVisible(false); // lying down, the camera would sit inside the chest

            if (faceOnWake != null)
            {
                Vector3 to = faceOnWake.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
            }

            Quaternion lying = Quaternion.Euler(lyingEuler);
            root.localPosition = lyingPosition;
            root.localRotation = lying;
            StartCoroutine(Eyelids());
            if (wakeClip != null)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
                src.PlayOneShot(wakeClip, 0.9f);
            }

            for (float t = 0f; t < lieStill; t += Time.deltaTime)
            {
                // shallow breathing while still dazed
                root.localRotation = lying * Quaternion.Euler(Mathf.Sin(t * 1.6f) * 1.2f, 0f, 0f);
                yield return null;
            }

            Vector3 standing = new(0f, 1.6f, 0f);
            for (float t = 0f; t < getUp; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / getUp);
                float stumble = Mathf.Sin(k * Mathf.PI) * 4f;
                root.localPosition = Vector3.Lerp(lyingPosition, standing, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k * 1.25f)));
                root.localRotation = Quaternion.Slerp(lying, Quaternion.Euler(faceOnWake != null ? facePitch : 0f, 0f, 0f), k) * Quaternion.Euler(0f, 0f, stumble);
                if (body != null && k > 0.85f) body.SetBodyVisible(true);
                yield return null;
            }

            controller.SetPitch(faceOnWake != null ? facePitch : 0f);
            if (body != null) body.SetBodyVisible(true);
            controller.ExternalCameraControl = false;
            controller.MovementLocked = false;
            Finished = true;

            yield return new WaitForSeconds(0.8f);
            if (HintUI.Instance != null && !string.IsNullOrEmpty(controlsHintKeys))
                HintUI.Instance.Show(controlsHintKeys, controlsHintText, 5f);
        }

        /// <summary>
        /// Coming to after the fall: two soft eyelids over the view that flutter open, close again, and finally stay
        /// open. The light leaks in warm and blurry at the edges.
        /// </summary>
        private IEnumerator Eyelids()
        {
            var canvasGo = new GameObject("Eyelids", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            UnityEngine.UI.Image Lid(string name, bool top)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(canvasGo.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(-0.1f, top ? 0.5f : -0.6f);
                rt.anchorMax = new Vector2(1.1f, top ? 1.6f : 0.5f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = go.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.16f, 0.01f, 0.01f, 1f);   // the inside of your eyelids, lit red
                img.raycastTarget = false;
                return img;
            }
            var topLid = Lid("Top", true);
            var bottomLid = Lid("Bottom", false);
            var warm = new GameObject("Bleed", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            warm.transform.SetParent(canvasGo.transform, false);
            var wrt = (RectTransform)warm.transform;
            wrt.anchorMin = Vector2.zero; wrt.anchorMax = Vector2.one; wrt.offsetMin = wrt.offsetMax = Vector2.zero;
            var bleed = warm.GetComponent<UnityEngine.UI.Image>();
            bleed.raycastTarget = false;

            // (open amount 0..1, seconds to get there): flutter, close, a longer look, blink, open
            (float open, float time)[] beats = { (0f, 0.6f), (0.18f, 0.55f), (0.02f, 0.3f), (0.4f, 0.8f), (0.08f, 0.25f), (0.55f, 0.6f), (0f, 0.18f), (1f, 1.1f) };
            float current = 0f;
            foreach (var (open, time) in beats)
            {
                float from = current;
                for (float t = 0f; t < time; t += Time.deltaTime)
                {
                    current = Mathf.Lerp(from, open, Mathf.SmoothStep(0f, 1f, t / time));
                    SetLids(topLid, bottomLid, current);
                    bleed.color = new Color(0.55f, 0.04f, 0.03f, (1f - current) * 0.6f); // blood-red haze while half-open
                    yield return null;
                }
            }
            SetLids(topLid, bottomLid, 1f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { bleed.color = new Color(0.35f, 0.05f, 0.03f, 0f); yield return null; }
            Destroy(canvasGo);
        }

        private static void SetLids(UnityEngine.UI.Image top, UnityEngine.UI.Image bottom, float open)
        {
            // each lid slides away from the centre line; slight curve via scale so the gap reads as an eye, not a slot
            float shift = open * 0.62f;
            var t = top.rectTransform; var b = bottom.rectTransform;
            t.anchorMin = new Vector2(-0.1f, 0.5f + shift); t.anchorMax = new Vector2(1.1f, 1.6f + shift);
            b.anchorMin = new Vector2(-0.1f, -0.6f - shift); b.anchorMax = new Vector2(1.1f, 0.5f - shift);
            float bow = Mathf.Lerp(1.35f, 1f, open);
            t.localScale = new Vector3(1f, bow, 1f);
            b.localScale = new Vector3(1f, bow, 1f);
        }
    }
}
