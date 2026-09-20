using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Takes the pictures used in the report: the game over screen, the win message
// and the screens their buttons lead to. Run it with the editor's own window
// open (not -batchmode), because it captures what the Game view draws.
public static class ScreenshotCapture
{
    private const string Folder = "Docs/Screenshots";

    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Written = new List<string>();

    private static int step;
    private static float waitUntil;
    private static int frames;

    [MenuItem("Tools/Soulbound Gate/Debug/Capture Report Screenshots")]
    public static void Run()
    {
        Directory.CreateDirectory(Folder);

        EditorSceneManager.OpenScene("Assets/Scenes/" + GameScenes.Scene1 + ".unity", OpenSceneMode.Single);
        PlayModeTestSettings.ApplyForTest();

        // Something to show in the run summary on the game over screen.
        ProfileStats.ClearAll();
        ProfileStats.RecordLevelCleared(1, 64f);
        ProfileStats.RecordLevelCleared(2, 131f);
        ProfileStats.RecordVictory(455f, 120);

        Report.Clear();
        Written.Clear();
        step = 0;
        frames = 0;
        waitUntil = 0f;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { return; }

        frames++;
        if (Time.time < waitUntil) { return; }

        switch (step)
        {
            case 0:
                if (frames < 90 || Time.time < 2f) { return; }
                step++;
                break;

            // Both states are reached the way a player reaches them, so the
            // pictures show what the game really does - touch controls hidden
            // included - rather than a panel switched on by hand.
            case 1: Die(); step++; Wait(2.6f); break;
            case 2: Capture("game-over.png"); step++; Wait(1.5f); break;

            case 3: ShowWin(); step++; Wait(1.2f); break;
            case 4: Capture("win-overlay.png"); step++; Wait(4f); break;

            case 5: EnsureVictoryScene(); step++; Wait(1.5f); break;
            case 6: Capture("victory.png"); step++; Wait(1.5f); break;

            case 7: Go(GameScenes.Progress); step++; Wait(1.5f); break;
            case 8: Capture("progress.png"); step++; Wait(1.5f); break;

            case 9: Go(GameScenes.Achievements); step++; Wait(1.5f); break;
            case 10: Capture("achievements.png"); step++; Wait(1.5f); break;

            case 11: Go(GameScenes.Settings); step++; Wait(1.5f); break;
            case 12: Capture("settings.png"); step++; Wait(1.5f); break;

            default: Finish(); break;
        }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    private static void Go(string sceneName)
    {
        if (sceneName == GameScenes.Victory)
        {
            SceneFlow.GoToVictory();
            return;
        }

        SceneFlow.GoToScreen(sceneName, GameScenes.MainMenu);
    }

    private static void Die()
    {
        PlayerHealth health = PlayerHealth.Instance;
        if (health != null) { health.TakeDamage(99, health.transform); }
    }

    // The real victory sequence: it plays the fanfare, shows the overlay and
    // carries the game to the victory screen on its own.
    private static void ShowWin()
    {
        GameplayRuntime runtime = GameplayRuntime.Instance;
        if (runtime != null && runtime.GameOver != null) { runtime.GameOver.Hide(); }

        VictorySequence.Begin();
    }

    private static void EnsureVictoryScene()
    {
        if (SceneManager.GetActiveScene().name != GameScenes.Victory) { SceneFlow.GoToVictory(); }
    }

    private static void Capture(string fileName)
    {
        string path = Folder + "/" + fileName;

        if (File.Exists(path)) { File.Delete(path); }

        ScreenCapture.CaptureScreenshot(path);
        Written.Add(path);

        Report.AppendLine("  captured " + path + " from scene " + SceneManager.GetActiveScene().name);
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;

        for (int i = 0; i < Written.Count; i++)
        {
            long size = File.Exists(Written[i]) ? new FileInfo(Written[i]).Length : 0L;
            Report.AppendLine("  " + Written[i] + ": " + (size > 0 ? size + " bytes" : "MISSING"));
        }

        Debug.Log("[SCREENSHOTS]\n" + Report);

        EditorApplication.ExitPlaymode();
        EditorApplication.update += QuitWhenStopped;
    }

    private static void QuitWhenStopped()
    {
        if (EditorApplication.isPlaying) { return; }

        EditorApplication.update -= QuitWhenStopped;

        ProfileStats.ClearAll();
        PlayModeTestSettings.Restore();

        EditorApplication.Exit(0);
    }
}
