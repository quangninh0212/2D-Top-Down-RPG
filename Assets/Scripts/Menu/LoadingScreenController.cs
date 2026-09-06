using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sits between every pair of gameplay scenes. It does the real async load and
// paces the bar so the screen never flashes past too fast to read.
public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private float minimumDisplayTime = 0.9f;

    // Longest the loading screen may ever hold the player before giving up on
    // the progress bar and letting the level in.
    private const float MaximumWaitTime = 8f;

    private CanvasGroup group;
    private Image fill;
    private Text percentText;
    private Text tipText;

    private void Awake()
    {
        PixelUI.EnsureEventSystem();
        EnsureCamera();
        Build();
    }

    private IEnumerator Start()
    {
        string target = SceneFlow.PendingScene;

        // Reaching this scene directly (pressing Play on it in the editor) has
        // nowhere to go, so it falls back to the menu rather than hanging.
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning("[LoadingScreen] No pending scene; returning to the main menu.");
            target = GameScenes.MainMenu;
        }

        yield return Fade(1f, 0.25f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(target);
        operation.allowSceneActivation = false;

        float shown = 0f;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            // Safety net. The bar is paced against operation.progress, and if
            // that ever stalls the player would sit on this screen forever with
            // no way out. After this long, go regardless.
            if (elapsed > MaximumWaitTime)
            {
                Debug.LogWarning("[LoadingScreen] " + target + " took too long to report progress; activating anyway.");
                break;
            }

            // Unity holds real progress at 0.9 until activation is allowed. The
            // bar shows whichever is further behind - the real load or the
            // minimum display time - so it never lies about being ready.
            float real = Mathf.Clamp01(operation.progress / 0.9f);
            float paced = Mathf.Clamp01(elapsed / minimumDisplayTime);

            shown = Mathf.MoveTowards(shown, Mathf.Min(real, paced), Time.unscaledDeltaTime * 1.8f);

            fill.fillAmount = shown;
            percentText.text = Mathf.RoundToInt(shown * 100f) + "%";

            if (shown >= 0.999f) { break; }

            yield return null;
        }

        yield return Fade(0f, 0.2f);

        SceneFlow.ClearPending();
        operation.allowSceneActivation = true;
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null) { return; }

        GameObject cameraGO = new GameObject("Loading Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";

        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        camera.orthographic = true;
    }

    private void Build()
    {
        Canvas canvas = PixelUI.CreateCanvas("LoadingCanvas", transform, 0);

        Image backdrop = PixelUI.NewImage("Backdrop", canvas.transform);
        PixelUI.Stretch(backdrop.rectTransform);
        backdrop.sprite = MenuArt.VerticalGradient(64,
            new Color(0.02f, 0.03f, 0.05f), new Color(0.08f, 0.07f, 0.13f));
        backdrop.raycastTarget = false;

        RectTransform safeArea = PixelUI.CreateSafeArea(canvas.transform, 24f);

        group = PixelUI.NewFullScreenGroup("Content", safeArea);
        group.blocksRaycasts = false;
        group.alpha = 0f;

        Text title = PixelUI.NewTitle("Title", group.transform, "SOULBOUND GATE", 72);
        title.rectTransform.sizeDelta = new Vector2(1700f, 110f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 210f);

        Text heading = PixelUI.NewBody("Heading", group.transform, "Đang tải...", 40);
        heading.rectTransform.sizeDelta = new Vector2(900f, 60f);
        heading.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        heading.color = PixelUI.Cream;

        Image track = PixelUI.NewImage("BarTrack", group.transform);
        track.sprite = PixelUI.Bar;
        track.type = Image.Type.Sliced;
        track.color = new Color(1f, 1f, 1f, 0.12f);
        track.raycastTarget = false;
        track.rectTransform.sizeDelta = new Vector2(1100f, 38f);
        track.rectTransform.anchoredPosition = new Vector2(0f, 30f);

        fill = PixelUI.NewImage("BarFill", track.transform);
        PixelUI.Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(4f, 4f);
        fill.rectTransform.offsetMax = new Vector2(-4f, -4f);
        fill.sprite = PixelUI.Bar;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        fill.color = PixelUI.Gold;
        fill.raycastTarget = false;

        percentText = PixelUI.NewBody("Percent", group.transform, "0%", 32);
        percentText.rectTransform.sizeDelta = new Vector2(400f, 50f);
        percentText.rectTransform.anchoredPosition = new Vector2(0f, -30f);
        percentText.color = PixelUI.Muted;

        tipText = PixelUI.NewBody("Tip", group.transform, LoadingTips.Random(), 34);
        tipText.rectTransform.sizeDelta = new Vector2(1500f, 60f);
        tipText.rectTransform.anchoredPosition = new Vector2(0f, -160f);
        tipText.color = new Color(0.75f, 0.82f, 0.95f);
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
