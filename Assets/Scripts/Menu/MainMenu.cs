using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The whole front end - splash, home screen, settings and loading bar - built
// from code so the menu scene only has to hold this one component and no
// imported art. Layout is authored against a 1920x1080 canvas and scaled from
// there, so it holds up on a phone and on a desktop window alike.
public class MainMenu : MonoBehaviour
{
    public const string MenuSceneName = "MainMenu";

    private const string FirstGameplayScene = "Scene1";
    private const string VolumeKey = "settings.volume";

    private const float SplashHold = 1.5f;
    private const float MinimumLoadTime = 1.8f;

    private RectTransform root;
    private CanvasGroup splashGroup;
    private CanvasGroup homeGroup;
    private CanvasGroup settingsGroup;
    private CanvasGroup loadingGroup;
    private Image loadingFill;
    private Text loadingPercent;
    private Button continueButton;

    private Font titleFont;
    private Font bodyFont;

    private Sprite panelSprite;
    private Sprite buttonSprite;
    private Sprite barSprite;
    private Sprite ringSprite;
    private Sprite discSprite;

    private void Awake()
    {
        EnsureCamera();
        EnsureEventSystem();

        titleFont = Resources.Load<Font>("Fonts/Gixel");
        bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (titleFont == null) { titleFont = bodyFont; }

        panelSprite = MenuArt.RoundedRect(96, 96, 22f, Color.white);
        buttonSprite = MenuArt.RoundedRect(96, 96, 16f, Color.white);
        barSprite = MenuArt.RoundedRect(64, 64, 18f, Color.white);
        ringSprite = MenuArt.Disc(192, 9f, new Color(1f, 1f, 1f, 0f), Color.white);
        discSprite = MenuArt.Disc(96, 4f, Color.white, Color.white);

        BuildCanvas();
        BuildBackground();

        splashGroup = BuildSplash();
        homeGroup = BuildHome();
        settingsGroup = BuildSettings();
        loadingGroup = BuildLoading();

        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);

        SetVisible(splashGroup, true);
        SetVisible(homeGroup, false);
        SetVisible(settingsGroup, false);
        SetVisible(loadingGroup, false);
    }

    private void Start()
    {
        StartCoroutine(OpeningRoutine());
    }

    // Splash first, then the home screen fades up behind it.
    private IEnumerator OpeningRoutine()
    {
        splashGroup.alpha = 0f;
        yield return Fade(splashGroup, 1f, 0.6f);
        yield return new WaitForSecondsRealtime(SplashHold);
        yield return Fade(splashGroup, 0f, 0.5f);

        SetVisible(splashGroup, false);
        SetVisible(homeGroup, true);
        homeGroup.alpha = 0f;
        yield return Fade(homeGroup, 1f, 0.5f);
    }

    // ----- scene plumbing -------------------------------------------------

    private static void EnsureCamera()
    {
        if (Camera.main != null) { return; }

        GameObject cameraGO = new GameObject("Menu Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";

        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.06f, 0.05f);
        camera.orthographic = true;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) { return; }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void BuildCanvas()
    {
        GameObject canvasGO = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasGO.GetComponent<RectTransform>();
    }

    private void BuildBackground()
    {
        GameObject backgroundGO = new GameObject("Background", typeof(RectTransform));
        backgroundGO.transform.SetParent(root, false);

        RectTransform backgroundRect = (RectTransform)backgroundGO.transform;
        Stretch(backgroundRect);

        // Laid out against the canvas size, which is only correct after the
        // layout system has run once on the freshly built hierarchy.
        Canvas.ForceUpdateCanvases();

        backgroundGO.AddComponent<MenuBackground>().Build(backgroundRect);
    }

    // ----- panels ---------------------------------------------------------

    private CanvasGroup BuildSplash()
    {
        CanvasGroup group = NewPanel("Splash");

        RectTransform emblem = BuildLogo(group.transform, 300f);
        emblem.anchoredPosition = new Vector2(0f, 90f);

        Text title = NewText("Title", group.transform, "SOULBOUND GATE", titleFont, 88);
        title.rectTransform.anchoredPosition = new Vector2(0f, -140f);
        title.rectTransform.sizeDelta = new Vector2(1400f, 120f);
        title.color = new Color(1f, 0.93f, 0.76f);

        Text hint = NewText("Hint", group.transform, "Đồ án môn học", bodyFont, 30);
        hint.rectTransform.anchoredPosition = new Vector2(0f, -230f);
        hint.rectTransform.sizeDelta = new Vector2(1000f, 60f);
        hint.color = new Color(1f, 1f, 1f, 0.45f);

        return group;
    }

    private CanvasGroup BuildHome()
    {
        CanvasGroup group = NewPanel("Home");

        RectTransform emblem = BuildLogo(group.transform, 190f);
        emblem.anchorMin = new Vector2(0.5f, 1f);
        emblem.anchorMax = new Vector2(0.5f, 1f);
        emblem.anchoredPosition = new Vector2(0f, -180f);

        Text title = NewText("Title", group.transform, "SOULBOUND GATE", titleFont, 68);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -330f);
        title.rectTransform.sizeDelta = new Vector2(1400f, 100f);
        title.color = new Color(1f, 0.93f, 0.76f);

        float y = -40f;
        NewButton("NewGameButton", group.transform, "CHƠI MỚI", new Vector2(0f, y), OnNewGame);

        continueButton = NewButton("ContinueButton", group.transform, "TIẾP TỤC", new Vector2(0f, y - 118f), OnContinue);
        if (!GameProgress.HasSave)
        {
            continueButton.interactable = false;
            continueButton.GetComponentInChildren<Text>().color = new Color(1f, 1f, 1f, 0.3f);
        }

        NewButton("SettingsButton", group.transform, "CÀI ĐẶT", new Vector2(0f, y - 236f), () => ShowSettings(true));
        NewButton("QuitButton", group.transform, "THOÁT", new Vector2(0f, y - 354f), OnQuit);

        return group;
    }

    private CanvasGroup BuildSettings()
    {
        CanvasGroup group = NewPanel("Settings");

        Image shade = NewImage("Shade", group.transform);
        Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0f, 0f, 0.6f);

        Image panel = NewImage("Panel", group.transform);
        panel.sprite = panelSprite;
        panel.type = Image.Type.Sliced;
        panel.color = new Color(0.09f, 0.13f, 0.11f, 0.97f);
        panel.rectTransform.sizeDelta = new Vector2(760f, 420f);

        // Vietnamese text uses the built-in font: the pixel face is ASCII-only
        // and would drop every diacritic.
        Text heading = NewText("Heading", panel.transform, "CÀI ĐẶT", bodyFont, 46);
        heading.rectTransform.anchoredPosition = new Vector2(0f, 140f);
        heading.rectTransform.sizeDelta = new Vector2(600f, 70f);
        heading.color = new Color(1f, 0.93f, 0.76f);

        Text label = NewText("VolumeLabel", panel.transform, "Âm lượng", bodyFont, 32);
        label.rectTransform.anchoredPosition = new Vector2(-230f, 40f);
        label.rectTransform.sizeDelta = new Vector2(280f, 50f);
        label.alignment = TextAnchor.MiddleLeft;

        BuildVolumeSlider(panel.transform, new Vector2(60f, 40f));

        NewButton("CloseButton", panel.transform, "ĐÓNG", new Vector2(0f, -120f), () => ShowSettings(false));

        return group;
    }

    private CanvasGroup BuildLoading()
    {
        CanvasGroup group = NewPanel("Loading");

        Image shade = NewImage("Shade", group.transform);
        Stretch(shade.rectTransform);
        shade.color = new Color(0.02f, 0.04f, 0.03f, 0.92f);

        Text heading = NewText("Heading", group.transform, "ĐANG TẢI...", bodyFont, 54);
        heading.rectTransform.anchoredPosition = new Vector2(0f, 90f);
        heading.rectTransform.sizeDelta = new Vector2(1200f, 90f);
        heading.color = new Color(1f, 0.93f, 0.76f);

        Image track = NewImage("BarTrack", group.transform);
        track.sprite = barSprite;
        track.type = Image.Type.Sliced;
        track.color = new Color(1f, 1f, 1f, 0.12f);
        track.rectTransform.sizeDelta = new Vector2(900f, 38f);
        track.rectTransform.anchoredPosition = new Vector2(0f, -10f);

        loadingFill = NewImage("BarFill", track.transform);
        Stretch(loadingFill.rectTransform);
        loadingFill.sprite = barSprite;
        loadingFill.type = Image.Type.Filled;
        loadingFill.fillMethod = Image.FillMethod.Horizontal;
        loadingFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        loadingFill.fillAmount = 0f;
        loadingFill.color = new Color(1f, 0.78f, 0.35f);

        loadingPercent = NewText("Percent", group.transform, "0%", bodyFont, 30);
        loadingPercent.rectTransform.anchoredPosition = new Vector2(0f, -70f);
        loadingPercent.rectTransform.sizeDelta = new Vector2(400f, 50f);
        loadingPercent.color = new Color(1f, 1f, 1f, 0.6f);

        return group;
    }

    // A sword inside a ring, assembled from the generated shapes rather than
    // drawn pixel by pixel, so the proportions stay easy to adjust.
    private RectTransform BuildLogo(Transform parent, float size)
    {
        GameObject logoGO = new GameObject("Logo", typeof(RectTransform));
        logoGO.transform.SetParent(parent, false);

        RectTransform logo = (RectTransform)logoGO.transform;
        logo.sizeDelta = new Vector2(size, size);

        Color gold = new Color(0.95f, 0.76f, 0.35f);
        Color steel = new Color(0.88f, 0.92f, 0.95f);

        Image ring = NewImage("Ring", logo);
        Stretch(ring.rectTransform);
        ring.sprite = ringSprite;
        ring.color = gold;

        float unit = size / 300f;

        AddShape(logo, "Blade", steel, new Vector2(26f, 150f) * unit, new Vector2(0f, 34f) * unit, 0f, buttonSprite);
        AddShape(logo, "Tip", steel, new Vector2(24f, 24f) * unit, new Vector2(0f, 116f) * unit, 45f, buttonSprite);
        AddShape(logo, "Guard", gold, new Vector2(130f, 18f) * unit, new Vector2(0f, -46f) * unit, 0f, buttonSprite);
        AddShape(logo, "Grip", new Color(0.42f, 0.27f, 0.18f), new Vector2(18f, 46f) * unit, new Vector2(0f, -76f) * unit, 0f, buttonSprite);
        AddShape(logo, "Pommel", gold, new Vector2(28f, 28f) * unit, new Vector2(0f, -106f) * unit, 0f, discSprite);

        return logo;
    }

    private static void AddShape(Transform parent, string name, Color colour, Vector2 size, Vector2 position, float rotation, Sprite sprite)
    {
        Image image = NewImage(name, parent);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = colour;
        image.raycastTarget = false;

        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private void BuildVolumeSlider(Transform parent, Vector2 position)
    {
        GameObject sliderGO = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(parent, false);

        RectTransform sliderRect = (RectTransform)sliderGO.transform;
        sliderRect.sizeDelta = new Vector2(380f, 34f);
        sliderRect.anchoredPosition = position;

        Image track = NewImage("Track", sliderRect);
        Stretch(track.rectTransform);
        track.sprite = barSprite;
        track.type = Image.Type.Sliced;
        track.color = new Color(1f, 1f, 1f, 0.15f);

        Image fill = NewImage("Fill", sliderRect);
        fill.sprite = barSprite;
        fill.type = Image.Type.Sliced;
        fill.color = new Color(1f, 0.78f, 0.35f);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(1f, 1f);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        Image handle = NewImage("Handle", sliderRect);
        handle.sprite = discSprite;
        handle.color = new Color(1f, 0.93f, 0.76f);
        handle.rectTransform.sizeDelta = new Vector2(40f, 40f);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = PlayerPrefs.GetFloat(VolumeKey, 1f);
        slider.onValueChanged.AddListener(OnVolumeChanged);
    }

    // ----- actions --------------------------------------------------------

    private void OnNewGame()
    {
        GameProgress.Clear();
        StartCoroutine(LoadRoutine(FirstGameplayScene));
    }

    private void OnContinue()
    {
        StartCoroutine(LoadRoutine(GameProgress.LastScene));
    }

    private void OnQuit()
    {
        Application.Quit();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ShowSettings(bool show)
    {
        SetVisible(settingsGroup, show);
        settingsGroup.alpha = show ? 1f : 0f;
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        SetVisible(loadingGroup, true);
        loadingGroup.alpha = 0f;
        loadingFill.fillAmount = 0f;
        yield return Fade(loadingGroup, 1f, 0.3f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float shown = 0f;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            // Unity holds real progress at 0.9 until activation is allowed, and
            // these scenes load almost instantly, so the bar is also paced by
            // time - otherwise it would jump straight to full.
            float real = Mathf.Clamp01(operation.progress / 0.9f);
            float paced = Mathf.Clamp01(elapsed / MinimumLoadTime);

            shown = Mathf.MoveTowards(shown, Mathf.Min(real, paced), Time.unscaledDeltaTime * 1.5f);
            loadingFill.fillAmount = shown;
            loadingPercent.text = Mathf.RoundToInt(shown * 100f) + "%";

            if (shown >= 0.999f) { break; }

            yield return null;
        }

        operation.allowSceneActivation = true;
    }

    // ----- small UI helpers ----------------------------------------------

    private CanvasGroup NewPanel(string name)
    {
        GameObject panelGO = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        panelGO.transform.SetParent(root, false);
        Stretch((RectTransform)panelGO.transform);

        return panelGO.GetComponent<CanvasGroup>();
    }

    private static void SetVisible(CanvasGroup group, bool visible)
    {
        group.gameObject.SetActive(visible);
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private static IEnumerator Fade(CanvasGroup group, float target, float duration)
    {
        float start = group.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        group.alpha = target;
    }

    private static Image NewImage(string name, Transform parent)
    {
        GameObject imageGO = new GameObject(name, typeof(Image));
        imageGO.transform.SetParent(parent, false);

        return imageGO.GetComponent<Image>();
    }

    private static Text NewText(string name, Transform parent, string content, Font font, int fontSize)
    {
        GameObject textGO = new GameObject(name, typeof(Text));
        textGO.transform.SetParent(parent, false);

        Text text = textGO.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private Button NewButton(string name, Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(name, typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)buttonGO.transform;
        rect.sizeDelta = new Vector2(440f, 96f);
        rect.anchoredPosition = position;

        Image image = buttonGO.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.13f, 0.20f, 0.16f, 0.95f);

        Text text = NewText("Label", buttonGO.transform, label, bodyFont, 36);
        Stretch(text.rectTransform);
        text.color = new Color(1f, 0.95f, 0.85f);

        Button button = buttonGO.GetComponent<Button>();
        ColorBlock colours = button.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colours.pressedColor = new Color(1f, 0.82f, 0.45f);
        colours.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        colours.fadeDuration = 0.08f;
        button.colors = colours;
        button.onClick.AddListener(onClick);

        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
