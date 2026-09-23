using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The opening and the ending, both told by this one screen: the key art above,
// the text typing itself out below, and a way to hurry it along. Which of the
// two it is showing comes from SceneFlow, so the scene itself holds nothing but
// this component.
public class StoryScreenController : MonoBehaviour
{
    private const float CharactersPerSecond = 46f;
    private const float PauseBetweenLines = 0.55f;

    private Text body;
    private Button next;
    private Text nextLabel;

    private string[] lines;
    private int lineIndex;
    private bool typing;
    private Coroutine typewriter;

    private void Awake()
    {
        lines = SceneFlow.StoryIsEpilogue ? StoryContent.Epilogue : StoryContent.Prologue;

        RectTransform safeArea = ScreenScaffold.Build(transform, "StoryCanvas", "",
            new Color(0.02f, 0.02f, 0.05f), new Color(0.08f, 0.07f, 0.16f));

        BuildArt(safeArea);
        BuildText(safeArea);
        BuildButtons(safeArea);
    }

    private void Start()
    {
        AudioManager.EnsureExists();
        AudioManager.PlayMusic(GameMusic.Menu);

        ShowLine(0);
    }

    private void Update()
    {
        // Android back, and a tap anywhere, both do what the button does.
        if (Input.GetKeyDown(KeyCode.Escape)) { Finish(); }
    }

    // ----- building -------------------------------------------------------

    private void BuildArt(RectTransform safeArea)
    {
        if (!Branding.HasKeyArt) { return; }

        Image art = PixelUI.NewImage("KeyArt", safeArea);
        art.sprite = Branding.KeyArt;
        art.preserveAspect = true;
        art.raycastTarget = false;
        art.color = new Color(1f, 1f, 1f, 0.85f);

        art.rectTransform.sizeDelta = new Vector2(300f, 300f);
        art.rectTransform.anchoredPosition = new Vector2(0f, 270f);
    }

    private void BuildText(RectTransform safeArea)
    {
        RectTransform panel = PixelUI.NewPanel("StoryPanel", safeArea, new Vector2(1240f, 300f));
        ((RectTransform)panel.parent).anchoredPosition = new Vector2(0f, -60f);

        body = PixelUI.NewBody("Body", panel, "", 36);
        body.alignment = TextAnchor.UpperLeft;
        body.color = PixelUI.Cream;

        // Paragraphs, not labels: these lines have to wrap inside the frame
        // rather than run off the side of it.
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Truncate;
        body.lineSpacing = 1.15f;

        body.rectTransform.sizeDelta = new Vector2(1150f, 240f);
        body.rectTransform.anchoredPosition = Vector2.zero;
    }

    private void BuildButtons(RectTransform safeArea)
    {
        next = PixelUI.NewButton("Next", safeArea, "TIẾP", new Vector2(420f, 84f),
                                 new Vector2(230f, -300f), OnNext);

        nextLabel = next.GetComponentInChildren<Text>();

        PixelUI.NewButton("Skip", safeArea, "BỎ QUA", new Vector2(420f, 84f),
                          new Vector2(-230f, -300f), Finish);
    }

    // ----- the telling ----------------------------------------------------

    private void ShowLine(int index)
    {
        lineIndex = index;

        if (typewriter != null) { StopCoroutine(typewriter); }
        typewriter = StartCoroutine(TypeRoutine(lines[index]));
    }

    private IEnumerator TypeRoutine(string line)
    {
        typing = true;
        body.text = "";

        if (nextLabel != null) { nextLabel.text = "NHANH HƠN"; }

        float shown = 0f;

        while (shown < line.Length)
        {
            shown += Time.unscaledDeltaTime * CharactersPerSecond;
            body.text = line.Substring(0, Mathf.Min(line.Length, Mathf.FloorToInt(shown)));

            yield return null;
        }

        body.text = line;
        typing = false;

        if (nextLabel != null) { nextLabel.text = IsLastLine ? "VÀO GAME" : "TIẾP"; }

        yield return new WaitForSecondsRealtime(PauseBetweenLines);
    }

    private bool IsLastLine
    {
        get { return lineIndex >= lines.Length - 1; }
    }

    // First press finishes the line being typed, second press moves on: the
    // same button never makes the player miss a sentence.
    private void OnNext()
    {
        if (typing)
        {
            if (typewriter != null) { StopCoroutine(typewriter); }

            body.text = lines[lineIndex];
            typing = false;

            if (nextLabel != null) { nextLabel.text = IsLastLine ? "VÀO GAME" : "TIẾP"; }
            return;
        }

        if (IsLastLine)
        {
            Finish();
            return;
        }

        ShowLine(lineIndex + 1);
    }

    private void Finish()
    {
        if (typewriter != null) { StopCoroutine(typewriter); }

        SceneFlow.ContinueFromStory();
    }
}
