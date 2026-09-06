using UnityEngine;
using UnityEngine.UI;

// Shown once the death animation has played. By the time this appears the run's
// save file is already gone, so both buttons lead somewhere with no Continue.
public class GameOverUI : MonoBehaviour
{
    private CanvasGroup group;

    public bool IsVisible
    {
        get { return group != null && group.gameObject.activeSelf; }
    }

    public void Build()
    {
        PixelUI.Stretch((RectTransform)transform);

        group = PixelUI.NewFullScreenGroup("GameOverRoot", transform);

        Image shade = PixelUI.NewImage("Shade", group.transform);
        PixelUI.Stretch(shade.rectTransform);
        shade.color = new Color(0.05f, 0.01f, 0.02f, 0.88f);

        Text title = PixelUI.NewTitle("Title", group.transform, "GAME OVER", 110);
        title.color = new Color(0.87f, 0.24f, 0.26f);
        title.rectTransform.sizeDelta = new Vector2(1400f, 150f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 190f);

        Text note = PixelUI.NewBody("Note", group.transform, "Lượt chơi đã kết thúc. Dữ liệu lưu đã bị xóa.", 34);
        note.color = PixelUI.Muted;
        note.rectTransform.sizeDelta = new Vector2(1400f, 60f);
        note.rectTransform.anchoredPosition = new Vector2(0f, 90f);

        PixelUI.NewButton("Retry", group.transform, "CHƠI LẠI", new Vector2(460f, 96f), new Vector2(0f, -40f), OnRetry);
        PixelUI.NewButton("Home", group.transform, "TRANG CHỦ", new Vector2(460f, 96f), new Vector2(0f, -160f), OnHome);

        Hide();
    }

    public void Show()
    {
        if (group == null) { return; }

        MobileInput.ResetAll();
        PixelUI.SetGroupVisible(group, true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        PixelUI.SetGroupVisible(group, false);
    }

    private void OnRetry()
    {
        Hide();
        SceneFlow.RestartFromScratch();
    }

    private void OnHome()
    {
        Hide();
        SceneFlow.GoToMainMenu();
    }
}
