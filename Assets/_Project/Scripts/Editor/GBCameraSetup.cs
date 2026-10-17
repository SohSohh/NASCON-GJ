using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NasconGJ.EditorTools
{
    /// <summary>Installs the imported GBCamera palette on each renderer used by project quality levels.</summary>
    public static class GBCameraSetup
    {
        public const string MaterialPath = "Assets/_Project/Art/Materials/M_GBCamera.mat";

        [MenuItem("NASCON/Rendering/Set Up GBCamera")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play mode before configuring renderer assets.");
            var shader = Shader.Find("NASCON/GBCameraPost");
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/RogueNoodle/GBCamera/Materials/GBRenderMaterial.mat");
            if (shader == null || original == null) throw new System.InvalidOperationException("GBCamera shader or imported material is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_GBCamera" };
                material.SetTexture("_Palette", original.GetTexture("_Palette"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var renderers = new HashSet<UniversalRendererData>();
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" }))
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(pipeline);
                var list = serialized.FindProperty("m_RendererDataList");
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue is UniversalRendererData data) renderers.Add(data);
            }
            foreach (var data in renderers)
            {
                FullScreenPassRendererFeature feature = null;
                foreach (var candidate in data.rendererFeatures)
                    if (candidate is FullScreenPassRendererFeature full && candidate.name == "GBCamera") feature = full;
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                    feature.name = "GBCamera";
                    AssetDatabase.AddObjectToAsset(feature, data);
                    data.rendererFeatures.Add(feature);
                }
                feature.passMaterial = material;
                feature.hideFlags |= HideFlags.HideInHierarchy;
                feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
                feature.fetchColorBuffer = true;
                feature.bindDepthStencilAttachment = true;
                feature.requirements = ScriptableRenderPassInput.None;
                feature.SetActive(true);
                feature.Create();
                // Keep URP's recovery map in sync so subasset references survive reimports.
                var rendererObject = new SerializedObject(data);
                var featureMap = rendererObject.FindProperty("m_RendererFeatureMap");
                featureMap.arraySize = data.rendererFeatures.Count;
                for (int i = 0; i < data.rendererFeatures.Count; i++)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[i], out string guid, out long id);
                    featureMap.GetArrayElementAtIndex(i).longValue = id;
                }
                rendererObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(data);
                data.SetDirty();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"GBCamera configured on {renderers.Count} URP renderers. Tune {MaterialPath}.");
        }
    }
}
