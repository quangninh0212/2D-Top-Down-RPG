using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Spawns the on-screen movement stick, aim stick and Dash button at game start.
// Built entirely from code so no scene/prefab wiring is required at all - it
// self-installs on load, and only shows on Android/iOS builds (plus the Editor,
// so it's visible in Play Mode while iterating).
public class MobileControlsBootstrap : MonoBehaviour
{
    // Temporary on-screen readout of the real screen/safe-area values, used to
    // diagnose UI being clipped by the device. Set to false once that's settled.
    private const bool ShowDebugReadout = true;

    // Nothing important is placed near the bottom edge: on some devices the lower
    // part of the canvas ends up outside the visible screen. The sticks float to
    // wherever the thumb lands, so they only need the zone to be reachable.
    private const float TouchZoneBottom = 0.2f;

    private readonly List<OnScreenJoystick> joysticks = new List<OnScreenJoystick>();
    private RectTransform safeAreaRect;
    private Rect lastSafeArea;
    private Text debugText;

    // Unity's built-in UI skin sprites come out as flat squares here, so the
    // control artwork is drawn from scratch instead.
    private Sprite ringSprite;
    private Sprite knobSprite;
    private Sprite buttonSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!Application.isMobilePlatform && !Application.isEditor)
        {
            return;
        }

        GameObject bootstrapGO = new GameObject("MobileControlsBootstrap");
        DontDestroyOnLoad(bootstrapGO);
        bootstrapGO.AddComponent<MobileControlsBootstrap>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // The controls survive scene loads but the EventSystem does not, so a touch
    // held across a load (dying, or walking through a door) never reports its
    // release. Clear the sticks whenever the world is rebuilt.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReleaseAllSticks();
    }

    // Switching away from the game on a phone loses the touch the same way.
    private void OnApplicationPause(bool paused)
    {
        if (paused) { ReleaseAllSticks(); }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) { ReleaseAllSticks(); }
    }

    // On death the player's weapon object is destroyed while the death animation
    // plays, so the buttons must not reach into it during that window.
    private static bool PlayerAlive()
    {
        return PlayerHealth.Instance != null && !PlayerHealth.Instance.isDead;
    }

    private void ReleaseAllSticks()
    {
        foreach (OnScreenJoystick joystick in joysticks)
        {
            if (joystick != null) { joystick.ResetStick(); }
        }

        MobileInput.ResetAll();
    }

    private void Update()
    {
        // Safe area changes when the device rotates or the system bars appear/hide.
        if (safeAreaRect != null && Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }

        if (debugText != null)
        {
            Rect safeArea = Screen.safeArea;
            debugText.text = $"screen {Screen.width}x{Screen.height}  " +
                             $"safe x{safeArea.x:0} y{safeArea.y:0} w{safeArea.width:0} h{safeArea.height:0}  " +
                             $"dpi {Screen.dpi:0}";
        }
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("MobileControlsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Everything lives inside this container so the controls stay clear of
        // notches, rounded corners and the system navigation bar.
        GameObject safeAreaGO = new GameObject("SafeArea", typeof(RectTransform));
        safeAreaGO.transform.SetParent(canvasGO.transform, false);
        safeAreaRect = safeAreaGO.GetComponent<RectTransform>();
        safeAreaRect.offsetMin = Vector2.zero;
        safeAreaRect.offsetMax = Vector2.zero;
        ApplySafeArea();

        CreateControlSprites();

        CreateJoystick(OnScreenJoystick.Role.Move,
                       new Vector2(0f, TouchZoneBottom), new Vector2(0.45f, 1f));
        CreateJoystick(OnScreenJoystick.Role.Aim,
                       new Vector2(0.55f, TouchZoneBottom), new Vector2(1f, 1f));
        CreateActionButtons();

        if (ShowDebugReadout)
        {
            CreateDebugReadout();
        }
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        safeAreaRect.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        safeAreaRect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
    }

    // Draws a filled circle with an outline ring. Both edges fade across one
    // pixel, which is what keeps the curve smooth at any on-screen size.
    private static Sprite CreateDiscSprite(int size, float outlineWidth, Color fill, Color outline)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float radius = size * 0.5f;
        float outerEdge = radius - 1f;
        float innerEdge = outerEdge - outlineWidth;
        Vector2 centre = new Vector2(radius, radius);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);

                float coverage = Mathf.Clamp01(outerEdge - distance + 0.5f);
                float onOutline = Mathf.Clamp01(distance - innerEdge + 0.5f);

                Color colour = Color.Lerp(fill, outline, onOutline);
                colour.a *= coverage;
                pixels[y * size + x] = colour;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private void CreateControlSprites()
    {
        // Hollow ring for the stick base, so it never hides the game behind it.
        ringSprite = CreateDiscSprite(192, 10f,
            new Color(1f, 1f, 1f, 0.06f), new Color(1f, 1f, 1f, 0.42f));

        knobSprite = CreateDiscSprite(128, 6f,
            new Color(1f, 1f, 1f, 0.5f), new Color(1f, 1f, 1f, 0.92f));

        buttonSprite = CreateDiscSprite(160, 6f,
            new Color(0.06f, 0.07f, 0.10f, 0.62f), new Color(1f, 0.93f, 0.75f, 0.85f));
    }

    private void CreateJoystick(OnScreenJoystick.Role role, Vector2 anchorMin, Vector2 anchorMax)
    {
        // Invisible full-height zone that catches the touch anywhere on its side
        // of the screen. A transparent Image still receives raycasts.
        GameObject zoneGO = new GameObject(role + "TouchZone", typeof(Image));
        zoneGO.transform.SetParent(safeAreaRect, false);
        RectTransform zoneRect = zoneGO.GetComponent<RectTransform>();
        zoneRect.anchorMin = anchorMin;
        zoneRect.anchorMax = anchorMax;
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;
        zoneGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        GameObject bgGO = new GameObject(role + "JoystickBackground", typeof(Image));
        bgGO.transform.SetParent(zoneGO.transform, false);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(240f, 240f);
        bgRect.anchoredPosition = new Vector2(0f, -40f);   // resting spot, kept clear of the bottom

        Image bgImage = bgGO.GetComponent<Image>();
        bgImage.sprite = ringSprite;
        bgImage.raycastTarget = false;

        GameObject handleGO = new GameObject(role + "JoystickHandle", typeof(Image));
        handleGO.transform.SetParent(bgGO.transform, false);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(110f, 110f);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImage = handleGO.GetComponent<Image>();
        handleImage.sprite = knobSprite;
        handleImage.raycastTarget = false;

        OnScreenJoystick joystick = zoneGO.AddComponent<OnScreenJoystick>();
        joystick.Init(role, zoneRect, bgRect, handleRect, 90f);
        joysticks.Add(joystick);
    }

    private void CreateActionButtons()
    {
        CreateButton("DashButton", "DASH", new Vector2(-150f, -85f), () =>
        {
            if (PlayerAlive() && PlayerController.Instance != null)
            {
                PlayerController.Instance.TouchDash();
            }
        });

        // The inventory bar sits at the very bottom of the screen, which is hard
        // to hit on a phone, so weapons can also be cycled from here.
        CreateButton("WeaponButton", "SWAP", new Vector2(-150f, 85f), () =>
        {
            if (PlayerAlive() && ActiveInventory.Instance != null)
            {
                ActiveInventory.Instance.SelectNextSlot();
            }
        });
    }

    private void CreateButton(string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(name, typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(safeAreaRect, false);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(150f, 150f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGO.GetComponent<Image>();
        image.sprite = buttonSprite;

        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGO.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.96f, 0.86f);
        text.raycastTarget = false;

        Button button = buttonGO.GetComponent<Button>();
        ColorBlock colours = button.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = Color.white;
        colours.pressedColor = new Color(1f, 0.82f, 0.45f);   // warm flash on press
        colours.selectedColor = Color.white;
        colours.fadeDuration = 0.06f;
        button.colors = colours;
        button.onClick.AddListener(onClick);
    }

    private void CreateDebugReadout()
    {
        GameObject textGO = new GameObject("DebugReadout", typeof(Text));
        textGO.transform.SetParent(safeAreaRect, false);
        // Bottom centre, out of the way of the health and stamina UI.
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(1400f, 40f);
        rect.anchoredPosition = new Vector2(0f, 24f);

        debugText = textGO.GetComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 22;
        debugText.alignment = TextAnchor.MiddleCenter;
        debugText.color = new Color(1f, 1f, 0.4f, 0.6f);
        debugText.raycastTarget = false;
    }
}
