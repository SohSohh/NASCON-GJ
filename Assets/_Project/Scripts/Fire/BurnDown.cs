using UnityEngine;

namespace NasconGJ.Fire
{
    /// <summary>
    /// Makes a burning item visibly burn away: the fuel section (torch head, stick, candle wax) shortens from the top
    /// and blackens as fuel runs out, a charred cap with glowing embers rides on top, and the flame follows it down.
    /// What's left when it goes out is a blackened stub.
    /// The fuel transform must be a direct child with its pivot at the bottom of the section.
    /// </summary>
    [RequireComponent(typeof(FlammableItem))]
    public class BurnDown : MonoBehaviour
    {
        [SerializeField] private Transform fuel;
        [Tooltip("Local height of the fuel section at full fuel.")]
        [SerializeField] private float fuelLength = 0.15f;
        [Tooltip("Fraction of the fuel section left when the fuel runs out.")]
        [SerializeField] private float minScale = 0.15f;
        [SerializeField] private Transform charCap;
        [Tooltip("Show the charred cap before the item has ever burned (a torch head is already tarred black).")]
        [SerializeField] private bool capBeforeLit = true;
        [SerializeField] private Transform flame;
        [SerializeField] private float flameAboveTop = 0.02f;
        [SerializeField] private Color charColor = new(0.05f, 0.035f, 0.03f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color emberColor = new(1.8f, 0.45f, 0.08f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private FlammableItem item;
        private Renderer fuelRenderer, capRenderer;
        private Color[] fuelColors;
        private MaterialPropertyBlock block;
        private Vector3 fuelScale, capBase, flameBase;
        private float level = 1f, appliedDarkness = -1f, seed;
        private bool everLit;

        /// <summary>0..1, how much of the fuel section is left (stays put once the item goes out).</summary>
        public float Level => level;

        private void Awake()
        {
            item = GetComponent<FlammableItem>();
            block = new MaterialPropertyBlock();
            seed = Random.value * 100f;
            if (fuel != null)
            {
                fuelScale = fuel.localScale;
                fuelRenderer = fuel.GetComponent<Renderer>();
            }
            if (charCap != null)
            {
                capBase = charCap.localPosition;
                capRenderer = charCap.GetComponent<Renderer>();
            }
            if (flame != null) flameBase = flame.localPosition;
            if (fuelRenderer != null)
            {
                var mats = fuelRenderer.sharedMaterials;
                fuelColors = new Color[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                    fuelColors[i] = mats[i] != null && mats[i].HasProperty(BaseColorId) ? mats[i].GetColor(BaseColorId) : Color.white;
            }
        }

        private void LateUpdate()
        {
            if (item.IsBurning)
            {
                everLit = true;
                level = item.InfiniteBurn ? 1f : item.NormalizedFuel;
            }
            if (fuel == null) return;

            float scale = Mathf.Lerp(minScale, 1f, level);
            fuel.localScale = new Vector3(fuelScale.x, fuelScale.y * scale, fuelScale.z);
            float top = fuel.localPosition.y + fuelLength * scale;

            if (charCap != null)
            {
                charCap.localPosition = new Vector3(capBase.x, top, capBase.z);
                if (capRenderer != null)
                {
                    // follow the item's own visibility (holstered items hide their renderers)
                    bool itemVisible = fuelRenderer == null || fuelRenderer.enabled;
                    capRenderer.enabled = itemVisible && (everLit || capBeforeLit);
                    float drainBoost = item.BurnRateMultiplier > 1.05f ? Mathf.Min(item.BurnRateMultiplier - 1f, 1.5f) : 0f;
                    float flicker = 0.65f + 0.35f * Mathf.PerlinNoise(Time.time * (3f + drainBoost * 5f), seed) + drainBoost * 0.45f;
                    capRenderer.GetPropertyBlock(block, 0); // per-material, like CarryableItem's highlight, so neither overrides the other
                    block.SetColor(EmissionId, item.IsBurning ? emberColor * flicker : Color.black);
                    capRenderer.SetPropertyBlock(block, 0);
                }
            }
            if (flame != null) flame.localPosition = new Vector3(flameBase.x, top + flameAboveTop, flameBase.z);

            // the wood/cloth blackens as it's consumed
            float darkness = everLit ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 0.1f, level)) * 0.85f : 0f;
            if (fuelRenderer == null || Mathf.Abs(darkness - appliedDarkness) < 0.01f) return;
            appliedDarkness = darkness;
            for (int i = 0; i < fuelColors.Length; i++)
            {
                fuelRenderer.GetPropertyBlock(block, i);
                block.SetColor(BaseColorId, Color.Lerp(fuelColors[i], charColor, darkness));
                fuelRenderer.SetPropertyBlock(block, i);
            }
        }
    }
}
