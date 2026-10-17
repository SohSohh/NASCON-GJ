using System.Collections.Generic;
using System.IO;
using NasconGJ.Core;
using NasconGJ.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NasconGJ.EditorTools.Generation
{
    public static class CrystalAssetGenerator
    {
        public const string CrystalMeshPath = GenUtil.PropMeshDir + "/SM_Crystal.asset";
        public const string CrystalMaterialPath = GenUtil.MaterialDir + "/M_Crystal.mat";
        public const string CrystalPrefabPath = GenUtil.ItemPrefabDir + "/PF_Crystal.prefab";
        public const string L05ScenePath = "Assets/_Project/Scenes/Levels/L05_Crystal.unity";

        [MenuItem("NASCON/Generate/Crystal Asset & Setup L05", priority = 315)]
        public static void GenerateAllAndSetupScene()
        {
            var mesh = GenerateCrystalMesh();
            var mat = GenerateCrystalMaterial();
            var prefab = GenerateCrystalPrefab(mesh, mat);
            SetupCrystalInL05Scene(prefab);
            Debug.Log("[CrystalAssetGenerator] Crystal asset generated and placed in L05_Crystal successfully!");
        }

        public static Mesh GenerateCrystalMesh()
        {
            var mesh = new Mesh { name = "SM_Crystal" };

            // Generate a multi-faceted 6-sided double-cone crystal with an equatorial facet band
            int sides = 6;
            float topY = 0.9f;
            float upperY = 0.45f;
            float lowerY = -0.45f;
            float bottomY = -0.9f;
            float rUpper = 0.32f;
            float rLower = 0.30f;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            Vector3 top = new Vector3(0, topY, 0);
            Vector3 bottom = new Vector3(0, bottomY, 0);

            Vector3[] upperRing = new Vector3[sides];
            Vector3[] lowerRing = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                upperRing[i] = new Vector3(Mathf.Cos(angle) * rUpper, upperY, Mathf.Sin(angle) * rUpper);
                // slightly twisted lower ring for dynamic facet cuts
                float angleLower = angle + (Mathf.PI / sides * 0.4f);
                lowerRing[i] = new Vector3(Mathf.Cos(angleLower) * rLower, lowerY, Mathf.Sin(angleLower) * rLower);
            }

            void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int idx = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                triangles.Add(idx);
                triangles.Add(idx + 1);
                triangles.Add(idx + 2);
            }

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                AddTriangle(a, b, c);
                AddTriangle(a, c, d);
            }

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;

                // Top tip cap faces
                AddTriangle(top, upperRing[i], upperRing[next]);

                // Equatorial middle facets
                AddQuad(upperRing[i], lowerRing[i], lowerRing[next], upperRing[next]);

                // Bottom tip cap faces
                AddTriangle(bottom, lowerRing[next], lowerRing[i]);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return GenUtil.SaveAsset(mesh, CrystalMeshPath);
        }

        public static Material GenerateCrystalMaterial()
        {
            GenUtil.EnsureFolder(Path.GetDirectoryName(CrystalMaterialPath));
            Shader shader = Shader.Find("NasconGJ/StylizedLit");
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Art/Shaders/StylizedLit.shader");
            }
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            var mat = new Material(shader) { name = "M_Crystal" };

            // Luminous mythical cyan crystal
            Color baseColor = new Color(0.28f, 0.85f, 1f, 1f);
            Color emissionColor = new Color(0.35f, 0.92f, 1.3f, 1f) * 1.6f;
            Color rimColor = new Color(0.6f, 1f, 1f, 1f);

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", rimColor);
            if (mat.HasProperty("_RimStrength")) mat.SetFloat("_RimStrength", 0.9f);
            if (mat.HasProperty("_Roughness")) mat.SetFloat("_Roughness", 0.15f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.2f);
            if (mat.HasProperty("_GBCameraBypass")) mat.SetFloat("_GBCameraBypass", 1f);
            if (mat.HasProperty("_GBCameraStencilMask")) mat.SetFloat("_GBCameraStencilMask", 192f);

            return GenUtil.SaveAsset(mat, CrystalMaterialPath);
        }

        public static GameObject GenerateCrystalPrefab(Mesh mesh, Material mat)
        {
            var root = new GameObject("PF_Crystal");
            root.tag = GameTags.Interactable;
            int interactableLayer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (interactableLayer >= 0) root.layer = interactableLayer;

            // Trigger collider for player interaction raycast
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0f, 0f);
            col.size = new Vector3(0.8f, 1.3f, 0.8f);

            // Model container child - this is the easily replaceable part
            var modelGo = new GameObject("Model");
            modelGo.transform.SetParent(root.transform, false);
            modelGo.transform.localScale = new Vector3(0.55f, 0.75f, 0.55f);
            if (interactableLayer >= 0) modelGo.layer = interactableLayer;

            var mf = modelGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = modelGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            // Point light child
            var lightGo = new GameObject("GlowLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.35f, 0.88f, 1f);
            light.range = 5f;
            light.intensity = 2.6f;

            // Crystal pickup script
            var pickup = root.AddComponent<CrystalPickup>();

            // Assign references via SerializedObject
            var so = new SerializedObject(pickup);
            so.FindProperty("prompt").stringValue = "pick up crystal";
            so.FindProperty("nextSceneOverride").stringValue = ""; // defaults to SceneNames.StoryOrder (L06_Escape)
            so.FindProperty("modelRoot").objectReferenceValue = modelGo.transform;
            so.FindProperty("glowLight").objectReferenceValue = light;

            var soundClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Generated/SFX_GuideChime.wav");
            if (soundClip != null)
            {
                so.FindProperty("pickupSound").objectReferenceValue = soundClip;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return GenUtil.SavePrefab(root, CrystalPrefabPath);
        }

        public static void SetupCrystalInL05Scene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(L05ScenePath, OpenSceneMode.Single);

            // Check if crystal already placed
            var existing = GameObject.Find("PF_Crystal");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            // Find parent group: "--- GAMEPLAY ---/Items" or root
            Transform parentTransform = null;
            var gameplayRoot = GameObject.Find("--- GAMEPLAY ---");
            if (gameplayRoot != null)
            {
                var itemsTr = gameplayRoot.transform.Find("Items");
                if (itemsTr != null) parentTransform = itemsTr;
                else parentTransform = gameplayRoot.transform;
            }

            // Instantiate prefab over the altar (Altar is at (0, 0, 6.5), height is ~1.14)
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parentTransform != null) instance.transform.SetParent(parentTransform, true);
            instance.transform.position = new Vector3(0f, 1.45f, 6.5f);
            instance.transform.rotation = Quaternion.identity;

            // Configure LevelExit so player doesn't skip the crystal
            var exitGo = GameObject.Find("LevelExit");
            if (exitGo != null)
            {
                var levelExit = exitGo.GetComponent<NasconGJ.Puzzles.LevelExit>();
                if (levelExit != null)
                {
                    var soExit = new SerializedObject(levelExit);
                    soExit.FindProperty("requiredPuzzleId").stringValue = "CrystalAcquired";
                    soExit.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
