using UnityEngine;
using UnityEngine.UI;

// End of the run. The totals shown here come from the finished SaveData; the
// records come from ProfileStats, which survives runs.
public class VictoryScreenController : MonoBehaviour
{
    private void Awake()
    {
        PixelUI.EnsureEventSystem();
        EnsureCamera();
        Build();
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null) { return; }

        GameObject cameraGO = new GameObject("Victory Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";

        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.03f, 0.07f);
        camera.orthographic = true;
    }

    private void Build()
    {
        Canvas canvas = PixelUI.CreateCanvas("VictoryCanvas", transform, 0);

        Image backdrop = PixelUI.NewImage("Backdrop", canvas.transform);
        PixelUI.Stretch(backdrop.rectTransform);
        backdrop.sprite = MenuArt.VerticalGradient(64,
            new Color(0.03f, 0.02f, 0.06f), new Color(0.16f, 0.10f, 0.22f));
        backdrop.raycastTarget = false;

        RectTransform safeArea = PixelUI.CreateSafeArea(canvas.transform, 24f);

        PixelUI.NewTitle("Game", safeArea, "SOULBOUND GATE", 56)
               .rectTransform.anchoredPosition = new Vector2(0f, 330f);

        Text title = PixelUI.NewTitle("Victory", safeArea, "VICTORY!", 118);
        title.color = new Color(1f, 0.87f, 0.45f);
        title.rectTransform.sizeDelta = new Vector2(1600f, 150f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 215f);

        SaveData data = GameSaveManager.Instance != null ? GameSaveManager.Instance.Data : new SaveData();

        // Coming back from the achievements or progress screen rebuilds this
        // one from scratch, and a cold boot into it has no run in memory at
        // all. The recorded run then stands in for the live numbers.
        int gold = data.playTime > 0f ? data.gold : ProfileStats.LastGold;
        float playTime = data.playTime > 0f ? data.playTime : ProfileStats.LastTime;

        RectTransform panel = PixelUI.NewPanel("Stats", safeArea, new Vector2(860f, 300f));
        ((RectTransform)panel.parent).anchoredPosition = new Vector2(0f, -10f);

        AddRow(panel, "Tổng vàng thu được", gold.ToString(), 96f);
        AddRow(panel, "Thời gian hoàn thành", ProfileStats.FormatTime(playTime), 32f);
        AddRow(panel, "Thời gian tốt nhất",
               ProfileStats.HasBestTime ? ProfileStats.FormatTime(ProfileStats.BestTime) : "--:--", -32f);
        AddRow(panel, "Vàng cao nhất", ProfileStats.BestGold.ToString(), -96f);

        BuildButtons(safeArea);
    }

    // Four ways out, two rows of two. Replay and Home start something new; the
    // other two open a screen of their own and come back here afterwards.
    private static void BuildButtons(RectTransform safeArea)
    {
        Vector2 size = new Vector2(430f, 94f);

        PixelUI.NewButton("Replay", safeArea, "CHƠI LẠI", size, new Vector2(-240f, -270f),
            () => SceneFlow.RestartFromScratch());

        PixelUI.NewButton("Home", safeArea, "TRANG CHỦ", size, new Vector2(240f, -270f),
            () => SceneFlow.GoToMainMenu());

        PixelUI.NewButton("Achievements", safeArea, "THÀNH TÍCH", size, new Vector2(-240f, -380f),
            () => SceneFlow.GoToScreen(GameScenes.Achievements, GameScenes.Victory));

        PixelUI.NewButton("Progress", safeArea, "TIẾN TRÌNH", size, new Vector2(240f, -380f),
            () => SceneFlow.GoToScreen(GameScenes.Progress, GameScenes.Victory));
    }

    // The screens this one leads to, in layout order. The smoke test reads this
    // so a lost destination cannot pass unnoticed.
    public static readonly string[] Destinations =
    {
        GameScenes.Scene1, GameScenes.MainMenu, GameScenes.Achievements, GameScenes.Progress
    };

    private static void AddRow(Transform parent, string label, string value, float y)
    {
        Text labelText = PixelUI.NewBody(label, parent, label, 34);
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.rectTransform.sizeDelta = new Vector2(520f, 50f);
        labelText.rectTransform.anchoredPosition = new Vector2(-170f, y);
        labelText.color = PixelUI.Cream;

        Text valueText = PixelUI.NewBody(label + "Value", parent, value, 38);
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.rectTransform.sizeDelta = new Vector2(280f, 50f);
        valueText.rectTransform.anchoredPosition = new Vector2(250f, y);
        valueText.color = PixelUI.Gold;
    }
}
