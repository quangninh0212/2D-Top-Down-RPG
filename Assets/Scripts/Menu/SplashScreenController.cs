using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The game's own title card, shown after Unity's mandatory splash. Fades in,
// holds, fades out, and moves on to the main menu on its own.
public class SplashScreenController : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 0.7f;
    [SerializeField] private float holdTime = 1.8f;
    [SerializeField] private float fadeOutTime = 0.6f;

    private CanvasGroup group;
    private RectTransform crest;

    private void Awake()
    {
        PixelUI.EnsureEventSystem();
        EnsureCamera();
        Build();
    }

    private IEnumerator Start()
    {
        group.alpha = 0f;

        yield return Fade(1f, fadeInTime);
        yield return new WaitForSecondsRealtime(holdTime);
        yield return Fade(0f, fadeOutTime);

        SceneManager.LoadScene(GameScenes.MainMenu);
    }

    private void Update()
    {
        if (crest == null) { return; }

        // A slow breath, so the card is not completely static.
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.02f;
        crest.localScale = new Vector3(pulse, pulse, 1f);
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null) { return; }

        GameObject cameraGO = new GameObject("Splash Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";

        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
        camera.orthographic = true;
    }

    private void Build()
    {
        Canvas canvas = PixelUI.CreateCanvas("SplashCanvas", transform, 0);

        Image backdrop = PixelUI.NewImage("Backdrop", canvas.transform);
        PixelUI.Stretch(backdrop.rectTransform);
        backdrop.sprite = MenuArt.VerticalGradient(64,
            new Color(0.02f, 0.03f, 0.05f), new Color(0.10f, 0.08f, 0.16f));
        backdrop.raycastTarget = false;

        RectTransform safeArea = PixelUI.CreateSafeArea(canvas.transform, 20f);

        group = PixelUI.NewFullScreenGroup("Content", safeArea);
        group.blocksRaycasts = false;

        // The supplied key art carries this screen. The old procedural hero
        // frame stands in only when the artwork is missing, so a clone without
        // it still shows something.
        GameArtLibrary art = GameArtLibrary.Instance;
        bool hasKeyArt = Branding.HasKeyArt;

        Image portrait = PixelUI.NewImage("KeyArt", group.transform);
        portrait.rectTransform.sizeDelta = hasKeyArt
            ? new Vector2(430f, 430f)
            : new Vector2(190f, 190f);
        portrait.rectTransform.anchoredPosition = new Vector2(0f, hasKeyArt ? 110f : 170f);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portrait.sprite = hasKeyArt ? Branding.KeyArt : (art != null ? art.playerIdle : null);
        portrait.enabled = portrait.sprite != null;
        crest = portrait.rectTransform;

        Text title = PixelUI.NewTitle("Title", group.transform, "SOULBOUND GATE", 96);
        title.rectTransform.sizeDelta = new Vector2(1700f, 130f);
        title.rectTransform.anchoredPosition = new Vector2(0f, hasKeyArt ? -180f : -20f);

        Text by = PixelUI.NewBody("DevelopedBy", group.transform, "Developed by", 30);
        by.rectTransform.sizeDelta = new Vector2(1200f, 50f);
        by.rectTransform.anchoredPosition = new Vector2(0f, hasKeyArt ? -280f : -130f);
        by.color = PixelUI.Muted;

        Text names = PixelUI.NewBody("Names", group.transform, "Quang Ninh and Hong Phong", 42);
        names.rectTransform.sizeDelta = new Vector2(1400f, 60f);
        names.rectTransform.anchoredPosition = new Vector2(0f, hasKeyArt ? -335f : -185f);
        names.color = PixelUI.Cream;
    }

    private IEnumerator Fade(float target, float duration)
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
