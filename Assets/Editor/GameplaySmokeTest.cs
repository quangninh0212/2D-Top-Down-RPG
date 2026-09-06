using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Enters play mode in Scene1, then loads Scene2 the way a gate does, and checks
// that the things the player needs are still alive on the other side.
//
// This exists because of a specific failure: the EventSystem used to be created
// per scene, so the second level a player reached had none and every touch
// control was dead. Nothing in the scene files shows that - only running it does.
public static class GameplaySmokeTest
{
    private const int FramesBeforeTransition = 40;
    private const int FramesAfterTransition = 40;

    private static int frames;
    private static bool transitionStarted;
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    [MenuItem("Tools/Soulbound Gate/Debug/Gameplay Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + GameScenes.Scene1 + ".unity", OpenSceneMode.Single);

        PlayModeTestSettings.ApplyForTest();

        frames = 0;
        transitionStarted = false;
        Report.Clear();
        Failures.Clear();

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { return; }

        frames++;

        if (!transitionStarted)
        {
            if (frames < FramesBeforeTransition) { return; }

            Inspect("Scene1 (first level entered)");

            transitionStarted = true;
            frames = 0;

            // Straight to Scene2, which is what the player ends up in after the
            // loading screen finishes.
            SceneManager.LoadScene(GameScenes.Scene2);
            return;
        }

        if (frames < FramesAfterTransition) { return; }

        EditorApplication.update -= Tick;

        Inspect("Scene2 (after a level transition)");

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? "GAMEPLAY SMOKE RESULT: all checks passed."
            : "GAMEPLAY SMOKE RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[GAMEPLAY SMOKE]\n" + Report);

        EditorApplication.ExitPlaymode();
        EditorApplication.update += QuitWhenStopped;
    }

    private static void QuitWhenStopped()
    {
        if (EditorApplication.isPlaying) { return; }

        EditorApplication.update -= QuitWhenStopped;

        // Put the project-wide play mode setting back before leaving.
        PlayModeTestSettings.Restore();

        EditorApplication.Exit(0);
    }

    private static void Inspect(string label)
    {
        Report.AppendLine();
        Report.AppendLine(label + ":");

        // Input is dead without exactly one EventSystem.
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>();
        Report.AppendLine("  EventSystems: " + eventSystems.Length +
                          (eventSystems.Length > 0 ? " (current=" + (EventSystem.current != null) + ")" : ""));

        if (eventSystems.Length == 0) { Failures.Add(label + ": no EventSystem - touch controls would be dead"); }
        if (eventSystems.Length > 1) { Failures.Add(label + ": " + eventSystems.Length + " EventSystems fighting over input"); }

        ReportSingleton<PlayerController>(label, "Player");
        ReportSingleton<ActiveWeapon>(label, "ActiveWeapon");
        ReportSingleton<UIFade>(label, "UICanvas");
        ReportSingleton<EconomyManager>(label, "EconomyManager");
        ReportSingleton<PlayerHealth>(label, "PlayerHealth");

        // The touch overlay and its buttons live on a persistent object.
        GameplayRuntime runtime = Object.FindObjectOfType<GameplayRuntime>();
        Report.AppendLine("  GameplayRuntime: " + (runtime != null));
        if (runtime == null) { Failures.Add(label + ": GameplayRuntime missing - no on-screen controls"); }

        int attackButtons = 0;
        foreach (Button button in Object.FindObjectsOfType<Button>(true))
        {
            if (button.name == "AttackButton") { attackButtons++; }
        }

        Report.AppendLine("  AttackButton instances: " + attackButtons);
        if (attackButtons != 1) { Failures.Add(label + ": expected exactly one AttackButton, found " + attackButtons); }

        // The HUD readouts the gameplay scripts look up by name.
        ReportNamed(label, "Health Slider");
        ReportNamed(label, "Stamina Container");
        ReportNamed(label, "Gold Amount Text");

        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            Report.AppendLine("  Player position: " + ((Vector2)player.transform.position).ToString("0.0") +
                              "  blocked=" + IsBlocked(player.transform.position));

            if (IsBlocked(player.transform.position))
            {
                Failures.Add(label + ": player is standing inside a collider and cannot move");
            }
        }

        Report.AppendLine("  Time.timeScale: " + Time.timeScale);
        if (!Mathf.Approximately(Time.timeScale, 1f)) { Failures.Add(label + ": time is not running"); }

        CheckHudLayout(label);
    }

    // The health, stamina and gold readouts used to sit on top of each other.
    // Their rects are laid out explicitly now, so this checks the arithmetic
    // rather than trusting it: three rows, stacked, no overlap, all within the
    // canvas.
    private static void CheckHudLayout(string label)
    {
        string[] names = { "Heart Container", "Stamina Container", "Gold Coin Container" };
        List<Rect> rects = new List<Rect>();

        for (int i = 0; i < names.Length; i++)
        {
            RectTransform rect = FindRect(names[i]);

            if (rect == null)
            {
                Failures.Add(label + ": HUD container '" + names[i] + "' not found");
                continue;
            }

            // Anchored to the top-left with a top-left pivot, so this reads as
            // "x across, y down" from the corner.
            Rect box = new Rect(rect.anchoredPosition.x, -rect.anchoredPosition.y,
                                rect.rect.width, rect.rect.height);

            rects.Add(box);
            Report.AppendLine("  HUD " + names[i] + ": x=" + box.x.ToString("0") + " y=" + box.y.ToString("0") +
                              " w=" + box.width.ToString("0") + " h=" + box.height.ToString("0") +
                              " parent=" + (rect.parent != null ? rect.parent.name : "none"));

            if (box.x < 0f || box.y < 0f)
            {
                Failures.Add(label + ": HUD '" + names[i] + "' starts outside the top-left corner");
            }

            if (rect.parent == null || rect.parent.GetComponent<SafeArea>() == null)
            {
                Failures.Add(label + ": HUD '" + names[i] + "' is not inside the safe area");
            }
        }

        for (int i = 0; i < rects.Count; i++)
        {
            for (int j = i + 1; j < rects.Count; j++)
            {
                if (!rects[i].Overlaps(rects[j])) { continue; }

                Failures.Add(label + ": HUD rows " + names[i] + " and " + names[j] + " overlap");
            }
        }
    }

    private static RectTransform FindRect(string name)
    {
        foreach (RectTransform rect in Object.FindObjectsOfType<RectTransform>(true))
        {
            if (rect.name == name) { return rect; }
        }

        return null;
    }

    private static bool IsBlocked(Vector2 position)
    {
        // At runtime the physics scene is live, so this is what the player
        // actually collides with.
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.25f);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.isTrigger) { continue; }
            if (hit.GetComponentInParent<PlayerController>() != null) { continue; }
            if (hit.transform.root.name == "Camera") { continue; }

            return true;
        }

        return false;
    }

    private static void ReportSingleton<T>(string label, string name) where T : Object
    {
        T[] found = Object.FindObjectsOfType<T>();
        Report.AppendLine("  " + name + ": " + found.Length);

        if (found.Length == 0) { Failures.Add(label + ": " + name + " is missing"); }
        if (found.Length > 1) { Failures.Add(label + ": " + found.Length + " copies of " + name); }
    }

    private static void ReportNamed(string label, string name)
    {
        int count = 0;

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.name == name) { count++; }
        }

        Report.AppendLine("  '" + name + "': " + count);

        if (count == 0) { Failures.Add(label + ": HUD element '" + name + "' not found"); }
    }
}
