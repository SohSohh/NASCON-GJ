using System.IO;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>Paths + save helpers shared by all generators. Re-saving keeps asset GUIDs so references survive regeneration.</summary>
    public static class GenUtil
    {
        public const string Root = "Assets/_Project";
        public const string EnvMeshDir = Root + "/Art/Models/Environment/Generated";
        public const string PropMeshDir = Root + "/Art/Models/Props/Generated";
        public const string TextureDir = Root + "/Art/Textures/Generated";
        public const string MaterialDir = Root + "/Art/Materials/Generated";
        public const string VolumeDir = Root + "/Settings/Volumes";
        public const string KitPrefabDir = Root + "/Prefabs/Environment/Kit";
        public const string ItemPrefabDir = Root + "/Prefabs/Items";
        public const string LightingPrefabDir = Root + "/Prefabs/Lighting";
        public const string ShadowPrefabDir = Root + "/Prefabs/Shadows";
        public const string GuidePrefabDir = Root + "/Prefabs/GuidingLight";
        public const string PlayerPrefabDir = Root + "/Prefabs/Player";
        public const string UIPrefabDir = Root + "/Prefabs/UI";
        public const string DialogueDir = Root + "/ScriptableObjects/Dialogue";
        public const string InputAsset = Root + "/Input/InputSystem_Actions.inputactions";

        public static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
        }

        public static T SaveAsset<T>(T asset, string path) where T : Object
        {
            EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            if (existing is Mesh existingMesh) existingMesh.Clear();
            EditorUtility.CopySerialized(asset, existing);
            existing.name = Path.GetFileNameWithoutExtension(path);
            Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        public static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) Debug.LogError($"[Gen] Missing prefab {path}. Run the earlier generation steps first.");
            return prefab;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        public static GameObject Child(string name, Transform parent, Vector3 localPos = default, Vector3 localEuler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = localEuler;
            return go;
        }

        // ---------- SerializedObject helpers (lets us fill private [SerializeField]s) ----------

        public static void Set(Object target, string property, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogError($"[Gen] {target.GetType().Name} has no serialized field '{property}'");
                return;
            }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector2 v2: p.vector2Value = v2; break;
                case Vector3 v3: p.vector3Value = v3; break;
                case Color c: p.colorValue = c; break;
                case Gradient g: p.gradientValue = g; break;
                case null: p.objectReferenceValue = null; break;
                default: Debug.LogError($"[Gen] Unsupported value type {value.GetType()}"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetStrings(Object target, string property, string[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColors(Object target, string property, Color[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).colorValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
