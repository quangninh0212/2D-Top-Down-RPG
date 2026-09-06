using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Enters play mode on the main menu and reports what actually got built and in
// what draw order. The menu is constructed entirely from code at runtime, so
// nothing about it can be checked by looking at the scene file - a mistake like
// the backdrop being drawn over the buttons is only visible once it runs.
public static class MenuSmokeTest
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    // Frames to let Awake, Start, the opening fade and the first layout pass all
    // finish before anything is measured.
    private const int FramesToWait = 90;

    private static int framesSeen;
    private static bool geometryTrustworthy;

    [MenuItem("Tools/Soulbound Gate/Debug/Menu Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Keeping the domain alive across the play-mode switch is what lets the
        // callback below survive to inspect the running scene.
        PlayModeTestSettings.ApplyForTest();

        framesSeen = 0;
        EditorApplication.update += WaitThenInspect;
        EditorApplication.EnterPlaymode();
    }

    private static void WaitThenInspect()
    {
        if (!EditorApplication.isPlaying) { return; }

        if (framesSeen++ < FramesToWait) { return; }

        EditorApplication.update -= WaitThenInspect;

        Debug.Log("[MENU SMOKE]\n" + Inspect());

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

    private static string Inspect()
    {
        StringBuilder report = new StringBuilder();
        List<string> failures = new List<string>();

        Canvas canvas = FindCanvas("MenuCanvas");

        if (canvas == null)
        {
            return "FAIL: MenuCanvas was never created.";
        }

        RectTransform canvasRect = (RectTransform)canvas.transform;
        Vector2 canvasSize = canvasRect.rect.size;

        report.AppendLine("Screen " + Screen.width + "x" + Screen.height +
                          "  canvas scale " + canvas.scaleFactor.ToString("0.000") +
                          "  canvas rect " + canvasSize);

        // Batch mode has no real render surface and the canvas reports a garbage
        // rect, so pixel geometry cannot be judged here - only structure can.
        // Anything measured in world units is reported but not asserted.
        geometryTrustworthy = canvasSize.x > 1f && canvasSize.y > 1f
                              && canvasSize.y < 100000f && !float.IsNaN(canvasSize.x);

        if (!geometryTrustworthy)
        {
            report.AppendLine("NOTE: canvas rect is not usable in batch mode - " +
                              "sizes below are informational, not asserted.");
        }

        report.AppendLine();
        report.AppendLine("MenuCanvas draw order (first = furthest back):");

        int backgroundIndex = -1;
        int safeAreaIndex = -1;

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            report.AppendLine("  " + i + "  " + child.name + (child.gameObject.activeSelf ? "" : "  (inactive)"));

            if (child.name == "Background") { backgroundIndex = i; }
            if (child.name == "SafeArea") { safeAreaIndex = i; }
        }

        if (backgroundIndex < 0) { failures.Add("No Background under MenuCanvas"); }
        if (safeAreaIndex < 0) { failures.Add("No SafeArea under MenuCanvas"); }

        // The whole point: the backdrop has to be behind the menu, not over it.
        if (backgroundIndex >= 0 && safeAreaIndex >= 0 && backgroundIndex > safeAreaIndex)
        {
            failures.Add("Background (index " + backgroundIndex + ") draws OVER SafeArea (index " +
                         safeAreaIndex + ") - the menu would be invisible");
        }

        report.AppendLine();
        report.AppendLine("Menu content:");

        ReportGroup(report, failures, "Home", true);
        ReportGroup(report, failures, "NewGameConfirm", false);
        ReportGroup(report, failures, "QuitConfirm", false);

        ReportButton(report, failures, "NewGame", true);
        ReportButton(report, failures, "Continue", false);
        ReportButton(report, failures, "Shop", true);
        ReportButton(report, failures, "Guide", true);
        ReportButton(report, failures, "Settings", true);
        ReportButton(report, failures, "Quit", true);

        ReportSprite(report, failures, "HeroPreview");
        ReportSprite(report, failures, "MonsterPreview");

        ReportText(report, failures, "SOULBOUND GATE");

        report.AppendLine();
        report.AppendLine(failures.Count == 0
            ? "MENU SMOKE RESULT: all checks passed."
            : "MENU SMOKE RESULT: " + failures.Count + " problem(s):");

        foreach (string failure in failures) { report.AppendLine("  FAIL " + failure); }

        return report.ToString();
    }

    private static Canvas FindCanvas(string name)
    {
        foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>(true))
        {
            if (canvas.name == name) { return canvas; }
        }

        return null;
    }

    private static void ReportGroup(StringBuilder report, List<string> failures, string name, bool shouldBeVisible)
    {
        CanvasGroup group = FindByName<CanvasGroup>(name);

        if (group == null)
        {
            report.AppendLine("  " + name + ": MISSING");
            failures.Add(name + " group was never built");
            return;
        }

        // The home panel fades in, so any alpha above zero means it is on its way
        // up rather than hidden.
        bool visible = group.gameObject.activeInHierarchy && group.alpha > 0.01f;

        report.AppendLine("  " + name + ": active=" + group.gameObject.activeSelf +
                          " alpha=" + group.alpha.ToString("0.00") +
                          " raycasts=" + group.blocksRaycasts);

        if (shouldBeVisible && !visible) { failures.Add(name + " should be visible but is not"); }
        if (!shouldBeVisible && visible) { failures.Add(name + " should start hidden but is showing"); }
    }

    private static void ReportButton(StringBuilder report, List<string> failures, string name, bool shouldBeInteractable)
    {
        Button button = FindByName<Button>(name);

        if (button == null)
        {
            report.AppendLine("  Button " + name + ": MISSING");
            failures.Add("Button " + name + " was never built");
            return;
        }

        RectTransform rect = (RectTransform)button.transform;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        // GetWorldCorners returns bottom-left, top-left, top-right, bottom-right.
        float width = corners[2].x - corners[1].x;
        float height = corners[1].y - corners[0].y;

        report.AppendLine("  Button " + name + ": interactable=" + button.interactable +
                          " size=" + width.ToString("0") + "x" + height.ToString("0") +
                          " onScreen=" + OnScreen(corners));

        if (geometryTrustworthy)
        {
            if (width < 1f || height < 1f) { failures.Add("Button " + name + " has zero size"); }
            if (!OnScreen(corners)) { failures.Add("Button " + name + " is off screen"); }
        }

        // Continue is expected to be greyed out when there is no save.
        if (shouldBeInteractable && !button.interactable)
        {
            failures.Add("Button " + name + " is not interactable");
        }
    }

    private static void ReportSprite(StringBuilder report, List<string> failures, string holderName)
    {
        Transform holder = FindByName<Transform>(holderName);

        if (holder == null)
        {
            report.AppendLine("  " + holderName + ": MISSING");
            failures.Add(holderName + " was never built");
            return;
        }

        // The holder also carries a glow behind the character, so the character
        // image has to be picked by name rather than taking the first one found.
        Transform spriteChild = holder.Find("Sprite");
        Image image = spriteChild != null ? spriteChild.GetComponent<Image>() : null;

        string sprite = image != null && image.sprite != null ? image.sprite.name : "none";
        bool animated = image != null && image.GetComponent<SpriteSequenceAnimator>() != null;

        report.AppendLine("  " + holderName + ": sprite=" + sprite + " animated=" + animated);

        if (image == null || image.sprite == null) { failures.Add(holderName + " has no sprite"); }
    }

    private static void ReportText(StringBuilder report, List<string> failures, string expected)
    {
        foreach (Text text in Object.FindObjectsOfType<Text>(true))
        {
            if (text.text != expected) { continue; }

            report.AppendLine("  Text \"" + expected + "\": found, font=" +
                              (text.font != null ? text.font.name : "none"));
            return;
        }

        report.AppendLine("  Text \"" + expected + "\": MISSING");
        failures.Add("Title text \"" + expected + "\" was never built");
    }

    // Screen space overlay puts world corners straight into pixel coordinates.
    private static bool OnScreen(Vector3[] corners)
    {
        return corners[0].x < Screen.width && corners[2].x > 0f
               && corners[0].y < Screen.height && corners[2].y > 0f;
    }

    private static T FindByName<T>(string name) where T : Component
    {
        foreach (T candidate in Object.FindObjectsOfType<T>(true))
        {
            if (candidate.name == name) { return candidate; }
        }

        return null;
    }
}
