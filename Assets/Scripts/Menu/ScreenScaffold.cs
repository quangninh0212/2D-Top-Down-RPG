using UnityEngine;
using UnityEngine.UI;

// The frame every stand-alone screen shares: its own camera, a canvas, the
// gradient backdrop, a heading and a back button. Written once here so the
// progress, achievements and settings screens cannot drift apart.
public static class ScreenScaffold
{
    public static RectTransform Build(Transform host, string canvasName, string heading,
                                      Color top, Color bottom)
    {
        PixelUI.EnsureEventSystem();
        EnsureCamera(bottom);

        Canvas canvas = PixelUI.CreateCanvas(canvasName, host, 0);

        Image backdrop = PixelUI.NewImage("Backdrop", canvas.transform);
        PixelUI.Stretch(backdrop.rectTransform);
        backdrop.sprite = MenuArt.VerticalGradient(64, top, bottom);
        backdrop.raycastTarget = false;

        RectTransform safeArea = PixelUI.CreateSafeArea(canvas.transform, 24f);

        // A screen whose sheet already carries its own title passes none here,
        // rather than showing the same words twice.
        if (!string.IsNullOrEmpty(heading))
        {
            Text title = PixelUI.NewTitle("Heading", safeArea, heading, 72);
            title.color = PixelUI.Gold;
            title.rectTransform.sizeDelta = new Vector2(1600f, 90f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 430f);
        }

        return safeArea;
    }

    // Every screen is entered from somewhere else, so the way back is part of
    // the frame rather than something each screen remembers to add.
    public static Button AddBackButton(RectTransform safeArea)
    {
        return PixelUI.NewButton("Back", safeArea, "QUAY LẠI", new Vector2(420f, 84f),
                                 new Vector2(0f, -450f), SceneFlow.ReturnFromScreen);
    }

    private static void EnsureCamera(Color background)
    {
        if (Camera.main != null) { return; }

        GameObject cameraGO = new GameObject("Screen Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";

        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.orthographic = true;
    }

    // A label on the left and a value on the right, the row shape used by every
    // table in the game.
    public static Text AddRow(Transform parent, string name, string label, string value,
                              float y, float width, Color valueColour)
    {
        Text labelText = PixelUI.NewBody(name + "Label", parent, label, 34);
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.rectTransform.sizeDelta = new Vector2(width * 0.62f, 50f);
        labelText.rectTransform.anchoredPosition = new Vector2(-width * 0.5f + width * 0.31f + 20f, y);
        labelText.color = PixelUI.Cream;

        Text valueText = PixelUI.NewBody(name + "Value", parent, value, 34);
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.rectTransform.sizeDelta = new Vector2(width * 0.34f, 50f);
        valueText.rectTransform.anchoredPosition = new Vector2(width * 0.5f - width * 0.17f - 20f, y);
        valueText.color = valueColour;

        return valueText;
    }
}
