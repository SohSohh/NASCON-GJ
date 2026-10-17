using System.Collections;
using NasconGJ.Core;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>
    /// The side-room jump scare. Walking the candle corridor you can glimpse shadow people bowed in a candle circle
    /// through a doorway on your left. The moment you draw level with that doorway every candle dies at once and
    /// something whispers from the dark room. Turn to look and it's already in your face; don't, and it's in your face
    /// anyway. Then the corridor candles come back one by one and the room is empty. Fires once.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SideRoomScare : MonoBehaviour
    {
        [SerializeField] private ShadowFigureDriver[] circle;
        [SerializeField] private ShadowFigureDriver doorwayFigure;
        [SerializeField] private CandleFlicker[] candles;          // the circle in the room
        [SerializeField] private CandleFlicker[] corridorCandles;  // the corridor's own
        [SerializeField] private Transform doorway;               // just outside the opening, at floor height
        [SerializeField] private Transform roomCentre;
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip snuffClip, whisperClip, screechClip;
        [Tooltip("World z at which the player is level with the doorway.")]
        [SerializeField] private float levelAt = 67.4f;

        private bool fired;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2; // Ignore Raycast: the pickup/look ray goes straight through
            if (doorwayFigure != null) doorwayFigure.Show(false);
            if (circle != null) foreach (var f in circle) { if (f == null) continue; f.Show(true); f.Stoop(0.35f); }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fired || !other.CompareTag(GameTags.Player)) return;
            fired = true;
            StartCoroutine(Run(other.transform));
        }

        private IEnumerator Run(Transform player)
        {
            var cam = Camera.main != null ? Camera.main.transform : null;
            if (cam == null || doorwayFigure == null) yield break;

            // 1. you can see them in there as you approach; wait until you're level with the doorway
            for (float w = 0f; w < 30f && player.position.z < levelAt; w += Time.deltaTime) yield return null;

            // 2. every candle dies at once; the room is suddenly empty and something whispers in it
            Snuff(candles, true);
            Snuff(corridorCandles, true);
            if (circle != null) foreach (var f in circle) if (f != null) f.Show(false);
            Play(snuffClip, roomCentre.position + Vector3.up, 1f);
            yield return new WaitForSeconds(0.35f);
            Play(whisperClip, doorway.position + Vector3.up * 1.5f, 1f);

            // 3. it waits in the doorway; look at it (or hesitate too long) and it's in your face
            var t = doorwayFigure.transform;
            t.SetPositionAndRotation(doorway.position, Quaternion.LookRotation(Flat(player.position - doorway.position)));
            doorwayFigure.Show(true);
            doorwayFigure.Eyes(true);
            for (float w = 0f; w < 2.2f; w += Time.deltaTime)
            {
                if (Vector3.Angle(cam.forward, t.position + Vector3.up * 1.3f - cam.position) < 38f) break;
                t.rotation = Quaternion.LookRotation(Flat(player.position - t.position));
                yield return null;
            }
            yield return ScareFx.FaceLunge(this, doorwayFigure, player, sfx, screechClip);

            // 4. the corridor candles come back one by one; the room stays dark
            yield return new WaitForSeconds(1.2f);
            if (corridorCandles != null)
                foreach (var c in corridorCandles) { if (c == null) continue; c.Snuff(false); yield return new WaitForSeconds(0.18f); }
        }

        private static void Snuff(CandleFlicker[] set, bool snuffed)
        {
            if (set != null) foreach (var c in set) if (c != null) c.Snuff(snuffed);
        }

        private void Play(AudioClip clip, Vector3 at, float volume)
        {
            if (sfx == null || clip == null) return;
            sfx.transform.position = at;
            sfx.PlayOneShot(clip, volume);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }
    }
}
