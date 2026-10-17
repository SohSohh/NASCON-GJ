using UnityEngine;

namespace NasconGJ.Entities
{
    /// <summary>Frame layout of the generated shadow sprite sheets (see Editor/Generation/ShadowSheetGenerator).</summary>
    public static class ShadowSheet
    {
        public const int FrameWidth = 320;
        public const int FrameHeight = 416;
        public const float Aspect = FrameWidth / (float)FrameHeight;

        // Creature sheet: 4 x 3
        public const int Cols = 4;
        public const int Rows = 3;
        public const int Idle = 0;      // 4 frames
        public const int Stalk = 4;     // 4 frames
        public const int Recoil = 8;    // 2 frames
        public const int Attack = 10;   // 2 frames (windup, lunge)

        // Townsfolk sheet: 3 x 1
        public const int TownsfolkCount = 3;

        public static Vector4 CreatureFrame(int index) => Frame(index, Cols, Rows);
        public static Vector4 TownsfolkFrame(int index) => Frame(Mathf.Clamp(index, 0, TownsfolkCount - 1), TownsfolkCount, 1);

        private static Vector4 Frame(int index, int cols, int rows)
        {
            int c = index % cols;
            int r = index / cols;
            return new Vector4(c / (float)cols, 1f - (r + 1) / (float)rows, 1f / cols, 1f / rows);
        }
    }
}
