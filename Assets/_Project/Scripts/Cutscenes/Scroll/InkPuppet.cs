using System.Collections;
using UnityEngine;

namespace NasconGJ.Cutscenes.Scroll
{
    /// <summary>
    /// A jointed shadow puppet (皮影戏 / paper cut-out): body plate + two arms on hinge pins, moved the way a
    /// puppeteer moves rods: a little bounce and rock as it walks, arms swinging on their pins, small
    /// purposeful gestures (selling, waving, telling a story, reaching, laughing).
    /// Optionally casts a big flickering shadow of itself onto a wall plane (the fire-light scene).
    /// Root = feet. Build with <see cref="Build"/> (editor) and drive with the public actions.
    /// </summary>
    public class InkPuppet : MonoBehaviour
    {
        public enum Act { Idle, Walk, Run, Sell, Wave, Story, Reach, Laugh, Listen, HoldTorch, Carry, Sit, Frozen }

        [SerializeField] private Transform bodyPivot;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform armFront, armBack;
        [SerializeField] private Renderer armFrontRenderer, armBackRenderer;
        [SerializeField] private float height = 2f;
        [SerializeField] private Act act = Act.Idle;
        [SerializeField] private float facing = 1f;
        [SerializeField] private float phase;
        [Tooltip("Optional held prop (torch) parented to the front hand.")]
        [SerializeField] private Transform heldProp;

        [Header("Wall shadow")]
        [SerializeField] private bool castShadow;
        [SerializeField] private Transform lightSource;
        [SerializeField] private float wallZ = 3f;
        [SerializeField] private float shadowScale = 1.6f;
        [SerializeField] private float shadowOpacity = 0.85f;

        public Act Current => act;
        public float Height => height;
        public InkPuppet Shadow => shadow;

        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int SilhouetteId = Shader.PropertyToID("_Silhouette");
        private static readonly int AccentGlowId = Shader.PropertyToID("_AccentGlow");

        private Material[] mats;
        private float time, opacity = 1f, silhouette, bodyZ, armFrontZ, armBackZ;
        private Vector3 bodyBase;
        private InkPuppet shadow, source;
        private Coroutine moveRoutine;
        private float reach;
        private Vector3 reachTarget;

        public void Configure(Transform pivot, Renderer body, Transform front, Renderer frontR, Transform back, Renderer backR, float h, Transform prop)
        {
            bodyPivot = pivot; bodyRenderer = body; armFront = front; armFrontRenderer = frontR; armBack = back; armBackRenderer = backR; height = h; heldProp = prop;
        }

        private void Awake()
        {
            mats = new[] { bodyRenderer.material, armFrontRenderer.material, armBackRenderer.material };
            bodyBase = bodyPivot.localPosition;
            time = phase * 10f;
            transform.localScale = new Vector3(facing, 1f, 1f);
            opacity = mats[0].GetFloat(OpacityId);
            if (heldProp != null) heldProp.gameObject.SetActive(opacity > 0.05f);
        }

        private void Start()
        {
            if (castShadow && source == null) MakeShadow();
        }

        // ------------------------------------------------------------ actions

        /// <summary>Scroll layers are all transparent; they sort back to front by this order (larger z = further back).</summary>
        public static int OrderFor(float z) => Mathf.RoundToInt((100f - z) * 10f);

        public void Do(Act a) => act = a;
        public void Face(float dir) { facing = Mathf.Sign(dir); transform.localScale = new Vector3(facing, 1f, 1f); }

        public Coroutine WalkTo(float x, float speed, bool run = false)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(WalkRoutine(x, speed, run));
            return moveRoutine;
        }

        private IEnumerator WalkRoutine(float x, float speed, bool run)
        {
            Act before = act == Act.Walk || act == Act.Run ? Act.Idle : act;
            act = run ? Act.Run : Act.Walk;
            Face(x - transform.position.x);
            while (Mathf.Abs(transform.position.x - x) > 0.02f)
            {
                var p = transform.position;
                p.x = Mathf.MoveTowards(p.x, x, speed * Time.deltaTime);
                transform.position = p;
                yield return null;
            }
            act = before == Act.Frozen ? Act.Idle : before;
            moveRoutine = null;
        }

        public void ReachToward(Vector3 worldPoint) { reachTarget = worldPoint; act = Act.Reach; }

        public void SetOpacity(float a)
        {
            opacity = a;
            foreach (var m in mats) m.SetFloat(OpacityId, a);
            if (heldProp != null && source == null) heldProp.gameObject.SetActive(a > 0.05f);
        }

        public void SetSilhouette(float s)
        {
            silhouette = s;
            foreach (var m in mats) m.SetFloat(SilhouetteId, s);
        }

        public void SetGlow(float g) { foreach (var m in mats) m.SetFloat(AccentGlowId, g); }

        public IEnumerator FadeTo(float target, float duration)
        {
            float from = opacity;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                SetOpacity(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            SetOpacity(target);
        }

        // ------------------------------------------------------------ animation

        private void LateUpdate()
        {
            if (source != null) { MirrorSource(); return; }
            time += Time.deltaTime;
            float t = time;
            float bob = 0f, rock = 0f, front, back;
            switch (act)
            {
                case Act.Walk:
                    bob = Mathf.Abs(Mathf.Sin(t * 5.5f)) * 0.035f * height;
                    rock = Mathf.Sin(t * 5.5f) * 4f;
                    front = 10f + Mathf.Sin(t * 5.5f) * 22f;
                    back = 10f - Mathf.Sin(t * 5.5f) * 22f;
                    break;
                case Act.Run:
                    bob = Mathf.Abs(Mathf.Sin(t * 9f)) * 0.07f * height;
                    rock = -8f + Mathf.Sin(t * 9f) * 5f;
                    front = 25f + Mathf.Sin(t * 9f) * 45f;
                    back = 25f - Mathf.Sin(t * 9f) * 45f;
                    break;
                case Act.Sell:
                    // holds goods out, draws them back, bows a little: the "come, look" of a market trader
                    float s = Mathf.Sin(t * 1.6f);
                    front = 70f + s * 25f;
                    back = 15f + Mathf.Sin(t * 0.8f) * 8f;
                    rock = -3f - Mathf.Max(0f, s) * 5f;
                    bob = Mathf.Max(0f, s) * 0.01f * height;
                    break;
                case Act.Wave:
                    front = 150f + Mathf.Sin(t * 7f) * 25f;
                    back = 8f;
                    rock = Mathf.Sin(t * 3.5f) * 2f;
                    break;
                case Act.Story:
                    // the elder telling his tale: a slow raised hand that rises and falls with the words
                    front = 95f + Mathf.Sin(t * 0.9f) * 30f + Mathf.PerlinNoise(t * 0.7f, phase) * 20f;
                    back = 40f + Mathf.Sin(t * 0.6f + 1f) * 10f;
                    rock = Mathf.Sin(t * 0.9f) * 3f;
                    break;
                case Act.Reach:
                    Vector3 local = transform.InverseTransformPoint(reachTarget);
                    float ang = Mathf.Atan2(local.x * facing, -(local.y - height * 0.7f)) * Mathf.Rad2Deg;
                    reach = Mathf.MoveTowards(reach, 1f, Time.deltaTime * 0.8f);
                    front = Mathf.Lerp(10f, ang, reach) + Mathf.Sin(t * 2f) * 3f;
                    back = 20f;
                    rock = -6f * reach;
                    break;
                case Act.Laugh:
                    bob = Mathf.Abs(Mathf.Sin(t * 11f)) * 0.03f * height;
                    rock = 8f + Mathf.Sin(t * 11f) * 4f;
                    front = 40f + Mathf.Sin(t * 11f) * 12f;
                    back = 30f;
                    break;
                case Act.Listen:
                    rock = Mathf.Sin(t * 0.7f + phase * 4f) * 2f + 4f;
                    front = 35f + Mathf.Sin(t * 0.5f) * 5f;
                    back = 25f;
                    bob = Mathf.Sin(t * 1.3f) * 0.004f * height;
                    break;
                case Act.HoldTorch:
                    front = 105f + Mathf.Sin(t * 1.1f) * 4f;
                    back = 8f;
                    rock = Mathf.Sin(t * 1.4f) * 1.5f;
                    break;
                case Act.Carry:
                    front = 160f; back = 160f;
                    bob = Mathf.Abs(Mathf.Sin(t * 5f)) * 0.02f * height;
                    break;
                case Act.Sit:
                    front = 30f + Mathf.Sin(t * 0.6f) * 4f;
                    back = 25f;
                    rock = Mathf.Sin(t * 0.5f) * 1.5f;
                    break;
                case Act.Frozen:
                    return; // mid-step: nothing moves any more
                default: // idle breathing, a weight shift now and then
                    bob = Mathf.Sin(t * 1.4f + phase * 6f) * 0.006f * height;
                    rock = Mathf.Sin(t * 0.5f + phase * 3f) * 1.5f;
                    front = 8f + Mathf.Sin(t * 0.9f + phase) * 3f;
                    back = 6f - Mathf.Sin(t * 0.8f + phase) * 3f;
                    break;
            }
            // the body plate rocks about the feet; arms hang from pins on it (positive angle = forward/up)
            bodyPivot.localPosition = bodyBase + Vector3.up * bob;
            bodyPivot.localRotation = Quaternion.Euler(0f, 0f, -rock);
            armFront.localRotation = Quaternion.Euler(0f, 0f, front);
            armBack.localRotation = Quaternion.Euler(0f, 0f, back);
            if (heldProp != null) heldProp.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.7f) * 3f); // the torch stays upright in the hand
        }

        // ------------------------------------------------------------ wall shadow

        private void MakeShadow()
        {
            var copy = Instantiate(gameObject, transform.parent);
            copy.name = name + "_WallShadow";
            var p = copy.GetComponent<InkPuppet>();
            p.castShadow = false;
            p.source = this;
            shadow = p;
            p.bodyRenderer.material.SetFloat(SilhouetteId, 1f);
            p.armFrontRenderer.material.SetFloat(SilhouetteId, 1f);
            p.armBackRenderer.material.SetFloat(SilhouetteId, 1f);
            if (p.heldProp != null) p.heldProp.gameObject.SetActive(false);
            int order = OrderFor(wallZ) + 2; // just in front of the wall it falls on
            p.armBackRenderer.sortingOrder = order;
            p.bodyRenderer.sortingOrder = order;
            p.armFrontRenderer.sortingOrder = order;
        }

        private void MirrorSource()
        {
            if (mats == null) return;
            // project from the light onto the wall: bigger, offset away from the fire, trembling with its flicker
            Vector3 light = source.lightSource != null ? source.lightSource.position : source.transform.position + Vector3.back;
            Vector3 feet = source.transform.position;
            float k = source.shadowScale * (1f + (Mathf.PerlinNoise(Time.time * 6f, source.phase) - 0.5f) * 0.06f);
            Vector3 dir = feet - light; dir.z = 0f;
            transform.position = new Vector3(light.x + dir.x * k, feet.y + (feet.y - light.y) * (k - 1f) * 0.3f, source.wallZ);
            transform.localScale = new Vector3(source.facing * k, k, 1f);
            bodyPivot.localPosition = source.bodyPivot.localPosition;
            bodyPivot.localRotation = source.bodyPivot.localRotation;
            armFront.localRotation = source.armFront.localRotation;
            armBack.localRotation = source.armBack.localRotation;
            float a = source.shadowOpacity * source.opacity;
            foreach (var m in mats) m.SetFloat(OpacityId, a);
        }

        /// <summary>Detach the wall shadow so it stays when the puppet is gone (the townsfolk's imprints).</summary>
        public InkPuppet LeaveShadow()
        {
            if (shadow == null) return null;
            var s = shadow;
            s.source = null;
            s.act = Act.Frozen;
            foreach (var m in s.mats) m.SetFloat(OpacityId, 0.92f); // burned in: darker than any passing shadow
            shadow = null;
            return s;
        }
    }
}
