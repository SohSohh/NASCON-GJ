using System.Collections.Generic;
using NasconGJ.Entities;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>
    /// The thing that chases you out with the crystal: one mass of shadow that pours along every surface at once,
    /// the walls, the ceiling and the floor, boiling and changing shape (ShadowMass shader), with eyes opening inside
    /// it, tendrils racing ahead of it along the walls, and ash tearing off its front. Runs along +z; its head is at
    /// <see cref="HeadZ"/>. The corridor around it is measured with raycasts, so it fills a wide hall as well as a
    /// narrow hallway.
    /// </summary>
    public class ChaseEntity : MonoBehaviour
    {
        private enum Surface { LeftWall, RightWall, Ceiling, Floor }

        private class Part
        {
            public ShadowSurfaceSkin Skin;
            public MeshRenderer Renderer;
            public Surface On;
            public int Segment;                 // body: 0 = the head segment; tendril: -1; veil: -2 (and -3 behind it)
            public float Z, Lateral, Seed;      // tendrils only
        }

        public float HeadZ;
        public float PlayerZ;
        [Range(0f, 1f)] public float Spread;
        [Range(0f, 1f)] public float Opacity = 1f;
        public float Speed;

        private const int Segments = 4;
        private const float Length = 8f;
        private const float TendrilLength = 3.2f;

        private readonly List<Part> parts = new();
        private float centreX;
        private LayerMask surfaceMask;
        private Material massMaterial;
        private MaterialPropertyBlock block;
        private ParticleSystem ash, smoke;
        private static readonly int SegId = Shader.PropertyToID("_Seg");
        private static readonly int Seg2Id = Shader.PropertyToID("_Seg2");
        private static readonly int SpreadId = Shader.PropertyToID("_Spread");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        public void Init(Material mass, Material ashMaterial, float x, LayerMask mask)
        {
            massMaterial = new Material(mass);
            centreX = x;
            surfaceMask = mask;
            block = new MaterialPropertyBlock();
            for (int s = 0; s < 4; s++)
                for (int k = 0; k < Segments; k++)
                    parts.Add(MakePart((Surface)s, k));
            parts.Add(Tendril(Surface.LeftWall, 0.4f));
            parts.Add(Tendril(Surface.RightWall, -0.3f));
            parts.Add(Tendril(Surface.Ceiling, 0.5f));
            parts.Add(Tendril(Surface.LeftWall, -0.6f));
            parts.Add(Veil(-2));
            parts.Add(Veil(-3));
            ash = MakeParticles("Ash", ashMaterial, 0.04f, 0.2f, 0.5f, 1.3f, new Color(0.02f, 0.008f, 0.02f, 0.95f), 90f, 400);
            smoke = MakeParticles("Smoke", ashMaterial, 0.5f, 1.5f, 1.4f, 2.6f, new Color(0.03f, 0.01f, 0.035f, 0.5f), 34f, 200);
        }

        /// <summary>A burst of ash as it tears out of the walls.</summary>
        public void Burst(int count) { if (ash != null) ash.Emit(count); if (smoke != null) smoke.Emit(count / 4); }

        private Part MakePart(Surface on, int segment)
        {
            var go = new GameObject($"Mass_{on}_{segment}", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            var skin = go.AddComponent<ShadowSurfaceSkin>();
            skin.SetSurfaceMask(surfaceMask);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = massMaterial;
            return new Part { Skin = skin, Renderer = r, On = on, Segment = segment, Seed = (int)on * 7.31f };
        }

        private Part Tendril(Surface on, float lateral)
        {
            var p = MakePart(on, -1);
            p.Lateral = lateral;
            p.Seed = 40f + lateral * 13f;
            p.Z = HeadZ;
            return p;
        }

        /// <summary>A sheet hung across the whole corridor at the head: the face of the dark, full of eyes.</summary>
        private Part Veil(int which)
        {
            var go = new GameObject("Veil" + -which, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            var mesh = new Mesh { name = "Veil" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = massMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new Part { Renderer = r, Segment = which, Seed = 90f - which * 11f };
        }

        private ParticleSystem MakeParticles(string name, Material mat, float size0, float size1, float life0, float life1, Color color, float rate, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life0, life1);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size0, size1);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.4f, 0.7f);
            vel.z = new ParticleSystem.MinMaxCurve(2f, 7f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.9f;
            noise.frequency = 0.7f;
            noise.scrollSpeed = 0.6f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            go.AddComponent<RateHolder>().Rate = rate;
            return ps;
        }

        /// <summary>Remembers the full emission rate of a particle system (scaled by spread).</summary>
        private class RateHolder : MonoBehaviour { public float Rate; }

        // ------------------------------------------------------------------ every frame

        private void LateUpdate()
        {
            if (massMaterial == null) return;
            massMaterial.SetFloat(SpreadId, Spread);
            massMaterial.SetFloat(OpacityId, Opacity);
            float segLen = Length / Segments;

            foreach (var p in parts)
            {
                if (p.Segment <= -2)
                {
                    // the veils: across the whole corridor, one just behind the head, a solid one behind that
                    float vz = HeadZ - (p.Segment == -2 ? 1.2f : 3.4f);
                    Measure(vz, out float vl, out float vr, out float vc);
                    float w = vl + vr, hgt = vc;
                    p.Renderer.transform.SetPositionAndRotation(new Vector3(centreX + (vr - vl) * 0.5f, hgt * 0.5f, vz), Quaternion.identity);
                    p.Renderer.transform.localScale = new Vector3(w, hgt, 1f);
                    p.Renderer.GetPropertyBlock(block);
                    block.SetVector(SegId, new Vector4(0f, 0f, w, p.Seed));
                    block.SetVector(Seg2Id, new Vector4(0f, 999f, 1f, hgt));   // no tail: solid in the middle
                    p.Renderer.SetPropertyBlock(block);
                    continue;
                }
                float zFront, zBack, dFront, dBack, len;
                if (p.Segment >= 0)
                {
                    zFront = HeadZ - p.Segment * segLen;
                    zBack = zFront - segLen - 0.35f;
                    dFront = p.Segment * segLen;
                    dBack = dFront + segLen + 0.35f;
                    len = 0f;
                }
                else
                {
                    // tendrils race along beside you, surging ahead and falling back
                    float target = Mathf.Max(HeadZ + 2f, PlayerZ + 2f + Mathf.Sin(Time.time * 0.8f + p.Seed) * 3.5f);
                    float v = Mathf.Clamp((target - p.Z) * 2.2f, -3f, 11f);
                    p.Z += v * Time.deltaTime;
                    p.Z = Mathf.Max(p.Z, HeadZ - 1f);
                    zFront = p.Z; zBack = p.Z - TendrilLength;
                    dFront = 0f; dBack = TendrilLength;
                    len = TendrilLength;
                }

                Measure((zFront + zBack) * 0.5f, out float left, out float right, out float ceil);
                Vector3 basePoint, normal;
                float across;
                bool wall = p.On == Surface.LeftWall || p.On == Surface.RightWall;
                if (p.Segment < 0 && wall)
                {
                    across = 1.1f;
                    float y = Mathf.Clamp(1.6f + p.Lateral + Mathf.Sin(Time.time * 1.7f + p.Seed) * 0.35f, 0.7f, ceil - 0.7f);
                    basePoint = new Vector3(p.On == Surface.LeftWall ? centreX - left : centreX + right, y, zBack);
                }
                else if (p.Segment < 0)
                {
                    across = 1.1f;
                    basePoint = new Vector3(centreX + p.Lateral + Mathf.Sin(Time.time * 1.3f + p.Seed) * 0.3f, ceil, zBack);
                }
                else switch (p.On)
                {
                    case Surface.LeftWall: across = ceil; basePoint = new Vector3(centreX - left, ceil * 0.5f, zBack); break;
                    case Surface.RightWall: across = ceil; basePoint = new Vector3(centreX + right, ceil * 0.5f, zBack); break;
                    case Surface.Ceiling: across = left + right; basePoint = new Vector3(centreX + (right - left) * 0.5f, ceil, zBack); break;
                    default: across = left + right; basePoint = new Vector3(centreX + (right - left) * 0.5f, 0f, zBack); break;
                }
                normal = p.On switch { Surface.LeftWall => Vector3.right, Surface.RightWall => Vector3.left, Surface.Ceiling => Vector3.down, _ => Vector3.up };
                p.Skin.Conform(basePoint, normal, Vector3.forward, across, zFront - zBack);

                p.Renderer.GetPropertyBlock(block);
                block.SetVector(SegId, new Vector4(dBack, dFront, across, p.Seed));
                block.SetVector(Seg2Id, new Vector4(wall ? 1f : 0f, len, 0f, 0f));
                p.Renderer.SetPropertyBlock(block);
            }

            // ash torn off the front, smoke rolling off the body
            Measure(HeadZ, out float l, out float rr, out float c);
            PlaceEmitter(ash, new Vector3(centreX + (rr - l) * 0.5f, c * 0.5f, HeadZ - 0.3f), new Vector3(l + rr - 0.2f, c - 0.2f, 1.2f), Speed);
            PlaceEmitter(smoke, new Vector3(centreX + (rr - l) * 0.5f, c * 0.5f, HeadZ - Length * 0.35f), new Vector3(l + rr - 0.4f, c - 0.4f, Length * 0.6f), Speed * 0.6f);
        }

        private void PlaceEmitter(ParticleSystem ps, Vector3 pos, Vector3 size, float speed)
        {
            if (ps == null) return;
            ps.transform.position = pos;
            var shape = ps.shape;
            shape.scale = size;
            var emission = ps.emission;
            emission.rateOverTime = ps.GetComponent<RateHolder>().Rate * Spread * Opacity;
            var vel = ps.velocityOverLifetime;
            vel.z = new ParticleSystem.MinMaxCurve(speed * 0.6f + 1f, speed * 1.3f + 3f);
        }

        /// <summary>Half-widths to the left and right walls and the ceiling height at <paramref name="z"/>.</summary>
        private void Measure(float z, out float left, out float right, out float ceil)
        {
            var o = new Vector3(centreX, 3f, z);                  // above the rubble and the broken pillars
            left = Physics.Raycast(o, Vector3.left, out var h, 20f, surfaceMask, QueryTriggerInteraction.Ignore) ? h.distance : 1.7f;
            right = Physics.Raycast(o, Vector3.right, out h, 20f, surfaceMask, QueryTriggerInteraction.Ignore) ? h.distance : 1.7f;
            ceil = Physics.Raycast(o + Vector3.down * 1.8f, Vector3.up, out h, 20f, surfaceMask, QueryTriggerInteraction.Ignore) ? h.point.y : 3.9f;
            left = Mathf.Min(left, 8f); right = Mathf.Min(right, 8f); ceil = Mathf.Min(ceil, 9f);
        }
    }
}
