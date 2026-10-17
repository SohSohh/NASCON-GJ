using NasconGJ.Guiding;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>Guiding-light dialogue. Edit the generated assets directly afterwards; regenerating overwrites them.</summary>
    public static class DialogueLibrary
    {
        public const string L01Intro = "SO_Dialogue_L01_Intro";
        public const string L01Shadows = "SO_Dialogue_L01_Shadows";
        public const string L01Onward = "SO_Dialogue_L01_Onward";
        public const string L01Draft = "SO_Dialogue_L01_Draft";
        public const string L01Stick = "SO_Dialogue_L01_Stick";
        public const string Sandbox = "SO_Dialogue_Sandbox";

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(GenUtil.DialogueDir);

            // Keep it to one or two short lines per stop: the level teaches, the light only hints.
            Make(L01Intro, "l01_intro",
                "You fell a long way.",
                "Your torch is still burning. Take it.",
                "Then follow me.");

            Make(L01Stick, "l01_stick",
                "Your torch is dying.",
                "Pass the flame to that stick.");

            Make(L01Draft, "l01_draft",
                "Feel the wind?",
                "Cover the flame, or lose it.");

            Make(L01Shadows, "l01_shadows",
                "These were the townsfolk.",
                "They can't touch the light. Stay in it.");

            Make(L01Onward, "l01_onward",
                "Deeper. I'll be waiting.");

            Make(Sandbox, "sandbox",
                "Sandbox guiding light. <color=#FFB060>[F]</color> or click to advance.",
                "Try: pass fire to the stick, drop it, cover your flame near the shadow on the north wall.",
                "Let your flame die to test the darkness seeker.");

            AssetDatabase.SaveAssets();
        }

        public static DialogueData Load(string name) => AssetDatabase.LoadAssetAtPath<DialogueData>($"{GenUtil.DialogueDir}/{name}.asset");

        private static void Make(string assetName, string id, params string[] lines)
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.id = id;
            data.lines = lines;
            data.speaker = "THE GUIDING LIGHT";
            GenUtil.SaveAsset(data, $"{GenUtil.DialogueDir}/{assetName}.asset");
        }
    }
}
