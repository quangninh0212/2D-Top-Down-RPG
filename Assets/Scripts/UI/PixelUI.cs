using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shared look and construction helpers for every screen in the game. Building
// the UI from code keeps all the layout in one readable place and means no
// prefab has to be hand-wired for the new screens.
public static class PixelUI
{
    // ----- palette --------------------------------------------------------

    public static readonly Color Ink = new Color(0.04f, 0.05f, 0.06f, 1f);
    public static readonly Color PanelFill = new Color(0.09f, 0.11f, 0.14f, 0.97f);
    public static readonly Color PanelEdge = new Color(0.62f, 0.52f, 0.30f, 1f);
    public static readonly Color Cream = new Color(1f, 0.96f, 0.88f, 1f);
    public static readonly Color Gold = new Color(1f, 0.80f, 0.35f, 1f);
    public static readonly Color HealthRed = new Color(0.85f, 0.25f, 0.28f, 1f);
    public static readonly Color Magic = new Color(0.62f, 0.48f, 0.92f, 1f);
    public static readonly Color Muted = new Color(1f, 1f, 1f, 0.35f);

    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    // The pixel face has no Vietnamese diacritics, so anything with accents
    // falls back to the built-in font. Titles stay pixel, body text stays legible.
    private static Font pixelFont;
    private static Font bodyFont;

    public static Font PixelFont
    {
        get
        {
            if (pixelFont == null) { pixelFont = Resources.Load<Font>("Fonts/Gixel"); }
            if (pixelFont == null) { pixelFont = BodyFont; }

            return pixelFont;
        }
    }

    public static Font BodyFont
    {
        get
        {
            if (bodyFont == null) { bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }

            return bodyFont;
        }
    }

    // ----- generated sprites ---------------------------------------------

    private static Sprite panelSprite;
    private static Sprite buttonSprite;
    private static Sprite barSprite;
    private static Sprite discSprite;
    private static Sprite ringSprite;
    private static Sprite glowSprite;

    public static Sprite Panel
    {
        get
        {
            if (panelSprite == null) { panelSprite = MenuArt.RoundedRect(96, 96, 14f, Color.white); }
            return panelSprite;
        }
    }

    public static Sprite ButtonBox
    {
        get
        {
            if (buttonSprite == null) { buttonSprite = MenuArt.RoundedRect(96, 96, 10f, Color.white); }
            return buttonSprite;
        }
    }

    public static Sprite Bar
    {
        get
        {
            if (barSprite == null) { barSprite = MenuArt.RoundedRect(64, 64, 14f, Color.white); }
            return barSprite;
        }
    }

    public static Sprite Disc
    {
        get
        {
            if (discSprite == null) { discSprite = MenuArt.Disc(96, 4f, Color.white, Color.white); }
            return discSprite;
        }
    }

    public static Sprite Ring
    {
        get
        {
            if (ringSprite == null) { ringSprite = MenuArt.Disc(192, 10f, new Color(1f, 1f, 1f, 0.06f), Color.white); }
            return ringSprite;
        }
    }

    public static Sprite Glow
    {
        get
        {
            if (glowSprite == null) { glowSprite = MenuArt.SoftGlow(128, Color.white); }
            return glowSprite;
        }
    }

    // ----- construction ---------------------------------------------------

    public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
    {
        GameObject canvasGO = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) { canvasGO.transform.SetParent(parent, false); }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;

        // Landscape phones are wider than 16:9, so matching height keeps the
        // vertical layout stable and lets the extra width simply show more.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        return canvas;
    }

    public static RectTransform CreateSafeArea(Transform parent, float padding = 12f)
    {
        GameObject go = new GameObject("SafeArea", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        SafeArea safeArea = go.AddComponent<SafeArea>();
        safeArea.Apply();

        RectTransform rect = (RectTransform)go.transform;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);

        return rect;
    }

    // Delegates to the one persistent EventSystem. Screens must not make their
    // own: a scene-owned EventSystem dies with its scene and leaves the next
    // one with no input at all.
    public static void EnsureEventSystem()
    {
        GameBootstrap.EnsureEventSystem();
    }

    public static RectTransform Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return rect;
    }

    public static Image NewImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);

        return go.GetComponent<Image>();
    }

    public static Text NewText(string name, Transform parent, string content, Font font, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Cream;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    public static Text NewTitle(string name, Transform parent, string content, int fontSize)
    {
        Text text = NewText(name, parent, content, PixelFont, fontSize);
        text.color = Gold;
        return text;
    }

    public static Text NewBody(string name, Transform parent, string content, int fontSize)
    {
        return NewText(name, parent, content, BodyFont, fontSize);
    }

    // A framed panel: dark fill with a warm border drawn as a slightly larger
    // rectangle behind it.
    public static RectTransform NewPanel(string name, Transform parent, Vector2 size)
    {
        Image border = NewImage(name, parent);
        border.sprite = Panel;
        border.type = Image.Type.Sliced;
        border.color = PanelEdge;
        border.rectTransform.sizeDelta = size;

        Image fill = NewImage("Fill", border.transform);
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(5f, 5f);
        fill.rectTransform.offsetMax = new Vector2(-5f, -5f);
        fill.sprite = Panel;
        fill.type = Image.Type.Sliced;
        fill.color = PanelFill;
        fill.raycastTarget = false;

        // Children are added to the fill so they sit inside the frame.
        return fill.rectTransform;
    }

    public static Button NewButton(string name, Transform parent, string label, Vector2 size, Vector2 position, Action onClick)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.sprite = ButtonBox;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.16f, 0.19f, 0.24f, 0.96f);

        Image edge = NewImage("Edge", go.transform);
        Stretch(edge.rectTransform);
        edge.sprite = ButtonBox;
        edge.type = Image.Type.Sliced;
        edge.color = new Color(0.62f, 0.52f, 0.30f, 0.55f);
        edge.raycastTarget = false;
        edge.transform.SetAsFirstSibling();
        edge.rectTransform.offsetMin = new Vector2(-3f, -3f);
        edge.rectTransform.offsetMax = new Vector2(3f, 3f);

        Text text = NewBody("Label", go.transform, label, Mathf.RoundToInt(size.y * 0.36f));
        Stretch(text.rectTransform);

        Button button = go.GetComponent<Button>();
        ApplyButtonColours(button);
        button.onClick.AddListener(() =>
        {
            AudioManager.PlaySfx(GameSfx.UiClick);
            onClick?.Invoke();
        });

        // Grows very slightly under the finger, which reads as a press on a
        // touchscreen where there is no hover state.
        go.AddComponent<ButtonPressFeedback>();

        return button;
    }

    public static void ApplyButtonColours(Button button)
    {
        ColorBlock colours = button.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1f, 0.97f, 0.90f);
        colours.pressedColor = new Color(1f, 0.82f, 0.45f);
        colours.selectedColor = Color.white;
        colours.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        colours.fadeDuration = 0.07f;
        button.colors = colours;
    }

    public static void SetButtonEnabled(Button button, bool enabled)
    {
        if (button == null) { return; }

        button.interactable = enabled;

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = enabled ? Cream : new Color(1f, 1f, 1f, 0.32f);
        }
    }

    // A round touch button - the shape used for the on-screen action controls.
    public static Button NewRoundButton(string name, Transform parent, string label, float diameter,
                                        Vector2 anchor, Vector2 position, Color tint, Action onClick)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(diameter, diameter);
        rect.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.sprite = Disc;
        image.color = tint;

        Image edge = NewImage("Edge", go.transform);
        Stretch(edge.rectTransform);
        edge.sprite = Ring;
        edge.color = new Color(1f, 0.93f, 0.75f, 0.85f);
        edge.raycastTarget = false;

        Text text = NewBody("Label", go.transform, label, Mathf.RoundToInt(diameter * 0.22f));
        Stretch(text.rectTransform);
        text.fontStyle = FontStyle.Bold;

        Button button = go.GetComponent<Button>();
        ApplyButtonColours(button);

        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        go.AddComponent<ButtonPressFeedback>();

        return button;
    }

    public static Slider NewSlider(string name, Transform parent, Vector2 size, Vector2 position, float value, Action<float> onChanged)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image track = NewImage("Track", rect);
        Stretch(track.rectTransform);
        track.sprite = Bar;
        track.type = Image.Type.Sliced;
        track.color = new Color(1f, 1f, 1f, 0.14f);

        RectTransform fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(rect, false);
        Stretch(fillArea);

        Image fill = NewImage("Fill", fillArea);
        Stretch(fill.rectTransform);
        fill.sprite = Bar;
        fill.type = Image.Type.Sliced;
        fill.color = Gold;

        Image handle = NewImage("Handle", rect);
        handle.sprite = Disc;
        handle.color = Cream;
        handle.rectTransform.sizeDelta = new Vector2(size.y * 1.5f, size.y * 1.5f);

        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));

        return slider;
    }

    public static CanvasGroup NewFullScreenGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);

        return go.GetComponent<CanvasGroup>();
    }

    public static void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null) { return; }

        group.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
