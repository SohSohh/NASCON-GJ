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
        [Tooltip("Reaction close-up: someone looks up and sees it.")]
        [SerializeField] private Transform lookUpFrom;
        [SerializeField] private Transform lookUpTo;
        [Tooltip("Behind the townsfolk, looking down the street as the darkness floods toward the lens.")]
        [SerializeField] private Transform floodFrom;
        [SerializeField] private Transform floodTo;
        [Tooltip("Afterwards: slow push in on the shadows burned into the walls.")]
        [SerializeField] private Transform imprintFrom;
        [SerializeField] private Transform imprintTo;
        [SerializeField] private Light imprintLight;
        [SerializeField] private float floodRadius = 140f;
        [SerializeField] private float floodSoftness = 6f;

        [Header("3–5. The bearer")]
        [SerializeField] private CutsceneActor bearer;
        [Header("Rooftop (present day)")]
        [SerializeField] private Villager girl;
        [SerializeField] private Transform roofBearer;
        [SerializeField] private Transform roofWideFrom, roofWideTo, roofGirl, roofBearerCam, roofLookUp, roofVoid;
        [SerializeField] private MemoryFall memoryFall;
        [Header("Voice-over (played in order, one per Line)")]
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioClip[] voiceLines;
        private int voiceIndex;
        [SerializeField] private AudioSource stormAmbience;
        [Header("Sound design")]
        [SerializeField] private AudioSource thunderSource;   // the long thunder roll (AMB_RainThunder), cued per strike
        [SerializeField] private AudioSource windAmbience;    // the Void's wind
        [SerializeField] private AudioSource lowSource;       // pitched-down one-shots (impacts, crackle)
        [SerializeField] private AudioClip laughClip, crackleClip, swishHeavyClip;
        [SerializeField] private AudioClip fallClip, cryClip;
        [SerializeField] private AudioSource insideAmbience;   // dripping dark inside the tomb
        [SerializeField] private AudioSource padSource;        // a lone pad under the walk to the tomb
        [SerializeField] private Transform[] walkPath;
        [SerializeField] private Vector3 followStart = new(0.5f, 1.55f, 1.7f);   // chest-up, in front: the torch passing the lens
        [SerializeField] private Vector3 followEnd = new(1.8f, 1.6f, 0.9f);      // drifting out to the side
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
        [SerializeField] private AudioSource townAmbience;
        [SerializeField] private AudioSource townMurmur;
        [SerializeField] private AudioClip whooshClip;
        [SerializeField] private AudioClip riserClip;
        [SerializeField] private AudioClip boomClip;
        [SerializeField] private AudioClip ringClip;
        [SerializeField] private AudioClip heartbeatClip;

        [Header("Atmospheres")]
        [SerializeField] private Atmosphere day = new()
        {
            sky = new Color(0.55f, 0.78f, 0.98f), ambient = new Color(0.5f, 0.47f, 0.55f), fog = new Color(0.72f, 0.82f, 0.95f),
            sun = new Color(1f, 0.9f, 0.72f), fogDensity = 0.004f, sunIntensity = 2.1f, sunEuler = new Vector3(32f, 28f, 0f), outdoor = 1f, palette = 0.85f,
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
        private static readonly int SkyTopId = Shader.PropertyToID("_NasconSkyTop");
        private static readonly int SkyHorizonId = Shader.PropertyToID("_NasconSkyHorizon");
        private static readonly int SkyCloudId = Shader.PropertyToID("_NasconSkyCloud");
        private static readonly int SkyGlowId = Shader.PropertyToID("_NasconSkyGlow");
        private static readonly int SkyGlowDirId = Shader.PropertyToID("_NasconSkyGlowDir");

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

        private void OnDestroy()
        {
            Shader.SetGlobalFloat(PaletteReduceId, 0f);
            ComicLook.SetVoid(Vector3.zero, 0f, 1f);
            ComicLook.SetImpact(0f);
            ComicLook.SetDrain(0f);
        }

        // ------------------------------------------------------------ the film

        private IEnumerator Run()
        {
            ui.Letterbox(true, 0.01f);
            Apply(day);
            if (music != null) { music.volume = 0f; StartCoroutine(FadeAudio(music, 0.55f, 2f)); }
            ComicLook.Apply(0.7f, 0.55f, 0f);
            ComicLook.SetDrain(0f);
            if (townAmbience != null) { townAmbience.volume = 0f; townAmbience.Play(); StartCoroutine(FadeAudio(townAmbience, 0.55f, 1.5f)); }
            if (townMurmur != null) { townMurmur.volume = 0f; townMurmur.Play(); StartCoroutine(FadeAudio(townMurmur, 0.3f, 2f)); }
            var pose = bearer != null ? bearer.GetComponent<NasconGJ.Player.TorchBearerPose>() : null;
            if (bearer != null && roofBearer != null) bearer.Teleport(roofBearer);
            if (pose != null && girl != null) pose.LookTarget = girl.transform;
            if (pose != null) pose.Holding = false;          // daytime: no torch

            // 1. the town today, busy: two quick moves
            CameraMove(craneFrom, craneTo, 4.2f, 40f, 34f);
            Fade(0f, 0.8f);
            ui.Caption("Years later.", 1.8f);
            yield return new WaitForSeconds(3.4f);
            Cut();
            var walker = villagers.Length > 0 ? villagers[Mathf.Clamp(dollyVillager, 0, villagers.Length - 1)] : null;
            if (walker != null) CameraFollow(walker.transform, new Vector3(-2.2f, 1.5f, -2.6f), new Vector3(0.6f, 1.45f, -3.1f), new Vector3(0.5f, 1.35f, 0f), 3.4f, false, 36f);
            if (sfx != null && laughClip != null) sfx.PlayOneShot(laughClip, 0.12f);   // kids somewhere down the street
            yield return new WaitForSeconds(2.8f);

            // 2. a rooftop at dusk: the bearer and their sister, talking about what's left after
            Cut();
            CameraMove(roofWideFrom, roofWideTo, 5f, 42f, 38f);
            StartCoroutine(SnapAfter(1.2f));
            yield return Line(GirlName, "You ever think about the old tomb? Past the hills.", 0.1f);
            Cut();
            CameraMove(roofGirl, roofGirl, 4f, 30f, 27f);
            StartCoroutine(SnapAfter(1f));
            yield return Line(GirlName, "Grandpa swears something's still in there.", 0f);
            Cut();
            CameraMove(roofBearerCam, roofBearerCam, 3f, 30f, 28f);
            StartCoroutine(SnapAfter(0.8f));
            yield return Line(BearerName, "I mean, he also swears the well talks back.", 0f);
            Cut();
            CameraMove(roofGirl, roofGirl, 3f, 27f, 25f);
            if (townAmbience != null) StartCoroutine(FadeAudio(townAmbience, 0f, 0.5f)); // the birds stop
            if (townMurmur != null) StartCoroutine(FadeAudio(townMurmur, 0f, 1f));
            if (music != null) StartCoroutine(FadeAudio(music, 0f, 0.6f));
            if (windAmbience != null) { windAmbience.volume = 0f; windAmbience.Play(); StartCoroutine(FadeAudio(windAmbience, 0.55f, 3f)); }
            yield return Line(GirlName, "...Hey. Why'd it go so quiet?", 0f);

            // 3. they look up. THE VOID.
            var head = voidApparition != null ? voidApparition.Head : null;
            if (girl != null) girl.LookAt(head);
            if (pose != null) pose.LookTarget = head;
            for (int i = 0; i < villagers.Length; i++) StartCoroutine(LookUp(villagers[i], head, 0.1f + i * 0.12f));
            Cut();
            CameraMove(roofLookUp, roofLookUp, 2f, 34f, 30f);
            StartCoroutine(Blend(day, theVoid, 2f));
            if (voidApparition != null) voidApparition.Rise(4.5f);
            // it arrives with the sky splitting open: the full thunder roll, a deep slowed boom, the air tearing
            ThunderRoll(1f);
            PlayLow(thunderClip, 0.5f, 1f);
            if (sfx != null && swishHeavyClip != null) sfx.PlayOneShot(swishHeavyClip, 1f);
            StartCoroutine(Shake(0.8f, 0.25f));
            yield return new WaitForSeconds(1.6f);
            CameraMove(roofVoid, roofVoid, 5.5f, 50f, 38f);
            if (sfx != null && riserClip != null) sfx.PlayOneShot(riserClip, 0.9f);
            if (dread != null) { dread.volume = 0f; dread.Play(); StartCoroutine(FadeAudio(dread, 0.8f, 1.5f)); }
            StartCoroutine(FadeLight(voidWash, voidWashIntensity, 2f));
            yield return Thunderbolt();
            if (voidApparition != null) voidApparition.SpreadArms(0.8f);
            StartCoroutine(SnapAfter(1.2f));
            yield return new WaitForSeconds(1.6f);
            yield return Thunderbolt();
            yield return new WaitForSeconds(1.1f);

            // 4. the shockwave: the street, then the rooftop. She turns to shadow beside them; the torch keeps them.
            Cut();
            CameraMove(floodFrom, floodTo, 3.2f, 44f, 40f);
            Vector3 origin = voidApparition != null ? voidApparition.transform.position + Vector3.up * 12f : Vector3.zero;
            float camDist = Vector3.Distance(origin, floodFrom.position);
            ComicLook.SetVoidDark(0.2f);
            var taken = new bool[villagers.Length];
            if (sfx != null && boomClip != null) sfx.PlayOneShot(boomClip, 1f);
            for (float t = 0f; t < 2.6f; t += Time.deltaTime)
            {
                float k = t / 2.6f;
                float r = Mathf.Lerp(10f, camDist + 25f, k * k * (3f - 2f * k));
                ComicLook.SetVoid(origin, r, floodSoftness);
                for (int i = 0; i < villagers.Length; i++)
                {
                    var v = villagers[i];
                    if (v == null || taken[i] || Vector3.Distance(origin, v.transform.position + Vector3.up) > r - floodSoftness) continue;
                    taken[i] = true;
                    v.Shadowify(0.4f, statue: true);
                    if (imprints != null && i < imprints.Length && imprints[i] != null) imprints[i].SetLook(0.95f, 0f);
                }
                shake = 0.22f * Mathf.Sin(k * Mathf.PI);
                yield return null;
            }
            shake = 0f;
            for (int i = 0; i < villagers.Length; i++)
                if (!taken[i] && villagers[i] != null) { villagers[i].Shadowify(0.4f, statue: true); if (imprints != null && i < imprints.Length && imprints[i] != null) imprints[i].SetLook(0.95f, 0f); }
            ComicLook.SetVoid(origin, 2000f, floodSoftness);
            StartCoroutine(LightsOut(1f));
            Cut();
            CameraMove(roofGirl, roofGirl, 2.6f, 27f, 24f);
            if (girl != null) girl.Shadowify(0.9f, statue: true);
            StartCoroutine(SnapAfter(1.5f));
            if (sfx != null && ringClip != null) sfx.PlayOneShot(ringClip, 0.6f);
            if (sfx != null && heartbeatClip != null) sfx.PlayOneShot(heartbeatClip, 0.8f);
            if (pose != null && girl != null) pose.LookTarget = girl.transform;
            yield return new WaitForSeconds(2.2f);
            Cut();
            ComicLook.SetDrain(0.25f);
            StartCoroutine(FadeLight(imprintLight, 40f, 0.8f));
            CameraMove(imprintFrom, imprintTo, 3.6f, 40f, 34f);
            StartCoroutine(SnapAfter(2f));
            yield return Line(null, "They were neither dead nor taken, but emptied.", 0f);
            yield return Fade(1f, 0.5f);
            ComicLook.SetDrain(0f);
            ComicLook.SetVoid(Vector3.zero, 0f, 1f);
            ComicLook.Apply(0.6f, 0.3f, 0f);

            // 5. the one left: the torch passes the lens
            Apply(waste);
            if (pose != null) { pose.LookTarget = null; pose.Holding = true; } // the light they carry from now on
            if (windAmbience != null) StartCoroutine(FadeAudio(windAmbience, 0.35f, 1f));
            if (padSource != null) { padSource.volume = 0f; padSource.Play(); StartCoroutine(FadeAudio(padSource, 0.5f, 2f)); }
            bearer.Teleport(walkPath[0]);
            bearer.Walk(walkPath, 1.3f);
            CameraFollow(bearer.transform, followStart, followEnd, new Vector3(0f, 1.45f, 0f), 7f, true, 42f);
            if (dread != null) StartCoroutine(FadeAudio(dread, 0.35f, 1.5f));
            Fade(0f, 0.6f);
            yield return Line(null, "One soul was left untouched. And they remembered the old man's story.", 0.3f);
            yield return Fade(1f, 0.4f);

            // 6. the tomb
            Apply(storm);
            bearer.Teleport(tombPath[0]);
            bearer.Walk(tombPath, 1.2f);
            CameraMove(tombFrom, tombTo, 4.5f, 52f, 47f);
            if (dread != null) StartCoroutine(FadeAudio(dread, 0.6f, 2f));
            Fade(0f, 0.4f);
            if (windAmbience != null) StartCoroutine(FadeAudio(windAmbience, 0f, 1.5f));
            if (padSource != null) StartCoroutine(FadeAudio(padSource, 0.25f, 2f));
            if (stormAmbience != null) { stormAmbience.time = Mathf.Min(14f, stormAmbience.clip.length - 1f); stormAmbience.volume = 0f; stormAmbience.Play(); StartCoroutine(FadeAudio(stormAmbience, 0.8f, 0.6f)); }
            yield return new WaitForSeconds(1.2f);
            StartCoroutine(Lightning());
            StartCoroutine(SnapAfter(0.06f));
            yield return Line(null, "Many had walked into that tomb over the ages.", 0f);
            CameraMove(doorwayFrom, doorwayTo, 3.8f, 46f, 42f);
            StartCoroutine(Lightning());
            yield return Line(null, "None had ever walked out.", 0.2f);

            // 7. inside, two steps into the dark... then nothing. Hard black, silence, and the fall through their memories.
            Apply(tomb);
            bearer.Teleport(insidePath[0]);
            bearer.Walk(insidePath, 1.2f);
            CameraFollow(bearer.transform, shoulderOffset, shoulderOffset + new Vector3(0f, 0.1f, 0.4f), new Vector3(0f, 1.3f, 0f), 30f, true, 50f);
            Fade(0f, 0.3f);
            if (stormAmbience != null) StartCoroutine(FadeAudio(stormAmbience, 0.2f, 1f)); // muffled, inside
            if (insideAmbience != null) { insideAmbience.volume = 0f; insideAmbience.Play(); StartCoroutine(FadeAudio(insideAmbience, 0.7f, 1f)); }
            if (padSource != null) StartCoroutine(FadeAudio(padSource, 0f, 1.5f));
            float waited = 0f;
            while (bearer.Busy && waited < 3f) { waited += Time.deltaTime; yield return null; }

            // the floor gives way: rumble, the slabs drop, they fall, the camera watches them go into the dark
            if (sfx != null && collapseClip != null) sfx.PlayOneShot(collapseClip, 1f);
            for (float t = 0f; t < 0.9f; t += Time.deltaTime) { shake = Mathf.Lerp(0.05f, 0.5f, t / 0.9f); yield return null; }
            if (collapseDust != null) collapseDust.Play();
            foreach (var p in collapsePieces)
            {
                if (p == null) continue;
                p.isKinematic = false;
                p.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
            }
            bearer.Fall();
            if (sfx != null && whooshClip != null) sfx.PlayOneShot(whooshClip, 1f);
            CameraTrack(bearer.transform, new Vector3(0f, 1.2f, 0f));
            // the floor cracks open under them: a hard crack, the camera jolts and judders
            PlayLow(crackleClip, 0.55f, 1f);
            PlayLow(impactClip, 0.7f, 1f);
            if (sfx != null && fallClip != null) sfx.PlayOneShot(fallClip, 1f);
            ComicLook.SetImpact(1f);
            yield return new WaitForSeconds(0.06f);
            ComicLook.SetImpact(0f);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime) { shake = Mathf.Lerp(0.9f, 0.4f, t / 0.7f) * (Random.value < 0.3f ? 1.4f : 1f); yield return null; }
            // ...and they drop away into the depth as the dark closes in
            StartCoroutine(FovDive(1.8f));
            for (float t = 0f; t < 1.8f; t += Time.deltaTime) { shake = Mathf.Lerp(0.4f, 0.05f, t / 1.8f); yield return null; }
            yield return Fade(1f, 0.8f);
            shake = 0f;
            foreach (var src in new[] { music, dread, townAmbience, townMurmur, stormAmbience, windAmbience, thunderSource, insideAmbience, padSource }) if (src != null) src.volume = 0f;
            yield return new WaitForSeconds(1f);                                  // a full second of black and silence
            if (memoryFall != null) yield return memoryFall.Play();
            if (sfx != null && cryClip != null) sfx.PlayOneShot(cryClip, 0.75f);  // only a heartbeat in the dark
            yield return new WaitForSeconds(4.2f);
            Finish();
        }

        private const string GirlName = "YUNA", BearerName = "REN";

        /// <summary>Cue the long thunder roll from the storm recording (its big crack is ~14 s in).</summary>
        private void ThunderRoll(float volume)
        {
            if (thunderSource == null || thunderSource.clip == null) return;
            thunderSource.Stop();
            thunderSource.time = 0f;
            thunderSource.volume = volume;
            thunderSource.Play();
            StartCoroutine(StopAfter(thunderSource, thunderSource.clip.length));
        }

        private IEnumerator StopAfter(AudioSource s, float seconds)
        {
            yield return new WaitForSeconds(seconds - 1.2f);
            yield return FadeAudio(s, 0f, 1.2f);
            s.Stop();
        }

        /// <summary>One-shot played slowed down (heavier, deeper) on its own source.</summary>
        private void PlayLow(AudioClip clip, float pitch, float volume)
        {
            if (lowSource == null || clip == null) return;
            lowSource.pitch = pitch;
            lowSource.PlayOneShot(clip, volume);
        }

        /// <summary>Narrow the lens while falling, so the pit seems to swallow the shot.</summary>
        private IEnumerator FovDive(float duration)
        {
            float from = cam.fieldOfView;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                cam.fieldOfView = Mathf.Lerp(from, from * 0.55f, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
        }

        private IEnumerator SnapAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return new WaitForEndOfFrame();
            if (memoryFall != null) memoryFall.Snap(cam);
        }

        /// <summary>A spoken line as a caption (speaker in gold), held long enough to read, then the next cut.</summary>
        private IEnumerator Line(string speaker, string text, float extra)
        {
            int words = text.Split(' ').Length;
            float hold = 0.7f + words * 0.26f + extra;
            AudioClip vo = voiceLines != null && voiceIndex < voiceLines.Length ? voiceLines[voiceIndex] : null;
            voiceIndex++;
            if (vo != null && voice != null) { voice.PlayOneShot(vo); hold = vo.length + 0.1f + extra; }
            ui.Caption(string.IsNullOrEmpty(speaker) ? text : $"<color=#E8C27A>{speaker}</color>   {text}", hold);
            yield return new WaitForSeconds(hold + 0.35f);
        }

        /// <summary>A thunderbolt hit: lightning around the Void, two hard impact frames, a crack of thunder, a jolt.</summary>
        private IEnumerator Thunderbolt()
        {
            if (voidApparition != null) voidApparition.Strike();
            if (sfx != null && thunderClip != null) sfx.PlayOneShot(thunderClip, 1f);
            if (sfx != null && swishHeavyClip != null) sfx.PlayOneShot(swishHeavyClip, 0.6f);
            StartCoroutine(Shake(0.6f, 0.35f));
            ComicLook.SetImpact(1f);
            yield return new WaitForSeconds(0.07f);
            ComicLook.SetImpact(0f);
            yield return new WaitForSeconds(0.06f);
            ComicLook.SetImpact(1f);
            yield return new WaitForSeconds(0.05f);
            ComicLook.SetImpact(0f);
        }

        private void Cut()
        {
            if (sfx != null && whooshClip != null) sfx.PlayOneShot(whooshClip, 0.35f);
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
                if (p >= 1f) { if (sfx != null && thunderClip != null) sfx.PlayOneShot(thunderClip, 0.65f); StartCoroutine(Shake(0.35f, 0.15f)); }
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
            Shader.SetGlobalColor(SkyTopId, a.sky * 0.6f);
            Shader.SetGlobalColor(SkyHorizonId, Color.Lerp(a.sky, a.fog, 0.5f) * 1.1f);
            Shader.SetGlobalColor(SkyCloudId, Color.Lerp(a.sky * 0.35f, new Color(1f, 0.97f, 0.92f), a.palette));
            Shader.SetGlobalColor(SkyGlowId, a.sun * a.sunIntensity * 0.35f);
            Shader.SetGlobalVector(SkyGlowDirId, -(Quaternion.Euler(a.sunEuler) * Vector3.forward));
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
