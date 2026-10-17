namespace NasconGJ.Core
{
    /// <summary>
    /// Single source of truth for scene names. Must match the files in Assets/_Project/Scenes
    /// and the order in Build Settings. Never hardcode scene strings elsewhere.
    /// </summary>
    public static class SceneNames
    {
        // Core
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";

        // Cutscenes
        public const string Prologue = "C01_Prologue";
        public const string Ending = "C02_Ending";

        // Levels (played in this order)
        public const string TombEntrance = "L01_TombEntrance";
        public const string NarrowPath = "L02_NarrowPath";
        public const string MultiStory = "L03_MultiStory";
        public const string DummyShadows = "L04_DummyShadows";
        public const string Crystal = "L05_Crystal";
        public const string Escape = "L06_Escape";

        /// <summary>Linear story order, used by SceneLoader.LoadNext().</summary>
        public static readonly string[] StoryOrder =
        {
            MainMenu,
            Prologue,
            TombEntrance,
            NarrowPath,
            MultiStory,
            DummyShadows,
            Crystal,
            Escape,
            Ending,
        };
    }
}
