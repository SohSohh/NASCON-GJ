using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// NASCON > Generate. Steps run in order; later steps load assets from earlier ones.
    /// Re-running steps 1–3 updates assets in place (GUIDs kept, so scene references survive).
    /// Step 4 OVERWRITES the generated scenes.
    /// </summary>
    public static class GenerationMenu
    {
        [MenuItem("NASCON/Generate/1. Textures, Audio, Fonts + Materials", priority = 300)]
        public static void TexturesAndMaterials()
        {
            TextureGenerator.GenerateAll();
            InkArtGenerator.GenerateAll();
            AudioGenerator.GenerateAll();
            FontLibrary.GenerateAll();
            MaterialLibrary.GenerateAll();
            Debug.Log("[Gen] Textures, shadow sheets, audio, fonts + materials done.");
        }

        [MenuItem("NASCON/Generate/2. Environment Kit", priority = 301)]
        public static void EnvironmentKit()
        {
            EnvironmentKitGenerator.GenerateAll();
            Debug.Log("[Gen] Environment kit done.");
        }

        [MenuItem("NASCON/Generate/3. Gameplay + UI Prefabs", priority = 302)]
        public static void GameplayPrefabs()
        {
            GameplayPrefabGenerator.GenerateAll();
            UIPrefabGenerator.GenerateAll();
            Debug.Log("[Gen] Gameplay + UI prefabs done.");
        }

        [MenuItem("NASCON/Generate/4. Rebuild Scenes (overwrites!)", priority = 320)]
        public static void RebuildScenesWithPrompt()
        {
            if (!EditorUtility.DisplayDialog("Rebuild generated scenes",
                    "This OVERWRITES MainMenu, L01–L06 and all Sandbox scenes with freshly generated layouts.\n\nHand edits in those scenes will be lost.",
                    "Rebuild", "Cancel"))
                return;
            RebuildScenes();
        }

        [MenuItem("NASCON/Generate/Regenerate All Assets (1–3)", priority = 340)]
        public static void AllAssets()
        {
            TexturesAndMaterials();
            EnvironmentKit();
            GameplayPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RebuildScenes()
        {
            LevelBuilder.BuildAll();
            Debug.Log("[Gen] Scenes rebuilt.");
        }
    }
}
