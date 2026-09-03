using UnityEngine;
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

    private RectTransform safeAreaRect;
    private Rect lastSafeArea;
    private Text debugText;

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

    private void CreateJoystick(OnScreenJoystick.Role role, Vector2 anchorMin, Vector2 anchorMax)
    {
        Sprite bgSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        Sprite knobSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

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
        bgImage.sprite = bgSprite;
        bgImage.type = Image.Type.Sliced;
        bgImage.color = new Color(1f, 1f, 1f, 0.3f);
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
        handleImage.color = new Color(1f, 1f, 1f, 0.8f);
        handleImage.raycastTarget = false;

        OnScreenJoystick joystick = zoneGO.AddComponent<OnScreenJoystick>();
        joystick.Init(role, zoneRect, bgRect, handleRect, 90f);
    }

    private void CreateActionButtons()
    {
        CreateButton("DashButton", "DASH", new Vector2(1f, 0.5f), new Vector2(-160f, -60f), () =>
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.TouchDash();
            }
        });

        // The inventory bar sits at the very bottom of the screen, which is hard
        // to hit on a phone, so weapons can also be cycled from here.
        CreateButton("WeaponButton", "WEAPON", new Vector2(1f, 0.5f), new Vector2(-160f, 80f), () =>
        {
            if (ActiveInventory.Instance != null)
            {
                ActiveInventory.Instance.SelectNextSlot();
            }
        });
    }

    private void CreateButton(string name, string label, Vector2 anchor, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        Sprite buttonSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject buttonGO = new GameObject(name, typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(safeAreaRect, false);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(190f, 110f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGO.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.55f);

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
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.raycastTarget = false;

        buttonGO.GetComponent<Button>().onClick.AddListener(onClick);
    }

    private void CreateDebugReadout()
    {
        GameObject textGO = new GameObject("DebugReadout", typeof(Text));
        textGO.transform.SetParent(safeAreaRect, false);
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(1400f, 60f);
        rect.anchoredPosition = new Vector2(20f, -20f);

        debugText = textGO.GetComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 34;
        debugText.color = Color.yellow;
        debugText.raycastTarget = false;
    }
}
