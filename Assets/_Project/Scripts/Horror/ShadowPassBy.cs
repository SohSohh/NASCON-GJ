using System.Collections;
using NasconGJ.Core;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>
    /// A shadow darts across a distant archway once: entering this trigger arms it, and it fires the moment the
    /// player is actually looking toward the arch (so it's seen, not missed), with a whoosh that crosses with it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShadowPassBy : MonoBehaviour
    {
        [SerializeField] private ShadowFigureDriver figure;
        [SerializeField] private Transform from, to;
        [SerializeField] private float duration = 1.1f;
        [Tooltip("How directly the player must face the arch (dot of view and direction).")]
        [SerializeField] private float facing = 0.75f;
        [SerializeField] private float maxWait = 8f;
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip passClip, whisperClip;

        private bool fired;

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
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var cam = Camera.main;
            Vector3 arch = (from.position + to.position) * 0.5f + Vector3.up;
            for (float w = 0f; w < maxWait && cam != null; w += Time.deltaTime)
            {
                if (Vector3.Dot(cam.transform.forward, (arch - cam.transform.position).normalized) > facing) break;
                yield return null;
            }

            if (figure == null) yield break;
            var t = figure.transform;
            Vector3 a = from.position, b = to.position;
            t.SetPositionAndRotation(a, Quaternion.LookRotation(Vector3.ProjectOnPlane(b - a, Vector3.up)));
            figure.Show(true);
            figure.Speed = Vector3.Distance(a, b) / duration;
            if (sfx != null && passClip != null) { sfx.transform.position = a; sfx.PlayOneShot(passClip, 1f); }

            for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
            {
                t.position = Vector3.Lerp(a, b, k);
                if (sfx != null) sfx.transform.position = t.position + Vector3.up;
                yield return null;
            }
            figure.Show(false);
            figure.Speed = 0f;

            yield return new WaitForSeconds(0.7f);
            if (sfx != null && whisperClip != null) sfx.PlayOneShot(whisperClip, 0.7f);
        }
    }
}
