using UnityEngine;

namespace NasconGJ.Audio
{
    /// <summary>Plays random clips at random points inside a box at random intervals: drips, distant stones, groans.</summary>
    public class RandomOneShots : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private Vector3 area = new(10f, 4f, 10f);
        [SerializeField] private Vector2 interval = new(2f, 6f);
        [SerializeField] private Vector2 volume = new(0.25f, 0.6f);
        [SerializeField] private Vector2 pitch = new(0.85f, 1.15f);
        [SerializeField] private float maxDistance = 25f;

        private AudioSource source;
        private float timer;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = maxDistance;
            timer = Random.Range(interval.x, interval.y);
        }

        private void Update()
        {
            if (clips == null || clips.Length == 0) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(interval.x, interval.y);
            source.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f) * area.x, Random.Range(-0.5f, 0.5f) * area.y, Random.Range(-0.5f, 0.5f) * area.z);
            source.pitch = Random.Range(pitch.x, pitch.y);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], Random.Range(volume.x, volume.y));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            Gizmos.DrawWireCube(transform.position, area);
        }
    }
}
