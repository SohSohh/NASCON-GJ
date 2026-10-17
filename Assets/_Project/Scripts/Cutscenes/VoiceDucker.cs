using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// Keeps the voice-over clear: through a stretch of narration or dialogue, every other AudioSource in the scene sits
    /// evenly at <see cref="duckTo"/> of whatever volume its owner set. It stays down across the gaps between lines
    /// (<see cref="hold"/>), so nothing pumps, and only eases back once the section is over. Runs after all other scripts, and
    /// notices when a director changes a source's volume (fades keep working, they're just scaled).
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class VoiceDucker : MonoBehaviour
    {
        [SerializeField] private AudioSource voice;
        [Range(0f, 1f)] [SerializeField] private float duckTo = 0.42f;
        [Tooltip("Seconds of no speech before the bed comes back up (covers the pauses between lines).")]
        [SerializeField] private float hold = 6f;
        [SerializeField] private float attack = 1.2f, release = 0.3f;   // gentle in, slow out
        [Tooltip("Sources left alone (e.g. the memory-fall track).")]
        [SerializeField] private AudioSource[] ignore;

        private class Entry { public float requested, written; }
        private readonly Dictionary<AudioSource, Entry> sources = new();
        private float duck = 1f, rescan, sinceVoice = 999f;

        private void LateUpdate()
        {
            if (voice == null) return;
            if ((rescan -= Time.unscaledDeltaTime) <= 0f) { Scan(); rescan = 1f; }

            sinceVoice = voice.isPlaying ? 0f : sinceVoice + Time.unscaledDeltaTime;
            float target = sinceVoice < hold ? duckTo : 1f;
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * (target < duck ? attack : release));

            foreach (var kv in sources)
            {
                var s = kv.Key;
                if (s == null) continue;
                var e = kv.Value;
                if (!Mathf.Approximately(s.volume, e.written)) e.requested = s.volume; // someone set a new volume this frame
                e.written = e.requested * duck;
                s.volume = e.written;
            }
        }

        private void Scan()
        {
            foreach (var s in FindObjectsByType<AudioSource>())
            {
                if (s == voice || sources.ContainsKey(s) || IsIgnored(s)) continue;
                sources[s] = new Entry { requested = s.volume, written = s.volume };
            }
        }

        private bool IsIgnored(AudioSource s)
        {
            if (ignore == null) return false;
            foreach (var i in ignore) if (i == s) return true;
            return false;
        }
    }
}
