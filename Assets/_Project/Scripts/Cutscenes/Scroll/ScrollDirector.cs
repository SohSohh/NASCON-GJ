using System.Collections;
using NasconGJ.Core;
using NasconGJ.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NasconGJ.Cutscenes.Scroll
{
    /// <summary>
    /// C01 prologue as a painted story scroll: shadow-puppet theatre and ink-wash landscapes, cut together with
    /// ink-drop and brush-sweep transitions (in the spirit of the 2D backstory sequences in Kung Fu Panda 2/3).
    /// Every line of the narration is a caption for now; the voice-over will replace them 1:1 (same timings).
    ///
    /// Scene layout (built by LevelBuilder.Prologue): panels are children named Panel_*, each an <see cref="InkPanel"/>;
    /// puppets and layers are found by name, so art can be moved around freely in the editor.
    /// </summary>
    public class ScrollDirector : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private CutsceneUI ui;
        [SerializeField] private InkTransitionOverlay ink;

        [Header("Audio")]
        [SerializeField] private AudioSource ambienceA;
        [SerializeField] private AudioSource ambienceB;
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource dread;
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip birds, fireplace, wind, rainThunder;
        [SerializeField] private AudioClip paperClip, pageFlip, swishLight, swishHeavy, gong, bell, laugh, boom, heartbeat, step, igniteClip;

        [Header("Timing")]
        [Tooltip("Seconds per word added to each caption's hold (tune to the recorded narration later).")]
        [SerializeField] private float secondsPerWord = 0.27f;
        [Header("Voice-over (played in order, one per Say)")]
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioClip[] voiceLines;
        private int voiceIndex;
        [SerializeField] private float holdToSkip = 1.2f;

        private static readonly Color Ink = new(0.04f, 0.025f, 0.03f, 1f);
        private static readonly Color PaperWhite = new(0.95f, 0.9f, 0.78f, 1f);

        private Coroutine run, camRoutine;
        private Vector3 camPos;
        private float camFov = 30f, shake, skipHeld, skipShown;
        private bool ending, dofSearched;
        private UnityEngine.Rendering.Universal.DepthOfField dof;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            camPos = cam.transform.position;
            run = StartCoroutine(Film());
        }

        // =====================================================================
        //  the film
        // =====================================================================

        private IEnumerator Film()
        {
            var town = Panel("Panel_Town");
            var fire = Panel("Panel_Fire");
            var legend = Panel("Panel_Legend");
            var voidP = Panel("Panel_Void");

            ink.SetCovered(1f, Ink);
            ui.Letterbox(true, 0.01f);
            if (music != null && music.clip != null && !music.isPlaying) { music.volume = 0f; music.Play(); }

            // 1. a lantern glows; the town paints itself in around it
            town.SetNight(1f);
            town.SetLampsOut(1f);
            SetCam(Pt(town, "Cam_Lantern"), 26f);
            StartCoroutine(ink.Drop(new Vector2(0.5f, 0.55f), 1.1f, Ink, cover: false));
            Play(igniteClip, 0.7f);
            yield return LanternUp(town, 1.2f);
            Play(paperClip, 0.6f);
            Ambience(ambienceA, birds, 0.6f, 2f);
            Fade(music, 0.55f, 2.5f);
            StartCoroutine(town.Blend(0f, 2.4f));
            StartCoroutine(town.PaintOn(2.2f));
            Move(Pt(town, "Cam_Lantern"), Pt(town, "Cam_TownWide"), 4.5f, 26f, 32f);
            StartTownLife(town);
            yield return Wait(0.8f);
            yield return Say("There was once a town that lived in the light.");

            // 2. life, in quick cuts
            Play(swishLight, 0.35f);
            SetCam(Pt(town, "Cam_Market"), 24f);
            Move(Pt(town, "Cam_Market"), Pt(town, "Cam_Street"), 6f, 24f, 28f);
            Play(laugh, 0.3f);
            yield return Say("They worked, they laughed, they quarreled and made up.");

            // 3. the elder by the fire
            Play(pageFlip, 0.7f);
            yield return ink.Sweep(new Vector2(1f, 0.2f), 0.5f, Ink);
            fire.SetNight(0.5f);
            Ambience(ambienceA, fireplace, 0.65f, 0.8f);
            Ambience(ambienceB, wind, 0.18f, 1.2f);                   // night air under the crackle
            Fade(music, 0.4f, 1f);
            SetCam(Pt(fire, "Cam_FireWide"), 30f);
            Move(Pt(fire, "Cam_FireWide"), Pt(fire, "Cam_FireClose"), 7f, 30f, 26f);
            Puppet(fire, "Elder").Do(InkPuppet.Act.Story);
            foreach (var c in fire.GetComponentsInChildren<InkPuppet>()) if (c.name.StartsWith("Child")) c.Do(InkPuppet.Act.Listen);
            yield return ink.Sweep(new Vector2(1f, 0.2f), 0.5f, Ink, cover: false);
            yield return Say("And in that town lived an old elder, who told the same story every night.");

            // 4. the legend: a tomb beyond the hills
            Play(swishLight, 0.6f);
            yield return ink.Drop(new Vector2(0.35f, 0.6f), 0.5f, PaperWhite);
            Fade(ambienceA, 0.15f, 0.8f);
            Ambience(ambienceB, wind, 0.4f, 1f);                      // wind over the hills
            Fade(music, 0.6f, 1.5f);
            SetCam(Pt(legend, "Cam_LegendFrom"), 30f);
            Move(Pt(legend, "Cam_LegendFrom"), Pt(legend, "Cam_LegendTo"), 7f, 30f, 22f);
            StartCoroutine(legend.PaintOn(1.6f));
            yield return ink.Drop(new Vector2(0.5f, 0.5f), 0.6f, PaperWhite, cover: false);
            yield return Say("Of a tomb beyond the hills, older than anyone's memory.");
            Play(gong, 0.4f);
            yield return Say("And of what lived inside it.");

            // 5. the children's shadows become something else
            yield return ink.Drop(new Vector2(0.5f, 0.5f), 0.5f, Ink);
            Ambience(ambienceA, fireplace, 0.5f, 0.5f);
            Fade(ambienceB, 0.1f, 0.8f);
            SetCam(Pt(fire, "Cam_Wall"), 28f);
            Move(Pt(fire, "Cam_Wall"), Pt(fire, "Cam_WallClose"), 6f, 28f, 24f);
            yield return ink.Drop(new Vector2(0.5f, 0.5f), 0.5f, Ink, cover: false);
            Fade(music, 0f, 2f);
            dread.volume = 0f; dread.Play(); Fade(dread, 0.55f, 2.5f);
            Play(boom, 0.55f);                                         // the shadow tears loose
            yield return MorphShadowsToVoid(fire, 2.4f);
            yield return Say("A spirit made of shadow. The Void.");

            // 6. the Void alone in the dark
            Play(swishHeavy, 0.5f);
            yield return ink.Drop(new Vector2(0.5f, 0.75f), 0.5f, Ink);
            Ambience(ambienceA, wind, 0.45f, 0.6f);                       // empty wind in the deepest dark
            SetCam(Pt(voidP, "Cam_VoidFrom"), 30f);
            Move(Pt(voidP, "Cam_VoidFrom"), Pt(voidP, "Cam_VoidTo"), 9f, 30f, 24f);
            yield return ink.Drop(new Vector2(0.5f, 0.6f), 0.8f, Ink, cover: false);
            yield return Say("It had no face, no voice, and no name.");
            Play(heartbeat, 0.5f);
            yield return Say("And because it had nothing, it hungered. For a face. A voice. A self.");

            // 7. it reaches for the children
            yield return ink.Sweep(new Vector2(-1f, 0f), 0.5f, Ink);
            Ambience(ambienceA, fireplace, 0.5f, 0.5f);
            SetCam(Pt(fire, "Cam_WallClose"), 24f);
            Move(Pt(fire, "Cam_WallClose"), Pt(fire, "Cam_Wall"), 5f, 24f, 28f);
            yield return ink.Sweep(new Vector2(-1f, 0f), 0.5f, Ink, cover: false);
            StartCoroutine(VoidReach(fire, 3f));
            yield return Say("“One day,” he would whisper, “it will come looking for what it lacks.”");

            // 8. nobody believes him; years pass
            StartCoroutine(FadeVoidShadow(fire, 0f, 0.8f));
            Fade(dread, 0f, 1.2f);
            Fade(music, 0.4f, 1.5f);
            SetCam(Pt(fire, "Cam_FireWide"), 30f);
            foreach (var c in fire.GetComponentsInChildren<InkPuppet>()) if (c.name.StartsWith("Child") && c.Shadow != null) c.Do(InkPuppet.Act.Laugh);
            Play(laugh, 0.35f);
            Fade(ambienceB, 0.12f, 1f);
            StartCoroutine(AdultsWalkPast(fire));
            yield return Say("The children laughed. The adults shook their heads.");
            yield return SeasonsPass(fire);
            yield return Say("In time, even the children stopped listening.");

            // out: the page turns to the present
            Play(pageFlip, 0.8f);
            Fade(music, 0f, 0.8f);
            Fade(ambienceA, 0f, 0.8f);
            Fade(ambienceB, 0f, 0.8f);
            yield return ink.Drop(new Vector2(0.5f, 0.5f), 0.7f, Ink);
            Finish();
        }

        // =====================================================================
        //  beats
        // =====================================================================

        private IEnumerator LanternUp(InkPanel town, float duration)
        {
            var glow = town.transform.Find("HeroLantern/Glow");
            var lantern = town.transform.Find("HeroLantern/Lantern")?.GetComponent<Renderer>();
            var mat = lantern != null ? lantern.material : null;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration) * (0.85f + 0.15f * Mathf.PerlinNoise(t * 6f, 0f));
                if (glow != null) glow.localScale = Vector3.one * Mathf.Lerp(0.2f, 6f, k);
                if (mat != null) mat.SetFloat("_AccentGlow", k * 2.5f);
                yield return null;
            }
        }

        private void StartTownLife(InkPanel town)
        {
            foreach (var p in town.GetComponentsInChildren<InkPuppet>(true))
            {
                if (p.name.StartsWith("Bearer")) { p.SetOpacity(0f); continue; }
                if (p.name.StartsWith("Vendor") || p.name.StartsWith("Baker")) p.Do(InkPuppet.Act.Sell);
                else if (p.name.StartsWith("Runner")) StartCoroutine(RunLoop(p));
                else if (p.name.StartsWith("Walker")) StartCoroutine(WalkLoop(p));
                else if (p.name.StartsWith("Mother")) p.Do(InkPuppet.Act.Wave);
                else if (p.name.StartsWith("Carrier")) StartCoroutine(WalkLoop(p, carry: true));
            }
        }

        private void StopTownLife(InkPanel town, bool idle)
        {
            StopCoroutineSafe();
            foreach (var p in town.GetComponentsInChildren<InkPuppet>(true))
                if (!p.name.StartsWith("Bearer") && !p.name.EndsWith("_WallShadow")) p.Do(idle ? InkPuppet.Act.Idle : InkPuppet.Act.Frozen);
        }

        private int lifeEpoch;
        private void StopCoroutineSafe() => lifeEpoch++;

        private IEnumerator RunLoop(InkPuppet p)
        {
            int epoch = lifeEpoch;
            float a = p.transform.position.x - 4f, b = p.transform.position.x + 4f;
            while (epoch == lifeEpoch)
            {
                yield return p.WalkTo(b, 2.6f, run: true);
                if (epoch != lifeEpoch) break;
                p.Do(InkPuppet.Act.Laugh);
                yield return new WaitForSeconds(0.8f);
                yield return p.WalkTo(a, 2.6f, run: true);
            }
        }

        private IEnumerator WalkLoop(InkPuppet p, bool carry = false)
        {
            int epoch = lifeEpoch;
            float a = p.transform.position.x - 5f, b = p.transform.position.x + 5f;
            while (epoch == lifeEpoch)
            {
                yield return p.WalkTo(b, 0.9f);
                if (epoch != lifeEpoch) break;
                if (carry) p.Do(InkPuppet.Act.Carry);
                yield return new WaitForSeconds(1.5f);
                yield return p.WalkTo(a, 0.9f);
                yield return new WaitForSeconds(1.2f);
            }
        }

        private IEnumerator MorphShadowsToVoid(InkPanel fire, float duration)
        {
            var voidShadow = fire.transform.Find("VoidShadow");
            var children = fire.GetComponentsInChildren<InkPuppet>();
            Play(heartbeat, 0.4f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                SetVoidShadow(fire, k);
                foreach (var c in children) if (c.name.StartsWith("Child")) c.SetGlow(1f - k);
                yield return null;
            }
        }

        private static void SetVoidShadow(InkPanel fire, float k)
        {
            var voidShadow = fire.transform.Find("VoidShadow");
            if (voidShadow == null) return;
            foreach (var r in voidShadow.GetComponentsInChildren<Renderer>()) r.material.SetFloat("_Opacity", k * 0.92f);
            voidShadow.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
        }

        private IEnumerator FadeVoidShadow(InkPanel fire, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                SetVoidShadow(fire, Mathf.Lerp(1f, to, t / duration));
                yield return null;
            }
            SetVoidShadow(fire, to);
        }

        private IEnumerator VoidReach(InkPanel fire, float duration)
        {
            var arm = fire.transform.Find("VoidShadow/Arm");
            if (arm == null) yield break;
            Quaternion from = arm.localRotation, to = Quaternion.Euler(0f, 0f, 42f); // sweeps down toward the children's shadows
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                arm.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
        }

        private IEnumerator AdultsWalkPast(InkPanel fire)
        {
            var man = Puppet(fire, "Man");
            var woman = Puppet(fire, "Woman");
            man.SetOpacity(1f); woman.SetOpacity(1f);
            man.WalkTo(man.transform.position.x + 9f, 0.9f);
            yield return new WaitForSeconds(1.2f);
            woman.WalkTo(woman.transform.position.x + 9f, 0.85f);
            yield return new WaitForSeconds(2.5f);
            man.Do(InkPuppet.Act.Wave); // waves him off
        }

        private IEnumerator SeasonsPass(InkPanel fire)
        {
            Play(pageFlip, 0.6f);
            var leaves = fire.transform.Find("Leaves")?.GetComponent<ParticleSystem>();
            var snow = fire.transform.Find("Snow")?.GetComponent<ParticleSystem>();
            var fireFx = fire.transform.Find("Fire");
            var kids = new System.Collections.Generic.List<InkPuppet>();
            foreach (var c in fire.GetComponentsInChildren<InkPuppet>()) if (c.name.StartsWith("Child") && c.Shadow != null) kids.Add(c);
            Move(Pt(fire, "Cam_FireWide"), Pt(fire, "Cam_FireClose"), 5f, 30f, 25f);
            if (leaves != null) leaves.Play();
            for (int i = 0; i < kids.Count; i++)
            {
                yield return new WaitForSeconds(0.7f);
                StartCoroutine(kids[i].FadeTo(0f, 0.8f));
                if (fireFx != null) fireFx.localScale *= 0.85f;
                if (i == 1 && leaves != null) { leaves.Stop(); if (snow != null) snow.Play(); }
            }
            Puppet(fire, "Elder").Do(InkPuppet.Act.Sit);
            yield return new WaitForSeconds(0.4f);
        }

        private IEnumerator LampsDie(InkPanel town, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                town.SetLampsOut(Mathf.Clamp01(Mathf.Floor(k * 6f) / 5f)); // in steps, house by house
                yield return null;
            }
            town.SetLampsOut(1f);
            Play(gong, 0.35f);
        }

        private IEnumerator VoidCloudPass(InkPanel town, float duration)
        {
            var cloud = town.transform.Find("VoidCloud");
            if (cloud == null) yield break;
            Vector3 a = Pt(town, "CloudFrom").position, b = Pt(town, "CloudTo").position;
            var mats = cloud.GetComponentsInChildren<Renderer>();
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                cloud.position = Vector3.Lerp(a, b, k);
                float o = Mathf.Sin(k * Mathf.PI);
                foreach (var r in mats) r.material.SetFloat("_Opacity", o);
                shake = o * 0.02f;
                yield return null;
            }
            shake = 0f;
        }

        private IEnumerator EmptyTheTown(InkPanel town, InkPanel fire, float duration)
        {
            Play(swishLight, 0.4f);
            foreach (var p in town.GetComponentsInChildren<InkPuppet>(true))
            {
                if (p.name.StartsWith("Bearer") || p.name.EndsWith("_WallShadow")) continue;
                p.Do(InkPuppet.Act.Frozen);
                p.LeaveShadow();
                StartCoroutine(p.FadeTo(0f, duration * (0.7f + Random.value * 0.3f)));
            }
            var elder = Puppet(fire, "Elder");
            if (elder != null) { elder.Do(InkPuppet.Act.Frozen); elder.LeaveShadow(); elder.SetOpacity(0f); }
            var fireFx = fire.transform.Find("Fire");
            if (fireFx != null) fireFx.gameObject.SetActive(false);
            yield return Wait(duration);
        }

        private IEnumerator Footsteps(float duration)
        {
            for (float t = 0f; t < duration; t += 0.62f)
            {
                Play(step, 0.25f);
                yield return new WaitForSeconds(0.62f);
            }
        }

        private IEnumerator Lightning(InkPanel tomb, bool revealRow)
        {
            var row = tomb.transform.Find("FigureRow")?.GetComponentInChildren<Renderer>();
            float[] pulses = { 1f, 0.2f, 0.85f, 0f };
            foreach (float p in pulses)
            {
                ink.SetCovered(p * 0.75f, new Color(0.92f, 0.94f, 1f, 1f));
                tomb.SetNight(1f - p * 0.9f);
                if (row != null) row.material.SetFloat("_Opacity", revealRow ? p : p * 0.5f);
                shake = p * 0.03f;
                yield return new WaitForSeconds(0.07f);
            }
            ink.SetCovered(0f, Ink);
            tomb.SetNight(1f);
            if (row != null) row.material.SetFloat("_Opacity", revealRow ? 0.12f : 0f);
            shake = 0f;
        }

        private IEnumerator Fall(InkPuppet p, float duration)
        {
            Vector3 start = p.transform.position;
            float vy = 0f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                vy -= 7f * Time.deltaTime;
                p.transform.position += Vector3.up * vy * Time.deltaTime;
                p.transform.rotation = Quaternion.Euler(0f, 0f, t * 140f);
                yield return null;
            }
        }

        // =====================================================================
        //  plumbing
        // =====================================================================

        private InkPanel Panel(string name) => transform.parent.Find(name).GetComponent<InkPanel>();
        private static Transform Pt(InkPanel panel, string name) => panel.transform.Find(name);
        private static InkPuppet Puppet(InkPanel panel, string name) => panel.transform.Find(name)?.GetComponent<InkPuppet>();

        private IEnumerator Say(string line, float extraHold = 0f)
        {
            int words = line.Split(' ').Length;
            float hold = 0.7f + words * secondsPerWord + extraHold;
            AudioClip vo = voiceLines != null && voiceIndex < voiceLines.Length ? voiceLines[voiceIndex] : null;
            voiceIndex++;
            if (vo != null && voice != null) { voice.PlayOneShot(vo); hold = vo.length + 0.15f + extraHold; }
            ui.Caption(line, hold);
            yield return new WaitForSeconds(hold + 0.35f);
        }

        private static IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        private void SetCam(Transform mark, float fov)
        {
            if (camRoutine != null) StopCoroutine(camRoutine);
            camPos = mark.position;
            camFov = fov;
        }

        private void Move(Transform from, Transform to, float duration, float fovFrom, float fovTo)
        {
            if (camRoutine != null) StopCoroutine(camRoutine);
            camRoutine = StartCoroutine(MoveRoutine(from.position, to.position, duration, fovFrom, fovTo));
        }

        private IEnumerator MoveRoutine(Vector3 a, Vector3 b, float duration, float fa, float fb)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                camPos = Vector3.Lerp(a, b, k);
                camFov = Mathf.Lerp(fa, fb, k);
                yield return null;
            }
        }

        private void LateUpdate()
        {
            float t = Time.time;
            Vector3 drift = new Vector3(Mathf.PerlinNoise(t * 0.15f, 1f) - 0.5f, Mathf.PerlinNoise(t * 0.13f, 2f) - 0.5f, 0f) * 0.15f;
            Vector3 jolt = shake > 0f ? new Vector3(Mathf.PerlinNoise(t * 25f, 3f) - 0.5f, Mathf.PerlinNoise(t * 27f, 4f) - 0.5f, 0f) * shake * 6f : Vector3.zero;
            cam.transform.position = camPos + drift + jolt;
            cam.transform.rotation = Quaternion.identity;
            cam.fieldOfView = camFov;
            if (dof == null && !dofSearched)
            {
                dofSearched = true;
                var vol = FindAnyObjectByType<UnityEngine.Rendering.Volume>();
                if (vol != null) vol.profile.TryGet(out dof);  // instance copy, the asset isn't touched
            }
            if (dof != null) dof.focusDistance.value = Mathf.Max(4f, -camPos.z); // the puppets live on z = 0
            UpdateSkip();
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && sfx != null) sfx.PlayOneShot(clip, volume);
        }

        private void Ambience(AudioSource src, AudioClip clip, float volume, float fade, float startAt = 0f)
        {
            if (src == null || clip == null) return;
            if (src.clip != clip) { src.clip = clip; src.volume = 0f; src.time = Mathf.Min(startAt, clip.length - 0.1f); src.Play(); }
            Fade(src, volume, fade);
        }

        private void Fade(AudioSource src, float to, float duration)
        {
            if (src != null) StartCoroutine(FadeRoutine(src, to, duration));
        }

        private static IEnumerator FadeRoutine(AudioSource s, float to, float d)
        {
            float from = s.volume;
            for (float t = 0f; t < d; t += Time.deltaTime) { s.volume = Mathf.Lerp(from, to, t / d); yield return null; }
            s.volume = to;
        }

        private void UpdateSkip()
        {
            if (ending) return;
            bool held = (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.enterKey.isPressed || Keyboard.current.escapeKey.isPressed)) ||
                        (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed));
            bool any = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (any || held) skipShown = 3f;
            skipShown -= Time.unscaledDeltaTime;
            skipHeld = held ? skipHeld + Time.unscaledDeltaTime : Mathf.MoveTowards(skipHeld, 0f, Time.unscaledDeltaTime * 2f);
            ui.SetSkip(Mathf.Clamp01(skipHeld / holdToSkip), skipShown > 0f);
            if (skipHeld >= holdToSkip) StartCoroutine(SkipOut());
        }

        private IEnumerator SkipOut()
        {
            if (ending) yield break;
            ending = true;
            if (run != null) StopCoroutine(run);
            yield return ink.Drop(new Vector2(0.5f, 0.5f), 0.7f, Ink);
            Finish();
        }

        private void Finish()
        {
            ending = true;
            SceneLoader.Load(SceneNames.PrologueTown);
        }
    }
}
