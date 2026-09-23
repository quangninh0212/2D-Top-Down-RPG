using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Dies, presses CHƠI LẠI, skips the opening, and checks the player who lands
// in the first level is actually alive and able to play - the exact sequence a
// player performs, driven through the real buttons.
public static class RetrySmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private static int step;
    private static float waitUntil;
    private static int frames;

    [MenuItem("Tools/Soulbound Gate/Debug/Retry After Death Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + GameScenes.Scene1 + ".unity", OpenSceneMode.Single);
        PlayModeTestSettings.ApplyForTest();

        ProfileStats.ClearAll();

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
                if (frames < 60 || Time.time < 1.5f) { return; }
                step++;
                break;

            case 1: Die(); step++; Wait(2.6f); break;
            case 2: PressRetry(); step++; Wait(2f); break;
            case 3: SkipTheStory(); step++; Wait(4f); break;
            case 4: CheckBackInTheGame(); step++; break;

            // The same situation from the other side: a player object that
            // survives into a new run has to be brought back by the save
            // system, not only by being thrown away and rebuilt.
            case 5: DieAgain(); step++; Wait(2.6f); break;
            case 6: StartARunWithoutRebuildingThePlayer(); step++; Wait(0.4f); break;
            case 7: CheckTheSafetyNet(); step++; break;

            default: Finish(); break;
        }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    private static void Die()
    {
        PlayerHealth health = PlayerHealth.Instance;
        if (health != null) { health.TakeDamage(99, health.transform); }

        Report.AppendLine("  player killed; dead = " + (health != null && health.isDead));
    }

    private static void PressRetry()
    {
        GameObject go = Find("Retry");
        Button retry = go != null ? go.GetComponent<Button>() : null;

        if (retry == null)
        {
            Expect(false, "the game over screen offers CHƠI LẠI");
            return;
        }

        retry.onClick.Invoke();
        Report.AppendLine("  pressed CHƠI LẠI");
    }

    private static void SkipTheStory()
    {
        Report.AppendLine("  scene after retry: " + SceneManager.GetActiveScene().name);

        GameObject go = Find("Skip");
        Button skip = go != null ? go.GetComponent<Button>() : null;

        if (skip != null)
        {
            skip.onClick.Invoke();
            Report.AppendLine("  skipped the opening");
        }
    }

    private static void CheckBackInTheGame()
    {
        string active = SceneManager.GetActiveScene().name;
        PlayerHealth health = PlayerHealth.Instance;
        PlayerController player = PlayerController.Instance;
        ActiveWeapon weapon = ActiveWeapon.Instance;

        Report.AppendLine("  scene now: " + active);

        Expect(active == GameScenes.Scene1, "the retry lands in the first level");

        if (health == null || player == null)
        {
            Expect(false, "there is a player in the level");
            return;
        }

        Report.AppendLine("  health: " + health.CurrentHealth + "/" + health.MaxHealth +
                          ", dead flag: " + health.isDead);

        Expect(!health.isDead, "the player is alive again");
        Expect(health.CurrentHealth == health.MaxHealth, "and starts on full health");

        // The three things death switches off, which a retry has to switch
        // back on: movement, the weapon, and the game over screen itself.
        bool controls = ReadPrivateBool(player, "controlsEnabled");
        Report.AppendLine("  controls enabled: " + controls);
        Expect(controls, "the player can move again");

        if (weapon != null)
        {
            bool weaponDisabled = ReadPrivateBool(weapon, "disabled");

            Report.AppendLine("  weapon disabled: " + weaponDisabled +
                              ", equipped: " + (weapon.CurrentActiveWeapon != null));

            Expect(!weaponDisabled, "the player can attack again");
            Expect(weapon.CurrentActiveWeapon != null, "and is holding a weapon");
        }

        GameplayRuntime runtime = GameplayRuntime.Instance;
        bool gameOverShowing = runtime != null && runtime.GameOver != null && runtime.GameOver.IsVisible;

        Report.AppendLine("  game over screen still up: " + gameOverShowing);
        Expect(!gameOverShowing, "the game over screen is gone");
    }

    private static void DieAgain()
    {
        PlayerHealth health = PlayerHealth.Instance;
        if (health != null) { health.TakeDamage(99, health.transform); }
    }

    // No scene change and no new player: only the saved run is replaced, the
    // way it would be if some future flow started a run without clearing out
    // what the last one left behind.
    private static void StartARunWithoutRebuildingThePlayer()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save == null) { return; }

        save.NewRun();
        save.ApplyToLiveScene(false);
    }

    private static void CheckTheSafetyNet()
    {
        PlayerHealth health = PlayerHealth.Instance;
        PlayerController player = PlayerController.Instance;
        ActiveWeapon weapon = ActiveWeapon.Instance;

        if (health == null || player == null)
        {
            Expect(false, "the player survived to be checked");
            return;
        }

        Report.AppendLine("  after a new run on the same player - dead: " + health.isDead +
                          ", health: " + health.CurrentHealth + "/" + health.MaxHealth);

        Expect(!health.isDead, "a player that outlives its run is revived by the new one");
        Expect(ReadPrivateBool(player, "controlsEnabled"), "its controls are handed back");

        if (weapon != null)
        {
            Expect(!ReadPrivateBool(weapon, "disabled"), "and so is its weapon");
        }
    }

    // ----- plumbing -------------------------------------------------------

    private static bool ReadPrivateBool(object target, string field)
    {
        FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        return info != null && (bool)info.GetValue(target);
    }

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

    private static void Finish()
    {
        EditorApplication.update -= Tick;

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? "RETRY RESULT: all checks passed."
            : "RETRY RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[RETRY SMOKE]\n" + Report);

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
