using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>
    /// A grid mesh that drapes a shadow sprite over whatever is behind it (walls, floors, pillars, props),
    /// so shadows bend around corners and spill onto the floor like real cast shadows.
    /// Each vertex is ray-cast onto the surface along -normal. If that misses (overhanging a convex edge),
    /// it wraps back toward the sprite's centre line to find the side face.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ShadowSurfaceSkin : MonoBehaviour
    {
        [SerializeField] private int columns = 10;
        [SerializeField] private int rows = 18;
        [SerializeField] private float surfaceOffset = 0.025f;
        [SerializeField] private float probeDepth = 0.6f;
        [SerializeField] private LayerMask surfaceMask = ~0;

        private static readonly int FrameId = Shader.PropertyToID("_Frame");
        private static readonly int FlipId = Shader.PropertyToID("_Flip");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        private Mesh mesh;
        private Vector3[] vertices;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock block;

        public MeshRenderer Renderer => meshRenderer;

        public void SetSurfaceMask(LayerMask mask) => surfaceMask = mask;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            block = new MaterialPropertyBlock();
            BuildGrid();
        }

        private void BuildGrid()
        {
            mesh = new Mesh { name = "ShadowSkin" };
            mesh.MarkDynamic();
            int vx = columns + 1, vy = rows + 1;
            vertices = new Vector3[vx * vy];
            var uvs = new Vector2[vertices.Length];
            for (int j = 0; j < vy; j++)
            for (int i = 0; i < vx; i++)
                uvs[j * vx + i] = new Vector2(i / (float)columns, j / (float)rows);

            var tris = new int[columns * rows * 6];
            int t = 0;
            for (int j = 0; j < rows; j++)
            for (int i = 0; i < columns; i++)
            {
                int a = j * vx + i, b = a + 1, c = a + vx, d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        /// <summary>Drape the sprite: feet at <paramref name="basePoint"/>, standing along <paramref name="up"/>, facing out along <paramref name="normal"/>.</summary>
        public void Conform(Vector3 basePoint, Vector3 normal, Vector3 up, float width, float height)
        {
            Vector3 right = Vector3.Cross(normal, up).normalized;
            int vx = columns + 1;
            for (int j = 0; j <= rows; j++)
            {
                float v = j / (float)rows;
                Vector3 axis = basePoint + up * (v * height);
                for (int i = 0; i <= columns; i++)
                {
                    float u = i / (float)columns;
                    Vector3 planar = axis + right * ((u - 0.5f) * width);
                    Vector3 p = planar;
                    if (Physics.Raycast(planar + normal * probeDepth, -normal, out RaycastHit hit, probeDepth * 2.2f, surfaceMask, QueryTriggerInteraction.Ignore))
                    {
                        p = hit.point + hit.normal * surfaceOffset;
                    }
                    else
                    {
                        Vector3 toAxis = axis - planar;
                        float len = toAxis.magnitude;
                        if (len > 0.01f && Physics.Raycast(planar - normal * 0.08f, toAxis / len, out hit, len, surfaceMask, QueryTriggerInteraction.Ignore))
                            p = hit.point + hit.normal * surfaceOffset;
                    }
                    vertices[j * vx + i] = transform.InverseTransformPoint(p);
                }
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        public void SetLook(Vector4 frame, bool flip, float glow, float dissolve, float opacity)
        {
            meshRenderer.GetPropertyBlock(block);
            block.SetVector(FrameId, frame);
            block.SetFloat(FlipId, flip ? 1f : 0f);
            block.SetFloat(GlowId, glow);
            block.SetFloat(DissolveId, dissolve);
            block.SetFloat(OpacityId, opacity);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
