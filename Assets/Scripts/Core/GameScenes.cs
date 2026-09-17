namespace ArtifactCourier.Core
{
    public static class GameScenes
    {
        public const string MainMenu = "MainMenu";

        public static readonly string[] Levels =
        {
            "Level_01_NewYork",
            "Level_02_Tokyo",
            "Level_03_Beijing",
            "Level_04_Paris",
            "Level_05_BuenosAires",
            "Level_06_Moscow",
            "Level_07_Cairo"
        };

        public static bool IsValidLevelIndex(int index) => index >= 0 && index < Levels.Length;
    }
}
