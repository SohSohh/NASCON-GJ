using System.Collections;
using NasconGJ.Core;
using NasconGJ.Death;
using NasconGJ.Fire;
using NasconGJ.Interaction;
using NasconGJ.Items;
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

        [Header("Fuel")]
        [SerializeField] private CanvasGroup fuelGroup;
        [SerializeField] private RectTransform fuelFill;
        [SerializeField] private Image fuelFillImage;
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
        private Coroutine messageRoutine;
        private FlammableItem lowFuelWarned;

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
            inventory = FindAnyObjectByType<PlayerInventory>();
            if (inventory == null) return;
            interactor = inventory.GetComponent<PlayerInteractor>();
            carrier = inventory.GetComponent<FireCarrier>();
            death = inventory.GetComponent<PlayerDeath>();
            carrier.HandBurned += OnHandBurned;
            messageText.alpha = 0f;
        }

        private void Update()
        {
            if (inventory == null) return;

            UpdatePrompt();
            UpdateFuel();

            transferRing.fillAmount = carrier.TransferProgress;

            bool showCover = carrier.IsCovering || carrier.CoverMeter < 0.999f;
            coverGroup.alpha = Mathf.MoveTowards(coverGroup.alpha, showCover ? 1f : 0f, Time.deltaTime * 4f);
            coverFill.anchorMax = new Vector2(carrier.CoverMeter, 1f);
        }

        private void UpdatePrompt()
        {
            string prompt = "";
            if (!DialogueUI.IsOpen && (death == null || !death.IsDead))
            {
                if (carrier.TransferTarget != null)
                    prompt = $"<color=#FFB462><b>F</b></color>  <alpha=#AA>hold<alpha=#FF>  pass the flame to the {carrier.TransferTarget.DisplayName.ToLowerInvariant()}";
                if (interactor.Current != null)
                    prompt = (prompt.Length > 0 ? prompt + "\n" : "") + $"<color=#FFB462><b>E</b></color>  {interactor.Current.Prompt.ToLowerInvariant()}";
            }
            promptText.text = prompt;
        }

        private void UpdateFuel()
        {
            FlammableItem flame = inventory.ActiveFlame;
            bool show = flame != null && flame.IsBurning && !flame.InfiniteBurn;
            fuelGroup.alpha = Mathf.MoveTowards(fuelGroup.alpha, show ? 1f : 0.25f, Time.deltaTime * 3f);

            float fuel = flame != null && flame.IsBurning ? flame.NormalizedFuel : 0f;
            fuelFill.anchorMax = new Vector2(fuel, 1f);
            fuelFillImage.color = fuelColor.Evaluate(fuel);

            if (show && fuel < 0.3f && lowFuelWarned != flame && !SuppressLowFuelWarning && HintUI.Instance != null)
            {
                lowFuelWarned = flame;
                HintUI.Instance.Show("", "Your flame is dying.  Find something that burns.", 4.5f);
            }

            var active = inventory.ActiveItem;
            var other = inventory.OtherItem;
            slotText.text = $"[{inventory.ActiveSlot + 1}] {(active != null ? active.ItemName : "empty")}" +
                            (other != null ? $"   <alpha=#66>[{2 - inventory.ActiveSlot}] {other.ItemName}" : "");
        }

        private void OnHandBurned() => ShowMessage("<i>It burns...</i>", 1.5f);
        private void OnFireExtinguished() => ShowMessage("<i>The flame is gone.</i>\n<size=55%><alpha=#99>Something is coming. Listen.</size>", 5f);

        private void OnPlayerDied()
        {
            ShowMessage(death != null ? death.DeathReason : "You were consumed.", 10f);
            if (ScreenFader.Instance != null) ScreenFader.Instance.FadeTo(1f, 2.5f);
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
