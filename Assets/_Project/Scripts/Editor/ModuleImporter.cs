using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools
{
    /// <summary>
    /// Mirrors loose scripts from the repo-root MODULES/ inbox into the Unity project so they compile.
    ///   MODULES/&lt;ModuleName&gt;/**  ->  Assets/_Project/Scripts/Modules/&lt;ModuleName&gt;/**
    /// Each import replaces the mirrored folder entirely, so MODULES/ stays the source of truth
    /// until a module is promoted into its real Scripts/ folder (see MODULES/README.md).
    /// </summary>
    public static class ModuleImporter
    {
        private const string TargetRoot = "Assets/_Project/Scripts/Modules";
        private static readonly string[] SkippedExtensions = { ".md", ".meta", ".txt" };

        private static string ModulesRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "MODULES"));

        [MenuItem("NASCON/Modules/Import All Modules", priority = 0)]
        public static void ImportAll()
        {
            if (!Directory.Exists(ModulesRoot))
            {
                Debug.LogError($"[Modules] Folder not found: {ModulesRoot}");
                return;
            }

            var modules = Directory.GetDirectories(ModulesRoot)
                .Where(d => !IsIgnored(Path.GetFileName(d)))
                .ToArray();

            int files = 0;
            foreach (var module in modules) files += ImportModule(module);

            AssetDatabase.Refresh();
            Debug.Log($"[Modules] Imported {modules.Length} module(s), {files} file(s) into {TargetRoot}. " +
                      "Check the Console for compile errors.");
        }

        [MenuItem("NASCON/Modules/Import Single Module...", priority = 1)]
        public static void ImportSingle()
        {
            string picked = EditorUtility.OpenFolderPanel("Pick a module folder", ModulesRoot, "");
            if (string.IsNullOrEmpty(picked)) return;

            int files = ImportModule(picked);
            AssetDatabase.Refresh();
            Debug.Log($"[Modules] Imported '{Path.GetFileName(picked)}' ({files} file(s)).");
        }

        [MenuItem("NASCON/Modules/Clear Imported Modules", priority = 20)]
        public static void ClearImported()
        {
            if (!EditorUtility.DisplayDialog("Clear imported modules",
                    $"Delete every folder under {TargetRoot}? The MODULES/ inbox is not touched.", "Delete", "Cancel"))
                return;

            foreach (var dir in Directory.GetDirectories(TargetRoot))
                AssetDatabase.DeleteAsset(ToAssetPath(dir));
            AssetDatabase.Refresh();
        }

        [MenuItem("NASCON/Modules/Open MODULES Folder", priority = 40)]
        public static void OpenFolder() => EditorUtility.RevealInFinder(ModulesRoot);

        private static int ImportModule(string sourceDir)
        {
            string name = Path.GetFileName(sourceDir.TrimEnd('/', '\\'));
            string targetDir = Path.Combine(TargetRoot, name).Replace('\\', '/');

            if (AssetDatabase.IsValidFolder(targetDir)) AssetDatabase.DeleteAsset(targetDir);

            int count = 0;
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                if (SkippedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant())) continue;
                if (Path.GetFileName(file).StartsWith(".")) continue;

                string relative = Path.GetRelativePath(sourceDir, file);
                string dest = Path.Combine(targetDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(file, dest, true);
                count++;
            }
            return count;
        }

        private static bool IsIgnored(string folderName) =>
            folderName.StartsWith("_") || folderName.StartsWith(".");

        private static string ToAssetPath(string fullPath)
        {
            string normalized = fullPath.Replace('\\', '/');
            int i = normalized.IndexOf("Assets/");
            return i >= 0 ? normalized.Substring(i) : normalized;
        }
    }
}
