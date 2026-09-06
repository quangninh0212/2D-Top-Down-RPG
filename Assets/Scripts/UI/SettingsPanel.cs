using System;
using UnityEngine;
using UnityEngine.UI;

// The settings sheet, shared by the main menu and the pause menu so there is
// only one place volumes and vibration are configured.
public class SettingsPanel : MonoBehaviour
{
    private CanvasGroup group;
    private Action onClosed;

    public bool IsOpen
    {
        get { return group != null && group.gameObject.activeSelf; }
    }

    public void Build(Transform parent, Action closed)
    {
        onClosed = closed;

        group = PixelUI.NewFullScreenGroup("SettingsPanel", parent);

        Image shade = PixelUI.NewImage("Shade", group.transform);
        PixelUI.Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0f, 0f, 0.72f);

        RectTransform panel = PixelUI.NewPanel("Panel", group.transform, new Vector2(880f, 620f));

        PixelUI.NewTitle("Heading", panel, "CÀI ĐẶT", 56)
               .rectTransform.anchoredPosition = new Vector2(0f, 236f);

        AudioManager audio = AudioManager.Instance;

        BuildSlider(panel, "Âm lượng chung", 130f,
                    audio != null ? audio.MasterVolume : 1f,
                    v => { if (AudioManager.Instance != null) { AudioManager.Instance.SetMasterVolume(v); } });

        BuildSlider(panel, "Nhạc nền", 40f,
                    audio != null ? audio.MusicVolume : 0.6f,
                    v => { if (AudioManager.Instance != null) { AudioManager.Instance.SetMusicVolume(v); } });

        BuildSlider(panel, "Hiệu ứng", -50f,
                    audio != null ? audio.SfxVolume : 0.9f,
                    v => { if (AudioManager.Instance != null) { AudioManager.Instance.SetSfxVolume(v); } });

        BuildVibrationToggle(panel, -150f);

        PixelUI.NewButton("Close", panel, "ĐÓNG", new Vector2(360f, 88f), new Vector2(0f, -240f), Close);

        PixelUI.SetGroupVisible(group, false);
    }

    private void BuildSlider(Transform parent, string label, float y, float value, Action<float> onChanged)
    {
        Text text = PixelUI.NewBody(label, parent, label, 34);
        text.alignment = TextAnchor.MiddleLeft;
        text.rectTransform.sizeDelta = new Vector2(360f, 50f);
        text.rectTransform.anchoredPosition = new Vector2(-230f, y);

        Text readout = PixelUI.NewBody("Value", parent, Percent(value), 30);
        readout.rectTransform.sizeDelta = new Vector2(120f, 50f);
        readout.rectTransform.anchoredPosition = new Vector2(330f, y);
        readout.color = PixelUI.Gold;

        PixelUI.NewSlider("Slider", parent, new Vector2(400f, 26f), new Vector2(90f, y), value, v =>
        {
            readout.text = Percent(v);
            onChanged(v);
        });
    }

    private void BuildVibrationToggle(Transform parent, float y)
    {
        Text label = PixelUI.NewBody("VibrationLabel", parent, "Rung", 34);
        label.alignment = TextAnchor.MiddleLeft;
        label.rectTransform.sizeDelta = new Vector2(360f, 50f);
        label.rectTransform.anchoredPosition = new Vector2(-230f, y);

        Button toggle = null;
        toggle = PixelUI.NewButton("VibrationToggle", parent, StateLabel(), new Vector2(240f, 72f), new Vector2(190f, y), () =>
        {
            Haptics.Enabled = !Haptics.Enabled;
            if (Haptics.Enabled) { Haptics.LightTap(); }

            Text buttonLabel = toggle.GetComponentInChildren<Text>();
            if (buttonLabel != null) { buttonLabel.text = StateLabel(); }
        });
    }

    private static string StateLabel()
    {
        return Haptics.Enabled ? "BẬT" : "TẮT";
    }

    private static string Percent(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }

    public void Open()
    {
        PixelUI.SetGroupVisible(group, true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        PixelUI.SetGroupVisible(group, false);
        onClosed?.Invoke();
    }
}
