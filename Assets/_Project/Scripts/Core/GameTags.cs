namespace NasconGJ.Core
{
    /// <summary>Tag and layer names registered in ProjectSettings/TagManager. Use these, not string literals.</summary>
    public static class GameTags
    {
        public const string Player = "Player";
        public const string Interactable = "Interactable";
        public const string Flammable = "Flammable";
        public const string LightSource = "LightSource";
        public const string GuidingLight = "GuidingLight";
        public const string ShadowEnemy = "ShadowEnemy";
        public const string Checkpoint = "Checkpoint";
    }

    public static class GameLayers
    {
        public const string Player = "Player";
        public const string Interactable = "Interactable";
        public const string ShadowSurface = "ShadowSurface";   // walls/floors shadows can live on
        public const string ShadowEnemy = "ShadowEnemy";
        public const string LightBlocker = "LightBlocker";     // props whose cast shadows block enemies
        public const string Ground = "Ground";
    }
}
