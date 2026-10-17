using System;
using System.Collections;
using NasconGJ.Core;
using NasconGJ.Fire;
using NasconGJ.Guiding;
using NasconGJ.Items;
using NasconGJ.Player;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Tutorial
{
    /// <summary>
    /// Scripts the L01 onboarding so a first-time player can't get lost or die before they've learned the rules:
    ///   wake up facing the torch, take it (the way out is blocked until you do), crawl, watch the torch fail,
    ///   light the stick and take it (blocked until you do), shield it through the draft, take a fresh torch,
    ///   cross the hall. Every beat sets a persistent objective plus one key hint that stays until done, and
    ///   the tutorial flames can't burn out until the player reaches the hall.
    /// </summary>
    public class IntroDirector : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField] private CarryableItem droppedTorch;
        [SerializeField] private CarryableItem stick;
        [SerializeField] private CarryableItem freshTorch;
        [SerializeField] private Material glintMaterial;

        [Header("Gates (blocking colliders, removed when the lesson is done)")]
        [SerializeField] private GameObject pitGate;
        [SerializeField] private GameObject narrowsGate;

        [Header("Zones")]
        [SerializeField] private TutorialZone crawlStart;
        [SerializeField] private TutorialZone narrowsStart;
        [SerializeField] private TutorialZone stickApproach;
        [SerializeField] private TutorialZone draftApproach;
        [SerializeField] private TutorialZone draftExit;
        [SerializeField] private TutorialZone hallEntrance;
        [SerializeField] private TutorialZone exitApproach;

        private PlayerInventory inventory;
        private WakeUpSequence wake;
        private GuidingLight guide;

        private IEnumerator Start()
        {
            var player = GameObject.FindWithTag(GameTags.Player);
            if (player == null) yield break;
            inventory = player.GetComponent<PlayerInventory>();
            wake = player.GetComponent<WakeUpSequence>();
            guide = FindAnyObjectByType<GuidingLight>();
            if (guide != null) guide.TutorialControlsSpeech = true;
            HoldGuide(true);
            HUDController.SuppressLowFuelWarning = true;
            Objective("");
            SetGate(pitGate, true);
            SetGate(narrowsGate, true);
            Protect(droppedTorch, 0.25f);

            while (wake != null && !wake.Finished) yield return null;
            yield return new WaitForSeconds(0.6f);
            yield return Narrate(0);
            Glint(droppedTorch, true);
            yield return Lesson("Take the torch beside you", "E", "Pick up the torch", () => Holding(droppedTorch));
            Glint(droppedTorch, false);
            SetGate(pitGate, false);
            HoldGuide(false);

            Objective("Follow the guiding light");
            while (!crawlStart.EverEntered) yield return null;
            yield return Lesson("Crawl under the fallen slab", "C", "Hold to crouch", () => narrowsStart.EverEntered);
            Objective("Follow the guiding light");
            while (!stickApproach.EverEntered) yield return null;
            Flame(droppedTorch)?.SetFuel(0.22f);
            Protect(droppedTorch, 0.12f);
            Glint(stick, true);
            yield return Narrate(1);
            yield return Lesson("Light the stick before your torch goes out", "F", "Look at the stick and hold to light it", () => Flame(stick) != null && Flame(stick).IsBurning);
            Protect(stick, 0.35f);
            Protect(droppedTorch, 0f);
            Flame(droppedTorch)?.SetFuel(0.07f);
            yield return Lesson("Take the burning stick", "E", "Pick up the stick", () => Holding(stick));
            Glint(stick, false);
            SetGate(narrowsGate, false);
            HoldGuide(false);

            Objective("Follow the guiding light");
            while (!draftApproach.EverEntered) yield return null;
            yield return Narrate(2);
            yield return Lesson("Shield the flame through the wind", "RMB", "Hold to cover the flame", () => draftExit.EverEntered);
            HoldGuide(false);

            Objective("Follow the guiding light into the hall");
            while (!hallEntrance.EverEntered) yield return null;
            yield return Narrate(3);
            Glint(freshTorch, true);
            Protect(freshTorch, 0.35f);
            yield return Lesson("Take the fresh torch by the entrance", "E", "Pick up the fresh torch", () => Holding(freshTorch));
            Glint(freshTorch, false);
            HoldGuide(false);
            Objective("Cross the hall. Stay in the light");
            yield return TimedHint("F", "Light a brazier to keep shadows away", 5f);
            if (!exitApproach.EverEntered && inventory.GetSlot(0) != null && inventory.GetSlot(1) != null)
            {
                yield return TimedHint("1 / 2", "Switch your active hand", 3f);
                if (!exitApproach.EverEntered) yield return TimedHint("G", "Set a light down", 3f);
            }
            Protect(stick, 0f);
            Protect(freshTorch, 0f);
            HUDController.SuppressLowFuelWarning = false;
            while (!exitApproach.EverEntered) yield return null;
            yield return Narrate(4);
            Objective("Reach the light");
            HoldGuide(false);
            if (guide != null) guide.TutorialControlsSpeech = false;
        }

        private IEnumerator ClearHint()
        {
            if (HintUI.Instance != null)
            {
                HintUI.Instance.Hide();
                while (HintUI.Instance != null && HintUI.Instance.IsShowing) yield return null;
            }
            yield return new WaitForSeconds(0.35f);
        }

        private IEnumerator Narrate(int stop)
        {
            yield return ClearHint();
            Objective("");
            // Let the previous objective fade away before introducing new dialogue.
            yield return new WaitForSeconds(0.4f);
            if (guide != null) yield return guide.SpeakTutorialStop(stop);
            yield return new WaitForSeconds(0.4f);
        }

        private IEnumerator Lesson(string objective, string keys, string instruction, Func<bool> done)
        {
            if (done()) yield break; // Do not replay instructions for an action already completed.
            Objective(objective);
            yield return new WaitForSeconds(0.45f);
            if (!done()) Hint(keys, instruction, done, 2f);
            while (!done()) yield return null;
            yield return ClearHint();
        }

        private IEnumerator TimedHint(string keys, string text, float duration)
        {
            Hint(keys, text, null, duration);
            // Include the fade-in; exit immediately if the player has reached the next beat.
            float elapsed = 0f;
            while (elapsed < duration + 0.8f && !exitApproach.EverEntered)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return ClearHint();
        }

        private void OnDestroy()
        {
            HUDController.SuppressLowFuelWarning = false;
            if (guide != null) { guide.TutorialControlsSpeech = false; guide.Hold = false; }
        }
        private void HoldGuide(bool hold)
        {
            if (guide != null) guide.Hold = hold;
        }

        // ------------------------------------------------------------ helpers

        private bool Holding(CarryableItem item) => item != null && inventory != null && (inventory.GetSlot(0) == item || inventory.GetSlot(1) == item);

        private static FlammableItem Flame(CarryableItem item) => item != null ? item.Flammable : null;

        private static void Protect(CarryableItem item, float floor)
        {
            var f = Flame(item);
            if (f != null) f.MinFuelFraction = floor;
        }

        private void Glint(CarryableItem item, bool on)
        {
            if (item == null) return;
            var g = item.GetComponent<InterestGlint>() ?? item.gameObject.AddComponent<InterestGlint>();
            if (glintMaterial != null) g.SetSparkMaterial(glintMaterial);
            g.SetActive(on);
        }

        private static void SetGate(GameObject gate, bool closed)
        {
            if (gate != null) gate.SetActive(closed);
        }

        private static void Objective(string text) => ObjectiveUI.Set(text);

        private static void Hint(string keys, string text, Func<bool> until, float minDuration = 1.5f)
        {
            if (HintUI.Instance != null) HintUI.Instance.Show(keys, text, minDuration, until);
        }
    }
}
