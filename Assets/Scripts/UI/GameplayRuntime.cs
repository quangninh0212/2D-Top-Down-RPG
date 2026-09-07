using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The in-game overlay: touch controls on the left and right, the pause button
// and weapon slots along the top, and the banners the level system posts.
// Built from code and kept alive across scene loads, so no gameplay scene has
// to carry any of it.
public class GameplayRuntime : MonoBehaviour
{
    public static GameplayRuntime Instance { get; private set; }

    private const float JoystickRange = 110f;

    private Canvas canvas;
    private RectTransform safeArea;
    private GameObject controlsRoot;

    private OnScreenJoystick joystick;
    private Button attackButton;
    private Button weaponButton;
    private Text weaponButtonLabel;

    private readonly List<WeaponSlotView> weaponSlots = new List<WeaponSlotView>();

    private CanvasGroup bannerGroup;
    private Text bannerTitle;
    private Text bannerSubtitle;
    private Coroutine bannerRoutine;

    private CanvasGroup toastGroup;
    private Text toastText;
    private readonly Queue<string> toastQueue = new Queue<string>();
    private Coroutine toastRoutine;

    private PauseMenuUI pauseMenu;
    private GameOverUI gameOver;
    private BossHealthBarUI bossBar;

    private class WeaponSlotView
    {
        public WeaponType Weapon;
        public Image Frame;
        public Image Icon;
        public Image Lock;
        public Image Highlight;
    }

    public static void EnsureExists()
    {
        if (Instance != null)
        {
            Instance.RefreshForScene();
            return;
        }

        GameObject go = new GameObject("GameplayRuntime");
        DontDestroyOnLoad(go);
        go.AddComponent<GameplayRuntime>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PixelUI.EnsureEventSystem();
        Build();

        // This object is created from the sceneLoaded callback of the first
        // gameplay level, which has already fired by now - so that level would
        // otherwise never get the per-scene setup that every later one gets.
        RefreshForScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameMessages.OnBanner += ShowBanner;
        GameMessages.OnToast += ShowToast;
        ActiveInventory.OnWeaponChanged += OnWeaponChanged;
        GameSaveManager.OnGoldOrProgressChanged += RefreshWeaponSlots;
        PlayerHealth.OnPlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameMessages.OnBanner -= ShowBanner;
        GameMessages.OnToast -= ShowToast;
        ActiveInventory.OnWeaponChanged -= OnWeaponChanged;
        GameSaveManager.OnGoldOrProgressChanged -= RefreshWeaponSlots;
        PlayerHealth.OnPlayerDied -= OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    // ----- lifecycle ------------------------------------------------------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // A touch held across a load never reports its release, because the old
        // EventSystem went with the old scene.
        ReleaseTouches();
        RefreshForScene();
    }

    public void RefreshForScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool inGameplay = GameScenes.IsGameplayScene(sceneName);

        if (controlsRoot != null) { controlsRoot.SetActive(inGameplay); }

        // The HUD canvas comes from a prefab whose scene instances override its
        // layout, so it is positioned here, on the live objects, every time a
        // level loads.
        if (inGameplay) { HudLayout.Apply(); }

        // The boss bar lives on this persistent object, and the boss only hides
        // it by dying. Walking back out of the boss room, or quitting to the
        // menu, used to leave it stuck across the top of every screen until the
        // app was restarted. Any scene without a boss in it clears the bar; the
        // boss shows it again from its own Start.
        if (bossBar != null && FindObjectOfType<BossHealth>() == null)
        {
            bossBar.Hide();
        }

        if (!inGameplay)
        {
            if (pauseMenu != null) { pauseMenu.Close(); }
            if (gameOver != null) { gameOver.Hide(); }
            HideBannerImmediately();
        }

        RefreshWeaponSlots();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) { ReleaseTouches(); }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) { ReleaseTouches(); }
    }

    private void ReleaseTouches()
    {
        if (joystick != null) { joystick.ResetStick(); }

        MobileInput.ResetAll();

        if (ActiveWeapon.Instance != null) { ActiveWeapon.Instance.StopAttackingTouch(); }
    }

    private void Update()
    {
        HandleBackButton();
    }

    // Android's back gesture should never drop the player straight out of a run.
    private void HandleBackButton()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) { return; }

        if (gameOver != null && gameOver.IsVisible) { return; }

        if (!GameScenes.IsGameplayScene(SceneManager.GetActiveScene().name)) { return; }

        TogglePause();
    }

    // ----- building -------------------------------------------------------

    private void Build()
    {
        canvas = PixelUI.CreateCanvas("GameplayCanvas", transform, 200);
        safeArea = PixelUI.CreateSafeArea(canvas.transform, 16f);

        controlsRoot = new GameObject("Controls", typeof(RectTransform));
        controlsRoot.transform.SetParent(safeArea, false);
        PixelUI.Stretch((RectTransform)controlsRoot.transform);

        BuildJoystick();
        BuildActionButtons();
        BuildTopBar();
        BuildWeaponSlots();
        BuildBanner();
        BuildToast();

        bossBar = new GameObject("BossHealthBar", typeof(RectTransform)).AddComponent<BossHealthBarUI>();
        bossBar.transform.SetParent(safeArea, false);
        bossBar.Build();

        pauseMenu = new GameObject("PauseMenu", typeof(RectTransform)).AddComponent<PauseMenuUI>();
        pauseMenu.transform.SetParent(canvas.transform, false);
        pauseMenu.Build();

        gameOver = new GameObject("GameOver", typeof(RectTransform)).AddComponent<GameOverUI>();
        gameOver.transform.SetParent(canvas.transform, false);
        gameOver.Build();
    }

    private void BuildJoystick()
    {
        // The whole lower-left quadrant accepts the touch and the stick
        // re-centres wherever the thumb lands, so it is always reachable.
        GameObject zoneGO = new GameObject("MoveTouchZone", typeof(Image));
        zoneGO.transform.SetParent(controlsRoot.transform, false);

        RectTransform zoneRect = zoneGO.GetComponent<RectTransform>();
        zoneRect.anchorMin = new Vector2(0f, 0f);
        zoneRect.anchorMax = new Vector2(0.42f, 0.82f);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;
        zoneGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        Image background = PixelUI.NewImage("JoystickBackground", zoneGO.transform);
        background.sprite = PixelUI.Ring;
        background.color = new Color(1f, 1f, 1f, 0.5f);
        background.raycastTarget = false;
        background.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        background.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        background.rectTransform.sizeDelta = new Vector2(280f, 280f);
        background.rectTransform.anchoredPosition = new Vector2(40f, -60f);

        Image handle = PixelUI.NewImage("JoystickHandle", background.transform);
        handle.sprite = PixelUI.Disc;
        handle.color = new Color(1f, 0.93f, 0.78f, 0.8f);
        handle.raycastTarget = false;
        handle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        handle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        handle.rectTransform.sizeDelta = new Vector2(120f, 120f);

        joystick = zoneGO.AddComponent<OnScreenJoystick>();
        joystick.Init(zoneRect, background.rectTransform, handle.rectTransform, JoystickRange);
    }

    private void BuildActionButtons()
    {
        Vector2 anchor = new Vector2(1f, 0f);

        // Attack is the biggest and closest to the thumb; dash and weapon sit
        // above and inside it, all well over the 48dp minimum touch target.
        attackButton = PixelUI.NewRoundButton("AttackButton", controlsRoot.transform, "TẤN CÔNG", 230f,
            anchor, new Vector2(-170f, 170f), new Color(0.55f, 0.16f, 0.18f, 0.75f), null);

        HoldToAttack hold = attackButton.gameObject.AddComponent<HoldToAttack>();
        hold.Bind();

        PixelUI.NewRoundButton("DashButton", controlsRoot.transform, "LƯỚT", 150f,
            anchor, new Vector2(-420f, 130f), new Color(0.15f, 0.32f, 0.48f, 0.75f), () =>
            {
                if (PlayerController.Instance != null && !PauseMenuUI.IsPaused)
                {
                    PlayerController.Instance.TouchDash();
                }
            });

        weaponButton = PixelUI.NewRoundButton("WeaponButton", controlsRoot.transform, "VŨ KHÍ", 150f,
            anchor, new Vector2(-190f, 400f), new Color(0.30f, 0.22f, 0.45f, 0.75f), () =>
            {
                if (ActiveInventory.Instance != null && !PauseMenuUI.IsPaused)
                {
                    ActiveInventory.Instance.CycleWeapon();
                }
            });

        weaponButtonLabel = weaponButton.GetComponentInChildren<Text>();
    }

    private void BuildTopBar()
    {
        PixelUI.NewRoundButton("PauseButton", controlsRoot.transform, "☰", 110f,
            new Vector2(1f, 1f), new Vector2(-70f, -70f), new Color(0.10f, 0.12f, 0.16f, 0.85f), TogglePause);
    }

    // Three slots showing what the player owns; locked ones are dimmed and
    // carry a padlock.
    private void BuildWeaponSlots()
    {
        GameObject strip = new GameObject("WeaponSlots", typeof(RectTransform));
        strip.transform.SetParent(controlsRoot.transform, false);

        RectTransform stripRect = (RectTransform)strip.transform;
        stripRect.anchorMin = new Vector2(1f, 1f);
        stripRect.anchorMax = new Vector2(1f, 1f);
        stripRect.pivot = new Vector2(1f, 1f);
        stripRect.sizeDelta = new Vector2(330f, 110f);
        stripRect.anchoredPosition = new Vector2(-150f, -22f);

        for (int i = 0; i < WeaponTypeInfo.Count; i++)
        {
            WeaponType weapon = (WeaponType)i;

            Image frame = PixelUI.NewImage("Slot_" + weapon, stripRect);
            frame.sprite = PixelUI.ButtonBox;
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.10f, 0.12f, 0.16f, 0.8f);
            frame.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            frame.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            frame.rectTransform.pivot = new Vector2(1f, 0.5f);
            frame.rectTransform.sizeDelta = new Vector2(96f, 96f);
            frame.rectTransform.anchoredPosition = new Vector2(-i * 108f, 0f);

            Image highlight = PixelUI.NewImage("Highlight", frame.transform);
            PixelUI.Stretch(highlight.rectTransform);
            highlight.rectTransform.offsetMin = new Vector2(-4f, -4f);
            highlight.rectTransform.offsetMax = new Vector2(4f, 4f);
            highlight.sprite = PixelUI.ButtonBox;
            highlight.type = Image.Type.Sliced;
            highlight.color = PixelUI.Gold;
            highlight.raycastTarget = false;
            highlight.transform.SetAsFirstSibling();

            Image icon = PixelUI.NewImage("Icon", frame.transform);
            PixelUI.Stretch(icon.rectTransform);
            icon.rectTransform.offsetMin = new Vector2(12f, 12f);
            icon.rectTransform.offsetMax = new Vector2(-12f, -12f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.sprite = WeaponIcons.Get(weapon);

            Image padlock = PixelUI.NewImage("Lock", frame.transform);
            padlock.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            padlock.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            padlock.rectTransform.sizeDelta = new Vector2(46f, 46f);
            padlock.sprite = WeaponIcons.Padlock;
            padlock.raycastTarget = false;
            padlock.preserveAspect = true;

            weaponSlots.Add(new WeaponSlotView
            {
                Weapon = weapon,
                Frame = frame,
                Icon = icon,
                Lock = padlock,
                Highlight = highlight
            });
        }

        RefreshWeaponSlots();
    }

    private void BuildBanner()
    {
        bannerGroup = PixelUI.NewFullScreenGroup("Banner", safeArea);
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;

        bannerTitle = PixelUI.NewTitle("Title", bannerGroup.transform, "", 84);
        bannerTitle.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        bannerTitle.rectTransform.anchoredPosition = new Vector2(0f, 90f);

        bannerSubtitle = PixelUI.NewBody("Subtitle", bannerGroup.transform, "", 40);
        bannerSubtitle.rectTransform.sizeDelta = new Vector2(1600f, 70f);
        bannerSubtitle.rectTransform.anchoredPosition = new Vector2(0f, 10f);
        bannerSubtitle.color = PixelUI.Cream;

        bannerGroup.alpha = 0f;
        bannerGroup.gameObject.SetActive(false);
    }

    private void BuildToast()
    {
        toastGroup = PixelUI.NewFullScreenGroup("Toast", safeArea);
        toastGroup.blocksRaycasts = false;
        toastGroup.interactable = false;

        RectTransform panel = PixelUI.NewPanel("Plate", toastGroup.transform, new Vector2(920f, 78f));
        RectTransform plateRoot = (RectTransform)panel.parent;
        plateRoot.anchorMin = new Vector2(0.5f, 1f);
        plateRoot.anchorMax = new Vector2(0.5f, 1f);
        plateRoot.pivot = new Vector2(0.5f, 1f);
        plateRoot.anchoredPosition = new Vector2(0f, -150f);

        toastText = PixelUI.NewBody("Text", panel, "", 34);
        PixelUI.Stretch(toastText.rectTransform);
        toastText.color = PixelUI.Gold;

        toastGroup.alpha = 0f;
        toastGroup.gameObject.SetActive(false);
    }

    // ----- weapon slots ---------------------------------------------------

    private void OnWeaponChanged(WeaponType weapon)
    {
        RefreshWeaponSlots();
    }

    private void RefreshWeaponSlots()
    {
        ActiveInventory inventory = ActiveInventory.Instance;

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponSlotView slot = weaponSlots[i];
            if (slot.Frame == null) { continue; }

            bool owned = inventory != null ? inventory.IsOwned(slot.Weapon) : slot.Weapon == WeaponType.Sword;
            bool equipped = inventory != null && inventory.CurrentWeapon == slot.Weapon;

            slot.Icon.color = owned ? Color.white : new Color(0.3f, 0.3f, 0.34f, 0.85f);
            slot.Lock.gameObject.SetActive(!owned);
            slot.Highlight.gameObject.SetActive(owned && equipped);
        }

        if (weaponButtonLabel != null && inventory != null)
        {
            weaponButtonLabel.text = WeaponTypeInfo.DisplayName(inventory.CurrentWeapon);
        }
    }

    // ----- banners and toasts --------------------------------------------

    private void ShowBanner(string title, string subtitle)
    {
        if (bannerRoutine != null) { StopCoroutine(bannerRoutine); }

        bannerTitle.text = title;
        bannerSubtitle.text = subtitle;

        bannerRoutine = StartCoroutine(BannerRoutine());
    }

    private void HideBannerImmediately()
    {
        if (bannerRoutine != null) { StopCoroutine(bannerRoutine); bannerRoutine = null; }
        if (bannerGroup != null) { bannerGroup.gameObject.SetActive(false); }
    }

    private IEnumerator BannerRoutine()
    {
        bannerGroup.gameObject.SetActive(true);

        yield return FadeGroup(bannerGroup, 1f, 0.35f);
        yield return new WaitForSecondsRealtime(1.6f);
        yield return FadeGroup(bannerGroup, 0f, 0.45f);

        bannerGroup.gameObject.SetActive(false);
        bannerRoutine = null;
    }

    private void ShowToast(string message)
    {
        toastQueue.Enqueue(message);

        if (toastRoutine == null) { toastRoutine = StartCoroutine(ToastRoutine()); }
    }

    private IEnumerator ToastRoutine()
    {
        while (toastQueue.Count > 0)
        {
            toastText.text = toastQueue.Dequeue();
            toastGroup.gameObject.SetActive(true);

            yield return FadeGroup(toastGroup, 1f, 0.2f);
            yield return new WaitForSecondsRealtime(1.7f);
            yield return FadeGroup(toastGroup, 0f, 0.3f);
        }

        toastGroup.gameObject.SetActive(false);
        toastRoutine = null;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float target, float duration)
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

    // ----- pause and death ------------------------------------------------

    public void TogglePause()
    {
        if (pauseMenu == null) { return; }
        if (gameOver != null && gameOver.IsVisible) { return; }
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.isDead) { return; }

        if (PauseMenuUI.IsPaused) { pauseMenu.Close(); }
        else { pauseMenu.Open(); }
    }

    private void OnPlayerDied()
    {
        if (gameOver != null) { gameOver.Show(); }
    }

    public BossHealthBarUI BossBar
    {
        get { return bossBar; }
    }
}
