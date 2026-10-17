using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NasconGJ.EditorTools
{
    /// <summary>Renders a camera (optionally with a screen-space canvas composited on top) to a PNG. Used for look-dev checks.</summary>
    public static class CameraCapture
    {
        public static void Capture(Camera cam, string filePath, int width = 1280, int height = 720)
        {
            var tex = Render(cam, width, height, false);
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        public static void Capture(string cameraObjectName, string filePath)
        {
            var go = GameObject.Find(cameraObjectName);
            if (go != null && go.TryGetComponent(out Camera cam)) Capture(cam, filePath);
            else Debug.LogError($"[Capture] No camera named {cameraObjectName}");
        }

        /// <summary>Scene through <paramref name="cam"/> (with post) plus <paramref name="canvas"/> composited crisp on top, like the real overlay.</summary>
        public static void CaptureWithUI(Camera cam, Canvas canvas, string filePath, int width = 1600, int height = 900)
        {
            var scene = Render(cam, width, height, false);

            var uiGo = new GameObject("UICaptureCam");
            var uiCam = uiGo.AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = new Color(0, 0, 0, 0);
            uiCam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            uiGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            // URP drops the alpha channel, so render the UI over black and over white and recover alpha from the difference.
            var overBlack = Render(uiCam, width, height, false);
            uiCam.backgroundColor = Color.white;
            var overWhite = Render(uiCam, width, height, false);
            canvas.renderMode = mode;
            Object.DestroyImmediate(uiGo);

            var s = scene.GetPixels();
            var b = overBlack.GetPixels();
            var w = overWhite.GetPixels();
            for (int i = 0; i < s.Length; i++)
            {
                float a = Mathf.Clamp01(1f - (w[i].g - b[i].g));
                // b = ui * a  (premultiplied over black)
                s[i] = new Color(b[i].r + s[i].r * (1 - a), b[i].g + s[i].g * (1 - a), b[i].b + s[i].b * (1 - a), 1f);
            }
            scene.SetPixels(s);
            scene.Apply();
            File.WriteAllBytes(filePath, scene.EncodeToPNG());
            Object.DestroyImmediate(scene);
            Object.DestroyImmediate(overBlack);
            Object.DestroyImmediate(overWhite);
        }

        private static Texture2D Render(Camera cam, int width, int height, bool alpha)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            return tex;
        }
    }
}
