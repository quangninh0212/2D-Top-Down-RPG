using UnityEngine;
using UnityEngine.UI;

// Lays out the health, stamina and gold readouts.
//
// These live in UICanvas.prefab and were positioned by hand for a windowed PC
// view, with no safe-area handling. Editing the prefab is not enough: the scenes
// carry instances whose transforms override the prefab, so the values have to be
// applied to the live objects. Doing it here means one definition of the layout
// that always wins.
public static class HudLayout
{
    private const float Left = 16f;
    private const float Top = -14f;
    private const float RowHeight = 52f;

    public static void Apply()
    {
        UIFade canvasOwner = UIFade.Instance;
        if (canvasOwner == null) { return; }

        Transform canvas = canvasOwner.transform;

        ConfigureScaler(canvas);

        RectTransform safeArea = EnsureSafeArea(canvas);
        if (safeArea == null) { return; }

        Adopt(canvas, safeArea, "Heart Container");
        Adopt(canvas, safeArea, "Stamina Container");
        Adopt(canvas, safeArea, "Gold Coin Container");

        Place(safeArea, "Heart Container", new Vector2(Left, Top), new Vector2(320f, 44f));
        Place(safeArea, "Heart Container/Heart Image", new Vector2(0f, 0f), new Vector2(44f, 44f));
        Place(safeArea, "Heart Container/Health Slider", new Vector2(56f, -5f), new Vector2(240f, 34f));

        Place(safeArea, "Stamina Container", new Vector2(Left, Top - RowHeight), new Vector2(320f, 40f));

        Place(safeArea, "Gold Coin Container", new Vector2(Left, Top - RowHeight * 2f), new Vector2(320f, 44f));
        Place(safeArea, "Gold Coin Container/Gold Coin Image", new Vector2(0f, 0f), new Vector2(40f, 40f));
        Place(safeArea, "Gold Coin Container/Gold Amount Text", new Vector2(52f, -2f), new Vector2(180f, 40f));

        ConfigureStaminaGrid(safeArea);
    }

    // The gameplay overlay matches screen height. The HUD canvas used to match
    // width, so on a tall-aspect phone the two disagreed by a quarter and the
    // readouts drifted out of the corner.
    private static void ConfigureScaler(Transform canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) { return; }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = PixelUI.ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
    }

    private static RectTransform EnsureSafeArea(Transform canvas)
    {
        Transform existing = canvas.Find("HUD Safe Area");
        if (existing != null) { return existing as RectTransform; }

        GameObject go = new GameObject("HUD Safe Area", typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        go.AddComponent<SafeArea>();

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Behind the fade overlay, which must stay on top of the HUD.
        go.transform.SetAsFirstSibling();

        return rect;
    }

    private static void Adopt(Transform canvas, RectTransform safeArea, string name)
    {
        Transform target = safeArea.Find(name);
        if (target != null) { return; }

        target = canvas.Find(name);
        if (target == null) { return; }

        target.SetParent(safeArea, false);
    }

    // Anchor and pivot both at the top-left corner, so the position reads
    // directly as "x across, y down" from that corner on any screen shape.
    private static void Place(RectTransform parent, string path, Vector2 position, Vector2 size)
    {
        Transform target = parent.Find(path);
        if (target == null) { return; }

        RectTransform rect = target as RectTransform;
        if (rect == null) { return; }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ConfigureStaminaGrid(RectTransform safeArea)
    {
        Transform stamina = safeArea.Find("Stamina Container");
        if (stamina == null) { return; }

        GridLayoutGroup grid = stamina.GetComponent<GridLayoutGroup>();
        if (grid == null) { return; }

        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.cellSize = new Vector2(34f, 34f);
        grid.spacing = new Vector2(6f, 6f);
        grid.padding = new RectOffset(0, 0, 0, 0);
    }
}
