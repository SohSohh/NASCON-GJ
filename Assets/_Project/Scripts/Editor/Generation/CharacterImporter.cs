using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Turns Meshy character exports into game-ready prefabs that match the painterly look.
    ///
    /// Folder convention (Art/Models/Characters/&lt;Name&gt;/):
    ///   SK_&lt;Name&gt;.fbx            rigged character (Meshy rig output, texture embedded or *_base_color.png beside it)
    ///   A_&lt;Name&gt;_&lt;Clip&gt;.fbx      Meshy animation exports (walk, run, ...)
    ///
    /// NASCON > Generate > 5. Character Prefabs creates, per character:
    ///   M_&lt;Name&gt;   NASCON/StylizedLit material using the base-colour texture
    ///   AC_&lt;Name&gt;  Animator Controller with one looping state per A_ clip (first clip is default)
    ///   PF_&lt;Name&gt;  prefab scaled to <see cref="DefaultHeight"/>, feet on the pivot
    /// </summary>
    public static class CharacterImporter
    {
        public const string CharacterRoot = GenUtil.Root + "/Art/Models/Characters";
        public const string CharacterPrefabDir = GenUtil.Root + "/Prefabs/Characters";
        public const string AnimatorDir = GenUtil.Root + "/Art/Animations";
        public const float DefaultHeight = 1.6f;

        [MenuItem("NASCON/Generate/5. Character Prefabs (Meshy imports)", priority = 303)]
        public static void ImportAll()
        {
            GenUtil.EnsureFolder(CharacterPrefabDir);
            GenUtil.EnsureFolder(AnimatorDir);
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { CharacterRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileName(path);
                if (!file.StartsWith("SK_") || !file.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
                Import(path);
            }
            AssetDatabase.SaveAssets();
        }

        public static GameObject Import(string fbxPath)
        {
            string dir = Path.GetDirectoryName(fbxPath)!.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(fbxPath).Substring(3);

            Avatar avatar = ConfigureCharacter(fbxPath, dir, name);
            var clips = ConfigureAnimations(dir, name, avatar);
            var controller = clips.Count > 0 ? BuildController(name, clips) : null;

            var texture = FindTexture(dir, name);
            var material = CharacterMaterial(name, texture);
            var fpMaterial = CharacterMaterial(name + "_FP", texture);
            fpMaterial.SetFloat("_MaxLight", 1.35f); // the torch is centimetres away in first person
            fpMaterial.SetVector("_CamClip", new Vector4(0.3f, 0.95f, 0f, 0f)); // never see your own head/shoulders

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = new GameObject("PF_" + name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = "Model";

            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }

            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // scale to target height, feet on the ground, centred on the pivot
            var bounds = CalculateBounds(instance);
            if (bounds.size.y > 0.001f)
            {
                instance.transform.localScale *= DefaultHeight / bounds.size.y;
                bounds = CalculateBounds(instance);
                instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }

            var prefab = GenUtil.SavePrefab(root, $"{CharacterPrefabDir}/PF_{name}.prefab");
            Debug.Log($"[Gen] PF_{name}: texture={(texture != null ? texture.name : "none")}, rig={(avatar != null && avatar.isHuman ? "Humanoid" : "Generic")}, clips={clips.Count}");
            return prefab;
        }

        // ---------------- import settings ----------------

        private static Avatar ConfigureCharacter(string fbxPath, string dir, string name)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();

            // Meshy embeds the texture in the FBX; pull it out so our own material can use it.
            string texDir = $"{dir}/Textures_{name}";
            if (!AssetDatabase.IsValidFolder(texDir))
            {
                GenUtil.EnsureFolder(texDir);
                importer.ExtractTextures(texDir);
                AssetDatabase.Refresh();
            }
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogWarning($"[Gen] {name}: humanoid avatar failed, falling back to Generic rig.");
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            }
            return avatar;
        }

        private static List<AnimationClip> ConfigureAnimations(string dir, string name, Avatar avatar)
        {
            var clips = new List<AnimationClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (!file.StartsWith($"A_{name}_")) continue;

                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = true;
                bool human = avatar != null && avatar.isHuman;
                importer.animationType = human ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;

                var defaults = importer.defaultClipAnimations;
                string clipName = file.Substring($"A_{name}_".Length);
                foreach (var c in defaults)
                {
                    c.name = clipName;
                    c.loopTime = true;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = defaults;
                importer.SaveAndReimport();

                clips.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")));
            }
            return clips.OrderByDescending(c => c.name == "Walk").ToList();
        }

        /// <summary>
        /// Stand (walk clip frozen on a neutral frame) -> Walk -> Run, driven by "Speed" (m/s) with "AnimSpeed"
        /// scaling playback so feet match ground speed. IK pass on, so scripts can pose hands and look direction.
        /// </summary>
        private static AnimatorController BuildController(string name, List<AnimationClip> clips)
        {
            // Rebuild in place (never delete) so the asset GUID - and every prefab referencing it - stays valid.
            string path = $"{AnimatorDir}/AC_{name}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var p in controller.parameters) controller.RemoveParameter(p);
            var oldMachine = controller.layers[0].stateMachine;
            foreach (var st in oldMachine.states) oldMachine.RemoveState(st.state);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("AnimSpeed", AnimatorControllerParameterType.Float);

            var layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;
            var machine = controller.layers[0].stateMachine;

            var walkClip = clips.FirstOrDefault(c => c.name == "Walk") ?? clips[0];
            var runClip = clips.FirstOrDefault(c => c.name == "Run");

            var stand = machine.AddState("Stand");
            stand.motion = walkClip;
            stand.speed = 0f;
            stand.cycleOffset = 0.22f; // passing pose: feet together, closest the walk has to a neutral stance
            machine.defaultState = stand;

            var walk = machine.AddState("Walk");
            walk.motion = walkClip;
            walk.speedParameterActive = true;
            walk.speedParameter = "AnimSpeed";

            Link(stand, walk, "Speed", AnimatorConditionMode.Greater, 0.15f, 0.2f);
            Link(walk, stand, "Speed", AnimatorConditionMode.Less, 0.1f, 0.3f);

            if (runClip != null)
            {
                var run = machine.AddState("Run");
                run.motion = runClip;
                run.speedParameterActive = true;
                run.speedParameter = "AnimSpeed";
                Link(walk, run, "Speed", AnimatorConditionMode.Greater, 3.9f, 0.2f);
                Link(run, walk, "Speed", AnimatorConditionMode.Less, 3.7f, 0.25f);
            }
            return controller;
        }

        private static void Link(AnimatorState from, AnimatorState to, string param, AnimatorConditionMode mode, float threshold, float duration)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = duration;
            t.AddCondition(mode, threshold, param);
        }

        // ---------------- material ----------------

        private static Texture2D FindTexture(string dir, string name)
        {
            var candidates = AssetDatabase.FindAssets("t:Texture2D", new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Raw"))
                .ToList();
            string best = candidates.FirstOrDefault(p => p.Contains("base_color") || p.Contains("BaseColor") || p.Contains("albedo") || p.Contains("diffuse"))
                          ?? candidates.FirstOrDefault(p => !p.ToLowerInvariant().Contains("normal") && !p.ToLowerInvariant().Contains("rough") && !p.ToLowerInvariant().Contains("metal"));
            return best != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(best) : null;
        }

        private static Material CharacterMaterial(string name, Texture2D texture)
        {
            string path = $"{GenUtil.MaterialDir}/M_{name}.mat";
            var shader = Shader.Find("NASCON/StylizedLit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_GritTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGenerator.StoneGritPath));
            mat.SetFloat("_GritStrength", 0.12f);
            mat.SetFloat("_GritScale", 2.5f);
            mat.SetFloat("_BandNoise", 0.06f);
            mat.SetFloat("_Bands", 3f);
            mat.SetFloat("_RimStrength", 0.3f);
            mat.SetFloat("_AmbientStrength", 1f);
            mat.SetColor("_ShadowTint", MaterialLibrary.ShadowTint);
            mat.SetColor("_MidtoneTint", MaterialLibrary.MidtoneTint);
            mat.SetFloat("_MidtoneStrength", 0.35f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
