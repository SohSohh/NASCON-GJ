using NasconGJ.Entities;
using NasconGJ.Player;
using UnityEngine;

namespace NasconGJ.EditorTools
{
    /// <summary>
    /// Play-mode look-dev helpers for automated screenshots: teleport the player, settle animation,
    /// particles and shadows (works even when the editor isn't ticking), then capture camera + HUD.
    /// </summary>
    public static class LookDev
    {
        public static string OutputDir = System.IO.Path.GetTempPath();

        public static GameObject Player => GameObject.FindWithTag("Player");

        public static void MovePlayer(Vector3 position, float yaw, float pitch = 0f, float eyeHeight = 1.6f)
        {
            var p = Player;
            if (p == null) return;
            var cc = p.GetComponent<CharacterController>();
            cc.enabled = false;
            p.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            cc.enabled = true;
            var fpc = p.GetComponent<FirstPersonController>();
            fpc.ExternalCameraControl = true;
            fpc.CameraRoot.localPosition = new Vector3(0f, eyeHeight, 0f);
            fpc.CameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public static void Settle(int animatorSteps = 8, int shadowSteps = 4)
        {
            for (int i = 0; i < animatorSteps; i++)
            {
                foreach (var body in Object.FindObjectsByType<FirstPersonBody>()) body.SendMessage("LateUpdate");
                foreach (var pose in Object.FindObjectsByType<TorchBearerPose>()) pose.SendMessage("LateUpdate");
            }
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>())
                if (ps.isPlaying || ps.isPaused || ps.main.playOnAwake)
                {
                    ps.Simulate(1.2f, true, true);
                    ps.Play(true); // Simulate leaves systems paused
                }
            for (int i = 0; i < shadowSteps; i++)
            {
                foreach (var e in Object.FindObjectsByType<ShadowEnemy>()) e.SendMessage("Update");
                foreach (var f in Object.FindObjectsByType<ShadowFigure>()) f.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            }
        }

        public static void Shot(string name, bool withHud = true)
        {
            var cam = Camera.main;
            if (cam == null) return;
            string path = System.IO.Path.Combine(OutputDir, name + ".png");
            var hud = GameObject.Find("PF_HUD");
            if (withHud && hud != null)
            {
                foreach (var g in hud.GetComponentsInChildren<CanvasGroup>(true))
                    if (g.gameObject.name == "Fader") g.alpha = 0f;
                CameraCapture.CaptureWithUI(cam, hud.GetComponent<Canvas>(), path);
            }
            else CameraCapture.Capture(cam, path, 1600, 900);
        }
    }
}
