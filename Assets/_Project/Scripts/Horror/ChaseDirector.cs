using System.Collections;
using System.Collections.Generic;
using NasconGJ.Core;
using NasconGJ.Death;
using NasconGJ.Entities;
using NasconGJ.Items;
using NasconGJ.Player;
using NasconGJ.UI;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>
    /// The escape. You lift the crystal and the room answers: it shudders, you turn, and the dark pours out of the
    /// walls behind you, one living mass on every surface, eyes opening in it. Turn, RUN, out through the doorway and
    /// down the long hallway: rubble, falling slabs, tendrils racing you along the walls. It touches you, it's over
    /// (you're back at the crystal, it's already on you). At the far end, white light: the ending.
    /// With <see cref="waitForCrystal"/> off it starts on its own (a level that is nothing but the chase).
    /// </summary>
    public class ChaseDirector : MonoBehaviour
    {
        [Header("Start")]
        [SerializeField] private bool waitForCrystal = true;
        [SerializeField] private Transform retryPoint;                 // where a retry puts you (the crystal is gone)
        [SerializeField] private GameObject[] disableOnBegin;           // the level's own shadows, its old exit
        [SerializeField] private GameObject[] collapseOnBegin;          // the wall that cracks open: the way out
        [SerializeField] private GameObject[] rubbleOnBegin;            // what's left of it
        [SerializeField] private CarryableItem torchPrefab;             // a retry hands you a lit torch

        [Header("Course (world, runs along +z)")]
        [SerializeField] private float corridorX;
        [SerializeField] private float hallStartZ;
        [SerializeField] private float lightZ = 96f;
        [SerializeField] private float entityBehind = 5f;               // how far behind you it tears out

        [Header("The entity")]
        [SerializeField] private Material massMaterial;
        [SerializeField] private Material ashMaterial;
        [SerializeField] private Material lungeMaterial;                // creature sheet, for the final lunge
        [SerializeField] private float catchDistance = 1.1f;
        [SerializeField] private float speedStart = 4.2f, speedEnd = 5.2f;
        [SerializeField] private float runBoost = 2f;

        [Header("Falling slabs (drop when you get this close)")]
        [SerializeField] private Rigidbody[] slabs;
        [SerializeField] private float slabWarnDistance = 10f;

        [Header("Light at the end")]
        [SerializeField] private Light[] endLights;

        [Header("Audio")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioSource windLoop;
        [SerializeField] private AudioSource whisperLoop;
        [SerializeField] private AudioClip collapseClip, screechClip, dreadClip, heartbeatClip, impactClip;

        private static bool retrying;                                  // caught last time: skip the walk and most of the intro

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => retrying = false;

        private ChaseEntity entity;
        private Transform player, cam;
        private FirstPersonController controller;
        private PlayerDeath death;
        private bool begun, running, over;
        private float runStarted, heartbeatTimer;
        private readonly HashSet<Rigidbody> dropped = new();

        private void Start()
        {
            var go = GameObject.FindWithTag(GameTags.Player);
            if (go == null) return;
            player = go.transform;
            controller = go.GetComponent<FirstPersonController>();
            death = go.GetComponent<PlayerDeath>();
            cam = Camera.main != null ? Camera.main.transform : null;
            foreach (var s in slabs) if (s != null) s.isKinematic = true;

            if (retrying || !waitForCrystal)
            {
                if (retrying && waitForCrystal)
                {
                    foreach (var c in FindObjectsByType<CrystalPickup>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
                    if (retryPoint != null) Teleport(retryPoint.position, retryPoint.eulerAngles.y);
                    var inv = go.GetComponent<PlayerInventory>();
                    if (inv != null && inv.ActiveItem == null && torchPrefab != null)
                    {
                        var torch = Instantiate(torchPrefab);
                        inv.PickUp(torch);
                        if (torch.Flammable != null) torch.Flammable.Ignite();
                    }
                }
                Begin();
            }
            else GameEvents.PuzzleSolved += OnPuzzleSolved;
        }

        private void OnDestroy()
        {
            GameEvents.PuzzleSolved -= OnPuzzleSolved;
            HUDController.SuppressLowFuelWarning = false;
            if (controller != null) { controller.LevelSpeedBoost = 1f; controller.SprintDisabled = false; }
            Time.timeScale = 1f;
        }

        private void OnPuzzleSolved(string id)
        {
            if (id == "CrystalAcquired") Begin();
        }

        public void Begin()
        {
            if (begun || player == null) return;
            begun = true;
            GameEvents.PuzzleSolved -= OnPuzzleSolved;
            StartCoroutine(Intro(retrying));
            retrying = false;
        }

        // ------------------------------------------------------------------ the cutscene

        private IEnumerator Intro(bool quick)
        {
            controller.MovementLocked = true;
            controller.ExternalCameraControl = true;
            HUDController.SuppressLowFuelWarning = true;
            KeepLight();

            var go = new GameObject("TheDark");
            entity = go.AddComponent<ChaseEntity>();
            entity.Init(massMaterial, ashMaterial, corridorX, ~LayerMask.GetMask("Player", "Ignore Raycast"));
            entity.HeadZ = player.position.z - entityBehind;
            entity.PlayerZ = player.position.z;

            float yaw0 = player.eulerAngles.y, back = 180f;
            if (!quick)
            {
                // the crystal's chime dies; the room shudders
                yield return new WaitForSeconds(0.5f);
                Play(collapseClip, 1f);
                StartCoroutine(ScareFx.Shake(cam, 1.6f, 0.06f));
                Collapse();
                if (windLoop != null) { windLoop.volume = 0f; windLoop.Play(); StartCoroutine(Fade(windLoop, 0.6f, 2.5f)); }
                // you turn: something behind you
                for (float t = 0f; t < 1.2f; t += Time.deltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / 1.2f);
                    player.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(yaw0, back, k), 0f);
                    controller.SetPitch(Mathf.Lerp(0f, 6f, k));
                    yield return null;
                }
                foreach (var g in disableOnBegin) if (g != null) g.SetActive(false);
                // it tears out of the walls, the floor, the ceiling
                Play(dreadClip, 1f);
                entity.Burst(220);
                for (float t = 0f; t < 2.2f; t += Time.deltaTime)
                {
                    entity.Spread = Mathf.SmoothStep(0f, 1f, t / 1.8f);
                    entity.HeadZ += 0.5f * Time.deltaTime;
                    entity.Speed = 1f;
                    entity.PlayerZ = player.position.z;
                    controller.SetPitch(Mathf.Lerp(6f, -8f, t / 2.2f));       // it climbs the walls, your eyes follow
                    if (t > 1.1f && t - Time.deltaTime <= 1.1f) { Play(screechClip, 1f); StartCoroutine(ScareFx.Shake(cam, 0.6f, 0.1f)); }
                    yield return null;
                }
                // it lunges a step closer
                for (float t = 0f; t < 0.35f; t += Time.deltaTime) { entity.HeadZ += 6f * Time.deltaTime; entity.Speed = 6f; yield return null; }
            }
            else
            {
                foreach (var g in disableOnBegin) if (g != null) g.SetActive(false);
                Collapse();
                if (windLoop != null) { windLoop.volume = 0.6f; windLoop.Play(); }
                entity.Spread = 1f;
                entity.Burst(150);
                player.rotation = Quaternion.Euler(0f, back, 0f);
                Play(screechClip, 0.8f);
                StartCoroutine(ScareFx.Shake(cam, 0.4f, 0.08f));
                yield return new WaitForSeconds(0.7f);
            }

            // whip round: RUN
            float from = player.eulerAngles.y, pitch0 = quick ? 0f : -8f;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.35f);
                player.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(from, 0f, k), 0f);
                controller.SetPitch(Mathf.Lerp(pitch0, 0f, k));
                yield return null;
            }
            player.rotation = Quaternion.identity;
            controller.SetPitch(0f);
            ObjectiveUI.Set("RUN. Reach the light");
            if (HintUI.Instance != null) HintUI.Instance.Show("W", "RUN. Dodge the rubble", 2.5f, () => player.position.z > hallStartZ + 12f);
            controller.MovementLocked = false;
            controller.SprintDisabled = true;                          // already running flat out
            controller.ExternalCameraControl = false;
            controller.LevelSpeedBoost = runBoost;
            if (whisperLoop != null) { whisperLoop.volume = 0.25f; whisperLoop.Play(); }
            runStarted = Time.time;
            running = true;
        }

        // ------------------------------------------------------------------ the chase

        private void Update()
        {
            if (!running || over || player == null || entity == null) return;
            float dt = Time.deltaTime;
            float pz = player.position.z;
            float progress = Mathf.InverseLerp(hallStartZ, lightZ, pz);
            KeepLight();

            float gap = pz - entity.HeadZ;
            float speed = Mathf.Lerp(speedStart, speedEnd, progress) + Mathf.Max(0f, gap - 7f) * 0.6f;   // never let you lose it
            speed *= Mathf.Clamp01((Time.time - runStarted) / 0.8f);                                   // a beat to turn and go
            entity.HeadZ += speed * dt;
            entity.Speed = speed;
            entity.PlayerZ = pz;
            gap = pz - entity.HeadZ;

            // closer = louder whispers, a heartbeat when it's on you
            if (whisperLoop != null) whisperLoop.volume = Mathf.Lerp(0.9f, 0.2f, Mathf.InverseLerp(1.5f, 10f, gap));
            heartbeatTimer -= dt;
            if (gap < 5f && heartbeatTimer <= 0f) { Play(heartbeatClip, 0.8f); heartbeatTimer = Mathf.Lerp(0.45f, 1f, gap / 5f); }

            // slabs come down in front of you
            foreach (var slab in slabs)
            {
                if (slab == null || dropped.Contains(slab)) continue;
                if (slab.position.z - pz < slabWarnDistance && slab.position.z > pz)
                {
                    dropped.Add(slab);
                    slab.isKinematic = false;
                    slab.linearVelocity = Vector3.down * 4f;
                    slab.constraints = RigidbodyConstraints.FreezeRotation;   // lands flat: a fair obstacle, never a wall
                    Play(impactClip, 0.8f);
                    StartCoroutine(ScareFx.Shake(cam, 0.35f, 0.05f));
                }
            }

            if (gap < catchDistance) StartCoroutine(Caught());
            else if (pz >= lightZ) StartCoroutine(IntoTheLight());
        }

        private IEnumerator Caught()
        {
            over = true;
            retrying = true;
            controller.MovementLocked = true;
            controller.LevelSpeedBoost = 1f;
            Play(screechClip, 1f);
            entity.Burst(200);
            if (lungeMaterial != null && cam != null)
                ShadowLunge.Play(lungeMaterial, ShadowSheet.CreatureFrame(ShadowSheet.Attack + 1), cam, () => { if (death != null) death.Kill("The dark caught up."); });
            else if (death != null) death.Kill("The dark caught up.");
            // it keeps coming over you
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { entity.HeadZ += 4f * Time.deltaTime; yield return null; }
        }

        private IEnumerator IntoTheLight()
        {
            over = true;
            controller.LevelSpeedBoost = 1f;
            Play(impactClip, 0.6f);
            if (whisperLoop != null) StartCoroutine(Fade(whisperLoop, 0f, 0.6f));
            if (windLoop != null) StartCoroutine(Fade(windLoop, 0f, 1.2f));
            foreach (var l in endLights) if (l != null) StartCoroutine(Flare(l));
            // the light burns it away behind you
            StartCoroutine(Recede());
            // white, not black: the ending opens out of this light
            var canvas = new GameObject("WhiteOut", typeof(Canvas), typeof(CanvasGroup)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var img = new GameObject("White", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            img.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            var group = canvas.GetComponent<CanvasGroup>();
            for (float t = 0f; t < 1.1f; t += Time.deltaTime) { group.alpha = Mathf.SmoothStep(0f, 1f, t / 1.1f); yield return null; }
            group.alpha = 1f;
            retrying = false;
            yield return new WaitForSeconds(0.4f);
            SceneLoader.Load(SceneNames.Ending);
        }

        private IEnumerator Recede()
        {
            for (float t = 0f; t < 1f && entity != null; t += Time.deltaTime) { entity.Opacity = 1f - t; yield return null; }
        }

        // ------------------------------------------------------------------ helpers

        private void Collapse()
        {
            foreach (var g in collapseOnBegin) if (g != null) g.SetActive(false);
            foreach (var g in rubbleOnBegin) if (g != null) g.SetActive(true);
        }

        private void Teleport(Vector3 pos, float yaw)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
        }

        /// <summary>Your light doesn't run out in the escape: there's no time for that.</summary>
        private void KeepLight()
        {
            var inv = player != null ? player.GetComponent<PlayerInventory>() : null;
            var flame = inv != null ? inv.ActiveFlame : null;
            if (flame == null) return;
            if (!flame.IsBurning) flame.Ignite();
            flame.MinFuelFraction = 1f;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip, volume);
        }

        private static IEnumerator Fade(AudioSource s, float to, float time)
        {
            float from = s.volume;
            for (float t = 0f; t < time; t += Time.deltaTime) { s.volume = Mathf.Lerp(from, to, t / time); yield return null; }
            s.volume = to;
        }

        private static IEnumerator Flare(Light l)
        {
            float from = l.intensity;
            for (float t = 0f; t < 1f; t += Time.deltaTime) { l.intensity = Mathf.Lerp(from, from * 4f, t); yield return null; }
        }
    }
}
