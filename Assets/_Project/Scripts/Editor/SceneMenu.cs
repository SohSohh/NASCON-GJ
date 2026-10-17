using UnityEditor;
using UnityEditor.SceneManagement;

namespace NasconGJ.EditorTools
{
    /// <summary>One-click scene switching: NASCON > Scenes > ...</summary>
    public static class SceneMenu
    {
        private const string Root = "Assets/_Project/Scenes/";

        [MenuItem("NASCON/Scenes/Boot", priority = 100)] private static void Boot() => Open("Core/Boot");
        [MenuItem("NASCON/Scenes/Main Menu", priority = 101)] private static void MainMenu() => Open("Core/MainMenu");

        [MenuItem("NASCON/Scenes/C01 Prologue", priority = 120)] private static void Prologue() => Open("Cutscenes/C01_Prologue");
        [MenuItem("NASCON/Scenes/C02 Ending", priority = 121)] private static void Ending() => Open("Cutscenes/C02_Ending");

        [MenuItem("NASCON/Scenes/L01 Tomb Entrance", priority = 140)] private static void L01() => Open("Levels/L01_TombEntrance");
        [MenuItem("NASCON/Scenes/L02 Narrow Path", priority = 141)] private static void L02() => Open("Levels/L02_NarrowPath");
        [MenuItem("NASCON/Scenes/L03 Multi Story", priority = 142)] private static void L03() => Open("Levels/L03_MultiStory");
        [MenuItem("NASCON/Scenes/L04 Dummy Shadows", priority = 143)] private static void L04() => Open("Levels/L04_DummyShadows");
        [MenuItem("NASCON/Scenes/L05 Crystal", priority = 144)] private static void L05() => Open("Levels/L05_Crystal");
        [MenuItem("NASCON/Scenes/L06 Escape", priority = 145)] private static void L06() => Open("Levels/L06_Escape");

        [MenuItem("NASCON/Scenes/Sandbox Dev A", priority = 160)] private static void SbA() => Open("Sandbox/Sandbox_DevA");
        [MenuItem("NASCON/Scenes/Sandbox Dev B", priority = 161)] private static void SbB() => Open("Sandbox/Sandbox_DevB");
        [MenuItem("NASCON/Scenes/Sandbox Dev C", priority = 162)] private static void SbC() => Open("Sandbox/Sandbox_DevC");

        private static void Open(string relativePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Root + relativePath + ".unity");
        }
    }
}
