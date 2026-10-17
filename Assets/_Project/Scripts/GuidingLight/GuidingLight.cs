using System;
using NasconGJ.Core;
using NasconGJ.Fire;
using NasconGJ.Interaction;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Guiding
{
    /// <summary>
    /// The sourceless guiding light. It waits at a stop, speaks its dialogue when the player
    /// approaches (or interacts), then drifts to the next stop. It also counts as a gameplay light
    /// source, so shadows avoid it.
    /// </summary>
    public class GuidingLight : MonoBehaviour, IInteractable, ILightSource
    {
        [Serializable]
        public class Stop
        {
            public Transform point;
            public DialogueData dialogue;
        }

        [SerializeField] private Stop[] stops = Array.Empty<Stop>();
        [SerializeField] private bool autoTriggerOnApproach = true;
        [SerializeField] private float triggerRadius = 3.5f;
        [SerializeField] private float travelSpeed = 2.5f;

        [Header("Visual")]
        [SerializeField] private Light glow;
        [SerializeField] private Transform visual;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 1.4f;

        public string Prompt => "Listen";
        public bool IsTalking { get; private set; }
        /// <summary>While true the light stays at its stop after speaking (tutorial scripts hold it until a lesson is done).</summary>
        public bool Hold { get; set; }

        // ILightSource
        public bool IsLit => glow != null && glow.enabled;
        public Vector3 Position => transform.position;
        public float Radius => glow != null ? glow.range : 0f;
        public Transform Transform => transform;
        public bool IsCarried => false;

        private int stopIndex;
        private bool moving;
        private bool currentPlayed;
        private bool advancePending;
        private Transform player;

        private void OnEnable() => LightSourceRegistry.Register(this);
        private void OnDisable() => LightSourceRegistry.Unregister(this);

        private void Start()
        {
            var go = GameObject.FindWithTag(GameTags.Player);
            if (go != null) player = go.transform;
            if (stops.Length > 0 && stops[0].point != null) transform.position = stops[0].point.position;
        }

        public bool CanInteract(GameObject interactor) => !IsTalking && !moving && CurrentDialogue != null && !currentPlayed;

        public void Interact(GameObject interactor) => Talk();

        private DialogueData CurrentDialogue => stopIndex < stops.Length ? stops[stopIndex].dialogue : null;

        private void Update()
        {
            if (visual != null)
            {
                visual.localPosition = Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
                // shrink when the camera is right on top of it, so it never fills the screen
                var cam = Camera.main;
                float near = cam != null ? Vector3.Distance(cam.transform.position, visual.position) : 10f;
                visual.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(0.5f, 2f, near));
            }

            if (moving)
            {
                Vector3 target = stops[stopIndex].point.position;
                transform.position = Vector3.MoveTowards(transform.position, target, travelSpeed * Time.deltaTime);
                if ((transform.position - target).sqrMagnitude < 0.0025f) moving = false;
                return;
            }

            if (advancePending && !Hold)
            {
                advancePending = false;
                stopIndex++;
                currentPlayed = false;
                moving = true;
                return;
            }

            if (autoTriggerOnApproach && !IsTalking && !currentPlayed && CurrentDialogue != null && player != null &&
                (player.position - transform.position).sqrMagnitude < triggerRadius * triggerRadius)
                Talk();
        }

        private void Talk()
        {
            if (DialogueUI.Instance == null || CurrentDialogue == null) return;
            IsTalking = true;
            DialogueUI.Instance.Show(CurrentDialogue, OnDialogueFinished);
        }

        private void OnDialogueFinished()
        {
            IsTalking = false;
            currentPlayed = true;
            if (stopIndex + 1 < stops.Length && stops[stopIndex + 1].point != null)
                advancePending = true; // leaves on the next Update unless held
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.7f, 0.95f, 1f, 0.8f);
            Vector3 previous = transform.position;
            foreach (var stop in stops)
            {
                if (stop?.point == null) continue;
                Gizmos.DrawWireSphere(stop.point.position, 0.25f);
                Gizmos.DrawLine(previous, stop.point.position);
                previous = stop.point.position;
            }
        }
    }
}
