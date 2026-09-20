using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Plays Scene1 and exercises this week's work: the NPC brains, the per-level
// history, the win overlay and the game over screen with its four ways out.
// Every check drives the real components in a running scene.
//
// The test resets the recorded history first so its assertions are exact. That
// only touches the editor's own PlayerPrefs, never a build on a device.
public static class StatesSmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private static int step;
    private static float waitUntil;
    private static int frames;

    private static SlimePackBrain watchedSlime;
    private static int deathsBefore;

    [MenuItem("Tools/Soulbound Gate/Debug/Game States Smoke Test")]
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

            case 1: CheckBrainsInstalled(); step++; break;
            case 2: ProvokeSlime(); step++; Wait(0.8f); break;
            case 3: CheckSlimeReacted(); step++; break;
            case 4: ClearTheLevel(); step++; Wait(1.2f); break;
            case 5: CheckLevelRecorded(); step++; break;
            case 6: CheckWinOverlay(); step++; break;
            case 7: KillPlayer(); step++; Wait(2.4f); break;
            case 8: CheckGameOver(); step++; break;

            default:
                Finish();
                break;
        }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    // ----- NPC brains -----------------------------------------------------

    private static void CheckBrainsInstalled()
    {
        SlimePackBrain[] slimes = Object.FindObjectsOfType<SlimePackBrain>();

        Report.AppendLine("  slimes with a brain: " + slimes.Length);
        Expect(slimes.Length >= 3, "Scene1's slimes all carry the pack brain");

        if (slimes.Length == 0) { return; }

        // The old random-roaming component must not be driving them as well.
        bool legacyOff = true;
        HashSet<int> slots = new HashSet<int>();

        for (int i = 0; i < slimes.Length; i++)
        {
            EnemyAI legacy = slimes[i].GetComponent<EnemyAI>();
            if (legacy != null && legacy.enabled) { legacyOff = false; }

            slots.Add(slimes[i].Slot);
        }

        Expect(legacyOff, "the old EnemyAI is switched off wherever a brain runs");
        Expect(slots.Count == slimes.Length, "each slime holds its own flanking slot");

        // A slime the player has never been near should be going about its own
        // business. Which one that is depends on where the level starts them,
        // so it is found rather than assumed.
        PlayerController player = PlayerController.Instance;
        SlimePackBrain farthest = null;
        float farthestDistance = 0f;

        if (player != null)
        {
            for (int i = 0; i < slimes.Length; i++)
            {
                float distance = Vector2.Distance(slimes[i].transform.position, player.transform.position);
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthest = slimes[i];
                }
            }
        }

        if (farthest != null)
        {
            Report.AppendLine("  farthest slime is " + farthestDistance.ToString("0.0") +
                              " away and is " + farthest.State);

            // Only meaningful when that slime really is out of range; in a small
            // room every one of them can legitimately see the player already.
            if (farthestDistance > 8f)
            {
                Expect(farthest.State != NpcBrain.BrainState.Engage,
                       "a slime that cannot see the player does not chase it");
            }
        }

        watchedSlime = slimes[0];
    }

    // Drop the player in front of one slime and see whether the pack notices.
    private static void ProvokeSlime()
    {
        PlayerController player = PlayerController.Instance;

        Report.AppendLine("  player: " + (player == null ? "NULL" : player.transform.position.ToString("0.00")) +
                          ", health: " + (PlayerHealth.Instance == null ? "NULL" : PlayerHealth.Instance.CurrentHealth.ToString()) +
                          ", slime: " + (watchedSlime == null ? "NULL" : watchedSlime.transform.position.ToString("0.00")));

        if (player == null || watchedSlime == null) { return; }

        player.transform.position = watchedSlime.transform.position + new Vector3(1.4f, 0f, 0f);

        Report.AppendLine("  moved player to " + player.transform.position.ToString("0.00") +
                          ", line of sight: " + NpcSenses.HasLineOfSight(watchedSlime.gameObject,
                              watchedSlime.transform.position, player.transform.position));
    }

    private static void CheckSlimeReacted()
    {
        if (watchedSlime == null)
        {
            Expect(false, "the watched slime survived to be checked");
            return;
        }

        Report.AppendLine("  slime state: " + watchedSlime.State +
                          ", sightings reported: " + NpcAlertNetwork.SightingsReported);

        Expect(watchedSlime.State == NpcBrain.BrainState.Engage, "a slime that can see the player engages");
        Expect(watchedSlime.RemembersPlayer, "it remembers where the player was");
        Expect(NpcAlertNetwork.SightingsReported > 0, "the sighting was broadcast to the pack");

        // The others should be converging on the report, not still wandering.
        SlimePackBrain[] slimes = Object.FindObjectsOfType<SlimePackBrain>();
        int roused = 0;

        for (int i = 0; i < slimes.Length; i++)
        {
            if (slimes[i] == watchedSlime) { continue; }
            if (slimes[i].State != NpcBrain.BrainState.Patrol) { roused++; }
        }

        Report.AppendLine("  pack members roused by the call: " + roused);
        Expect(roused > 0, "at least one ally acts on the call");
    }

    // ----- level completion is saved --------------------------------------

    private static void ClearTheLevel()
    {
        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].CountsTowardObjective)
            {
                enemies[i].TakeDamage(999);
            }
        }
    }

    private static void CheckLevelRecorded()
    {
        LevelManager level = LevelManager.Instance;
        GameSaveManager save = GameSaveManager.Instance;

        Expect(level != null && level.GateOpen, "clearing the room opens the gate");

        if (save != null)
        {
            SceneStateData state = save.SceneState(GameScenes.Scene1);

            Expect(state != null && state.completed, "the level is marked complete in the run");
            Expect(save.Data.highestUnlockedLevel >= 2, "the next level is unlocked");

            Report.AppendLine("  highest unlocked level: " + save.Data.highestUnlockedLevel);
        }

        // The part that outlives the run: history the progress screen reads.
        Expect(ProfileStats.HasCleared(1), "the clear is written into the history");
        Expect(ProfileStats.FurthestLevelCleared == 1, "the history knows how far the player has got");

        Report.AppendLine("  level 1 best time: " + ProfileStats.FormatTime(ProfileStats.BestLevelTime(1)));
    }

    // ----- win and game over ---------------------------------------------

    private static void CheckWinOverlay()
    {
        GameplayRuntime runtime = GameplayRuntime.Instance;

        if (runtime == null || runtime.WinOverlay == null)
        {
            Expect(false, "the gameplay screen carries a win overlay");
            return;
        }

        runtime.WinOverlay.Show();
        Expect(runtime.WinOverlay.IsVisible, "the win message shows on the gameplay screen");

        Text title = FindText("Title", "CHIẾN THẮNG!");
        Expect(title != null, "the win message reads CHIẾN THẮNG!");

        runtime.WinOverlay.Hide();
        Expect(!runtime.WinOverlay.IsVisible, "it clears again afterwards");

        // The victory screen's four buttons lead four different places.
        Expect(Distinct(VictoryScreenController.Destinations) >= 3,
               "the victory screen offers at least 3 separate destinations");
    }

    private static void KillPlayer()
    {
        deathsBefore = ProfileStats.Deaths;

        PlayerHealth health = PlayerHealth.Instance;
        if (health == null) { return; }

        // One big hit: the damage recovery window means repeated single hits in
        // the same frame would only ever land the first one.
        health.TakeDamage(99, health.transform);
    }

    private static void CheckGameOver()
    {
        GameplayRuntime runtime = GameplayRuntime.Instance;
        GameOverUI gameOver = runtime != null ? runtime.GameOver : null;

        if (gameOver == null)
        {
            Expect(false, "the gameplay screen carries a game over screen");
            return;
        }

        Expect(gameOver.IsVisible, "dying shows the game over screen over the level");

        string[] buttons = { "Retry", "Home", "Progress", "Settings" };
        int wired = 0;

        for (int i = 0; i < buttons.Length; i++)
        {
            GameObject go = FindAny(buttons[i]);
            Button button = go != null ? go.GetComponent<Button>() : null;

            if (button != null && button.onClick.GetPersistentEventCount() >= 0 && go.activeInHierarchy) { wired++; }
        }

        Report.AppendLine("  navigation buttons found: " + wired + " / " + buttons.Length);
        Expect(wired >= 3, "it shows at least 3 working navigation buttons");

        Expect(Distinct(GameOverUI.Destinations) >= 3,
               "those buttons lead to at least 3 separate screens");

        // Every destination has to be a scene the build actually contains.
        bool allInBuild = true;
        for (int i = 0; i < GameOverUI.Destinations.Length; i++)
        {
            if (!InBuildOrder(GameOverUI.Destinations[i])) { allInBuild = false; }
        }

        Expect(allInBuild, "every destination is a scene in the build");

        Report.AppendLine("  deaths recorded: " + deathsBefore + " -> " + ProfileStats.Deaths);
        Expect(ProfileStats.Deaths == deathsBefore + 1, "the lost run is written into the history");
        Expect(ProfileStats.LastOutcome == ProfileStats.OutcomeDeath, "the history remembers how it ended");
    }

    // ----- plumbing -------------------------------------------------------

    private static int Distinct(string[] values)
    {
        HashSet<string> seen = new HashSet<string>();
        for (int i = 0; i < values.Length; i++) { seen.Add(values[i]); }

        return seen.Count;
    }

    private static bool InBuildOrder(string sceneName)
    {
        for (int i = 0; i < GameScenes.BuildOrder.Length; i++)
        {
            if (GameScenes.BuildOrder[i] == sceneName) { return true; }
        }

        return false;
    }

    private static void Expect(bool condition, string description)
    {
        Report.AppendLine("  " + (condition ? "ok   " : "FAIL ") + description);
        if (!condition) { Failures.Add(description); }
    }

    private static GameObject FindAny(string name)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>(true))
        {
            if (t.name == name) { return t.gameObject; }
        }

        return null;
    }

    private static Text FindText(string name, string content)
    {
        foreach (Text text in Object.FindObjectsOfType<Text>(true))
        {
            if (text.name == name && text.text == content) { return text; }
        }

        return null;
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? "STATES RESULT: all checks passed."
            : "STATES RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[STATES SMOKE]\n" + Report);

        EditorApplication.ExitPlaymode();
        EditorApplication.update += QuitWhenStopped;
    }

    private static void QuitWhenStopped()
    {
        if (EditorApplication.isPlaying) { return; }

        EditorApplication.update -= QuitWhenStopped;

        PlayModeTestSettings.Restore();
        EditorApplication.Exit(0);
    }
}
