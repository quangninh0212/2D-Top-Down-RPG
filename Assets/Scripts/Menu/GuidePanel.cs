using UnityEngine;
using UnityEngine.UI;

// The how-to-play sheet. Two columns of short entries so it fits a landscape
// phone without scrolling.
public class GuidePanel : MonoBehaviour
{
    private struct Entry
    {
        public string Title;
        public string Body;

        public Entry(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }

    private static readonly Entry[] Entries =
    {
        new Entry("JOYSTICK", "Di chuyển nhân vật."),
        new Entry("TẤN CÔNG", "Tấn công bằng vũ khí đang trang bị. Giữ nút để đánh liên tục."),
        new Entry("LƯỚT (DASH)", "Lướt nhanh để né đòn. Mỗi lần Dash tiêu tốn 1 Stamina."),
        new Entry("ĐỔI VŨ KHÍ", "Đổi giữa các vũ khí đã sở hữu."),
        new Entry("HEALTH", "Nếu Health về 0, lượt chơi kết thúc và dữ liệu lưu bị xóa."),
        new Entry("STAMINA", "Tự hồi lại sau một khoảng thời gian."),
        new Entry("GOLD", "Thu thập từ quái vật và dùng tại Cửa hàng."),
        new Entry("MỤC TIÊU", "Tiêu diệt toàn bộ quái vật trong màn để mở cổng sang khu vực tiếp theo.")
    };

    private CanvasGroup group;

    public bool IsOpen
    {
        get { return group != null && group.gameObject.activeSelf; }
    }

    public void Build()
    {
        group = PixelUI.NewFullScreenGroup("GuideRoot", transform);

        Image shade = PixelUI.NewImage("Shade", group.transform);
        PixelUI.Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0f, 0f, 0.8f);

        RectTransform panel = PixelUI.NewPanel("Panel", group.transform, new Vector2(1380f, 720f));

        PixelUI.NewTitle("Heading", panel, "HƯỚNG DẪN", 56)
               .rectTransform.anchoredPosition = new Vector2(0f, 288f);

        for (int i = 0; i < Entries.Length; i++)
        {
            bool rightColumn = i >= Entries.Length / 2;
            int row = rightColumn ? i - Entries.Length / 2 : i;

            float x = rightColumn ? 340f : -340f;
            float y = 180f - row * 118f;

            AddEntry(panel, Entries[i], new Vector2(x, y));
        }

        PixelUI.NewButton("Close", panel, "ĐÓNG", new Vector2(360f, 84f), new Vector2(0f, -290f), Close);

        PixelUI.SetGroupVisible(group, false);
    }

    private static void AddEntry(Transform parent, Entry entry, Vector2 position)
    {
        Text title = PixelUI.NewBody(entry.Title, parent, entry.Title, 32);
        title.alignment = TextAnchor.UpperLeft;
        title.fontStyle = FontStyle.Bold;
        title.rectTransform.sizeDelta = new Vector2(600f, 40f);
        title.rectTransform.anchoredPosition = position;
        title.color = PixelUI.Gold;

        Text body = PixelUI.NewBody(entry.Title + "Body", parent, entry.Body, 26);
        body.alignment = TextAnchor.UpperLeft;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.rectTransform.sizeDelta = new Vector2(600f, 70f);
        body.rectTransform.anchoredPosition = position + new Vector2(0f, -46f);
        body.color = PixelUI.Cream;
    }

    public void Open()
    {
        PixelUI.SetGroupVisible(group, true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        PixelUI.SetGroupVisible(group, false);
    }
}
