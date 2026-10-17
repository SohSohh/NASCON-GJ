using System.Collections;
using NasconGJ.Core;
using NasconGJ.Entities;
using NasconGJ.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// C01 prologue (~75 s), straight from the GDD, as a sequence of shots:
    ///   1a. crane down over a bright, colourful town          1b. dolly beside a townsperson walking the street
    ///   2a. the sky turns red, the Void rises over the hills; everyone stops and looks up
    ///   2b. close: two townsfolk drain into black smoke, their shadows left on the wall, the lamps die
    ///   2c. wide: the dead town under the Void
    ///   3.  the bearer crosses the wasteland with the last light (title card)
    ///   4a. the tomb in the storm          4b. reverse from the doorway: the bearer against the lightning
    ///   5.  inside, the floor gives way, they fall into the dark → L01 (they wake at the bottom)
    /// Hold Space / Enter / Esc (or A) to skip. Every set piece is a scene object wired by LevelBuilder.
    /// </summary>
    public class PrologueDirector : MonoBehaviour
    {
        [System.Serializable]
        public struct Atmosphere
        {
            public Color sky, ambient, fog, sun;
            public float fogDensity, sunIntensity;
            public Vector3 sunEuler;
            [Tooltip("Weight of the outdoor exposure volume (0 = tomb grading).")]
            public float outdoor;
            [Tooltip("How much the warm painterly palette is eased off (1 = natural colours).")]
            public float palette;
        }

        [Header("Rig")]
        [SerializeField] private Camera cam;
        [SerializeField] private CutsceneUI ui;
        [SerializeField] private Light sun;
        [SerializeField] private Volume outdoorVolume;

        [Header("1. The living town")]
        [SerializeField] private Transform craneFrom;
        [SerializeField] private Transform craneTo;
        [SerializeField] private int dollyVillager;
        [SerializeField] private Villager[] villagers;
        [Tooltip("Shadow left on the wall behind each villager (same order; entries may be empty).")]
        [SerializeField] private ShadowFigure[] imprints;
        [SerializeField] private Light[] lamps;
        [SerializeField] private Renderer[] glowingWindows;

        [Header("2. The Void")]
        [SerializeField] private VoidApparition voidApparition;
        [SerializeField] private Light voidWash;
        [SerializeField] private float voidWashIntensity = 40f;
        [SerializeField] private Transform riseFrom;
        [SerializeField] private Transform riseTo;
        [SerializeField] private Transform takenFrom;
        [SerializeField] private Transform takenTo;
        [SerializeField] private float[] takenDelays = { 3.0f, 1.2f, 1.9f, 2.6f, 3.3f, 3.7f, 4.1f };
        [SerializeField] private Transform darkFrom;
        [SerializeField] private Transform darkTo;

        [Header("3–5. The bearer")]
        [SerializeField] private CutsceneActor bearer;
        [SerializeField] private Transform[] walkPath;
        [SerializeField] private Vector3 followStart = new(0.6f, 0.75f, 2.4f);   // low, in front: the torch passing the lens
        [SerializeField] private Vector3 followEnd = new(3.2f, 1.6f, 0.4f);      // drifting out to the side
        [SerializeField] private Transform tombFrom;
        [SerializeField] private Transform tombTo;
        [SerializeField] private Transform doorwayFrom;
        [SerializeField] private Transform doorwayTo;
        [SerializeField] private Transform[] tombPath;
        [SerializeField] private Light lightning;
        [SerializeField] private Transform[] insidePath;
        [SerializeField] private Vector3 shoulderOffset = new(0.55f, 1.8f, -2.3f);
        [SerializeField] private Rigidbody[] collapsePieces;
        [SerializeField] private ParticleSystem collapseDust;

        [Header("Audio")]
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource dread;
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip thunderClip;
        [SerializeField] private AudioClip collapseClip;
        [SerializeField] private AudioClip impactClip;

        [Header("Atmospheres")]
        [SerializeField] private Atmosphere day = new()
        {
            sky = new Color(0.62f, 0.74f, 0.88f), ambient = new Color(0.42f, 0.4f, 0.45f), fog = new Color(0.66f, 0.72f, 0.8f),
            sun = new Color(1f, 0.86f, 0.66f), fogDensity = 0.006f, sunIntensity = 1.9f, sunEuler = new Vector3(32f, 28f, 0f), outdoor = 1f, palette = 0.85f,
        };
        [SerializeField] private Atmosphere theVoid = new()
        {
            sky = new Color(0.85f, 0.2f, 0.09f), ambient = new Color(0.16f, 0.05f, 0.07f), fog = new Color(0.55f, 0.12f, 0.07f),
            sun = new Color(0.9f, 0.22f, 0.1f), fogDensity = 0.0028f, sunIntensity = 0.45f, sunEuler = new Vector3(10f, 28f, 0f), outdoor = 1f, palette = 0.35f,
        };
        [SerializeField] private Atmosphere waste = new()
        {
            sky = new Color(0.16f, 0.09f, 0.15f), ambient = new Color(0.14f, 0.09f, 0.14f), fog = new Color(0.17f, 0.1f, 0.15f),
            sun = new Color(0.6f, 0.62f, 0.95f), fogDensity = 0.03f, sunIntensity = 0.55f, sunEuler = new Vector3(25f, -50f, 0f), outdoor = 0.85f,
        };
        [SerializeField] private Atmosphere storm = new()
        {
            sky = new Color(0.1f, 0.095f, 0.17f), ambient = new Color(0.15f, 0.13f, 0.2f), fog = new Color(0.11f, 0.1f, 0.17f),
            sun = new Color(0.55f, 0.6f, 0.95f), fogDensity = 0.014f, sunIntensity = 0.95f, sunEuler = new Vector3(24f, 15f, 0f), outdoor = 1f,
        };
        [SerializeField] private Atmosphere tomb = new()
        {
            sky = Color.black, ambient = new Color(0.035f, 0.02f, 0.04f), fog = new Color(0.02f, 0.01f, 0.02f),
            sun = Color.black, fogDensity = 0.05f, sunIntensity = 0f, sunEuler = new Vector3(90f, 0f, 0f), outdoor = 0.25f,
        };

        [Header("Skip")]
        [SerializeField] private float holdToSkip = 1.2f;

        private static readonly int PaletteReduceId = Shader.PropertyToID("_NasconPaletteReduce");

        private Vector3 basePos;
        private Quaternion baseRot = Quaternion.identity;
        private float shake, skipHeld, skipShown;
        private bool ending;
        private Coroutine cameraRoutine, runRoutine;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            foreach (var p in collapsePieces) if (p != null) p.isKinematic = true;
            if (lightning != null) lightning.intensity = 0f;
            if (voidWash != null) voidWash.intensity = 0f;
            runRoutine = StartCoroutine(Run());
        }

        private void OnDestroy() => Shader.SetGlobalFloat(PaletteReduceId, 0f);

        // ------------------------------------------------------------ the film

        private IEnumerator Run()
        {
            ui.Letterbox(true, 0.01f);
            Apply(day);
            if (music != null) { music.volume = 0f; StartCoroutine(FadeAudio(music, 0.65f, 3f)); }

            // 1a. crane down over the living town
            CameraMove(craneFrom, craneTo, 7.5f, 40f, 34f);
            Fade(0f, 2f);
            yield return new WaitForSeconds(1.6f);
            ui.Caption("There was a town.", 3.2f);
            yield return new WaitForSeconds(5.9f);

            // 1b. dolly beside someone walking the street
            var walker = villagers.Length > 0 ? villagers[Mathf.Clamp(dollyVillager, 0, villagers.Length - 1)] : null;
            if (walker != null) CameraFollow(walker.transform, new Vector3(-1.4f, 1.55f, -3.4f), new Vector3(1.6f, 1.45f, -3.0f), new Vector3(0f, 1.25f, 0f), 7f, false, 36f);
            yield return new WaitForSeconds(1.1f);
            ui.Caption("Its people lived in the light.", 3.2f);
            yield return new WaitForSeconds(5.9f);

            // 2a. the sky turns, the Void rises; everyone stops and looks up
            CameraMove(riseFrom, riseTo, 8.5f, 46f, 38f);
            if (sfx != null && thunderClip != null) sfx.PlayOneShot(thunderClip, 0.8f);
            StartCoroutine(Shake(1.2f, 0.25f));
            StartCoroutine(Blend(day, theVoid, 3.5f));
            if (voidApparition != null) voidApparition.Rise(6.5f);
            StartCoroutine(FadeLight(voidWash, voidWashIntensity, 4f));
            if (music != null) StartCoroutine(FadeAudio(music, 0f, 3f));
            if (dread != null) { dread.volume = 0f; dread.Play(); StartCoroutine(FadeAudio(dread, 0.8f, 3f)); }
            var head = voidApparition != null ? voidApparition.Head : null;
            for (int i = 0; i < villagers.Length; i++) StartCoroutine(LookUp(villagers[i], head, 0.6f + i * 0.18f));
            yield return new WaitForSeconds(1.6f);
            ui.Caption("Then the Void came.", 3.2f);
            yield return new WaitForSeconds(6.9f);

            // 2b. close: they are taken
            CameraMove(takenFrom, takenTo, 8.5f, 40f, 34f);
            if (voidApparition != null) voidApparition.Flare();
            for (int i = 0; i < villagers.Length; i++)
                StartCoroutine(TakeVillager(i, takenDelays != null && i < takenDelays.Length ? takenDelays[i] : 1f + i * 0.4f));
            yield return new WaitForSeconds(2.6f);
            ui.Caption("Everyone it touched became a shadow.", 3.2f);
            yield return new WaitForSeconds(1.6f);
            StartCoroutine(LightsOut(3f)); // the lamps die after the people do
            yield return new WaitForSeconds(4.3f);

            // 2c. wide: the dead town under the Void
            CameraMove(darkFrom, darkTo, 5.5f, 50f, 44f);
            yield return new WaitForSeconds(4.6f);
            yield return Fade(1f, 0.9f);

            // 3. THE BEARER: the torch passes the lens, then we drift out beside them
            Apply(waste);
            bearer.Teleport(walkPath[0]);
            bearer.Walk(walkPath, 1.25f);
            CameraFollow(bearer.transform, followStart, followEnd, new Vector3(0f, 1.3f, 0f), 13f, true, 44f);
            if (dread != null) StartCoroutine(FadeAudio(dread, 0.35f, 2f));
            Fade(0f, 1.4f);
            yield return new WaitForSeconds(2f);
            ui.Caption("One light was left.", 2.6f);
            yield return new WaitForSeconds(3.4f);
            ui.Title(3.4f);
            yield return new WaitForSeconds(7.2f);
            yield return Fade(1f, 0.8f);

            // 4a. THE TOMB, wide and low
            Apply(storm);
            bearer.Teleport(tombPath[0]);
            bearer.Walk(tombPath, 1.1f);
            CameraMove(tombFrom, tombTo, 6f, 52f, 47f);
            if (dread != null) StartCoroutine(FadeAudio(dread, 0.6f, 3f));
            Fade(0f, 0.8f);
            yield return new WaitForSeconds(2.2f);
            StartCoroutine(Lightning());
            yield return new WaitForSeconds(3.8f);

            // 4b. reverse from the doorway: a silhouette against the lightning
            CameraMove(doorwayFrom, doorwayTo, 5.5f, 46f, 42f);
            yield return new WaitForSeconds(1.1f);
            StartCoroutine(Lightning());
            yield return new WaitForSeconds(3.6f);
            yield return Fade(1f, 0.6f);

            // 5. THE FALL
            Apply(tomb);
            bearer.Teleport(insidePath[0]);
            bearer.Walk(insidePath, 1.2f);
            CameraFollow(bearer.transform, shoulderOffset, shoulderOffset + new Vector3(0f, 0.1f, 0.4f), new Vector3(0f, 1.3f, 0f), 30f, true, 50f);
            Fade(0f, 0.8f);
            float waited = 0f;
            while (bearer.Busy && waited < 7f) { waited += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.5f);

            if (sfx != null && collapseClip != null) sfx.PlayOneShot(collapseClip, 1f);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { shake = Mathf.Lerp(0.05f, 0.5f, t / 1.4f); yield return null; }
            if (collapseDust != null) collapseDust.Play();
            foreach (var p in collapsePieces)
            {
                if (p == null) continue;
                p.isKinematic = false;
                p.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
            }
            bearer.Fall();
            CameraTrack(bearer.transform, new Vector3(0f, 1.2f, 0f));
            for (float t = 0f; t < 1.3f; t += Time.deltaTime) { shake = Mathf.Lerp(0.5f, 0.1f, t / 1.3f); yield return null; }
            yield return Fade(1f, 0.45f);
            shake = 0f;
            yield return new WaitForSeconds(0.5f);
            if (sfx != null && impactClip != null) sfx.PlayOneShot(impactClip, 1f);
            if (dread != null) StartCoroutine(FadeAudio(dread, 0f, 1.5f));
            yield return new WaitForSeconds(2f);
            Finish();
        }

        private static IEnumerator LookUp(Villager v, Transform target, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (v != null && target != null) v.LookAt(target);
        }

        private IEnumerator TakeVillager(int i, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (villagers[i] != null) villagers[i].Shadowify(1.4f);
            yield return new WaitForSeconds(1.2f);
            if (imprints != null && i < imprints.Length && imprints[i] != null) StartCoroutine(FadeFigure(imprints[i], 0.85f, 0f, 1.4f));
        }

        private IEnumerator LightsOut(float duration)
        {
            int total = lamps.Length + glowingWindows.Length;
            float step = total > 0 ? duration / total : 0f;
            var block = new MaterialPropertyBlock();
            int emission = Shader.PropertyToID("_EmissionColor");
            for (int i = 0; i < total; i++)
            {
                if (i % 2 == 0 && i / 2 < lamps.Length && lamps[i / 2] != null) StartCoroutine(Gutter(lamps[i / 2]));
                int w = i / 2;
                if (i % 2 == 1 && w < glowingWindows.Length && glowingWindows[w] != null)
                {
                    var r = glowingWindows[w];
                    var mats = r.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        if (mats[m] == null || !mats[m].name.Contains("Window")) continue;
                        r.GetPropertyBlock(block, m);
                        block.SetColor(emission, Color.black);
                        r.SetPropertyBlock(block, m);
                    }
                }
                yield return new WaitForSeconds(step);
            }
            foreach (var l in lamps) if (l != null && l.intensity > 0f) StartCoroutine(Gutter(l));
        }

        private static IEnumerator Gutter(Light l)
        {
            float start = l.intensity;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                l.intensity = start * (1f - t / 0.6f) * (0.6f + 0.4f * Mathf.PerlinNoise(t * 20f, 0f));
                yield return null;
            }
            l.intensity = 0f;
        }

        private IEnumerator Lightning()
        {
            Color sky = cam.backgroundColor;
            float[] pulses = { 1f, 0.15f, 0.8f, 0f };
            foreach (float p in pulses)
            {
                if (lightning != null) lightning.intensity = 4f * p;
                cam.backgroundColor = Color.Lerp(sky, new Color(0.75f, 0.78f, 1f), p * 0.75f);
                yield return new WaitForSeconds(0.06f);
            }
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float k = 1f - t / 0.5f;
                if (lightning != null) lightning.intensity = 1.2f * k * k;
                cam.backgroundColor = Color.Lerp(sky, new Color(0.75f, 0.78f, 1f), 0.25f * k * k);
                yield return null;
            }
            if (lightning != null) lightning.intensity = 0f;
            cam.backgroundColor = sky;
            yield return new WaitForSeconds(0.35f);
            if (sfx != null && thunderClip != null) sfx.PlayOneShot(thunderClip, 0.9f);
        }

        private IEnumerator Shake(float duration, float amount)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                shake = amount * (1f - t / duration);
                yield return null;
            }
            shake = 0f;
        }

        private static IEnumerator FadeLight(Light l, float to, float duration)
        {
            if (l == null) yield break;
            float from = l.intensity;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                l.intensity = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            l.intensity = to;
        }

        private static IEnumerator FadeFigure(ShadowFigure figure, float opacity, float glow, float duration)
        {
            if (figure == null) yield break;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                figure.SetLook(opacity * k, glow * k);
                yield return null;
            }
            figure.SetLook(opacity, glow);
        }

        // ------------------------------------------------------------ camera

        private void CameraMove(Transform from, Transform to, float duration, float fovFrom, float fovTo)
        {
            if (from == null || to == null) return;
            if (cameraRoutine != null) StopCoroutine(cameraRoutine);
            cameraRoutine = StartCoroutine(MoveRoutine(from, to, duration, fovFrom, fovTo));
        }

        private IEnumerator MoveRoutine(Transform from, Transform to, float duration, float fovFrom, float fovTo)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                basePos = Vector3.Lerp(from.position, to.position, k);
                baseRot = Quaternion.Slerp(from.rotation, to.rotation, k);
                cam.fieldOfView = Mathf.Lerp(fovFrom, fovTo, k);
                yield return null;
            }
        }

        /// <summary>Tracks a moving subject from an offset (world-space, or in the subject's heading frame).</summary>
        private void CameraFollow(Transform target, Vector3 offset, Vector3 offsetEnd, Vector3 look, float duration, bool local, float fov)
        {
            if (cameraRoutine != null) StopCoroutine(cameraRoutine);
            cameraRoutine = StartCoroutine(FollowRoutine(target, offset, offsetEnd, look, duration, local, fov));
        }

        private IEnumerator FollowRoutine(Transform target, Vector3 offset, Vector3 offsetEnd, Vector3 look, float duration, bool local, float fov)
        {
            cam.fieldOfView = fov;
            Vector3 Desired(float k)
            {
                Vector3 o = Vector3.Lerp(offset, offsetEnd, k);
                return target.position + (local ? Quaternion.Euler(0f, target.eulerAngles.y, 0f) * o : o);
            }
            Vector3 LookAt() => target.position + look;
            basePos = Desired(0f);
            baseRot = Quaternion.LookRotation(LookAt() - basePos);
            Vector3 vel = Vector3.zero;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                basePos = Vector3.SmoothDamp(basePos, Desired(Mathf.SmoothStep(0f, 1f, t / duration)), ref vel, 0.5f);
                baseRot = Quaternion.Slerp(baseRot, Quaternion.LookRotation(LookAt() - basePos), Time.deltaTime * 6f);
                yield return null;
            }
        }

        /// <summary>Hold position, keep the subject framed (the fall).</summary>
        private void CameraTrack(Transform target, Vector3 look)
        {
            if (cameraRoutine != null) StopCoroutine(cameraRoutine);
            cameraRoutine = StartCoroutine(TrackRoutine(target, look));
        }

        private IEnumerator TrackRoutine(Transform target, Vector3 look)
        {
            while (true)
            {
                baseRot = Quaternion.Slerp(baseRot, Quaternion.LookRotation(target.position + look - basePos), Time.deltaTime * 5f);
                yield return null;
            }
        }

        private void LateUpdate()
        {
            float t = Time.time;
            float hand = 0.004f + shake;
            Vector3 jitter = new Vector3(Mathf.PerlinNoise(t * 0.7f, 1f) - 0.5f, Mathf.PerlinNoise(t * 0.6f, 2f) - 0.5f, 0f) * hand;
            Vector3 rot = new Vector3(Mathf.PerlinNoise(t * 0.5f, 3f) - 0.5f, Mathf.PerlinNoise(t * 0.45f, 4f) - 0.5f, 0f) * (0.6f + shake * 14f);
            if (shake > 0f)
            {
                jitter += new Vector3(Mathf.PerlinNoise(t * 22f, 5f) - 0.5f, Mathf.PerlinNoise(t * 25f, 6f) - 0.5f, 0f) * shake * 0.12f;
                rot += new Vector3(Mathf.PerlinNoise(t * 20f, 7f) - 0.5f, 0f, Mathf.PerlinNoise(t * 18f, 8f) - 0.5f) * shake * 6f;
            }
            cam.transform.SetPositionAndRotation(basePos + baseRot * jitter, baseRot * Quaternion.Euler(rot));
            UpdateSkip();
        }

        // ------------------------------------------------------------ look

        private void Apply(Atmosphere a)
        {
            cam.backgroundColor = a.sky;
            RenderSettings.ambientLight = a.ambient;
            RenderSettings.fogColor = a.fog;
            RenderSettings.fogDensity = a.fogDensity;
            Shader.SetGlobalFloat(PaletteReduceId, a.palette);
            if (outdoorVolume != null) outdoorVolume.weight = a.outdoor;
            if (sun != null)
            {
                sun.color = a.sun;
                sun.intensity = a.sunIntensity;
                sun.enabled = a.sunIntensity > 0.001f;
                sun.transform.rotation = Quaternion.Euler(a.sunEuler);
            }
        }

        private IEnumerator Blend(Atmosphere from, Atmosphere to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                Apply(new Atmosphere
                {
                    sky = Color.Lerp(from.sky, to.sky, k), ambient = Color.Lerp(from.ambient, to.ambient, k),
                    fog = Color.Lerp(from.fog, to.fog, k), sun = Color.Lerp(from.sun, to.sun, k),
                    fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, k), sunIntensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, k),
                    sunEuler = Vector3.Lerp(from.sunEuler, to.sunEuler, k),
                    outdoor = Mathf.Lerp(from.outdoor, to.outdoor, k), palette = Mathf.Lerp(from.palette, to.palette, k),
                });
                yield return null;
            }
            Apply(to);
        }

        private static Coroutine Fade(float alpha, float duration) =>
            ScreenFader.Instance != null ? ScreenFader.Instance.FadeTo(alpha, duration) : null;

        private static IEnumerator FadeAudio(AudioSource source, float to, float duration)
        {
            if (source == null) yield break;
            float from = source.volume;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            source.volume = to;
        }

        // ------------------------------------------------------------ skip / end

        private void UpdateSkip()
        {
            if (ending) return;
            bool held = (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.enterKey.isPressed || Keyboard.current.escapeKey.isPressed)) ||
                        (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed));
            bool anyKey = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                          (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (anyKey || held) skipShown = 3f;
            skipShown -= Time.unscaledDeltaTime;
            skipHeld = held ? skipHeld + Time.unscaledDeltaTime : Mathf.MoveTowards(skipHeld, 0f, Time.unscaledDeltaTime * 2f);
            ui.SetSkip(Mathf.Clamp01(skipHeld / holdToSkip), skipShown > 0f);
            if (skipHeld >= holdToSkip) StartCoroutine(SkipOut());
        }

        private IEnumerator SkipOut()
        {
            if (ending) yield break;
            ending = true;
            if (runRoutine != null) StopCoroutine(runRoutine);
            if (music != null) StartCoroutine(FadeAudio(music, 0f, 0.6f));
            if (dread != null) StartCoroutine(FadeAudio(dread, 0f, 0.6f));
            yield return Fade(1f, 0.6f);
            Finish();
        }

        private void Finish()
        {
            ending = true;
            Shader.SetGlobalFloat(PaletteReduceId, 0f);
            SceneLoader.Load(SceneNames.TombEntrance);
        }
    }
}
