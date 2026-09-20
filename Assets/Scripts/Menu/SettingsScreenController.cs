using UnityEngine;

// The settings sheet as a screen of its own, so the game over screen can hand
// the player straight to it. The sheet itself is the same SettingsPanel the
// main menu and the pause menu use; only the frame around it differs.
public class SettingsScreenController : MonoBehaviour
{
    private SettingsPanel settings;

    private void Awake()
    {
        // No heading of its own: the settings sheet below already has one.
        RectTransform safeArea = ScreenScaffold.Build(transform, "SettingsCanvas", "",
            new Color(0.03f, 0.05f, 0.05f), new Color(0.10f, 0.18f, 0.18f));

        AudioManager.EnsureExists();

        GameObject host = new GameObject("SettingsHost", typeof(RectTransform));
        host.transform.SetParent(safeArea.parent, false);
        PixelUI.Stretch((RectTransform)host.transform);

        settings = host.AddComponent<SettingsPanel>();

        // Closing the sheet is the way out of the screen: there is nothing
        // behind it to go back to.
        settings.Build(host.transform, SceneFlow.ReturnFromScreen);
        settings.Open();

        BuildAudioSwitches(safeArea);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { SceneFlow.ReturnFromScreen(); }
    }

    // The two on/off switches live in the gameplay HUD as well; repeating them
    // here means the player does not have to start a run to silence the game.
    private void BuildAudioSwitches(RectTransform safeArea)
    {
        RectTransform panel = PixelUI.NewPanel("AudioSwitches", settings.transform, new Vector2(880f, 190f));
        ((RectTransform)panel.parent).anchoredPosition = new Vector2(0f, -410f);

        AddSwitch(panel, "SfxSwitch", "Hiệu ứng âm thanh", 44f,
                  () => AudioToggles.SfxEnabled,
                  value => AudioToggles.SfxEnabled = value);

        AddSwitch(panel, "MusicSwitch", "Nhạc nền", -44f,
                  () => AudioToggles.MusicEnabled,
                  value => AudioToggles.MusicEnabled = value);
    }

    private static void AddSwitch(Transform parent, string name, string label, float y,
                                  System.Func<bool> read, System.Action<bool> write)
    {
        UnityEngine.UI.Text text = PixelUI.NewBody(name + "Label", parent, label, 34);
        text.alignment = TextAnchor.MiddleLeft;
        text.rectTransform.sizeDelta = new Vector2(420f, 50f);
        text.rectTransform.anchoredPosition = new Vector2(-190f, y);

        UnityEngine.UI.Button button = null;
        button = PixelUI.NewButton(name, parent, read() ? "BẬT" : "TẮT", new Vector2(240f, 72f),
            new Vector2(230f, y), () =>
            {
                write(!read());

                UnityEngine.UI.Text buttonLabel = button.GetComponentInChildren<UnityEngine.UI.Text>();
                if (buttonLabel != null) { buttonLabel.text = read() ? "BẬT" : "TẮT"; }
            });
    }
}
