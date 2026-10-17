using TMPro;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// TMP font assets for the game's typography (both SIL OFL, see Art/Fonts/OFL-*.txt):
    ///   Cinzel: carved Roman capitals for titles, menu items and key caps.
    ///   EB Garamond (+ italic): an old-book serif for dialogue, hints and body text.
    /// Also creates a glowing "ember" material preset for the main-menu title.
    /// </summary>
    public static class FontLibrary
    {
        private const string FontDir = GenUtil.Root + "/Art/Fonts";

        public static TMP_FontAsset Title => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Cinzel SDF.asset");
        public static TMP_FontAsset Body => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/EBGaramond SDF.asset");
        public static TMP_FontAsset Italic => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/EBGaramond-Italic SDF.asset");
        public static Material TitleGlow => AssetDatabase.LoadAssetAtPath<Material>($"{FontDir}/Cinzel SDF Glow.mat");

        public static void GenerateAll()
        {
            Create("Cinzel");
            Create("EBGaramond");
            Create("EBGaramond-Italic");
            CreateGlowPreset();
            ApplyTextOutline(Title.material);
            ApplyTextOutline(Body.material);
            ApplyTextOutline(Italic.material);
            ApplyTextOutline(TitleGlow);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyTextOutline(Material material)
        {
            if (material == null) return;
            material.SetFloat("_OutlineWidth", 0.15f);
            material.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0.9f));
            EditorUtility.SetDirty(material);
        }

        private static void Create(string fileName)
        {
            string path = $"{FontDir}/{fileName} SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) return;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{fileName}.ttf");
            if (font == null)
            {
                Debug.LogError($"[Gen] Missing font {fileName}.ttf");
                return;
            }
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            asset.name = $"{fileName} SDF";
            AssetDatabase.CreateAsset(asset, path);
            asset.material.name = $"{fileName} SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.atlasTextures[0].name = $"{fileName} SDF Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            // pre-bake the common glyphs so the atlas is ready before play
            asset.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·—–…’“”");
            EditorUtility.SetDirty(asset);
        }

        private static void CreateGlowPreset()
        {
            var title = Title;
            if (title == null) return;
            string path = $"{FontDir}/Cinzel SDF Glow.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(title.material) { name = "Cinzel SDF Glow" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = title.material.shader;
            mat.SetTexture("_MainTex", title.atlasTexture);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(1f, 0.42f, 0.1f, 0.55f));
            mat.SetFloat("_UnderlayDilate", 0.45f);
            mat.SetFloat("_UnderlaySoftness", 0.85f);
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", 0f);
            mat.SetFloat("_FaceDilate", 0.05f);
            EditorUtility.SetDirty(mat);
        }
    }
}
