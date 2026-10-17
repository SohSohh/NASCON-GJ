using System.Collections;
using NasconGJ.Core;
using NasconGJ.Entities;
using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// C02, "The Cycle". Same town, rooftop and tomb as the prologue (built from the same scene), played forward:
    /// Ren walks out at dawn with a white flame; a ring of light races from the tomb to the town and the emptied people
    /// come back; Yuna turns and says Ren's name. Then the price: ink climbs Ren's hand and the flame dies, and out of the
    /// dark behind Ren steps the one who was the Void before, a man again, free. He left the town as bait so the next
    /// saviour would come and take his place. Ren walks back into the dark. Days later the old storyteller tells the
    /// children the tale that will bring the next one. Title, credits, menu.
    /// </summary>
    public class EndingDirector : PrologueDirector
    {
        [Header("Ending")]
        [SerializeField] private Transform[] exitPath;            // Ren out of the tomb into the dawn
        [SerializeField] private Transform dawnFrom, dawnTo;      // low, in front of the tomb
        [SerializeField] private Transform ringFrom, ringTo;      // high over the town, looking at the hills
        [SerializeField] private Transform streetFrom, streetTo;  // down the street as the light arrives
        [SerializeField] private Transform mouthFrom, mouthTo;    // the tomb mouth, Ren small in the doorway light
        [SerializeField] private LineRenderer lightRing;
        [SerializeField] private Transform crystal;               // what Ren brings out of the tomb
        [SerializeField] private Light crystalLight;
        [SerializeField] private Renderer crystalRenderer;
        [SerializeField] private Transform raiseCam;              // low, looking up as Ren lifts it
        [SerializeField] private Vector3 ringOrigin = new(200f, 0.6f, 30f);
        [SerializeField] private Villager formerVoid;             // the one who was the Void before
        [SerializeField] private Transform formerVoidStart;
        [SerializeField] private Transform[] formerVoidPath;
        [SerializeField] private Transform[] returnPath;          // Ren back into the dark
        [SerializeField] private Villager storyteller;            // the elder in the town square
        [SerializeField] private ShadowFigure storytellerShadow;  // his shadow on the wall, the one that stays
        [SerializeField] private Transform storyFrom, storyTo;
        [SerializeField] private Atmosphere dawn = new()
        {
            sky = new Color(1f, 0.74f, 0.52f), ambient = new Color(0.46f, 0.38f, 0.36f), fog = new Color(0.98f, 0.8f, 0.62f),
            sun = new Color(1f, 0.78f, 0.52f), fogDensity = 0.005f, sunIntensity = 1.9f, sunEuler = new Vector3(11f, 18f, 0f), outdoor = 1f, palette = 0.8f,
        };

        private const string VoidName = "THE ONE BEFORE", ElderName = "ELDER";

        protected override IEnumerator Run()
        {
            yield return null; // let every villager set itself up first
            ui.Letterbox(true, 0.01f);
            Fade(1f, 0.01f);
            ComicLook.Apply(0.65f, 0.4f, 0f);
            ComicLook.SetDrain(0f);
            var pose = bearer != null ? bearer.GetComponent<NasconGJ.Player.TorchBearerPose>() : null;

            // the town as it was left: everyone a black statue, every shadow burned into the walls
            foreach (var v in villagers) if (v != null) v.SetShadowed();
            if (girl != null) girl.SetShadowed();
            if (formerVoid != null) { formerVoid.transform.SetPositionAndRotation(formerVoidStart.position, formerVoidStart.rotation); formerVoid.SetShadowed(); }
            if (imprints != null) foreach (var f in imprints) if (f != null) f.SetLook(0.95f, 0f);
            if (storytellerShadow != null) storytellerShadow.SetLook(0f, 0f);   // revealed only in the last shot
            if (lightRing != null) lightRing.enabled = false;
            if (voidApparition != null) voidApparition.gameObject.SetActive(false);
            StartCoroutine(LightsOut(0.01f));

            // 1. white. Ren steps out of the tomb into the dawn, the crystal cradled in both hands
            Apply(waste);
            if (pose != null) { pose.Holding = false; pose.LookTarget = null; pose.Carried = crystal; pose.Raise = 0f; }
            if (crystalLight != null) crystalLight.intensity = 2.2f;
            bearer.Teleport(exitPath[0]);
            bearer.Walk(exitPath, 1.05f);
            CameraMove(dawnFrom, dawnTo, 7.5f, 44f, 38f);
            if (padSource != null) { padSource.volume = 0f; padSource.Play(); StartCoroutine(FadeAudio(padSource, 0.5f, 3f)); }
            if (windAmbience != null) { windAmbience.volume = 0f; windAmbience.Play(); StartCoroutine(FadeAudio(windAmbience, 0.25f, 2f)); }
            ComicLook.SetImpact(1f);
            yield return new WaitForSeconds(0.08f);
            ComicLook.SetImpact(0f);
            Fade(0f, 2.2f);
            yield return new WaitForSeconds(0.5f);
            yield return Line(null, "And from the tomb no one had ever left, someone finally came out.", 0f);

            // 2. Ren lifts the crystal high with both hands; it swells with light; a white burst, and a ring races to the town
            Cut();
            CameraMove(raiseCam, raiseCam, 2.4f, 40f, 34f);
            if (pose != null) pose.Raise = 1f;
            if (sfx != null && riserClip != null) sfx.PlayOneShot(riserClip, 0.9f);
            for (float t = 0f; t < 2.1f; t += Time.deltaTime)
            {
                if (crystalLight != null) crystalLight.intensity = Mathf.Lerp(2.2f, 16f, t / 2.1f * (t / 2.1f));
                yield return null;
            }
            ComicLook.SetImpact(1f);
            if (sfx != null && boomClip != null) sfx.PlayOneShot(boomClip, 1f);
            if (sfx != null && ringClip != null) sfx.PlayOneShot(ringClip, 0.8f);
            StartCoroutine(Shake(0.7f, 0.2f));
            yield return new WaitForSeconds(0.1f);
            ComicLook.SetImpact(0f);
            Cut();
            CameraMove(ringFrom, ringTo, 5f, 48f, 42f);
            StartCoroutine(Blend(waste, dawn, 4.5f));
            if (windAmbience != null) StartCoroutine(FadeAudio(windAmbience, 0f, 3f));
            if (pose != null) pose.Raise = 0f;   // (off screen) lowered again, cradled
            if (crystalLight != null) StartCoroutine(FadeLight(crystalLight, 4f, 2f));
            if (music != null) { music.volume = 0f; music.Play(); StartCoroutine(FadeAudio(music, 0.6f, 4f)); }
            StartCoroutine(RingAcross(4.6f));
            yield return Line(null, "They brought back everything the darkness had taken.", 0.5f);

            // 3. the street: the light arrives and they come back, one by one, as it reaches them
            Cut();
            CameraMove(streetFrom, streetTo, 6.5f, 44f, 38f);
            if (townAmbience != null) { townAmbience.volume = 0f; townAmbience.Play(); StartCoroutine(FadeAudio(townAmbience, 0.5f, 3f)); }
            if (townMurmur != null) { townMurmur.volume = 0f; townMurmur.Play(); StartCoroutine(FadeAudio(townMurmur, 0.3f, 4f)); }
            var order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < villagers.Length; i++) order.Add(i);
            order.Sort((a, b) => Distance(b).CompareTo(Distance(a)));   // nearest the tomb first (the light comes from there)
            for (int n = 0; n < order.Count; n++) StartCoroutine(RestoreVillager(order[n], 0.35f + n * 0.28f));
            yield return new WaitForSeconds(0.6f);
            if (sfx != null && laughClip != null) sfx.PlayOneShot(laughClip, 0.25f);
            yield return Line(null, "Their faces, their voices, their names. They were themselves again.", 0.2f);

            // 4. the rooftop: Yuna breathes, turns toward the hills, smiles
            Cut();
            CameraMove(roofGirl, roofGirl, 5f, 30f, 26f);
            if (girl != null) girl.Restore(1.6f);
            yield return new WaitForSeconds(2.2f);
            if (girl != null && exitPath.Length > 0) girl.LookAt(exitPath[exitPath.Length - 1]);
            yield return Line(GirlName, "Ren?", 1.2f);

            // 5. the price: ink climbs Ren's hands, the crystal's light drains out into dull stone
            Cut();
            if (music != null) StartCoroutine(FadeAudio(music, 0f, 2.5f));
            if (townAmbience != null) StartCoroutine(FadeAudio(townAmbience, 0f, 2f));
            if (townMurmur != null) StartCoroutine(FadeAudio(townMurmur, 0f, 2f));
            if (dread != null) { dread.volume = 0f; dread.Play(); StartCoroutine(FadeAudio(dread, 0.55f, 3f)); }
            CameraFollow(bearer.transform, new Vector3(0.6f, 1.38f, 1.45f), new Vector3(0.42f, 1.3f, 1.15f), new Vector3(0f, 1.12f, 0.3f), 6f, true, 32f);
            StartCoroutine(InkOver(bearer.gameObject, 4.5f));
            if (sfx != null && heartbeatClip != null) sfx.PlayOneShot(heartbeatClip, 0.9f);
            yield return new WaitForSeconds(1.2f);
            StartCoroutine(CrystalDies(2.4f));
            yield return Line(null, "But the darkness always takes something in return.", 0.6f);

            // 6. out of the dark behind Ren: the one who was the Void before. In the sunlight he is a man again.
            Cut();
            CameraMove(mouthFrom, mouthTo, 12f, 40f, 34f);
            if (formerVoid != null)
            {
                if (pose != null) pose.LookTarget = formerVoid.transform;
                StartCoroutine(WalkPath(formerVoid, formerVoidPath, 0.85f));
                yield return new WaitForSeconds(1.8f);
                formerVoid.Restore(3.5f);
            }
            yield return Line(VoidName, "Every age, someone comes to save them.", 0.2f);
            yield return Line(VoidName, "I was the one before you.", 0.2f);
            yield return Line(VoidName, "I left the door open for you. Thanks.", 0.6f);

            // 7. Ren, faceless now, turns and walks back into the tomb; the dark takes them
            Cut();
            if (pose != null) pose.LookTarget = null;
            bearer.Walk(returnPath, 0.9f);
            CameraMove(dawnTo, dawnFrom, 6f, 38f, 46f);
            if (sfx != null && ringClip != null) PlayLow(ringClip, 0.5f, 0.7f);
            yield return Line(null, "The light had returned to the town, but the darkness had found someone new.", 1f);
            if (dread != null) StartCoroutine(FadeAudio(dread, 0f, 1.5f));
            yield return Fade(1f, 1.2f);

            // 8. days later: the storyteller and the children. The same face. His shadow never left the wall.
            Apply(day);
            if (bearer != null) bearer.gameObject.SetActive(false);
            if (formerVoid != null) formerVoid.gameObject.SetActive(false);
            if (storytellerShadow != null) storytellerShadow.SetLook(0.95f, 0.15f);
            if (townAmbience != null) { townAmbience.volume = 0f; townAmbience.Play(); StartCoroutine(FadeAudio(townAmbience, 0.45f, 2f)); }
            if (music != null) { music.volume = 0f; music.Play(); StartCoroutine(FadeAudio(music, 0.4f, 3f)); }
            ui.Caption("Days later.", 1.6f);
            CameraMove(storyFrom, storyTo, 9f, 36f, 28f);
            Fade(0f, 1.2f);
            yield return new WaitForSeconds(1.8f);
            yield return Line(null, "And in that town lived an old man who told the same story every night.", 0.3f);
            yield return Line(ElderName, "There was once again a town that lived in the light...", 1.2f);
            yield return Fade(1f, 1.4f);

            // 9. title, credits, menu
            if (music != null) StartCoroutine(FadeAudio(music, 0.55f, 1f));
            ui.Title(4.5f);
            yield return new WaitForSeconds(7.5f);
            ui.Caption("A NASCON 2026 Game Jam project", 3f);
            yield return new WaitForSeconds(3.6f);
            ui.Caption("Thank you for playing.", 3f);
            yield return new WaitForSeconds(3.8f);
            if (music != null) StartCoroutine(FadeAudio(music, 0f, 1.5f));
            yield return new WaitForSeconds(1.6f);
            Finish();
        }

        protected override void Finish()
        {
            ending = true;
            Shader.SetGlobalFloat(PaletteReduceId, 0f);
            SceneLoader.Load(SceneNames.MainMenu);
        }

        // ------------------------------------------------------------ helpers

        private float Distance(int i) => villagers[i] != null ? -Vector3.Distance(villagers[i].transform.position, ringOrigin) : 0f;

        private IEnumerator RestoreVillager(int i, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (villagers[i] != null) villagers[i].Restore(1.1f);
            if (imprints != null && i < imprints.Length && imprints[i] != null) StartCoroutine(FadeFigure(imprints[i], 0f, 0f, 1.3f));
        }

        /// <summary>The ring of light: a glowing band racing outward from the tomb over the hills and the roofs.</summary>
        private IEnumerator RingAcross(float duration)
        {
            if (lightRing == null) yield break;
            lightRing.enabled = true;
            int n = lightRing.positionCount;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float r = Mathf.Lerp(2f, 240f, k * k * (3f - 2f * k));
                for (int i = 0; i < n; i++)
                {
                    float a = i / (float)(n - 1) * Mathf.PI * 2f;
                    lightRing.SetPosition(i, ringOrigin + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
                lightRing.widthMultiplier = Mathf.Lerp(1.5f, 9f, k);
                yield return null;
            }
            lightRing.enabled = false;
        }

        /// <summary>The crystal gives the last of its light: it flickers, dims, and goes to dull grey stone.</summary>
        private IEnumerator CrystalDies(float duration)
        {
            if (lowSource != null && crackleClip != null) PlayLow(crackleClip, 0.45f, 0.8f);
            float start = crystalLight != null ? crystalLight.intensity : 0f;
            var block = new MaterialPropertyBlock();
            int baseColor = Shader.PropertyToID("_BaseColor");
            Color lit = crystalRenderer != null ? crystalRenderer.sharedMaterial.GetColor(baseColor) : Color.white;
            Color dead = new Color(0.07f, 0.07f, 0.09f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float flicker = 0.55f + 0.45f * Mathf.PerlinNoise(t * 16f, 0.3f);
                if (crystalLight != null) crystalLight.intensity = start * (1f - k) * flicker;
                if (crystalRenderer != null)
                {
                    block.SetColor(baseColor, Color.Lerp(lit, dead, k * k) * Mathf.Lerp(1f, flicker, 1f - k));
                    crystalRenderer.SetPropertyBlock(block);
                }
                yield return null;
            }
            if (crystalLight != null) crystalLight.intensity = 0f;
            if (crystalRenderer != null) { block.SetColor(baseColor, dead); crystalRenderer.SetPropertyBlock(block); }
        }

        /// <summary>Ink takes Ren: every surface drains to black (the camera is on the hand, so it starts there).</summary>
        private static IEnumerator InkOver(GameObject who, float duration)
        {
            var block = new MaterialPropertyBlock();
            int baseColor = Shader.PropertyToID("_BaseColor"), rim = Shader.PropertyToID("_RimStrength");
            var rends = who.GetComponentsInChildren<Renderer>();
            var ink = new Color(0.02f, 0.008f, 0.016f, 1f);
            for (float t = 0f; t <= duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                foreach (var r in rends)
                {
                    if (r == null || r is ParticleSystemRenderer || r.name.Contains("Flame")) continue;
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null || !mats[i].HasProperty(baseColor)) continue;
                        r.GetPropertyBlock(block, i);
                        block.SetColor(baseColor, Color.Lerp(mats[i].GetColor(baseColor), ink, k));
                        block.SetFloat(rim, 0f);
                        r.SetPropertyBlock(block, i);
                    }
                }
                yield return null;
            }
        }

        private static IEnumerator WalkPath(Villager v, Transform[] path, float speed)
        {
            foreach (var p in path)
            {
                if (p == null) continue;
                v.WalkTo(p.position, speed);
                for (float w = 0f; w < 12f; w += Time.deltaTime)
                {
                    Vector3 d = p.position - v.transform.position; d.y = 0f;
                    if (d.magnitude < 0.15f) break;
                    yield return null;
                }
            }
        }
    }
}
