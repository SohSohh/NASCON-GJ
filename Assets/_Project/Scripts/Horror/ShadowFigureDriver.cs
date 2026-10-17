using NasconGJ.Player;
using UnityEngine;

namespace NasconGJ.Horror
{
    /// <summary>
    /// A scripted (non-hunting) shadow person for scares: a humanoid model drawn solid black, walked by
    /// <see cref="ProceduralHumanoid"/>, with two small red eyes that light up for the jump scare.
    /// Scripts set <see cref="Speed"/> and move the transform; this keeps the legs honest.
    /// </summary>
    public class ShadowFigureDriver : MonoBehaviour
    {
        [SerializeField] private Material shadowMaterial;
        [SerializeField] private Color eyeColor = new(1f, 0.12f, 0.05f);

        public float Speed { get; set; }
        /// <summary>Height of the head above the feet (to put the face right at eye level).</summary>
        public float HeadHeight { get; private set; } = 1.6f;
        public bool IsShown { get; private set; } = true;

        private ProceduralHumanoid rig;
        private Renderer[] renderers;
        private Renderer[] eyes;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (shadowMaterial == null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = shadowMaterial;
                r.sharedMaterials = mats;
            }
            rig = GetComponentInChildren<ProceduralHumanoid>() ?? gameObject.AddComponent<ProceduralHumanoid>();

            var anim = GetComponentInChildren<Animator>();
            var head = anim != null ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head != null)
            {
                HeadHeight = head.position.y - transform.position.y + 0.08f;
                var eyeMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                eyeMat.SetColor("_BaseColor", eyeColor * 3f);
                eyes = new Renderer[2];
                for (int i = 0; i < 2; i++)
                {
                    var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(e.GetComponent<Collider>());
                    e.name = "Eye";
                    e.transform.position = head.position + transform.forward * 0.1f + transform.up * 0.06f + transform.right * (i == 0 ? -0.035f : 0.035f);
                    e.transform.localScale = new Vector3(0.022f, 0.012f, 0.01f);
                    e.transform.SetParent(head, true);
                    eyes[i] = e.GetComponent<Renderer>();
                    eyes[i].sharedMaterial = eyeMat;
                    eyes[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    eyes[i].enabled = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (rig == null || !rig.enabled) return;
            rig.Speed = Speed;
            rig.Apply(Time.deltaTime);
        }

        public void Stoop(float amount)
        {
            if (rig != null) rig.Stoop = amount;
        }

        public void Eyes(bool on)
        {
            if (eyes != null) foreach (var e in eyes) if (e != null) e.enabled = on;
        }

        public void Show(bool visible)
        {
            IsShown = visible;
            foreach (var r in renderers) if (r != null) r.enabled = visible;
            if (!visible) Eyes(false);
        }
    }
}
