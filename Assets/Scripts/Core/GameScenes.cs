using System;

// Single source of truth for scene names and level ordering. Everything that
// loads a scene goes through here so a rename never leaves a dangling string.
public static class GameScenes
{
    public const string Splash = "SplashScene";
    public const string MainMenu = "MainMenu";
    public const string Loading = "LoadingScene";
    public const string Victory = "VictoryScene";

    public const string Scene1 = "Scene1";
    public const string Scene2 = "Scene2";
    public const string Scene3 = "Scene3";
    public const string Scene4 = "Scene4";
    public const string Scene5 = "Scene5";

    // Index 0 is the first scene the app boots into, and the order here is the
    // order written into Build Settings by the editor setup tool.
    public static readonly string[] BuildOrder =
    {
        Splash, MainMenu, Loading, Scene1, Scene2, Scene3, Scene4, Scene5, Victory
    };

    public static readonly string[] Levels = { Scene1, Scene2, Scene3, Scene4, Scene5 };

    public static bool IsGameplayScene(string sceneName)
    {
        return LevelNumberOf(sceneName) > 0;
    }

    // 1-based level number, or 0 when the scene is not a gameplay level.
    public static int LevelNumberOf(string sceneName)
    {
        for (int i = 0; i < Levels.Length; i++)
        {
            if (string.Equals(Levels[i], sceneName, StringComparison.Ordinal)) { return i + 1; }
        }

        return 0;
    }

    public static string LevelScene(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > Levels.Length) { return Scene1; }

        return Levels[levelNumber - 1];
    }
}
