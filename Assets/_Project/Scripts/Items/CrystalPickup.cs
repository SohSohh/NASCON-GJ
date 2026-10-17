using System.Collections;
using NasconGJ.Core;
using NasconGJ.Interaction;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Items
{
    /// <summary>
    /// An interactable crystal pickup.
    /// When the player picks it up:
    /// - Plays chime SFX
    /// - Notifies game systems
    /// - Fades screen to black
    /// - Loads the next scene in story order (or an override)
    /// </summary>
    [SelectionBase]
    public class CrystalPickup : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [Tooltip("HUD prompt displayed to the player (e.g. 'pick up crystal' -> 'Hold [E] to pick up crystal')")]
        [SerializeField] private string prompt = "pick up crystal";

        [Tooltip("Leave empty to use SceneNames.StoryOrder (L06_Escape).")]
        [SerializeField] private string nextSceneOverride = "";

        [SerializeField] private float fadeDuration = 1.2f;
        [SerializeField] private float delayBeforeLoad = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;
        [Range(0f, 1f)] [SerializeField] private float soundVolume = 0.9f;

        [Header("Visuals & Animation")]
        [Tooltip("Child transform containing the crystal mesh. Can be swapped with any model by the user.")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private bool floatAndRotate = true;
        [SerializeField] private float floatAmplitude = 0.08f;
        [SerializeField] private float floatFrequency = 2.0f;
        [SerializeField] private float rotateSpeed = 45f;

        [Header("Glow")]
        [SerializeField] private Light glowLight;
        [SerializeField] private float pulseSpeed = 2.5f;
        [SerializeField] private float minIntensity = 1.8f;
        [SerializeField] private float maxIntensity = 3.2f;

        private bool pickedUp;
        private Vector3 initialLocalPos;
        private Collider col;

        public string Prompt => prompt;

        public bool CanInteract(GameObject interactor) => !pickedUp;

        private void Awake()
        {
            col = GetComponent<Collider>();
            if (modelRoot != null)
                initialLocalPos = modelRoot.localPosition;
            else
                initialLocalPos = Vector3.zero;
        }

        private void Update()
        {
            if (pickedUp) return;

            if (floatAndRotate)
            {
                if (modelRoot != null)
                {
                    modelRoot.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
                    float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
                    modelRoot.localPosition = initialLocalPos + new Vector3(0f, yOffset, 0f);
                }
                else
                {
                    transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
                }
            }

            if (glowLight != null)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                glowLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (pickedUp) return;
            pickedUp = true;
            StartCoroutine(PickupRoutine());
        }

        private IEnumerator PickupRoutine()
        {
            if (col != null) col.enabled = false;

            // Notify puzzle / objective listeners
            GameEvents.RaisePuzzleSolved("CrystalAcquired");

            // Play pickup sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
            }

            // Hide visuals
            if (modelRoot != null)
            {
                modelRoot.gameObject.SetActive(false);
            }
            if (glowLight != null)
            {
                glowLight.enabled = false;
            }

            // Fade screen out smoothly
            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeTo(1f, fadeDuration);
            }
            else
            {
                yield return new WaitForSeconds(fadeDuration);
            }

            if (delayBeforeLoad > 0f)
            {
                yield return new WaitForSeconds(delayBeforeLoad);
            }

            // Transition to next scene
            if (string.IsNullOrEmpty(nextSceneOverride))
            {
                SceneLoader.LoadNext();
            }
            else
            {
                SceneLoader.Load(nextSceneOverride);
            }
        }
    }
}
