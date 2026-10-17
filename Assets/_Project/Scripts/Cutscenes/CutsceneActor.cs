using System.Collections;
using NasconGJ.Player;
using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// Moves a procedurally animated character for cutscenes: walk a path (easing in and out, turning smoothly,
    /// legs driven through <see cref="TorchBearerPose.Speed"/>), teleport between locations, or fall.
    /// </summary>
    public class CutsceneActor : MonoBehaviour
    {
        [SerializeField] private TorchBearerPose pose;
        [SerializeField] private float acceleration = 1.6f;
        [SerializeField] private float turnSpeed = 3.5f;

        public bool Busy { get; private set; }

        private Coroutine routine;

        private void Awake()
        {
            if (pose == null) pose = GetComponent<TorchBearerPose>();
        }

        public void Teleport(Transform mark)
        {
            Stop();
            transform.SetPositionAndRotation(mark.position, Quaternion.Euler(0f, mark.eulerAngles.y, 0f));
            if (pose != null) pose.Speed = 0f;
        }

        public void Walk(Transform[] path, float speed)
        {
            Stop();
            routine = StartCoroutine(WalkRoutine(path, speed));
        }

        public void Fall()
        {
            Stop();
            routine = StartCoroutine(FallRoutine());
        }

        private void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            Busy = false;
        }

        private IEnumerator WalkRoutine(Transform[] path, float speed)
        {
            Busy = true;
            float v = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                Vector3 target = path[i].position;
                target.y = transform.position.y;
                bool last = i == path.Length - 1;
                while (true)
                {
                    Vector3 to = target - transform.position;
                    to.y = 0f;
                    float dist = to.magnitude;
                    if (dist < 0.03f) break;
                    float goal = last ? Mathf.Min(speed, dist * 1.2f + 0.15f) : speed; // ease out on the final segment
                    v = Mathf.MoveTowards(v, goal, acceleration * Time.deltaTime);
                    Vector3 dir = to / dist;
                    transform.position += dir * Mathf.Min(dist, v * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                    if (pose != null) pose.Speed = v;
                    yield return null;
                }
            }
            if (pose != null) pose.Speed = 0f;
            Busy = false;
        }

        private IEnumerator FallRoutine()
        {
            Busy = true;
            if (pose != null) pose.Speed = 0f;
            float vy = 0f;
            Quaternion start = transform.rotation;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                vy -= 9.81f * Time.deltaTime;
                transform.position += Vector3.up * vy * Time.deltaTime;
                transform.rotation = start * Quaternion.Euler(Mathf.Min(t * 50f, 70f), 0f, Mathf.Sin(t * 3f) * 10f);
                yield return null;
            }
            Busy = false;
        }
    }
}
