using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The other two brains, each in the level it actually appears in: the thrower
// in Scene2 and the ambusher in Scene4. Scene1's pack brain is covered by
// StatesSmokeTest, which needs the same level for its progress checks.
public static class NpcBrainSmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private static string scene;
    private static bool ghostRun;
    private static int step;
    private static float waitUntil;
    private static int frames;

    private static GrapeThrowerBrain grape;
    private static GhostAmbusherBrain ghost;
    private static float distanceWhenCrowded;
    private static int throwsAtRange;

    [MenuItem("Tools/Soulbound Gate/Debug/Thrower Brain Smoke Test")]
    public static void RunGrape()
    {
        ghostRun = false;
        scene = GameScenes.Scene2;
        Begin();
    }

    [MenuItem("Tools/Soulbound Gate/Debug/Ambusher Brain Smoke Test")]
    public static void RunGhost()
    {
        ghostRun = true;
        scene = GameScenes.Scene4;
        Begin();
    }

    private static void Begin()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity", OpenSceneMode.Single);
        PlayModeTestSettings.ApplyForTest();

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

        if (ghostRun) { TickGhost(); }
        else { TickGrape(); }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    // ----- the thrower ----------------------------------------------------

    private static void TickGrape()
    {
        switch (step)
        {
            case 0:
                if (frames < 60 || Time.time < 1.5f) { return; }
                step++;
                break;

            case 1: FindGrape(); step++; Wait(0.2f); break;
            case 2: CrowdTheGrape(); step++; Wait(1.2f); break;
            case 3: CheckGrapeBackedOff(); step++; Wait(2.5f); break;
            case 4: CheckGrapeThrows(); step++; Wait(0.2f); break;
            case 5: HideFromGrape(); step++; Wait(2.5f); break;
            case 6: CheckGrapeHeldFire(); step++; break;

            default: Finish("THROWER"); break;
        }
    }

    private static void FindGrape()
    {
        GrapeThrowerBrain[] brains = Object.FindObjectsOfType<GrapeThrowerBrain>();

        Report.AppendLine("  throwers in " + scene + ": " + brains.Length);
        Expect(brains.Length >= 1, "the level's throwers carry the thrower brain");

        if (brains.Length == 0) { return; }

        grape = brains[0];

        EnemyAI legacy = grape.GetComponent<EnemyAI>();
        Expect(legacy == null || !legacy.enabled, "the old EnemyAI is switched off");
    }

    // Walk right up to it: a thrower should not stand there and be hit.
    private static void CrowdTheGrape()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || grape == null) { return; }

        player.transform.position = grape.transform.position + new Vector3(1.2f, 0f, 0f);
        distanceWhenCrowded = 1.2f;
    }

    private static void CheckGrapeBackedOff()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || grape == null) { Expect(false, "the thrower survived to be checked"); return; }

        float distance = Vector2.Distance(grape.transform.position, player.transform.position);

        Report.AppendLine("  distance after crowding: " + distanceWhenCrowded.ToString("0.00") +
                          " -> " + distance.ToString("0.00") + ", state " + grape.State);

        Expect(grape.State == NpcBrain.BrainState.Engage, "it engages the player it can see");
        Expect(distance > distanceWhenCrowded, "it backs away when the player closes in");

        throwsAtRange = grape.ThrowsMade;
    }

    private static void CheckGrapeThrows()
    {
        if (grape == null) { return; }

        Report.AppendLine("  throws while in sight: " + grape.ThrowsMade);
        Expect(grape.ThrowsMade > 0, "it throws at a player it has a clear line to");
    }

    // Put the player far out of its world, where it has nothing to shoot at.
    private static void HideFromGrape()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || grape == null) { return; }

        player.transform.position = grape.transform.position + new Vector3(0f, 40f, 0f);
        throwsAtRange = grape.ThrowsMade;
    }

    private static void CheckGrapeHeldFire()
    {
        if (grape == null) { return; }

        Report.AppendLine("  throws with no target: " + throwsAtRange + " -> " + grape.ThrowsMade);
        Expect(grape.ThrowsMade == throwsAtRange, "it holds its fire with nothing in sight");
        Expect(grape.State != NpcBrain.BrainState.Engage, "it stops engaging once the player is gone");
    }

    // ----- the ambusher ---------------------------------------------------

    private static void TickGhost()
    {
        switch (step)
        {
            case 0:
                if (frames < 60 || Time.time < 1.5f) { return; }
                step++;
                break;

            case 1: FindGhost(); step++; Wait(0.2f); break;
            case 2: CheckGhostDormant(); step++; break;
            case 3: WalkIntoAmbush(); step++; Wait(1f); break;
            case 4: CheckGhostSprang(); step++; break;
            case 5: RunFromGhost(); step++; Wait(1.5f); break;
            case 6: CheckGhostBlinked(); step++; break;

            default: Finish("AMBUSHER"); break;
        }
    }

    private static void FindGhost()
    {
        GhostAmbusherBrain[] brains = Object.FindObjectsOfType<GhostAmbusherBrain>();

        Report.AppendLine("  ambushers in " + scene + ": " + brains.Length);
        Expect(brains.Length >= 1, "the level's ghosts carry the ambusher brain");

        if (brains.Length == 0) { return; }

        // The one furthest from the player, so it is genuinely still waiting.
        PlayerController player = PlayerController.Instance;
        float best = -1f;

        for (int i = 0; i < brains.Length; i++)
        {
            float distance = player == null
                ? 0f
                : Vector2.Distance(brains[i].transform.position, player.transform.position);

            if (distance > best)
            {
                best = distance;
                ghost = brains[i];
            }
        }

        EnemyAI legacy = ghost.GetComponent<EnemyAI>();
        Expect(legacy == null || !legacy.enabled, "the old EnemyAI is switched off");
    }

    private static void CheckGhostDormant()
    {
        if (ghost == null) { return; }

        SpriteRenderer body = ghost.GetComponent<SpriteRenderer>();

        Report.AppendLine("  waiting ghost: state " + ghost.State +
                          ", materialised " + ghost.Materialised +
                          ", alpha " + (body != null ? body.color.a.ToString("0.00") : "n/a"));

        Expect(!ghost.Materialised, "an ambusher waits unseen until the player is close");
        Expect(body == null || body.color.a < 0.6f, "it sits faded out while it waits");
    }

    private static void WalkIntoAmbush()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || ghost == null) { return; }

        player.transform.position = ghost.transform.position + new Vector3(2.5f, 0f, 0f);
    }

    private static void CheckGhostSprang()
    {
        if (ghost == null) { Expect(false, "the ambusher survived to be checked"); return; }

        SpriteRenderer body = ghost.GetComponent<SpriteRenderer>();

        Report.AppendLine("  sprung ghost: state " + ghost.State +
                          ", materialised " + ghost.Materialised +
                          ", alpha " + (body != null ? body.color.a.ToString("0.00") : "n/a"));

        Expect(ghost.Materialised, "walking into its reach makes it materialise");
        Expect(ghost.State == NpcBrain.BrainState.Engage, "it engages once it has sprung");
    }

    // Break away: the ghost should reappear rather than follow on foot.
    private static void RunFromGhost()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || ghost == null) { return; }

        player.transform.position = ghost.transform.position + new Vector3(7f, 0f, 0f);
    }

    private static void CheckGhostBlinked()
    {
        PlayerController player = PlayerController.Instance;
        if (ghost == null || player == null) { return; }

        float distance = Vector2.Distance(ghost.transform.position, player.transform.position);

        Report.AppendLine("  blinks: " + ghost.Blinks + ", distance to player now " + distance.ToString("0.00"));

        Expect(ghost.Blinks > 0, "it blinks after the player instead of walking");
        Expect(distance < 5f, "the blink puts it back within reach");
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

        Debug.Log("[NPC BRAIN SMOKE]\n" + Report);

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
