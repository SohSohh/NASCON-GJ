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
    }
}
