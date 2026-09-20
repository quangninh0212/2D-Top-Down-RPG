using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shown over the gameplay screen once the death animation has played: a
// message, a red pulse behind it, a sting, a summary of the lost run, and four
// buttons that each lead somewhere else. By the time this appears the run's
// save file is already gone, so nothing here offers Continue.
public class GameOverUI : MonoBehaviour
{
    private CanvasGroup group;
    private Image shade;
    private Image vignette;
    private Text title;
    private Text summary;

    public bool IsVisible
    {
        get { return group != null && group.gameObject.activeSelf; }
    }

    // The screens the buttons lead to, in the order they are laid out. Read by
    // the smoke test so the navigation cannot silently lose a destination.
    public static readonly string[] Destinations =
    {
        GameScenes.Scene1, GameScenes.MainMenu, GameScenes.Progress, GameScenes.Settings
    };

    public void Build()
    {
        PixelUI.Stretch((RectTransform)transform);

        group = PixelUI.NewFullScreenGroup("GameOverRoot", transform);

        shade = PixelUI.NewImage("Shade", group.transform);
        PixelUI.Stretch(shade.rectTransform);
        shade.color = new Color(0.05f, 0.01f, 0.02f, 0.88f);

        // A slow red pulse behind the title, so the screen is not simply a
        // static caption on a dark rectangle.
        vignette = PixelUI.NewImage("Vignette", group.transform);
        PixelUI.Stretch(vignette.rectTransform);
        vignette.rectTransform.offsetMin = new Vector2(-200f, -200f);
        vignette.rectTransform.offsetMax = new Vector2(200f, 200f);
        vignette.sprite = PixelUI.Glow;
        vignette.color = new Color(0.75f, 0.10f, 0.12f, 0.22f);
        vignette.raycastTarget = false;

        title = PixelUI.NewTitle("Title", group.transform, "GAME OVER", 110);
        title.color = new Color(0.87f, 0.24f, 0.26f);
        title.rectTransform.sizeDelta = new Vector2(1400f, 150f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 250f);

        Text note = PixelUI.NewBody("Note", group.transform, "Lượt chơi đã kết thúc. Dữ liệu lưu đã bị xóa.", 34);
        note.color = PixelUI.Muted;
        note.rectTransform.sizeDelta = new Vector2(1400f, 60f);
        note.rectTransform.anchoredPosition = new Vector2(0f, 160f);

        summary = PixelUI.NewBody("Summary", group.transform, "", 34);
        summary.color = PixelUI.Cream;
        summary.rectTransform.sizeDelta = new Vector2(1400f, 60f);
        summary.rectTransform.anchoredPosition = new Vector2(0f, 96f);

        BuildButtons();

        Hide();
    }

    // Two rows of two. Every one of them leaves this screen: the first two
    // start somewhere new, the other two open a screen of their own.
    private void BuildButtons()
    {
        Vector2 size = new Vector2(420f, 92f);

        PixelUI.NewButton("Retry", group.transform, "CHƠI LẠI", size, new Vector2(-230f, -20f), OnRetry);
        PixelUI.NewButton("Home", group.transform, "TRANG CHỦ", size, new Vector2(230f, -20f), OnHome);
        PixelUI.NewButton("Progress", group.transform, "TIẾN TRÌNH", size, new Vector2(-230f, -140f), OnProgress);
        PixelUI.NewButton("Settings", group.transform, "CÀI ĐẶT", size, new Vector2(230f, -140f), OnSettings);
    }

    public void Show()
    {
        if (group == null) { return; }

        MobileInput.ResetAll();
        PixelUI.SetGroupVisible(group, true);
        transform.SetAsLastSibling();

        summary.text = SummaryText();

        AudioManager.PlaySfx(GameSfx.GameOver);

        StopAllCoroutines();
        StartCoroutine(EntranceRoutine());
    }

    public void Hide()
    {
        StopAllCoroutines();
        PixelUI.SetGroupVisible(group, false);
    }

    // How far the lost run got. Pulled from the save data, which still holds
    // the run's numbers even though its file has been deleted.
    private static string SummaryText()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save == null) { return ""; }

        SaveData data = save.Data;
        int level = GameScenes.LevelNumberOf(data.currentScene);
        if (level < 1) { level = 1; }

        return "Màn " + level + " / " + LevelCatalog.Count +
               "     Vàng: " + data.gold +
               "     Thời gian: " + ProfileStats.FormatTime(data.playTime);
    }

    // The title drops in and settles, the glow breathes underneath. Unscaled
    // time throughout: death leaves the world paused behind this screen.
    private IEnumerator EntranceRoutine()
    {
        const float duration = 0.45f;

        float elapsed = 0f;
        Vector3 from = new Vector3(1.6f, 1.6f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            title.transform.localScale = Vector3.Lerp(from, Vector3.one, t * t);
            group.alpha = t;

            yield return null;
        }

        title.transform.localScale = Vector3.one;
        group.alpha = 1f;

        while (true)
        {
            float pulse = 0.18f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 1.6f)) * 0.16f;
            vignette.color = new Color(0.75f, 0.10f, 0.12f, pulse);

            yield return null;
        }
    }

    // ----- buttons --------------------------------------------------------

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

    // The run is over, so both screens hand the player back to the main menu
    // rather than to the corpse they were standing over.
    private void OnProgress()
    {
        Hide();
        SceneFlow.GoToScreen(GameScenes.Progress, GameScenes.MainMenu);
    }

    private void OnSettings()
    {
        Hide();
        SceneFlow.GoToScreen(GameScenes.Settings, GameScenes.MainMenu);
    }
}
