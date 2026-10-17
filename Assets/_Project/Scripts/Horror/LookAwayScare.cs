using System.Collections;
using NasconGJ.Core;
using NasconGJ.Items;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>
    /// The side-glance scare: a shadow person stands still in the dark between the pillars. Once you've seen it and
    /// looked away, it's gone. A few seconds later something whispers behind you; turn around and it's right there
    /// (screech, the flame gutters, the view jolts), then it sinks into the floor. Non-lethal, fires once.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LookAwayScare : MonoBehaviour
    {
        [SerializeField] private ShadowFigureDriver figure;
        [Tooltip("Where it stands when you first glimpse it.")]
        [SerializeField] private Transform standPoint;
        [SerializeField] private float behindDistance = 1.7f;
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip whisperClip, screechClip, dreadClip;

        private bool fired;
        private Transform cam;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2; // Ignore Raycast: the pickup/look ray goes straight through
            if (figure != null) figure.Show(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fired || !other.CompareTag(GameTags.Player)) return;
            fired = true;
            StartCoroutine(Run(other.transform));
        }

        private float AngleTo(Vector3 p) => Vector3.Angle(cam.forward, p - cam.position);

        private IEnumerator Run(Transform player)
        {
            if (figure == null || Camera.main == null) yield break;
            cam = Camera.main.transform;
            var t = figure.transform;

            // 1. standing in the dark, facing you
            t.SetPositionAndRotation(standPoint.position, Quaternion.LookRotation(Flat(player.position - standPoint.position)));
            figure.Show(true);
            float seen = 0f;
            for (float w = 0f; w < 14f && seen < 0.35f; w += Time.deltaTime)
            {
                seen = AngleTo(t.position + Vector3.up * 1.2f) < 22f ? seen + Time.deltaTime : 0f;
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(Flat(player.position - t.position)), Time.deltaTime * 2f);
                yield return null;
            }

            // 2. look away and it's gone (if it was never seen, it just slips out of sight)
            float away = 0f;
            for (float w = 0f; w < 10f && away < 0.5f; w += Time.deltaTime)
            {
                away = AngleTo(t.position + Vector3.up * 1.2f) > 60f ? away + Time.deltaTime : 0f;
                yield return null;
            }
            figure.Show(false);
            if (sfx != null && dreadClip != null) { sfx.transform.position = player.position; sfx.PlayOneShot(dreadClip, 0.55f); }
            yield return new WaitForSeconds(Random.Range(2.5f, 4f));

            // 3. right behind you; a whisper makes you turn
            Vector3 back = -Flat(cam.forward);
            Vector3 spot = player.position + back * behindDistance;
            if (Physics.Raycast(player.position + Vector3.up, back, out var wall, behindDistance + 0.3f, ~0, QueryTriggerInteraction.Ignore))
                spot = player.position + back * Mathf.Max(0.9f, wall.distance - 0.35f);
            t.SetPositionAndRotation(spot, Quaternion.LookRotation(-back));
            figure.Show(true);
            if (sfx != null && whisperClip != null) { sfx.transform.position = spot + Vector3.up * 1.5f; sfx.PlayOneShot(whisperClip, 1f); }

            bool turned = false;
            Vector3 gap = spot - player.position; gap.y = 0f;
            float keep = gap.magnitude;
            for (float w = 0f; w < 9f; w += Time.deltaTime)
            {
                // it keeps itself right behind you until you look
                Vector3 behind = player.position - Flat(cam.forward) * keep;
                if (AngleTo(t.position + Vector3.up * 1.3f) < 40f) { turned = true; break; }
                t.position = Vector3.Lerp(t.position, new Vector3(behind.x, spot.y, behind.z), Time.deltaTime * 3f);
                t.rotation = Quaternion.LookRotation(Flat(player.position - t.position));
                yield return null;
            }
            // 4. the scare (turned to it, or it gave up waiting): straight into your face
            yield return ScareFx.FaceLunge(this, figure, player, sfx, screechClip);
        }

        private IEnumerator Jolt(float time, float strength)
        {
            for (float e = 0f; e < time; e += Time.deltaTime)
            {
                float k = 1f - e / time;
                Vector3 offset = Random.insideUnitSphere * strength * k;
                cam.localPosition += offset;
                yield return null;
                if (cam != null) cam.localPosition -= offset;
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }
    }
}
