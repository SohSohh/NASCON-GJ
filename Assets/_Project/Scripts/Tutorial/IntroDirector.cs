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
            HoldGuide(true); // it waits beside you until you have the torch
            HUDController.SuppressLowFuelWarning = true;

            SetGate(pitGate, true);
            SetGate(narrowsGate, true);
            Protect(droppedTorch, 0.25f);

            // ---------- 1. take the torch
            while (wake != null && !wake.Finished) yield return null;
            yield return new WaitForSeconds(0.6f);
            // let the guiding light finish its first words before any UI appears
            for (float t = 0f; guide != null && guide.IsTalking && t < 12f; t += Time.deltaTime) yield return null;
            Glint(droppedTorch, true);
            Objective("Your torch fell beside you");
            Hint("E", "Pick up", () => Holding(droppedTorch));
            while (!Holding(droppedTorch)) yield return null;
            Glint(droppedTorch, false);
            SetGate(pitGate, false);
            HoldGuide(false);

            // ---------- 2. crawl
            Objective("Crawl under the fallen slab");
            Hint("C", "Hold to crouch", () => narrowsStart.EverEntered);
            while (!narrowsStart.EverEntered) yield return null;

            // ---------- 3. the torch fails: pass the flame
            Objective("Follow the light");
            while (!stickApproach.EverEntered) yield return null;
            HoldGuide(true); // and waits at the stick until you carry its flame
            Flame(droppedTorch)?.SetFuel(0.22f);
            Protect(droppedTorch, 0.12f);
            Glint(stick, true);
            Objective("Your torch is dying. Light the stick");
            Hint("F", "Look at the stick, hold to pass the flame", () => Flame(stick) != null && Flame(stick).IsBurning);
            while (Flame(stick) == null || !Flame(stick).IsBurning) yield return null;

            Protect(droppedTorch, 0f);
            Flame(droppedTorch)?.SetFuel(0.07f); // spent: it gutters out a few seconds later
            Protect(stick, 0.35f);
            Objective("Take the burning stick");
            Hint("E", "Pick up", () => Holding(stick));
            while (!Holding(stick)) yield return null;
            Glint(stick, false);
            SetGate(narrowsGate, false);
            HoldGuide(false);

            // ---------- 4. the draft: shield it
            Objective("Follow the light");
            while (!draftApproach.EverEntered) yield return null;
            Objective("Wind ahead. Shield your flame");
            Hint("RMB", "Hold to cover the flame", () => draftExit.EverEntered);
            while (!draftExit.EverEntered) yield return null;

            // ---------- 5. the hall: real stakes from here
            Objective("Follow the light");
            while (!hallEntrance.EverEntered) yield return null;
            Glint(freshTorch, true);
            Objective("Take the fresh torch by the entrance");
            Hint("E", "Pick up", () => Holding(freshTorch));
            float waited = 0f;
            while (!Holding(freshTorch) && waited < 25f) { waited += Time.deltaTime; yield return null; }
            Glint(freshTorch, false);
            Protect(stick, 0f);
            HUDController.SuppressLowFuelWarning = false;

            Objective("Cross the hall. Stay in the light");
            Hint("F", "Light a brazier to push shadows back", null, 6f);
            yield return new WaitForSeconds(7f);
            if (inventory.GetSlot(0) != null && inventory.GetSlot(1) != null)
            {
                Hint("1 / 2", "Switch hands", null, 4f);
                yield return new WaitForSeconds(5f);
                Hint("G", "Set a light down", null, 4f);
            }

            while (!exitApproach.EverEntered) yield return null;
            Objective("Reach the light");
        }

        private void OnDestroy() => HUDController.SuppressLowFuelWarning = false;

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
