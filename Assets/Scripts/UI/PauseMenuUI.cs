using UnityEngine;
using UnityEngine.UI;

// The pause sheet. Freezing the game is what makes this different from every
// other panel, so it owns the timeScale and is careful to always put it back.
public class PauseMenuUI : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    private CanvasGroup group;
    private SettingsPanel settings;
    private Button saveButton;
    private Text saveLabel;

    public void Build()
    {
        PixelUI.Stretch((RectTransform)transform);
        group = PixelUI.NewFullScreenGroup("PauseRoot", transform);

        Image shade = PixelUI.NewImage("Shade", group.transform);
        PixelUI.Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform panel = PixelUI.NewPanel("Panel", group.transform, new Vector2(720f, 760f));

        PixelUI.NewTitle("Heading", panel, "TẠM DỪNG", 62)
               .rectTransform.anchoredPosition = new Vector2(0f, 300f);

        Vector2 buttonSize = new Vector2(500f, 92f);

        PixelUI.NewButton("Resume", panel, "CHƠI TIẾP", buttonSize, new Vector2(0f, 170f), Close);

        saveButton = PixelUI.NewButton("Save", panel, "LƯU GAME", buttonSize, new Vector2(0f, 60f), OnSave);
        saveLabel = saveButton.GetComponentInChildren<Text>();

        PixelUI.NewButton("Settings", panel, "CÀI ĐẶT", buttonSize, new Vector2(0f, -50f), () => settings.Open());
        PixelUI.NewButton("Home", panel, "VỀ TRANG CHỦ", buttonSize, new Vector2(0f, -160f), OnGoHome);
        PixelUI.NewButton("Quit", panel, "THOÁT GAME", buttonSize, new Vector2(0f, -270f), OnQuit);

        settings = new GameObject("Settings", typeof(RectTransform)).AddComponent<SettingsPanel>();
        settings.transform.SetParent(transform, false);
        PixelUI.Stretch((RectTransform)settings.transform);
        settings.Build(settings.transform, null);

        PixelUI.SetGroupVisible(group, false);
        IsPaused = false;
    }

    public void Open()
    {
        if (IsPaused) { return; }

        IsPaused = true;
        Time.timeScale = 0f;

        MobileInput.ResetAll();
        if (PlayerController.Instance != null) { PlayerController.Instance.SetControlsEnabled(false); }

        RefreshSaveButton();
        PixelUI.SetGroupVisible(group, true);
        transform.SetAsLastSibling();

        AudioManager.PlaySfx(GameSfx.UiClick);
    }

    public void Close()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (settings != null && settings.IsOpen) { settings.Close(); }

        PixelUI.SetGroupVisible(group, false);

        bool playerAlive = PlayerHealth.Instance == null || !PlayerHealth.Instance.isDead;

        if (PlayerController.Instance != null && playerAlive)
        {
            PlayerController.Instance.SetControlsEnabled(true);
        }
    }

    private void OnDisable()
    {
        // Never leave the game frozen behind a panel that went away.
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }
    }

    private void RefreshSaveButton()
    {
        GameSaveManager save = GameSaveManager.Instance;
        bool canSave = save != null && save.CanSave();

        PixelUI.SetButtonEnabled(saveButton, canSave);

        if (saveLabel != null) { saveLabel.text = canSave ? "LƯU GAME" : "KHÔNG THỂ LƯU"; }
    }

    private void OnSave()
    {
        GameSaveManager save = GameSaveManager.Instance;

        if (save != null && save.SaveRun())
        {
            GameMessages.Toast("ĐÃ LƯU GAME");
            if (saveLabel != null) { saveLabel.text = "ĐÃ LƯU!"; }
        }
        else
        {
            GameMessages.Toast("KHÔNG THỂ LƯU LÚC NÀY");
        }
    }

    private void OnGoHome()
    {
        // Leaving mid-run should not lose progress, so the run is written out
        // first - but only while the player is actually alive.
        GameSaveManager save = GameSaveManager.Instance;
        if (save != null) { save.SaveRun(); }

        Close();
        SceneFlow.GoToMainMenu();
    }

    private void OnQuit()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save != null) { save.SaveRun(); }

        Close();

#if UNITY_EDITOR
        Debug.Log("[Soulbound Gate] Application.Quit does not close the Unity Editor; this exits on a device.");
#endif
        Application.Quit();
    }
}
