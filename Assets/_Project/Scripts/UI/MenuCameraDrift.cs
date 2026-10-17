using UnityEngine;
using UnityEngine.InputSystem;

namespace NasconGJ.UI
{
    /// <summary>Slow handheld drift plus a little mouse parallax, so the menu backdrop feels alive.</summary>
    public class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private float swayAngle = 0.9f;
        [SerializeField] private float swaySpeed = 0.18f;
        [SerializeField] private float positionSway = 0.05f;
        [SerializeField] private float parallaxAngle = 1.6f;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Vector2 smoothedMouse;

        private void Start()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }

        private void Update()
        {
            Vector2 mouse = Vector2.zero;
            if (Mouse.current != null && Screen.width > 0)
            {
                Vector2 p = Mouse.current.position.ReadValue();
                mouse = new Vector2(p.x / Screen.width - 0.5f, p.y / Screen.height - 0.5f) * 2f;
            }
            smoothedMouse = Vector2.Lerp(smoothedMouse, Vector2.ClampMagnitude(mouse, 1.5f), Time.unscaledDeltaTime * 2f);

            float t = Time.unscaledTime * swaySpeed;
            transform.rotation = baseRotation * Quaternion.Euler(
                -smoothedMouse.y * parallaxAngle + Mathf.Sin(t * 1.3f) * swayAngle * 0.5f,
                smoothedMouse.x * parallaxAngle + Mathf.Sin(t) * swayAngle,
                Mathf.Sin(t * 0.7f) * swayAngle * 0.2f);
            transform.position = basePosition + transform.right * (Mathf.Sin(t * 0.9f) * positionSway) + transform.up * (Mathf.Sin(t * 1.7f) * positionSway * 0.4f);
        }
    }
}
