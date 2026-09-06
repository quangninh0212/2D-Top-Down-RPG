using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The home screen: the hero on the left, a monster on the right, the title and
// the menu down the middle, over the animated backdrop. Built entirely from
// code, so the menu scene holds nothing but this one component.
public class MainMenu : MonoBehaviour
{
    public const string MenuSceneName = GameScenes.MainMenu;

    private RectTransform root;
    private RectTransform safeArea;

    private CanvasGroup homeGroup;
    private CanvasGroup confirmGroup;
    private CanvasGroup quitConfirmGroup;

    private Button continueButton;

    private SettingsPanel settings;
    private ShopPanel shop;
    private GuidePanel guide;

    private void Awake()
    {
        EnsureCamera();
        PixelUI.EnsureEventSystem();

        BuildCanvas();
        BuildBackground();
        BuildCharacters();

        homeGroup = BuildHome();

        confirmGroup = BuildConfirm("NewGameConfirm", "Bắt đầu trò chơi mới?",
                                    "Dữ liệu lưu hiện tại sẽ bị xóa.", "ĐỒNG Ý", StartNewRun);

        quitConfirmGroup = BuildConfirm("QuitConfirm", "Thoát game?",
                                        "Tiến độ chưa lưu sẽ bị mất.", "THOÁT", QuitApplication);

        settings = NewPanelHost<SettingsPanel>("Settings");
        settings.Build(settings.transform, null);

        shop = NewPanelHost<ShopPanel>("Shop");
        shop.Build();

        guide = NewPanelHost<GuidePanel>("Guide");
        guide.Build();

        PixelUI.SetGroupVisible(confirmGroup, false);
        PixelUI.SetGroupVisible(quitConfirmGroup, false);
    }

    private void Start()
    {
        AudioManager.EnsureExists();
        AudioManager.PlayMusic(GameMusic.Menu);

        RefreshContinueButton();

        homeGroup.alpha = 0f;
        StartCoroutine(Fade(homeGroup, 1f, 0.45f));
    }

    private void Update()
    {
        // Android back closes whatever is open, and only asks about quitting
        // once the player is already at the top level.
        if (!Input.GetKeyDown(KeyCode.Escape)) { return; }

        if (settings.IsOpen) { settings.Close(); return; }
        if (shop.IsOpen) { shop.Close(); return; }
        if (guide.IsOpen) { guide.Close(); return; }

        if (confirmGroup.gameObject.activeSelf) { PixelUI.SetGroupVisible(confirmGroup, false); return; }

        // Back at the top level asks first. Quitting on a single stray press is
        // indistinguishable from the app crashing.
        if (quitConfirmGroup.gameObject.activeSelf)
        {
            PixelUI.SetGroupVisible(quitConfirmGroup, false);
            return;
        }

        OnQuit();
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

    private void BuildCanvas()
    {
        Canvas canvas = PixelUI.CreateCanvas("MenuCanvas", transform, 0);
        root = canvas.GetComponent<RectTransform>();
        safeArea = PixelUI.CreateSafeArea(canvas.transform, 20f);
    }

    private void BuildBackground()
    {
        GameObject backgroundGO = new GameObject("Background", typeof(RectTransform));
        backgroundGO.transform.SetParent(root, false);

        RectTransform backgroundRect = (RectTransform)backgroundGO.transform;
        PixelUI.Stretch(backgroundRect);

        // uGUI draws later siblings on top, and the safe area holding the whole
        // menu was created first. Without this the backdrop covers the title,
        // the buttons and the characters completely.
        backgroundRect.SetAsFirstSibling();

        // Laid out against the canvas size, which is only correct once the
        // layout system has run on the freshly built hierarchy.
        Canvas.ForceUpdateCanvases();

        backgroundGO.AddComponent<MenuBackground>().Build(backgroundRect);
    }

    private T NewPanelHost<T>(string name) where T : Component
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root, false);
        PixelUI.Stretch((RectTransform)go.transform);

        return go.AddComponent<T>();
    }

    // ----- characters -----------------------------------------------------

    // Visual only: an Image playing the idle frames, never the gameplay prefab.
    // Instantiating the real Player here would create its singletons and steal
    // input from the menu.
    private void BuildCharacters()
    {
        GameArtLibrary art = GameArtLibrary.Instance;
        if (art == null) { return; }

        AddCharacter("HeroPreview", art.playerIdleFrames, art.playerIdle,
                     new Vector2(0f, 0.5f), new Vector2(300f, -40f), new Vector2(320f, 320f), false, 0f);

        AddCharacter("MonsterPreview", art.menuMonsterFrames, art.ghost,
                     new Vector2(1f, 0.5f), new Vector2(-300f, 10f), new Vector2(300f, 300f), true, 1.9f);
    }

    private void AddCharacter(string name, Sprite[] frames, Sprite still, Vector2 anchor,
                              Vector2 position, Vector2 size, bool mirrored, float phase)
    {
        if ((frames == null || frames.Length == 0) && still == null) { return; }

        GameObject holder = new GameObject(name, typeof(RectTransform));
        holder.transform.SetParent(safeArea, false);

        RectTransform holderRect = (RectTransform)holder.transform;
        holderRect.anchorMin = anchor;
        holderRect.anchorMax = anchor;
        holderRect.pivot = new Vector2(0.5f, 0.5f);
        holderRect.sizeDelta = size;
        holderRect.anchoredPosition = position;

        MenuFloatAnimation float2 = holder.AddComponent<MenuFloatAnimation>();
        float2.SetPhase(phase);

        Image image = PixelUI.NewImage("Sprite", holderRect);
        PixelUI.Stretch(image.rectTransform);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = frames != null && frames.Length > 0 ? frames[0] : still;

        // The art faces right; the monster is placed on the right, looking in.
        if (mirrored) { image.rectTransform.localScale = new Vector3(-1f, 1f, 1f); }

        if (frames != null && frames.Length > 1)
        {
            image.gameObject.AddComponent<SpriteSequenceAnimator>().Play(frames, 7f);
        }

        // A soft glow behind each character lifts them off the backdrop.
        Image glow = PixelUI.NewImage("Glow", holderRect);
        PixelUI.Stretch(glow.rectTransform);
        glow.rectTransform.offsetMin = new Vector2(-70f, -70f);
        glow.rectTransform.offsetMax = new Vector2(70f, 70f);
        glow.sprite = PixelUI.Glow;
        glow.color = mirrored ? new Color(0.55f, 0.4f, 0.9f, 0.28f) : new Color(1f, 0.8f, 0.45f, 0.25f);
        glow.raycastTarget = false;
        glow.transform.SetAsFirstSibling();
    }

    // ----- panels ---------------------------------------------------------

    private CanvasGroup BuildHome()
    {
        CanvasGroup group = PixelUI.NewFullScreenGroup("Home", safeArea);

        Text title = PixelUI.NewTitle("Title", group.transform, "SOULBOUND GATE", 82);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -80f);

        Text credit = PixelUI.NewBody("Credit", group.transform, "Developed by Quang Ninh and Hong Phong", 26);
        credit.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        credit.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        credit.rectTransform.sizeDelta = new Vector2(1400f, 40f);
        credit.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        credit.color = PixelUI.Muted;

        Vector2 size = new Vector2(460f, 82f);
        float top = 250f;
        float gap = 96f;

        PixelUI.NewButton("NewGame", group.transform, "CHƠI MỚI", size, new Vector2(0f, top), OnNewGame);

        continueButton = PixelUI.NewButton("Continue", group.transform, "TIẾP TỤC", size,
                                           new Vector2(0f, top - gap), OnContinue);

        PixelUI.NewButton("Shop", group.transform, "CỬA HÀNG", size, new Vector2(0f, top - gap * 2f), () => shop.Open());
        PixelUI.NewButton("Guide", group.transform, "HƯỚNG DẪN", size, new Vector2(0f, top - gap * 3f), () => guide.Open());
        PixelUI.NewButton("Settings", group.transform, "CÀI ĐẶT", size, new Vector2(0f, top - gap * 4f), () => settings.Open());
        PixelUI.NewButton("Quit", group.transform, "THOÁT GAME", size, new Vector2(0f, top - gap * 5f), OnQuit);

        return group;
    }

    // Shared by the "start a new game" and "quit" prompts, so both look and
    // behave identically.
    private CanvasGroup BuildConfirm(string name, string heading, string body,
                                     string confirmLabel, System.Action onConfirm)
    {
        CanvasGroup group = PixelUI.NewFullScreenGroup(name, root);

        Image shade = PixelUI.NewImage("Shade", group.transform);
        PixelUI.Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0f, 0f, 0.78f);

        RectTransform panel = PixelUI.NewPanel("Panel", group.transform, new Vector2(920f, 400f));

        Text headingText = PixelUI.NewBody("Heading", panel, heading, 44);
        headingText.rectTransform.sizeDelta = new Vector2(840f, 60f);
        headingText.rectTransform.anchoredPosition = new Vector2(0f, 100f);
        headingText.color = PixelUI.Gold;

        Text bodyText = PixelUI.NewBody("Body", panel, body, 34);
        bodyText.rectTransform.sizeDelta = new Vector2(840f, 60f);
        bodyText.rectTransform.anchoredPosition = new Vector2(0f, 30f);

        PixelUI.NewButton("Confirm", panel, confirmLabel, new Vector2(340f, 88f), new Vector2(-190f, -90f),
                          () => onConfirm());

        PixelUI.NewButton("Cancel", panel, "HỦY", new Vector2(340f, 88f), new Vector2(190f, -90f),
                          () => PixelUI.SetGroupVisible(group, false));

        return group;
    }

    // ----- actions --------------------------------------------------------

    private void RefreshContinueButton()
    {
        GameSaveManager save = GameSaveManager.Instance;
        bool canContinue = save != null && save.HasValidSave();

        PixelUI.SetButtonEnabled(continueButton, canContinue);
    }

    private void OnNewGame()
    {
        GameSaveManager save = GameSaveManager.Instance;

        // Only warn when there is actually something to lose.
        if (save != null && save.HasValidSave())
        {
            PixelUI.SetGroupVisible(confirmGroup, true);
            confirmGroup.transform.SetAsLastSibling();
            return;
        }

        StartNewRun();
    }

    private void StartNewRun()
    {
        PixelUI.SetGroupVisible(confirmGroup, false);
        SceneFlow.StartNewRun();
    }

    private void OnContinue()
    {
        if (!SceneFlow.ContinueRun())
        {
            RefreshContinueButton();
        }
    }

    private void OnQuit()
    {
        PixelUI.SetGroupVisible(quitConfirmGroup, true);
        quitConfirmGroup.transform.SetAsLastSibling();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        Debug.Log("[Soulbound Gate] Application.Quit does not close the Unity Editor; this exits on a device.");
#endif
        Application.Quit();
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
}
