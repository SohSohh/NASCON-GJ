using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Builds flat-shaded low-poly meshes out of boxes and prisms, grouped by material slot.
    /// The result is one Mesh with one sub-mesh per material used.
    /// </summary>
    public class MeshBuilder
    {
        private static Mesh unitBox;
        private static readonly Dictionary<int, Mesh> prisms = new();

        private readonly SortedDictionary<int, List<CombineInstance>> groups = new();
        public readonly System.Random Rng;

        public MeshBuilder(int seed) => Rng = new System.Random(seed);

        public float Range(float min, float max) => min + (float)Rng.NextDouble() * (max - min);
        public bool Chance(float p) => Rng.NextDouble() < p;

        /// <summary>Axis-aligned (then rotated) box centred on <paramref name="center"/>.</summary>
        public MeshBuilder Box(MatId mat, Vector3 center, Vector3 size, Vector3 euler = default) =>
            Add(mat, UnitBox, Matrix4x4.TRS(center, Quaternion.Euler(euler), size));

        /// <summary>N-sided prism standing on <paramref name="baseCenter"/>.</summary>
        public MeshBuilder Prism(MatId mat, int sides, Vector3 baseCenter, float radius, float height, Vector3 euler = default) =>
            Add(mat, UnitPrism(sides), Matrix4x4.TRS(baseCenter, Quaternion.Euler(euler), new Vector3(radius, height, radius)));

        /// <summary>Triangular prism (a roof): base centred on <paramref name="baseCenter"/>, ridge along X at height size.y.</summary>
        public MeshBuilder Wedge(MatId mat, Vector3 baseCenter, Vector3 size, Vector3 euler = default) =>
            Add(mat, UnitWedge, Matrix4x4.TRS(baseCenter, Quaternion.Euler(euler), size));

        private MeshBuilder Add(MatId mat, Mesh mesh, Matrix4x4 matrix)
        {
            if (!groups.TryGetValue((int)mat, out var list)) groups[(int)mat] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mesh, transform = matrix });
            return this;
        }

        public Mesh Build(string name, out MatId[] materialSlots)
        {
            var subMeshes = new List<CombineInstance>();
            var temps = new List<Mesh>();
            foreach (var pair in groups)
            {
                var m = new Mesh { indexFormat = IndexFormat.UInt32 };
                m.CombineMeshes(pair.Value.ToArray(), true, true);
                temps.Add(m);
                subMeshes.Add(new CombineInstance { mesh = m, transform = Matrix4x4.identity });
            }

            var final = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            final.CombineMeshes(subMeshes.ToArray(), false, false);
            final.RecalculateBounds();
            final.RecalculateTangents();
            foreach (var t in temps) Object.DestroyImmediate(t);

            materialSlots = groups.Keys.Select(k => (MatId)k).ToArray();
            return final;
        }

        // ---------------- primitive meshes ----------------

        private static Mesh UnitBox
        {
            get
            {
                if (unitBox != null) return unitBox;
                var verts = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                foreach (var n in dirs)
                {
                    Vector3 a = n.y != 0 ? Vector3.forward : Vector3.up;
                    Vector3 b = Vector3.Cross(n, a);
                    int start = verts.Count;
                    Vector3 c = n * 0.5f;
                    verts.Add(c - a * 0.5f - b * 0.5f);
                    verts.Add(c - a * 0.5f + b * 0.5f);
                    verts.Add(c + a * 0.5f + b * 0.5f);
                    verts.Add(c + a * 0.5f - b * 0.5f);
                    for (int i = 0; i < 4; i++) normals.Add(n);
                    uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                    tris.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                }
                unitBox = new Mesh { name = "UnitBox" };
                unitBox.SetVertices(verts);
                unitBox.SetNormals(normals);
                unitBox.SetUVs(0, uvs);
                unitBox.SetTriangles(tris, 0);
                FixWinding(unitBox);
                return unitBox;
            }
        }

        private static Mesh unitWedge;

        private static Mesh UnitWedge
        {
            get
            {
                if (unitWedge != null) return unitWedge;
                var verts = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                void Face(Vector3[] p)
                {
                    Vector3 n = Vector3.Cross(p[1] - p[0], p[2] - p[0]).normalized;
                    Vector3 centroid = Vector3.zero;
                    foreach (var q in p) centroid += q / p.Length;
                    if (Vector3.Dot(n, centroid - new Vector3(0f, 0.33f, 0f)) < 0f) n = -n; // outward from the wedge's centre
                    int s = verts.Count;
                    foreach (var q in p) { verts.Add(q); normals.Add(n); uvs.Add(new Vector2(q.x + 0.5f, q.y + q.z)); }
                    for (int i = 1; i + 1 < p.Length; i++) tris.AddRange(new[] { s, s + i, s + i + 1 });
                }
                Vector3 a = new(-0.5f, 0, -0.5f), b = new(0.5f, 0, -0.5f), c = new(0.5f, 0, 0.5f), d = new(-0.5f, 0, 0.5f);
                Vector3 r0 = new(-0.5f, 1, 0), r1 = new(0.5f, 1, 0);
                Face(new[] { a, b, c, d });     // bottom
                Face(new[] { a, b, r1, r0 });   // front slope
                Face(new[] { d, c, r1, r0 });   // back slope
                Face(new[] { a, d, r0 });       // gable ends
                Face(new[] { b, c, r1 });
                unitWedge = new Mesh { name = "UnitWedge" };
                unitWedge.SetVertices(verts);
                unitWedge.SetNormals(normals);
                unitWedge.SetUVs(0, uvs);
                unitWedge.SetTriangles(tris, 0);
                FixWinding(unitWedge);
                return unitWedge;
            }
        }

        private static Mesh UnitPrism(int sides)
        {
            if (prisms.TryGetValue(sides, out var cached) && cached != null) return cached;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            Vector3 Ring(int i) { float a = i * Mathf.PI * 2f / sides; return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); }

            for (int i = 0; i < sides; i++)
            {
                Vector3 p0 = Ring(i), p1 = Ring(i + 1);
                Vector3 n = ((p0 + p1) * 0.5f).normalized;
                int s = verts.Count;
                verts.AddRange(new[] { p0, p1, p1 + Vector3.up, p0 + Vector3.up });
                for (int k = 0; k < 4; k++) normals.Add(n);
                uvs.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
                tris.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
            }

            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap;
                Vector3 n = cap == 1 ? Vector3.up : Vector3.down;
                int center = verts.Count;
                verts.Add(new Vector3(0, y, 0)); normals.Add(n); uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= sides; i++)
                {
                    Vector3 p = Ring(i);
                    verts.Add(p + Vector3.up * y); normals.Add(n); uvs.Add(new Vector2(p.x * 0.5f + 0.5f, p.z * 0.5f + 0.5f));
                }
                for (int i = 0; i < sides; i++)
                {
                    if (cap == 1) tris.AddRange(new[] { center, center + i + 2, center + i + 1 });
                    else tris.AddRange(new[] { center, center + i + 1, center + i + 2 });
                }
            }

            var mesh = new Mesh { name = "Prism" + sides };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            FixWinding(mesh);
            prisms[sides] = mesh;
            return mesh;
        }

        /// <summary>Makes every triangle wind so its face normal agrees with the vertex normals (outward).</summary>
        private static void FixWinding(Mesh mesh)
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (Vector3.Dot(face, n[t[i]]) < 0f) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
            }
            mesh.triangles = t;
            mesh.RecalculateBounds();
        }

        /// <summary>Simple 1x1 quad facing -Z (Unity convention), used for shadow sprites.</summary>
        public static Mesh Quad(string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) });
            m.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            m.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
