using System.Collections.Generic;
using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// A gaunt figure woven out of twisted, thorny black vines (think Something Wicked from ULTRAKILL):
    /// a tiny tilted head on a long neck, a narrow wrapped torso with a smouldering heart, impossibly long arms
    /// ending in claw fingers, long legs. Built entirely in code at runtime and posed procedurally every frame.
    /// Used at 45 m for the Void in the prologue and at ~3 m for the shadows in the tomb.
    ///
    /// Local space: feet at the origin, +Y up, +Z forward (out of the wall it clings to / toward what it faces).
    /// Drive it with the public blend weights; the pose eases toward them on its own.
    /// </summary>
    public class VineCreature : MonoBehaviour
    {
        [SerializeField] private Material vineMaterial;
        [SerializeField] private Material heartMaterial;
        [SerializeField] private float height = 3f;
        [SerializeField] private int seed = 7;
        [Tooltip("Vine thickness multiplier (the colossal Void reads better a little heavier).")]
        [SerializeField] private float thickness = 1f;

        [Header("Pose (0..1 blends)")]
        [Tooltip("Arms raised out to the sides, looming.")]
        public float Spread;
        [Tooltip("Splayed flat like something pinned to a wall (spider pose).")]
        public float Flatten;
        [Tooltip("Hunched, arms up over the face: recoiling from light.")]
        public float Curl;
        [Tooltip("Both claws reach toward ReachTarget (local space).")]
        public float Reach;
        [Tooltip("Hovering: legs dangle, toes point down.")]
        public float Float;
        [Tooltip("How often and how hard the head jerks.")]
        public float Twitch = 0.6f;
        public Vector3 ReachTarget = new(0f, 1.4f, 2f);

        public float Height => height;

        private struct Segment
        {
            public Transform t;
            public int from, to;
        }

        private enum J
        {
            Pelvis, SpineMid, NeckBase, HeadBase, HeadTop,
            ShoulderL, ElbowL, WristL, PalmL, ShoulderR, ElbowR, WristR, PalmR,
            HipL, KneeL, AnkleL, ToeL, HipR, KneeR, AnkleR, ToeR,
            FingerL0, FingerL1, FingerL2, FingerL3, FingerR0, FingerR1, FingerR2, FingerR3,
            Count
        }

        private readonly Vector3[] joints = new Vector3[(int)J.Count];
        private readonly List<Segment> segments = new();
        private Transform heart;
        private Material vine, heartMat;
        private float time, nextTwitch, spread, flatten, curl, reach, floatAmount;
        private Vector3 headTilt, headTiltTarget;
        private System.Random rng;
        private bool built;

        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        private void Awake() => Build();

        public void Configure(float newHeight, Material vineMat, Material heartMaterialAsset, int newSeed, float newThickness = 1f)
        {
            thickness = newThickness;
            height = newHeight;
            vineMaterial = vineMat;
            heartMaterial = heartMaterialAsset;
            seed = newSeed;
        }

        public void SetGlow(float glow, float dissolve)
        {
            if (vine == null) return;
            vine.SetFloat(GlowId, glow);
            vine.SetFloat(DissolveId, dissolve);
            if (heartMat != null) heartMat.SetFloat(DissolveId, dissolve);
        }

        public void SetVisible(bool visible)
        {
            foreach (var s in segments) s.t.gameObject.SetActive(visible);
            if (heart != null) heart.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------ build

        public void Build()
        {
            if (built || vineMaterial == null) return;
            built = true;
            rng = new System.Random(seed);
            time = (float)rng.NextDouble() * 10f;
            vine = new Material(vineMaterial);
            heartMat = heartMaterial != null ? new Material(heartMaterial) : null;

            Seg(J.Pelvis, J.SpineMid, 0.03f, 7);
            Seg(J.SpineMid, J.NeckBase, 0.032f, 7);
            Seg(J.ShoulderL, J.SpineMid, 0.01f, 3);   // vines wrapping the ribcage
            Seg(J.ShoulderR, J.SpineMid, 0.01f, 3);
            Seg(J.NeckBase, J.HeadBase, 0.011f, 4);
            Seg(J.HeadBase, J.HeadTop, 0.034f, 6);
            foreach (bool left in new[] { true, false })
            {
                J sh = left ? J.ShoulderL : J.ShoulderR, el = left ? J.ElbowL : J.ElbowR, wr = left ? J.WristL : J.WristR, pa = left ? J.PalmL : J.PalmR;
                Seg(J.NeckBase, sh, 0.013f, 4);
                Seg(sh, el, 0.016f, 5);
                Seg(el, wr, 0.013f, 5);
                Seg(wr, pa, 0.012f, 4);
                for (int f = 0; f < 4; f++) Seg(pa, (left ? J.FingerL0 : J.FingerR0) + f, 0.005f, 2);
                J hip = left ? J.HipL : J.HipR, kn = left ? J.KneeL : J.KneeR, an = left ? J.AnkleL : J.AnkleR, toe = left ? J.ToeL : J.ToeR;
                Seg(J.Pelvis, hip, 0.02f, 4);
                Seg(hip, kn, 0.019f, 5);
                Seg(kn, an, 0.015f, 5);
                Seg(an, toe, 0.012f, 4);
            }

            if (heartMat != null)
            {
                var h = new GameObject("Heart");
                h.transform.SetParent(transform, false);
                h.AddComponent<MeshFilter>().sharedMesh = HeartMesh();
                var r = h.AddComponent<MeshRenderer>();
                r.sharedMaterial = heartMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                heart = h.transform;
            }
            Solve(0f);
        }

        private void Seg(J from, J to, float radius, int strands)
        {
            var go = new GameObject("Vine_" + from + "_" + to);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = Bundle(radius * thickness, strands, rng.Next(0, 3));
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = vine;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = false;
            segments.Add(new Segment { t = go.transform, from = (int)from, to = (int)to });
        }

        // ------------------------------------------------------------ animate

        private void LateUpdate()
        {
            if (!built) return;
            Solve(Time.deltaTime);
        }

        private void Solve(float dt)
        {
            time += dt;
            float k = 1f - Mathf.Exp(-dt * 4f);
            spread = Mathf.Lerp(spread, Spread, k);
            flatten = Mathf.Lerp(flatten, Flatten, k);
            curl = Mathf.Lerp(curl, Curl, 1f - Mathf.Exp(-dt * 9f));
            reach = Mathf.Lerp(reach, Reach, k);
            floatAmount = Mathf.Lerp(floatAmount, Float, k);

            // creepy head: holds still, then snaps to a new angle
            if (time >= nextTwitch)
            {
                float amp = 18f + Twitch * 30f;
                headTiltTarget = new Vector3(Range(-0.4f, 0.8f) * amp, Range(-1f, 1f) * amp, Range(-1f, 1f) * amp);
                nextTwitch = time + Mathf.Lerp(2.5f, 0.5f, Twitch) * Range(0.5f, 1.4f);
            }
            headTilt = Vector3.Lerp(headTilt, headTiltTarget, 1f - Mathf.Exp(-dt * 14f));

            float H = height;
            float breath = Mathf.Sin(time * 1.3f);
            float sway = Mathf.Sin(time * 0.55f) * 0.02f * H;
            float hunch = 0.25f + curl * 0.9f - spread * 0.15f;

            Vector3 pelvis = new(sway, H * (0.5f - 0.12f * curl) + floatAmount * 0.02f * H * Mathf.Sin(time * 0.8f), 0f);
            Vector3 spineMid = pelvis + new Vector3(sway * 0.4f, 0.14f * H, hunch * 0.03f * H);
            Vector3 neckBase = spineMid + new Vector3(sway * 0.3f, 0.14f * H + breath * 0.004f * H, hunch * 0.07f * H);
            Vector3 headBase = neckBase + new Vector3(0f, 0.065f * H, hunch * 0.03f * H);
            Vector3 headTop = headBase + Quaternion.Euler(headTilt + new Vector3(hunch * 25f, 0f, 0f)) * new Vector3(0f, 0.13f * H, 0f);
            Set(J.Pelvis, pelvis); Set(J.SpineMid, spineMid); Set(J.NeckBase, neckBase); Set(J.HeadBase, headBase); Set(J.HeadTop, headTop);

            float U = 0.22f * H, F = 0.22f * H;
            for (int side = -1; side <= 1; side += 2)
            {
                bool left = side < 0;
                Vector3 shoulder = neckBase + new Vector3(side * 0.11f * H, -0.015f * H + spread * 0.02f * H, 0f);
                float ph = time * 0.7f + (left ? 0f : 1.7f);
                Vector3 hang = shoulder + new Vector3(side * 0.07f * H, -0.42f * H, 0.05f * H) + new Vector3(Mathf.Sin(ph) * 0.02f, 0f, Mathf.Cos(ph) * 0.03f) * H;
                Vector3 up = shoulder + new Vector3(side * 0.36f * H, 0.15f * H, 0.08f * H) + new Vector3(0f, Mathf.Sin(ph) * 0.02f * H, 0f);
                Vector3 splay = shoulder + new Vector3(side * 0.3f * H, 0.22f * H + Mathf.Sin(ph * 1.3f) * 0.03f * H, 0f);
                Vector3 shield = shoulder + new Vector3(-side * 0.04f * H, 0.14f * H, 0.18f * H);
                Vector3 toward = ReachTarget - shoulder;
                Vector3 grab = shoulder + toward.normalized * Mathf.Min(toward.magnitude, 0.42f * H) + new Vector3(side * 0.05f * H, 0f, 0f);

                Vector3 wrist = hang;
                wrist = Vector3.Lerp(wrist, up, spread);
                wrist = Vector3.Lerp(wrist, splay, flatten);
                wrist = Vector3.Lerp(wrist, grab, reach * (left ? 0.75f : 1f));
                wrist = Vector3.Lerp(wrist, shield, curl);

                Vector3 pole = new Vector3(side * 0.6f, -0.2f, -0.7f) + new Vector3(0f, flatten * 0.6f, 0.7f * flatten);
                Vector3 elbow = Elbow(shoulder, wrist, U, F, pole);
                Vector3 handDir = (wrist - elbow).normalized;
                handDir = Vector3.Slerp(handDir, Vector3.down, 0.25f * (1f - reach) * (1f - spread)).normalized;
                Vector3 palm = wrist + handDir * 0.055f * H;
                Set(left ? J.ShoulderL : J.ShoulderR, shoulder);
                Set(left ? J.ElbowL : J.ElbowR, elbow);
                Set(left ? J.WristL : J.WristR, wrist);
                Set(left ? J.PalmL : J.PalmR, palm);

                // long claw fingers, fanned, curling in when they close
                Vector3 across = Vector3.Cross(handDir, Vector3.forward).normalized;
                if (across.sqrMagnitude < 0.01f) across = Vector3.right;
                float splayAngle = 14f + reach * 12f + spread * 8f;
                float curlAngle = 25f + curl * 50f - reach * 15f + Mathf.Sin(time * 2.1f + side) * 6f;
                for (int f = 0; f < 4; f++)
                {
                    float a = (f - 1.5f) * splayAngle;
                    Vector3 dir = Quaternion.AngleAxis(a, Vector3.Cross(handDir, across)) * handDir;
                    dir = Quaternion.AngleAxis(curlAngle * -side, across) * dir;
                    Set((left ? J.FingerL0 : J.FingerR0) + f, palm + dir * (0.075f + (f == 1 || f == 2 ? 0.015f : 0f)) * H);
                }

                // legs
                Vector3 hip = pelvis + new Vector3(side * 0.06f * H, -0.01f * H, 0f);
                Vector3 stand = new Vector3(side * 0.09f * H, 0.035f * H, 0.01f * H);
                Vector3 wide = new Vector3(side * 0.24f * H, 0.06f * H, 0f);
                Vector3 dangle = hip + new Vector3(side * 0.03f * H, -0.47f * H, -0.03f * H) + new Vector3(0f, 0f, Mathf.Sin(time * 0.9f + side) * 0.03f * H);
                Vector3 ankle = Vector3.Lerp(Vector3.Lerp(stand, wide, flatten), dangle, floatAmount);
                Vector3 knee = Elbow(hip, ankle, 0.25f * H, 0.25f * H, new Vector3(side * 0.3f * (1f + flatten * 2f), 0f, 1f));
                Vector3 toeDir = Vector3.Lerp(new Vector3(side * 0.15f, -0.25f, 1f), new Vector3(0f, -1f, 0.15f), floatAmount).normalized;
                Set(left ? J.HipL : J.HipR, hip);
                Set(left ? J.KneeL : J.KneeR, knee);
                Set(left ? J.AnkleL : J.AnkleR, ankle);
                Set(left ? J.ToeL : J.ToeR, ankle + toeDir * 0.075f * H);
            }

            foreach (var s in segments)
            {
                Vector3 a = joints[s.from], b = joints[s.to];
                Vector3 d = b - a;
                float len = d.magnitude;
                s.t.localPosition = a;
                s.t.localRotation = len > 1e-4f ? Quaternion.FromToRotation(Vector3.up, d / len) : Quaternion.identity;
                s.t.localScale = new Vector3(H, Mathf.Max(len, 1e-3f), H);
            }

            if (heart != null)
            {
                float beat = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 5.2f)), 6f) + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 5.2f - 0.9f)), 8f) * 0.6f;
                heart.localPosition = Vector3.Lerp(spineMid, neckBase, 0.55f) + new Vector3(-0.025f * H, 0f, 0.02f * H);
                heart.localRotation = Quaternion.Euler(0f, 0f, 20f);
                heart.localScale = Vector3.one * (0.055f * H * (1f + beat * 0.18f));
            }
        }

        private void Set(J j, Vector3 p) => joints[(int)j] = p;

        private float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>Two-bone elbow/knee placement in a plane chosen by the pole direction.</summary>
        private static Vector3 Elbow(Vector3 a, Vector3 c, float lenA, float lenB, Vector3 pole)
        {
            Vector3 ac = c - a;
            float d = Mathf.Clamp(ac.magnitude, 0.01f, lenA + lenB - 0.001f);
            Vector3 dir = ac.normalized;
            float x = (lenA * lenA - lenB * lenB + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, lenA * lenA - x * x));
            Vector3 bend = Vector3.ProjectOnPlane(pole, dir);
            if (bend.sqrMagnitude < 1e-5f) bend = Vector3.ProjectOnPlane(Vector3.forward, dir);
            return a + dir * x + bend.normalized * h;
        }

        // ------------------------------------------------------------ meshes

        private static readonly Dictionary<(int, int, int), Mesh> bundles = new();
        private static Mesh heartMesh;

        /// <summary>
        /// A bundle of twisted strands running from y=0 to y=1 (x/z in units of creature height), pinched at the ends
        /// like knotted joints, with a few thorns. Cached per (radius, strands, variant).
        /// </summary>
        private static Mesh Bundle(float radius, int strands, int variant)
        {
            var key = (Mathf.RoundToInt(radius * 10000f), strands, variant);
            if (bundles.TryGetValue(key, out var cached) && cached != null) return cached;

            var rnd = new System.Random(key.GetHashCode());
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            const int segs = 9, sides = 5;

            for (int s = 0; s < strands; s++)
            {
                float a0 = s / (float)strands * Mathf.PI * 2f + R(-0.4f, 0.4f);
                float twist = R(1.2f, 3.2f) * Mathf.PI * (rnd.NextDouble() < 0.5 ? -1f : 1f);
                float thick = radius * R(0.2f, 0.36f);
                float spreadR = radius * R(0.65f, 1.05f);
                float wob = R(0f, 6.28f);
                Vector3 Path(float t)
                {
                    float pinch = 0.45f + 0.55f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    float ang = a0 + t * twist;
                    float r = spreadR * pinch * (1f + 0.15f * Mathf.Sin(t * 9f + wob));
                    return new Vector3(Mathf.Cos(ang) * r, t, Mathf.Sin(ang) * r);
                }

                int start = verts.Count;
                for (int i = 0; i <= segs; i++)
                {
                    float t = Mathf.Lerp(-0.04f, 1.04f, i / (float)segs);
                    Vector3 c = Path(t);
                    Vector3 tangent = (Path(t + 0.01f) - Path(t - 0.01f)).normalized;
                    Vector3 side = Vector3.Cross(tangent, Vector3.forward);
                    if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(tangent, Vector3.right);
                    side.Normalize();
                    Vector3 other = Vector3.Cross(tangent, side);
                    float th = thick * (i == 0 || i == segs ? 0.55f : 1f);
                    for (int k = 0; k < sides; k++)
                    {
                        float ang = k / (float)sides * Mathf.PI * 2f;
                        // x/z offsets are in height units, y in bone-length units: scale y offset down so rings stay round-ish
                        Vector3 n = side * Mathf.Cos(ang) + other * Mathf.Sin(ang);
                        verts.Add(c + new Vector3(n.x * th, n.y * th * 0.15f, n.z * th));
                        normals.Add(n);
                    }
                }
                for (int i = 0; i < segs; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a = start + i * sides + k, b = start + i * sides + (k + 1) % sides;
                    int c = a + sides, d = b + sides;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }

                // thorns
                int thorns = rnd.Next(0, 3);
                for (int n = 0; n < thorns; n++)
                {
                    float t = R(0.15f, 0.85f);
                    Vector3 c = Path(t);
                    Vector3 outward = new Vector3(c.x, 0f, c.z);
                    outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Vector3.right;
                    Vector3 tip = c + outward * thick * R(2.5f, 4f) + new Vector3(0f, R(0.02f, 0.06f), 0f);
                    Vector3 along = Vector3.Cross(outward, Vector3.up).normalized * thick;
                    int b0 = verts.Count;
                    verts.Add(c + along); verts.Add(c - along); verts.Add(c + new Vector3(0f, 0.03f, 0f)); verts.Add(tip);
                    for (int q = 0; q < 4; q++) normals.Add(outward);
                    tris.AddRange(new[] { b0, b0 + 1, b0 + 3, b0 + 1, b0 + 2, b0 + 3, b0 + 2, b0, b0 + 3 });
                }
            }

            // make every triangle face along its vertex normals (outward)
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 f = Vector3.Cross(verts[tris[i + 1]] - verts[tris[i]], verts[tris[i + 2]] - verts[tris[i]]);
                if (Vector3.Dot(f, normals[tris[i]]) < 0f) (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            }

            var mesh = new Mesh { name = $"VineBundle_{key.Item1}_{strands}_{variant}" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            bundles[key] = mesh;
            return mesh;
        }

        /// <summary>A lumpy knot of a heart (unit size), wrapped by the chest vines.</summary>
        private static Mesh HeartMesh()
        {
            if (heartMesh != null) return heartMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var src = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            var v = src.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = v[i];
                float lump = 1f + 0.18f * Mathf.Sin(p.x * 11f) * Mathf.Sin(p.y * 9f + 1f) * Mathf.Sin(p.z * 13f + 2f);
                p *= lump;
                p.y *= p.y < 0f ? 1.35f : 1f; // pointed at the bottom
                v[i] = p;
            }
            heartMesh = new Mesh { name = "VineHeart" };
            heartMesh.vertices = v;
            heartMesh.triangles = src.triangles;
            heartMesh.RecalculateNormals();
            heartMesh.RecalculateBounds();
            return heartMesh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            bundles.Clear();
            heartMesh = null;
        }
    }
}
