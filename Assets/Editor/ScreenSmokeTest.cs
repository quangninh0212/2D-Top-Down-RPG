using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Walks the three stand-alone screens the game over and victory screens lead
// to, in play mode, one after another: progress, achievements, settings, and
// finally the way back out. Each is a real scene load, which is the part a
// panel laid over the old screen would not prove.
public static class ScreenSmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private static int step;
    private static float waitUntil;
    private static int frames;

    [MenuItem("Tools/Soulbound Gate/Debug/Screens Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + GameScenes.Progress + ".unity", OpenSceneMode.Single);
        PlayModeTestSettings.ApplyForTest();

        // A finished run, so the screens have something to show.
        ProfileStats.ClearAll();
        ProfileStats.RecordLevelCleared(1, 64f);
        ProfileStats.RecordLevelCleared(2, 130f);
        ProfileStats.RecordDeath(3, 42, 210f);

        Report.Clear();
        Failures.Clear();
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
                if (frames < 30) { return; }
                step++;
                break;

            case 1: CheckProgress(); step++; break;
            case 2: Go(GameScenes.Achievements); step++; Wait(0.5f); break;
            case 3: CheckAchievements(); step++; break;
            case 4: Go(GameScenes.Settings); step++; Wait(0.5f); break;
            case 5: CheckSettings(); step++; break;
            case 6: PressBack(); step++; Wait(0.6f); break;
            case 7: CheckReturned(); step++; break;

            default: Finish(); break;
        }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    private static void Go(string sceneName)
    {
        SceneFlow.GoToScreen(sceneName, GameScenes.MainMenu);
    }

    // ----- the screens ----------------------------------------------------

    private static void CheckProgress()
    {
        Expect(HasText("TIẾN TRÌNH"), "the progress screen names itself");

        int levelRows = 0;
        for (int level = 1; level <= LevelCatalog.Count; level++)
        {
            if (Find("Level" + level + "Value") != null) { levelRows++; }
        }

        Report.AppendLine("  level rows: " + levelRows + " / " + LevelCatalog.Count);
        Expect(levelRows == LevelCatalog.Count, "it lists every level");

        Text cleared = TextOf("Level1Value");
        Report.AppendLine("  level 1 row reads: " + (cleared != null ? cleared.text : "missing"));
        Expect(cleared != null && cleared.text.Contains("HOÀN THÀNH"), "a cleared level shows as finished");

        Text locked = TextOf("Level5Value");
        Expect(locked != null && !locked.text.Contains("HOÀN THÀNH"), "an unplayed level does not");

        Expect(TextOf("DeathsValue") != null && TextOf("DeathsValue").text == "1",
               "the lost run is counted on the screen");

        Expect(Find("Back") != null, "it has a way back");
        Expect(Find("Achievements") != null, "and a way across to the records");
    }

    private static void CheckAchievements()
    {
        Report.AppendLine("  active scene: " + SceneManager.GetActiveScene().name);
        Expect(SceneManager.GetActiveScene().name == GameScenes.Achievements, "the records open as a scene of their own");

        Expect(HasText("THÀNH TÍCH"), "the achievements screen names itself");
        Expect(TextOf("BestTimeValue") != null, "it shows the best time");

        int milestones = 0;
        for (int i = 0; i < AchievementsScreenController.TotalCount; i++)
        {
            if (Find("Milestone" + i + "Value") != null) { milestones++; }
        }

        Report.AppendLine("  milestone rows: " + milestones + " / " + AchievementsScreenController.TotalCount);
        Expect(milestones == AchievementsScreenController.TotalCount, "it lists every milestone");

        Text first = TextOf("Milestone0Label");
        Report.AppendLine("  first milestone reads: " + (first != null ? first.text : "missing"));
        Expect(first != null && first.text.Contains("[x]"), "a milestone that has been earned is ticked");
    }

    private static void CheckSettings()
    {
        Expect(SceneManager.GetActiveScene().name == GameScenes.Settings, "settings open as a scene of their own");

        Expect(HasText("CÀI ĐẶT"), "the settings screen names itself");
        Expect(Find("Slider") != null, "the volume sliders are there");
        Expect(Find("SfxSwitch") != null && Find("MusicSwitch") != null, "so are the sound and music switches");
        Expect(Find("Close") != null, "and the button that closes it");
    }

    private static void PressBack()
    {
        // Driven through the button itself rather than the method behind it,
        // so a button that was never wired up would fail here.
        GameObject close = Find("Close");
        Button button = close != null ? close.GetComponent<Button>() : null;

        if (button == null)
        {
            Expect(false, "the settings screen has a working close button");
            return;
        }

        button.onClick.Invoke();
    }

    private static void CheckReturned()
    {
        string active = SceneManager.GetActiveScene().name;

        Report.AppendLine("  after pressing close: " + active);
        Expect(active == GameScenes.MainMenu, "closing a screen returns to where it was opened from");
    }

    // ----- plumbing -------------------------------------------------------

    private static void Expect(bool condition, string description)
    {
        Report.AppendLine("  " + (condition ? "ok   " : "FAIL ") + description);
        if (!condition) { Failures.Add(description); }
    }

    private static GameObject Find(string name)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>(true))
        {
            if (t.name == name) { return t.gameObject; }
        }

        return null;
    }

    private static Text TextOf(string name)
    {
        GameObject go = Find(name);
        return go != null ? go.GetComponent<Text>() : null;
    }

    private static bool HasText(string content)
    {
        foreach (Text text in Object.FindObjectsOfType<Text>(true))
        {
            if (text.text == content) { return true; }
        }

        return false;
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? "SCREENS RESULT: all checks passed."
            : "SCREENS RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[SCREENS SMOKE]\n" + Report);

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
