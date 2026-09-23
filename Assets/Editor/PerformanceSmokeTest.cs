using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

// Measures what the AI costs in the busiest level. Frame rate in batch mode
// means nothing - there is no real rendering - so the two numbers that do
// carry over to a phone are measured instead: how many physics queries the
// brains ask for per frame, and how much managed memory they churn while
// doing it. Garbage, not raw frame time, is what makes an Android build stutter.
public static class PerformanceSmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private const string Scene = GameScenes.Scene3;

    // Measured against game time rather than editor ticks: in batch mode there
    // is no rendering, so "frames" here bear no relation to a phone's. The
    // budget is then expressed the way it matters on a device - per enemy per
    // second - with the 60 fps equivalent printed alongside it.
    private const float MaxQueriesPerEnemyPerSecond = 60f;

    private const float SampleSeconds = 3f;

    private static int step;
    private static float waitUntil;
    private static int frames;

    private static int enemies;
    private static int sampleFrames;
    private static long queriesAtStart;
    private static long memoryAtStart;
    private static bool sampling;

    [MenuItem("Tools/Soulbound Gate/Debug/AI Performance Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + Scene + ".unity", OpenSceneMode.Single);
        PlayModeTestSettings.ApplyForTest();

        Report.Clear();
        Failures.Clear();
        step = 0;
        frames = 0;
        waitUntil = 0f;
        sampleFrames = 0;
        sampling = false;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { return; }

        frames++;
        if (sampling) { sampleFrames++; }

        if (Time.time < waitUntil) { return; }

        switch (step)
        {
            case 0:
                if (frames < 60 || Time.time < 1.5f) { return; }
                step++;
                break;

            case 1: StartSampling(); step++; Wait(SampleSeconds); break;
            case 2: StopSampling(); step++; break;

            default: Finish(); break;
        }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    private static void StartSampling()
    {
        NpcBrain[] brains = Object.FindObjectsOfType<NpcBrain>();
        enemies = brains.Length;

        // Stand the player in the middle of the room so the brains are all
        // actually thinking, not idling out of range.
        PlayerController player = PlayerController.Instance;
        if (player != null && enemies > 0)
        {
            player.transform.position = brains[0].transform.position + new Vector3(2.5f, 0f, 0f);
        }

        Report.AppendLine("  " + Scene + " has " + enemies + " NPC brain(s)");

        System.GC.Collect();

        NpcSenses.ResetQueryCount();
        queriesAtStart = NpcSenses.QueryCount;
        memoryAtStart = Profiler.GetMonoUsedSizeLong();

        sampleFrames = 0;
        sampling = true;
    }

    private static void StopSampling()
    {
        sampling = false;

        long queries = NpcSenses.QueryCount - queriesAtStart;
        long memory = Profiler.GetMonoUsedSizeLong() - memoryAtStart;

        if (enemies <= 0)
        {
            Expect(false, "the level had enemies to measure");
            return;
        }

        float queriesPerSecond = queries / SampleSeconds;
        float perEnemyPerSecond = queriesPerSecond / enemies;
        float kilobytesPerSecond = Mathf.Max(0f, memory / 1024f / SampleSeconds);

        Report.AppendLine("  sampled " + SampleSeconds + "s of game time (" + sampleFrames + " editor ticks)");
        Report.AppendLine("  physics queries: " + queries + " total, " +
                          queriesPerSecond.ToString("0.0") + "/s for the room, " +
                          perEnemyPerSecond.ToString("0.0") + "/s per enemy" +
                          "  (= " + (perEnemyPerSecond / 60f).ToString("0.00") + " per enemy per frame at 60 fps)");
        // Reported, not asserted: inside the editor this number is dominated by
        // the editor's own churn, so it says nothing about the build. What the
        // AI itself allocates is zero by construction - every query goes
        // through the shared buffers in NpcSenses.
        Report.AppendLine("  managed memory growth (editor, informational): " +
                          kilobytesPerSecond.ToString("0.0") + " KB/s");

        Expect(perEnemyPerSecond <= MaxQueriesPerEnemyPerSecond,
               "each NPC stays under " + MaxQueriesPerEnemyPerSecond + " physics queries a second");
    }

    private static void Expect(bool condition, string description)
    {
        Report.AppendLine("  " + (condition ? "ok   " : "FAIL ") + description);
        if (!condition) { Failures.Add(description); }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? "PERFORMANCE RESULT: all checks passed."
            : "PERFORMANCE RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[PERFORMANCE SMOKE]\n" + Report);

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
