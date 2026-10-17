using System.Collections;
using NasconGJ.Core;
using NasconGJ.Items;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>
    /// The shared jump-scare beat: the shadow snaps right up to the lens (head at eye height, red eyes filling the
    /// frame), screech, ink impact flash, the view shakes, the flame gutters, then a beat of black and it's gone.
    /// </summary>
    public static class ScareFx
    {
        public static IEnumerator FaceLunge(MonoBehaviour host, ShadowFigureDriver figure, Transform player, AudioSource sfx, AudioClip screech)
        {
            var cam = Camera.main != null ? Camera.main.transform : null;
            if (figure == null || cam == null) yield break;
            var t = figure.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = player.forward;
            Vector3 end = cam.position + fwd * 0.42f - Vector3.up * figure.HeadHeight;
            Vector3 start = t.position;
            if (!figure.IsShown || Vector3.Distance(start, end) > 6f) start = end + fwd * 2.2f; // came out of nowhere
            t.rotation = Quaternion.LookRotation(-fwd);
            figure.Show(true);
            figure.Eyes(true);

            if (sfx != null && screech != null) { sfx.transform.position = cam.position + fwd * 0.5f; sfx.PlayOneShot(screech, 1f); }
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null && inv.ActiveFlame != null) inv.ActiveFlame.SetGust(1f);
            host.StartCoroutine(Shake(cam, 0.6f, 0.11f));

            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.11f)
            {
                t.position = Vector3.Lerp(start, end, k * k);
                ComicLook.SetImpact(k);
                yield return null;
            }
            t.position = end;
            ComicLook.SetImpact(1f);
            yield return new WaitForSeconds(0.38f);
            figure.Show(false);
            figure.Eyes(false);
            for (float k = 1f; k > 0f; k -= Time.deltaTime / 0.5f) { ComicLook.SetImpact(k); yield return null; }
            ComicLook.SetImpact(0f);
        }

        public static IEnumerator Shake(Transform cam, float time, float strength)
        {
            for (float e = 0f; e < time && cam != null; e += Time.deltaTime)
            {
                Vector3 offset = Random.insideUnitSphere * strength * (1f - e / time);
                cam.localPosition += offset;
                yield return null;
                if (cam != null) cam.localPosition -= offset;
            }
        }
    }
}
