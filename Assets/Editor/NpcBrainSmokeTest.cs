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
    private static GhostAmbusherBrain shotGhost;
    private static Vector2 ghostRestingPlace;
    private static bool sampling;
    private static float closestAfterFlee;
    private static bool measuringDrift;
    private static float ghostPathLength;
    private static Vector2 lastSampledGhostPosition;

    private static void SampleGhostDistance()
    {
        PlayerController player = PlayerController.Instance;
        if (ghost == null || player == null) { return; }

        float distance = Vector2.Distance(ghost.transform.position, player.transform.position);
        if (distance < closestAfterFlee) { closestAfterFlee = distance; }
    }

    // How far it actually walked, not how far it ended up from where it began:
    // a wanderer that loops back looks stationary by the second measure.
    private static void SampleGhostPath()
    {
        if (ghost == null) { return; }

        Vector2 position = ghost.transform.position;
        ghostPathLength += Vector2.Distance(position, lastSampledGhostPosition);
        lastSampledGhostPosition = position;
    }
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

        // Sampled every frame, not once at the end: the ghost blinks in, and
        // then carries on with its own business, so a single reading taken
        // later says nothing about whether the blink landed near the player.
        if (sampling) { SampleGhostDistance(); }
        if (measuringDrift) { SampleGhostPath(); }

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
            case 7: WoundGrapeAndRunAway(); step++; Wait(3f); break;
            case 8: CheckWoundedGrapeHeldFire(); step++; break;

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

    // A wounded thrower used to keep lobbing at a player who had run off: it
    // was in its retreat state, and that state threw at any distance at all.
    private static void WoundGrapeAndRunAway()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || grape == null) { return; }

        // Back in view and in range first, so it is properly engaged.
        player.transform.position = grape.transform.position + new Vector3(3f, 0f, 0f);

        EnemyHealth health = grape.GetComponent<EnemyHealth>();
        if (health != null) { health.TakeDamage(1); }

        // Now sprint well past its throwing range but stay in the open.
        player.transform.position = grape.transform.position + new Vector3(9f, 0f, 0f);

        throwsAtRange = grape.ThrowsMade;
    }

    private static void CheckWoundedGrapeHeldFire()
    {
        PlayerController player = PlayerController.Instance;
        if (grape == null || player == null) { return; }

        float distance = Vector2.Distance(grape.transform.position, player.transform.position);

        Report.AppendLine("  wounded, player " + distance.ToString("0.0") + " away, state " + grape.State +
                          ", throws " + throwsAtRange + " -> " + grape.ThrowsMade);

        Expect(distance > grape.AttackRange, "the player really is out of its range");
        Expect(grape.ThrowsMade == throwsAtRange, "a wounded thrower does not throw across the map");
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
            case 2: NoteGhostRestingPlace(); step++; Wait(2f); break;
            case 3: CheckGhostDrifts(); step++; break;
            case 4: ShootTheOtherGhost(); step++; Wait(1.2f); break;
            case 5: CheckShotGhostWokeUp(); step++; break;
            case 6: WalkIntoAmbush(); step++; Wait(1f); break;
            case 7: CheckGhostSprang(); step++; break;
            case 8: RunFromGhost(); step++; Wait(2.5f); break;
            case 9: CheckGhostBlinked(); step++; break;

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

        // A second one, far from the first, for the "shot from out of sight"
        // check - so waking it cannot disturb the ambush test.
        shotGhost = null;
        float bestSeparation = 4f;

        for (int i = 0; i < brains.Length; i++)
        {
            if (brains[i] == ghost) { continue; }

            float separation = Vector2.Distance(brains[i].transform.position, ghost.transform.position);
            if (separation > bestSeparation)
            {
                bestSeparation = separation;
                shotGhost = brains[i];
            }
        }

        EnemyAI legacy = ghost.GetComponent<EnemyAI>();
        Expect(legacy == null || !legacy.enabled, "the old EnemyAI is switched off");
    }

    private static void NoteGhostRestingPlace()
    {
        if (ghost == null) { return; }

        ghostRestingPlace = ghost.transform.position;
        lastSampledGhostPosition = ghostRestingPlace;
        ghostPathLength = 0f;
        measuringDrift = true;
    }

    // It should be drifting around its haunt, not standing perfectly still:
    // a motionless enemy reads as a broken one.
    private static void CheckGhostDrifts()
    {
        if (ghost == null) { return; }

        measuringDrift = false;

        SpriteRenderer body = ghost.GetComponent<SpriteRenderer>();
        float netDrift = Vector2.Distance(ghost.transform.position, ghostRestingPlace);

        Report.AppendLine("  waiting ghost: state " + ghost.State +
                          ", materialised " + ghost.Materialised +
                          ", alpha " + (body != null ? body.color.a.ToString("0.00") : "n/a") +
                          ", walked " + ghostPathLength.ToString("0.00") +
                          " (ended " + netDrift.ToString("0.00") + " from where it started)");

        Expect(!ghost.Materialised, "an ambusher waits unseen until the player is close");
        Expect(body == null || body.color.a < 0.6f, "it waits half faded");
        Expect(ghostPathLength > 0.5f, "it wanders its haunt while it waits");
    }

    // Shot from outside its ambush range: it must not simply stand there.
    private static void ShootTheOtherGhost()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || shotGhost == null) { return; }

        player.transform.position = shotGhost.transform.position + new Vector3(9f, 0f, 0f);

        EnemyHealth health = shotGhost.GetComponent<EnemyHealth>();
        if (health != null) { health.TakeDamage(1); }
    }

    private static void CheckShotGhostWokeUp()
    {
        if (shotGhost == null)
        {
            Report.AppendLine("  only one ghost in the level; the shot test was skipped");
            return;
        }

        Report.AppendLine("  ghost shot from 9 away: state " + shotGhost.State +
                          ", materialised " + shotGhost.Materialised);

        Expect(shotGhost.State != NpcBrain.BrainState.Patrol, "being shot wakes an NPC that never saw it coming");
        Expect(shotGhost.Materialised, "a ghost hit from out of sight shows itself");
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

    // Break away properly: far enough that it could not walk the distance in
    // the time, and to a spot in the open with a clear line back to the ghost,
    // so the test is not at the mercy of wherever the ghost had wandered.
    private static void RunFromGhost()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || ghost == null) { return; }

        Vector2 origin = ghost.transform.position;
        Vector2 spot = origin + Vector2.right * 9f;

        for (int i = 0; i < 12; i++)
        {
            Vector2 candidate = origin + NpcSenses.Rotate(Vector2.right, i * 30f) * 9f;

            if (!NpcSenses.IsFree(player.gameObject, candidate, 0.5f)) { continue; }
            if (!NpcSenses.HasLineOfSight(ghost.gameObject, origin, candidate)) { continue; }

            spot = candidate;
            break;
        }

        player.transform.position = spot;
        Report.AppendLine("  player fled to " + Vector2.Distance(origin, spot).ToString("0.0") + " away");

        closestAfterFlee = float.MaxValue;
        sampling = true;
    }

    private static void CheckGhostBlinked()
    {
        PlayerController player = PlayerController.Instance;
        if (ghost == null || player == null) { return; }

        float distance = Vector2.Distance(ghost.transform.position, player.transform.position);

        sampling = false;

        Report.AppendLine("  blinks: " + ghost.Blinks + ", closest it got: " + closestAfterFlee.ToString("0.00") +
                          ", distance now " + distance.ToString("0.00") + ", state " + ghost.State);

        Expect(ghost.Blinks > 0, "it blinks after the player instead of walking");
        Expect(closestAfterFlee < 5f, "the blink puts it back within reach");
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
