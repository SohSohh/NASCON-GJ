using UnityEngine;

namespace NasconGJ.Cutscenes
{
    /// <summary>
    /// Recolours shared kit materials on one object at runtime (per material slot, matched by material name),
    /// e.g. a blue house next to an ochre one, without making new material assets.
    /// </summary>
    public class SurfaceTint : MonoBehaviour
    {
        [SerializeField] private string[] materialNames = { };
        [SerializeField] private Color[] colors = { };

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    for (int n = 0; n < materialNames.Length && n < colors.Length; n++)
                    {
                        if (!mats[i].name.Contains(materialNames[n])) continue;
                        r.GetPropertyBlock(block, i);
                        block.SetColor(BaseColorId, colors[n]);
                        r.SetPropertyBlock(block, i);
                    }
                }
            }
        }
    }
}
