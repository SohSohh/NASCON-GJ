using System.Collections;
using NasconGJ.Core;
using NasconGJ.Death;
using NasconGJ.Entities;
using NasconGJ.Fire;
using NasconGJ.Interaction;
using NasconGJ.Items;
using NasconGJ.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NasconGJ.UI
{
    /// <summary>Minimal diegetic-feeling HUD: crosshair, prompt, flame fuel, cover meter, transfer ring, messages.</summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Image transferRing;
        [SerializeField] private Image transferRingOutline;

        [Header("Fuel")]
        [SerializeField] private CanvasGroup fuelGroup;
        [SerializeField] private RectTransform fuelFill;
        [SerializeField] private Image fuelFillImage;
        [SerializeField] private CanvasGroup fuelBarFlashGroup;
        [SerializeField] private Gradient fuelColor;
        [SerializeField] private TMP_Text slotText;

        [Header("Cover")]
        [SerializeField] private CanvasGroup coverGroup;
        [SerializeField] private RectTransform coverFill;

        [Header("Messages")]
        [SerializeField] private TMP_Text messageText;

        private PlayerInventory inventory;
        private PlayerInteractor interactor;
        private FireCarrier carrier;
        private PlayerDeath death;
        private WakeUpSequence wake;
        private Coroutine messageRoutine;
        private FlammableItem lowFuelWarned;
        private float shadowFlashTime;
        private bool wasShadowAccelerated;

        /// <summary>Tutorials that script their own "your flame is dying" beat turn the generic warning off.</summary>
        public static bool SuppressLowFuelWarning;

        private void OnEnable()
        {
            GameEvents.FireExtinguished += OnFireExtinguished;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.FireExtinguished -= OnFireExtinguished;
            GameEvents.PlayerDied -= OnPlayerDied;
        }

        private void Start()
        {
            if (promptText != null)
            {
                var promptMaterial = promptText.fontMaterial;
                promptMaterial.SetColor("_OutlineColor", Color.black);
                promptMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                promptMaterial.SetFloat("_OutlineWidth", 0.4f);
            }

            inventory = FindAnyObjectByType<PlayerInventory>();
            if (inventory == null) return;
            interactor = inventory.GetComponent<PlayerInteractor>();
            carrier = inventory.GetComponent<FireCarrier>();
            death = inventory.GetComponent<PlayerDeath>();
            wake = inventory.GetComponent<WakeUpSequence>();
            carrier.HandBurned += OnHandBurned;
            messageText.alpha = 0f;
        }

        private void Update()
        {
            if (inventory == null) return;

            UpdateFuel();

            transferRing.fillAmount = carrier.TransferProgress;
            if (transferRingOutline != null)
                transferRingOutline.fillAmount = carrier.TransferProgress;

            bool showCover = carrier.IsCovering || carrier.CoverMeter < 0.999f;
            coverGroup.alpha = Mathf.MoveTowards(coverGroup.alpha, showCover ? 1f : 0f, Time.deltaTime * 4f);
            coverFill.anchorMax = new Vector2(carrier.CoverMeter, 1f);
        }

        private void LateUpdate()
        {
            if (inventory != null) UpdatePrompt();
        }

        private string shownPrompt;

        /// <summary>
        /// World prompts ("F  Hold to light the candle", "F  Pick up stick") use the same card as the tutorial hints, so
        /// every on-screen instruction looks the same. Tutorial hints take priority; the prompt waits until they're gone.
        /// </summary>
        private void UpdatePrompt()
        {
            promptText.text = "";
            string label = null;
            if (!DialogueUI.IsOpen && (wake == null || wake.Finished) && (death == null || !death.IsDead))
            {
                if (carrier.TransferTarget != null) // lighting comes first; once it burns, the same key picks it up
                    label = $"Hold to light the {carrier.TransferTarget.DisplayName.ToLowerInvariant()}";
                else if (interactor.Current != null)
                {
                    string p = interactor.Current.Prompt;
                    label = p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1) : p;
                }
            }

            var hint = HintUI.Instance;
            // a lesson replaced our card: forget it so it comes back once the lesson is gone
            if (hint != null && shownPrompt != null && hint.CurrentId != shownPrompt) shownPrompt = null;
            string id = label != null ? "prompt:" + label : null;
            if (hint == null || id == shownPrompt) return;
            bool tutorialUp = hint.IsShowing && (hint.CurrentId == null || !hint.CurrentId.StartsWith("prompt:"));
            if (tutorialUp) return;                    // never cover a lesson; show once it has faded
            shownPrompt = id;
            if (id != null) hint.Show("F", label, 0.1f, () => shownPrompt != id, id, quiet: true);
        }

        private void UpdateFuel()
        {
            FlammableItem flame = inventory.ActiveFlame;
            bool show = flame != null && flame.IsBurning && !flame.InfiniteBurn;
            fuelGroup.alpha = Mathf.MoveTowards(fuelGroup.alpha, show ? 1f : 0.7f, Time.deltaTime * 3f);

            float fuel = flame != null && flame.IsBurning ? flame.NormalizedFuel : 0f;
            fuelFill.anchorMax = new Vector2(fuel, 1f);
            bool shadowAccelerated = show && flame.BurnRateMultiplier >= ShadowCoordinator.TorchBurnRateInShadowRange;
            if (!shadowAccelerated || !wasShadowAccelerated) shadowFlashTime = 0f;
            else shadowFlashTime += Time.unscaledDeltaTime;
            wasShadowAccelerated = shadowAccelerated;

            // Hard transitions at five flashes per second, starting bright on entry.
            bool flashOn = Mathf.Repeat(shadowFlashTime, 0.2f) < 0.1f;
            fuelFillImage.color = shadowAccelerated
                ? (flashOn ? new Color(1f, 0.05f, 0.02f, 1f) : new Color(0.2f, 0.01f, 0.01f, 1f))
                : fuelColor.Evaluate(fuel);
            // Older scene instances may not have the optional bar group assigned.
            if (fuelBarFlashGroup != null)
                fuelBarFlashGroup.alpha = shadowAccelerated && !flashOn ? 0.15f : 1f;
            if (show && fuel < 0.3f && lowFuelWarned != flame && !SuppressLowFuelWarning && HintUI.Instance != null && SomethingToBurnNear(flame))
            {
                lowFuelWarned = flame;
                HintUI.Instance.Show("", "Your flame is dying.  Find something that burns.", 4.5f);
            }

            var active = inventory.ActiveItem;
            var other = inventory.OtherItem;
            slotText.text = (other == null ? (active != null ? active.ItemName : "") : $"[{inventory.ActiveSlot + 1}] {(active != null ? active.ItemName : "empty")}") +
                            (other != null ? $"   <alpha=#66>[{2 - inventory.ActiveSlot}] {other.ItemName}" : "");
        }

        private void OnHandBurned() => ShowMessage("<i>It burns...</i>", 1.5f);
        private void OnFireExtinguished() => ShowMessage("<i>The flame is gone.</i>\n<size=55%><alpha=#99>Something is coming. Listen.</size>", 5f);

        private void OnPlayerDied()
        {
            ShowMessage(death != null ? death.DeathReason : "You were consumed.", 10f);
        }

        /// <summary>Only tell the player to find fuel when there is some within reach (a torch, a stick, a brazier).</summary>
        private static bool SomethingToBurnNear(FlammableItem held)
        {
            Vector3 p = held.transform.position;
            foreach (var f in FindObjectsByType<FlammableItem>())
                if (f != held && (f.transform.position - p).sqrMagnitude < 22f * 22f) return true;
            return false;
        }

        public void ShowMessage(string text, float duration)
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(text, duration));
        }

        private IEnumerator MessageRoutine(string text, float duration)
        {
            messageText.text = text;
            for (float a = messageText.alpha; a < 1f; a += Time.deltaTime * 2f) { messageText.alpha = a; yield return null; }
            messageText.alpha = 1f;
            yield return new WaitForSeconds(duration);
            for (float a = 1f; a > 0f; a -= Time.deltaTime) { messageText.alpha = a; yield return null; }
            messageText.alpha = 0f;
        }
    }
}
