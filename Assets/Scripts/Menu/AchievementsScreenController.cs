using UnityEngine;
using UnityEngine.UI;

// "High Achievements": the player's records, and a list of milestones that
// unlock themselves from those records. Nothing here is stored twice - every
// row is derived from ProfileStats when the screen is built.
public class AchievementsScreenController : MonoBehaviour
{
    private struct Achievement
    {
        public string Title;
        public string Detail;
        public bool Unlocked;
    }

    private void Awake()
    {
        RectTransform safeArea = ScreenScaffold.Build(transform, "AchievementsCanvas", "THÀNH TÍCH",
            new Color(0.06f, 0.03f, 0.02f), new Color(0.24f, 0.15f, 0.06f));

        BuildRecords(safeArea);
        BuildMilestones(safeArea);

        ScreenScaffold.AddBackButton(safeArea);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { SceneFlow.ReturnFromScreen(); }
    }

    private static void BuildRecords(RectTransform safeArea)
    {
        RectTransform panel = PixelUI.NewPanel("Records", safeArea, new Vector2(1000f, 250f));
        ((RectTransform)panel.parent).anchoredPosition = new Vector2(0f, 190f);

        PixelUI.NewBody("RecordsHeading", panel, "KỶ LỤC", 30)
               .rectTransform.anchoredPosition = new Vector2(0f, 95f);

        ScreenScaffold.AddRow(panel, "BestTime", "Thời gian phá đảo nhanh nhất",
            ProfileStats.HasBestTime ? ProfileStats.FormatTime(ProfileStats.BestTime) : "--:--",
            30f, 1000f, PixelUI.Gold);

        ScreenScaffold.AddRow(panel, "BestGold", "Vàng cao nhất một lượt",
            ProfileStats.BestGold.ToString(), -30f, 1000f, PixelUI.Gold);

        ScreenScaffold.AddRow(panel, "Runs", "Số lần phá đảo",
            ProfileStats.RunsCompleted.ToString(), -90f, 1000f, PixelUI.Cream);
    }

    private static void BuildMilestones(RectTransform safeArea)
    {
        RectTransform panel = PixelUI.NewPanel("Milestones", safeArea, new Vector2(1000f, 380f));
        ((RectTransform)panel.parent).anchoredPosition = new Vector2(0f, -190f);

        PixelUI.NewBody("MilestonesHeading", panel, "CỘT MỐC", 30)
               .rectTransform.anchoredPosition = new Vector2(0f, 160f);

        Achievement[] list = All();
        float y = 100f;

        for (int i = 0; i < list.Length; i++)
        {
            string name = "Milestone" + i;

            ScreenScaffold.AddRow(panel, name,
                (list[i].Unlocked ? "[x] " : "[ ] ") + list[i].Title, list[i].Detail,
                y, 1000f, list[i].Unlocked ? PixelUI.Gold : PixelUI.Muted);

            // The label greys out with its value, or a locked row reads as done.
            Transform label = panel.Find(name + "Label");
            if (label != null)
            {
                Text labelText = label.GetComponent<Text>();
                if (labelText != null) { labelText.color = list[i].Unlocked ? PixelUI.Cream : PixelUI.Muted; }
            }

            y -= 62f;
        }
    }

    // Public so the smoke test can check the unlock rules without rebuilding
    // the screen.
    public static int UnlockedCount()
    {
        Achievement[] list = All();
        int unlocked = 0;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].Unlocked) { unlocked++; }
        }

        return unlocked;
    }

    public static int TotalCount
    {
        get { return All().Length; }
    }

    private static Achievement[] All()
    {
        return new[]
        {
            new Achievement
            {
                Title = "Bước chân đầu tiên",
                Detail = "Hoàn thành màn 1",
                Unlocked = ProfileStats.HasCleared(1)
            },
            new Achievement
            {
                Title = "Người mở đường",
                Detail = "Hoàn thành cả 5 màn",
                Unlocked = ProfileStats.AllLevelsCleared
            },
            new Achievement
            {
                Title = "Kẻ diệt Soul Warden",
                Detail = "Phá đảo trò chơi",
                Unlocked = ProfileStats.RunsCompleted > 0
            },
            new Achievement
            {
                Title = "Phú hộ",
                Detail = "Đạt 100 vàng một lượt",
                Unlocked = ProfileStats.BestGold >= 100
            },
            new Achievement
            {
                Title = "Tốc hành",
                Detail = "Phá đảo dưới 10 phút",
                Unlocked = ProfileStats.HasBestTime && ProfileStats.BestTime <= 600f
            }
        };
    }
}
