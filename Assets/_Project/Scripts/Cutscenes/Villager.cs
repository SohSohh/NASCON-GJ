using System.Collections;
using NasconGJ.Player;
using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// A townsperson in the prologue. Strolls between two points (turning at the ends) or idles, using
    /// <see cref="ProceduralHumanoid"/> when the model is rigged (planted-foot walk, breathing, head look).
    /// When the Void comes: stops, turns and looks up at it, then drains to black and comes apart as smoke,
    /// leaving only their shadow behind.
    /// </summary>
    public class Villager : MonoBehaviour
    {
        public enum Activity { Idle, Chat, Sell, HandsBehind, Carry, Sit, Story, Listen }

        [Tooltip("What they're doing when the cutscene catches them (all procedural, eased in and out).")]
        [SerializeField] private Activity activity = Activity.Idle;
        [Tooltip("0 = upright, 1 = old and stooped.")]
        [SerializeField, Range(0f, 1f)] private float stoop;
        [Tooltip("World offset to stroll to and back. Zero = stand and idle.")]
        [SerializeField] private Vector3 strollOffset;
        [SerializeField] private float strollSpeed = 1.05f;
        [SerializeField] private float phase;
        [Tooltip("Someone to talk to: faces them, nods, gestures while it's their turn to speak.")]
        [SerializeField] private Transform talkTo;
        [Tooltip("Dark smoke the body comes apart into.")]
        [SerializeField] private Material smokeMaterial;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Shadow = new(0.02f, 0.008f, 0.016f, 1f);

        private ProceduralHumanoid rig;
        private Transform lookTarget, gestureHand, otherHand, partnerHead;
        private Vector3 home;
        private Quaternion baseRotation;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private ParticleSystem smoke;
        private bool stopped;
        private float time, speed;
        private int leg; // 0 = going out, 1 = coming back
        private float pause;

        private void Start()
        {
            home = transform.position;
            baseRotation = transform.rotation;
            rig = GetComponent<ProceduralHumanoid>();
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            time = phase * 10f;
            if (rig != null)
            {
                gestureHand = new GameObject("GestureHand").transform;
                gestureHand.SetParent(transform, false);
                otherHand = new GameObject("OtherHand").transform;
                otherHand.SetParent(transform, false);
                rig.Stoop = stoop;
                if (talkTo != null)
                {
                    partnerHead = new GameObject("PartnerHead").transform;
                    partnerHead.SetParent(talkTo, false);
                    partnerHead.localPosition = Vector3.up * 1.55f;
                }
            }
            if (strollOffset.sqrMagnitude > 0.01f)
            {
                // start somewhere along the route so the street doesn't move in lockstep
                float start = Mathf.Repeat(phase * 1.7f, 1f);
                transform.position = home + strollOffset * start;
                transform.rotation = Quaternion.LookRotation(Flat(strollOffset));
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }

        private void Update()
        {
            time += Time.deltaTime;
            float dt = Time.deltaTime;

            if (!stopped && talkTo != null)
            {
                Vector3 to = Flat(talkTo.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 2f);
            }

            if (scripted)
            {
                // cutscene-driven walk to a point, then stand
                Vector3 to = scriptTarget - transform.position;
                to.y = 0f;
                if (to.magnitude < 0.08f) { scripted = false; speed = 0f; }
                else
                {
                    speed = Mathf.MoveTowards(speed, Mathf.Min(scriptSpeed, to.magnitude * 1.5f + 0.2f), dt * 1.5f);
                    transform.position += to.normalized * speed * dt;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 4f);
                }
            }
            else if (stopped)
            {
                speed = Mathf.MoveTowards(speed, 0f, dt * 2.5f);
                if (lookTarget != null)
                {
                    Vector3 to = Flat(lookTarget.position - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 2.2f);
                }
            }
            else if (strollOffset.sqrMagnitude > 0.01f)
            {
                Vector3 goal = leg == 0 ? home + strollOffset : home;
                Vector3 to = goal - transform.position;
                to.y = 0f;
                if (pause > 0f)
                {
                    pause -= dt;
                    speed = Mathf.MoveTowards(speed, 0f, dt * 3f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Flat(to)), dt * 3f);
                }
                else if (to.magnitude < 0.08f)
                {
                    leg = 1 - leg;
                    pause = 0.9f + Mathf.Repeat(phase * 3.1f, 1f);
                }
                else
                {
                    speed = Mathf.MoveTowards(speed, Mathf.Min(strollSpeed, to.magnitude * 1.5f + 0.2f), dt * 1.5f);
                    Vector3 dir = to.normalized;
                    transform.position += dir * speed * dt;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 4f);
                }
            }

            if (rig == null)
            {
                // unrigged placeholder: a little sway so it isn't a statue
                float bob = speed > 0.05f ? Mathf.Abs(Mathf.Sin(time * 6f)) * 0.03f : Mathf.Sin(time * 1.4f) * 0.006f;
                var p = transform.position;
                p.y = home.y + bob;
                transform.position = p;
            }
        }

        private void LateUpdate()
        {
            if (rig == null || !rig.enabled) return;
            rig.Speed = speed;
            rig.LookTarget = lookTarget != null ? lookTarget : partnerHead;

            float weight = 0f, otherWeight = 0f;
            float beat = Mathf.PerlinNoise(time * 1.6f, phase * 7f);
            float slow = Mathf.PerlinNoise(time * 0.5f, phase * 3f);
            bool afraid = lookTarget != null;
            if (gestureHand != null && !afraid)
            {
                switch (activity)
                {
                    case Activity.Chat when talkTo != null:
                    case Activity.Idle when talkTo != null:
                    {
                        // take turns: the speaker talks with their hands (sometimes both), the listener nods
                        float turn = Mathf.Sin(time * 0.45f + phase * 3.1f);
                        float speaking = Mathf.SmoothStep(0f, 1f, turn * 2f);
                        gestureHand.localPosition = new Vector3(0.14f + beat * 0.08f, 0.98f + Mathf.PerlinNoise(phase * 5f, time * 1.3f) * 0.14f, 0.28f + beat * 0.07f);
                        otherHand.localPosition = new Vector3(-0.16f - slow * 0.06f, 0.95f + slow * 0.12f, 0.26f + beat * 0.04f);
                        weight = speaking * 0.55f;
                        otherWeight = speaking * Mathf.SmoothStep(0f, 1f, (slow - 0.45f) * 4f) * 0.45f;
                        break;
                    }
                    case Activity.Sell:
                        // leaning over the counter, showing goods: one hand out, the other resting on the counter
                        gestureHand.localPosition = new Vector3(0.12f, 1.0f + beat * 0.12f, 0.42f + slow * 0.08f);
                        otherHand.localPosition = new Vector3(-0.2f, 0.92f, 0.38f);
                        weight = 0.75f; otherWeight = 0.7f;
                        break;
                    case Activity.HandsBehind:
                        gestureHand.localPosition = new Vector3(0.06f, 0.92f, -0.17f);
                        otherHand.localPosition = new Vector3(-0.06f, 0.92f, -0.17f);
                        weight = otherWeight = 0.85f;
                        break;
                    case Activity.Carry:
                        // a crate or basket held against the belly
                        gestureHand.localPosition = new Vector3(0.17f, 0.98f, 0.3f);
                        otherHand.localPosition = new Vector3(-0.17f, 0.98f, 0.3f);
                        weight = otherWeight = 0.95f;
                        break;
                    case Activity.Story:
                        // the old man telling the tale: a slow hand that rises and falls with the words
                        gestureHand.localPosition = new Vector3(0.18f, 0.85f + Mathf.Sin(time * 0.9f) * 0.12f + beat * 0.15f, 0.34f + slow * 0.1f);
                        otherHand.localPosition = new Vector3(-0.16f, 0.62f, 0.24f);            // resting on the knee
                        weight = 0.7f; otherWeight = 0.6f;
                        break;
                    case Activity.Listen:
                        gestureHand.localPosition = new Vector3(0.12f, 0.5f, 0.28f);            // hands on knees, rapt
                        otherHand.localPosition = new Vector3(-0.12f, 0.5f, 0.28f);
                        weight = otherWeight = 0.6f;
                        break;
                }
            }
            else if (gestureHand != null && afraid)
            {
                // afraid: a hand comes up toward the mouth, the other half-raised
                gestureHand.localPosition = new Vector3(0.05f, 1.42f, 0.22f);
                otherHand.localPosition = new Vector3(-0.2f, 1.1f, 0.25f);
                weight = 0.9f; otherWeight = 0.5f;
            }
            rig.Sit = !afraid && (activity == Activity.Sit || activity == Activity.Story || activity == Activity.Listen) ? 1f : 0f;
            rig.RightHandTarget = gestureHand;
            rig.RightHandWeight = Mathf.MoveTowards(rig.RightHandWeight, weight, Time.deltaTime * 1.5f);
            rig.LeftHandTarget = otherHand;
            rig.LeftHandWeight = Mathf.MoveTowards(rig.LeftHandWeight, otherWeight, Time.deltaTime * 1.5f);
            rig.Apply(Time.deltaTime);
        }

        /// <summary>Stop and look up at something (the Void rising).</summary>
        public void LookAt(Transform target)
        {
            stopped = true;
            lookTarget = target;
        }

        /// <summary>Gone in an instant (inside the darkness, nobody sees it happen).</summary>
        public void Erase()
        {
            stopped = true;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            enabled = false;
        }

        /// <summary>The Void takes them: they drain to black, then come apart as smoke.</summary>
        public void Shadowify(float duration, bool statue = false)
        {
            keepStatue = statue;
            StartCoroutine(Drain(duration));
        }

        private bool keepStatue;
        private bool scripted;
        private Vector3 scriptTarget;
        private float scriptSpeed;

        /// <summary>Walk to a point (cutscene-driven), then stand there.</summary>
        public void WalkTo(Vector3 target, float walkSpeed)
        {
            stopped = true;
            lookTarget = null;
            scripted = true;
            scriptTarget = target;
            scriptSpeed = walkSpeed;
        }

        /// <summary>Start already emptied: a black statue of themselves with smoke curling off.</summary>
        public void SetShadowed()
        {
            if (renderers == null) return;
            keepStatue = true;
            stopped = true;
            BuildSmoke();
            if (smoke != null) { smoke.Play(); var em = smoke.emission; em.rateOverTime = 6f; }
            Tint(1f);
        }

        /// <summary>The light reaches them: colour floods back, the smoke blows off, and they go on with their day.</summary>
        public void Restore(float duration) => StartCoroutine(RestoreRoutine(duration));

        private IEnumerator RestoreRoutine(float duration)
        {
            if (smoke != null) smoke.Emit(40);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Tint(1f - Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                for (int i = 0; i < r.sharedMaterials.Length; i++) r.SetPropertyBlock(null, i);
            }
            if (smoke != null) { var em = smoke.emission; em.enabled = false; }
            keepStatue = false;
            lookTarget = null;
            stopped = false;
        }

        private IEnumerator Drain(float duration)
        {
            stopped = true;
            BuildSmoke();
            if (smoke != null) smoke.Play();
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                Tint(k);
                yield return null;
            }
            Tint(1f);
            if (keepStatue)
            {
                // they stay where they stood: a black statue of themselves, smoke curling off
                if (smoke != null) { var em = smoke.emission; em.rateOverTime = 6f; }
                yield break;
            }
            yield return new WaitForSeconds(0.25f);
            if (smoke != null) smoke.Emit(60);
            // the body is gone; only the shadow on the wall remains
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * (1f - t / 0.5f * 0.9f);
                yield return null;
            }
            foreach (var r in renderers) if (r != null && r != (smoke != null ? smoke.GetComponent<Renderer>() : null)) r.enabled = false;
            if (smoke != null) { var em = smoke.emission; em.enabled = false; }
        }

        private void Tint(float k)
        {
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].HasProperty(BaseColorId)) continue;
                    r.GetPropertyBlock(block, i);
                    block.SetColor(BaseColorId, Color.Lerp(mats[i].GetColor(BaseColorId), Shadow, k));
                    block.SetFloat(RimStrengthId, 0f);
                    block.SetColor(EmissionId, Color.black);
                    r.SetPropertyBlock(block, i);
                }
            }
        }

        private void BuildSmoke()
        {
            if (smoke != null || smokeMaterial == null) return;
            var go = new GameObject("Smoke");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.9f;
            smoke = go.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = smoke.emission;
            em.rateOverTime = 26f;
            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.45f, 1.6f, 0.3f);
            var noise = smoke.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 1.5f;
            var col = smoke.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.4f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = smokeMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
