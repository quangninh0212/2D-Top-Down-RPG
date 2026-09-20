using UnityEngine;
using UnityEngine.UI;

// "Historical Progress": what the player has cleared, how fast, and how the
// last run ended. Everything here comes from ProfileStats, which survives a
// wiped run, plus the live save file when a run is still going.
public class ProgressScreenController : MonoBehaviour
{
    private void Awake()
    {
        RectTransform safeArea = ScreenScaffold.Build(transform, "ProgressCanvas", "TIẾN TRÌNH",
            new Color(0.03f, 0.04f, 0.08f), new Color(0.10f, 0.14f, 0.24f));

        BuildLevelTable(safeArea);
        BuildSummary(safeArea);

        // Two screens that belong together: the records live next door, and
        // hopping straight across beats going back to the menu first.
        PixelUI.NewButton("Achievements", safeArea, "THÀNH TÍCH", new Vector2(420f, 92f),
            new Vector2(230f, -400f),
            () => SceneFlow.GoToScreen(GameScenes.Achievements, SceneFlow.ScreenReturnScene));

        Button back = ScreenScaffold.AddBackButton(safeArea);
        back.GetComponent<RectTransform>().anchoredPosition = new Vector2(-230f, -400f);
    }

    private void Update()
    {
        // Android back.
        if (Input.GetKeyDown(KeyCode.Escape)) { SceneFlow.ReturnFromScreen(); }
    }

    private static void BuildLevelTable(RectTransform safeArea)
    {
        RectTransform panel = PixelUI.NewPanel("Levels", safeArea, new Vector2(1000f, 430f));
        ((RectTransform)panel.parent).anchoredPosition = new Vector2(0f, 120f);

        PixelUI.NewBody("TableHeading", panel, "CÁC CẤP ĐỘ", 30)
               .rectTransform.anchoredPosition = new Vector2(0f, 180f);

        GameSaveManager save = GameSaveManager.Instance;
        SaveData data = save != null ? save.Data : null;
        bool runActive = save != null && save.HasValidSave();

        float y = 120f;

        for (int level = 1; level <= LevelCatalog.Count; level++)
        {
            LevelInfo info = LevelCatalog.Get(level);

            string label = level + ". " + info.DisplayName;
            string value;
            Color colour;

            if (ProfileStats.HasCleared(level))
            {
                value = "HOÀN THÀNH  " + ProfileStats.FormatTime(ProfileStats.BestLevelTime(level));
                colour = PixelUI.Gold;
            }
            else if (runActive && data != null && data.highestUnlockedLevel >= level)
            {
                value = "ĐANG CHƠI";
                colour = PixelUI.Cream;
            }
            else
            {
                value = "CHƯA MỞ";
                colour = PixelUI.Muted;
            }

            ScreenScaffold.AddRow(panel, "Level" + level, label, value, y, 1000f, colour);
            y -= 62f;
        }
    }

    private static void BuildSummary(RectTransform safeArea)
    {
        RectTransform panel = PixelUI.NewPanel("Summary", safeArea, new Vector2(1000f, 250f));
        ((RectTransform)panel.parent).anchoredPosition = new Vector2(0f, -240f);

        int furthest = ProfileStats.FurthestLevelCleared;

        ScreenScaffold.AddRow(panel, "Furthest", "Cấp độ xa nhất đã qua",
            furthest > 0 ? furthest + " / " + LevelCatalog.Count : "Chưa qua cấp nào",
            80f, 1000f, PixelUI.Gold);

        ScreenScaffold.AddRow(panel, "Runs", "Số lần phá đảo",
            ProfileStats.RunsCompleted.ToString(), 20f, 1000f, PixelUI.Cream);

        ScreenScaffold.AddRow(panel, "Deaths", "Số lần thất bại",
            ProfileStats.Deaths.ToString(), -40f, 1000f, PixelUI.HealthRed);

        ScreenScaffold.AddRow(panel, "LastRun", "Lượt chơi gần nhất", LastRunText(), -100f, 1000f, PixelUI.Cream);
    }

    private static string LastRunText()
    {
        string outcome = ProfileStats.LastOutcome;

        if (outcome == ProfileStats.OutcomeWin)
        {
            return "Thắng · " + ProfileStats.FormatTime(ProfileStats.LastTime) +
                   " · " + ProfileStats.LastGold + " vàng";
        }

        if (outcome == ProfileStats.OutcomeDeath)
        {
            string where = ProfileStats.LastLevel > 0 ? "màn " + ProfileStats.LastLevel : "màn 1";
            return "Thua ở " + where + " · " + ProfileStats.FormatTime(ProfileStats.LastTime);
        }

        return "Chưa có";
    }
}
