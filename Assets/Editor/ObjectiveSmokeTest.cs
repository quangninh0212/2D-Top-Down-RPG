using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The two levels that no longer ask the player to kill everything: the
// crossroads, where the gate opens once they have held out, and the marsh,
// where it stays shut until the key is found. Both are driven in play mode.
public static class ObjectiveSmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private static string scene;
    private static bool keyRun;
    private static int step;
    private static float waitUntil;
    private static int frames;

    private static GateKey key;

    [MenuItem("Tools/Soulbound Gate/Debug/Survive Objective Smoke Test")]
    public static void RunSurvive()
    {
        keyRun = false;
        scene = GameScenes.Scene3;
        Begin();
    }

    [MenuItem("Tools/Soulbound Gate/Debug/Key Objective Smoke Test")]
    public static void RunKey()
    {
        keyRun = true;
        scene = GameScenes.Scene4;
        Begin();
    }

    private static void Begin()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity", OpenSceneMode.Single);
        PlayModeTestSettings.ApplyForTest();

        ProfileStats.ClearAll();

        Report.Clear();
        Failures.Clear();
        step = 0;
        frames = 0;
        waitUntil = 0f;
        key = null;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { return; }

        frames++;
        if (Time.time < waitUntil) { return; }

        if (keyRun) { TickKey(); }
        else { TickSurvive(); }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    // ----- survive --------------------------------------------------------

    private static void TickSurvive()
    {
        switch (step)
        {
            case 0:
                if (frames < 60 || Time.time < 1.5f) { return; }
                step++;
                break;

            case 1: CheckSurviveStarted(); step++; Wait(1f); break;
            case 2: CheckClockIsRunning(); step++; break;
            case 3: HurryTheClock(); step++; Wait(1.5f); break;
            case 4: CheckSurviveFinished(); step++; break;

            default: Finish("SURVIVE"); break;
        }
    }

    private static void CheckSurviveStarted()
    {
        LevelManager level = LevelManager.Instance;

        if (level == null) { Expect(false, "the level has a manager"); return; }

        Report.AppendLine("  goal: " + level.Goal + ", clock: " + level.SurviveRemaining.ToString("0.0") +
                          "s, gate open: " + level.GateOpen);

        Expect(level.Goal == LevelGoal.Survive, "the crossroads asks the player to hold out");
        Expect(!level.GateOpen, "the gate starts shut");
        Expect(level.IsSurviving && level.SurviveRemaining > 40f, "the clock starts at its full length");
        Expect(level.ObjectiveLine.Contains("SỐNG SÓT"), "the HUD says what this level wants");

        // Nobody is holding the joystick, so seven enemies would kill the
        // player in a few seconds and the clock would stop - which is correct
        // behaviour, and useless for testing the clock. The player is parked
        // out of reach instead, which is what surviving looks like anyway.
        PlayerController player = PlayerController.Instance;
        if (player != null) { player.transform.position += new Vector3(0f, 30f, 0f); }
    }

    private static float clockAtFirstCheck;

    private static void CheckClockIsRunning()
    {
        LevelManager level = LevelManager.Instance;
        if (level == null) { return; }

        clockAtFirstCheck = level.SurviveRemaining;

        Report.AppendLine("  clock after a second: " + clockAtFirstCheck.ToString("0.0") + "s");
        Expect(clockAtFirstCheck < 45f, "the clock counts down while the player survives");
        Expect(!level.GateOpen, "and the gate is still shut part way through");
    }

    // Forty-five seconds is the right length to play and the wrong length to
    // test, so the clock is wound forward rather than waited out.
    private static void HurryTheClock()
    {
        LevelManager level = LevelManager.Instance;
        if (level == null) { return; }

        FieldInfo field = typeof(LevelManager).GetField("surviveRemaining",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            Expect(false, "the survival clock could be wound forward");
            return;
        }

        field.SetValue(level, 0.4f);
    }

    private static void CheckSurviveFinished()
    {
        LevelManager level = LevelManager.Instance;
        if (level == null) { return; }

        Report.AppendLine("  after the clock ran out - gate open: " + level.GateOpen +
                          ", objective line: " + level.ObjectiveLine);

        Expect(level.GateOpen, "holding out opens the gate");
        Expect(ProfileStats.HasCleared(3), "the clear is written into the history");
    }

    // ----- the key --------------------------------------------------------

    private static void TickKey()
    {
        switch (step)
        {
            case 0:
                if (frames < 60 || Time.time < 1.5f) { return; }
                step++;
                break;

            case 1: CheckKeyLevelStarted(); step++; break;
            case 2: KillEverything(); step++; Wait(1.2f); break;
            case 3: CheckKillingIsNotEnough(); step++; break;
            case 4: TakeTheKey(); step++; Wait(0.5f); break;
            case 5: CheckKeyOpenedTheGate(); step++; break;

            default: Finish("KEY"); break;
        }
    }

    private static void CheckKeyLevelStarted()
    {
        LevelManager level = LevelManager.Instance;
        key = Object.FindObjectOfType<GateKey>();

        if (level == null) { Expect(false, "the level has a manager"); return; }

        Report.AppendLine("  goal: " + level.Goal + ", key in the level: " + (key != null) +
                          ", gate open: " + level.GateOpen);

        Expect(level.Goal == LevelGoal.FindKey, "the marsh asks the player to find a key");
        Expect(key != null, "the key is actually placed in the level");
        Expect(!level.GateOpen, "the gate starts shut");
        Expect(level.ObjectiveLine.Contains("CHÌA KHOÁ"), "the HUD says what this level wants");
    }

    private static void KillEverything()
    {
        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].CountsTowardObjective) { enemies[i].TakeDamage(999); }
        }
    }

    private static void CheckKillingIsNotEnough()
    {
        LevelManager level = LevelManager.Instance;
        if (level == null) { return; }

        Report.AppendLine("  enemies left: " + level.RemainingMandatoryEnemies +
                          ", gate open: " + level.GateOpen);

        Expect(level.RemainingMandatoryEnemies == 0, "the room really was cleared");
        Expect(!level.GateOpen, "an empty room does not open a gate that needs a key");
    }

    private static void TakeTheKey()
    {
        if (key != null) { key.Take(); }
    }

    private static void CheckKeyOpenedTheGate()
    {
        LevelManager level = LevelManager.Instance;
        if (level == null) { return; }

        GameSaveManager save = GameSaveManager.Instance;
        SceneStateData state = save != null ? save.SceneState(GameScenes.Scene4) : null;

        Report.AppendLine("  after taking the key - gate open: " + level.GateOpen +
                          ", key still in the level: " + (Object.FindObjectOfType<GateKey>() != null));

        Expect(level.GateOpen, "the key opens the gate");
        Expect(state != null && state.completed, "the level is marked complete");
        Expect(ProfileStats.HasCleared(4), "the clear is written into the history");
    }

    // ----- plumbing -------------------------------------------------------

    private static void Expect(bool condition, string description)
    {
        Report.AppendLine("  " + (condition ? "ok   " : "FAIL ") + description);
        if (!condition) { Failures.Add(description); }
    }

    private static void Finish(string label)
    {
        EditorApplication.update -= Tick;

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? label + " RESULT: all checks passed."
            : label + " RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[OBJECTIVE SMOKE]\n" + Report);

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
