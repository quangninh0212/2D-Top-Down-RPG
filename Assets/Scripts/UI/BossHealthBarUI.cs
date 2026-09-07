using UnityEngine;
using UnityEngine.UI;

// Wide health bar across the top of the screen during the boss fight. Hidden
// until a boss registers itself, so it costs nothing in the other levels.
public class BossHealthBarUI : MonoBehaviour
{
    private CanvasGroup group;
    private Image fill;
    private Image damageTrail;
    private Text nameLabel;

    private float displayed = 1f;
    private float target = 1f;

    public void Build()
    {
        RectTransform root = (RectTransform)transform;
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(1100f, 110f);
        root.anchoredPosition = new Vector2(0f, -18f);

        group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        nameLabel = PixelUI.NewTitle("Name", transform, "SOUL WARDEN", 40);
        nameLabel.rectTransform.sizeDelta = new Vector2(1100f, 46f);
        nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -22f);

        Image track = PixelUI.NewImage("Track", transform);
        track.sprite = PixelUI.Bar;
        track.type = Image.Type.Sliced;
        track.color = new Color(0.08f, 0.05f, 0.07f, 0.92f);
        track.raycastTarget = false;
        track.rectTransform.sizeDelta = new Vector2(1060f, 34f);
        track.rectTransform.anchoredPosition = new Vector2(0f, -72f);

        // A slower trailing bar behind the real one makes each hit readable.
        damageTrail = PixelUI.NewImage("Trail", track.transform);
        PixelUI.Stretch(damageTrail.rectTransform);
        damageTrail.rectTransform.offsetMin = new Vector2(4f, 4f);
        damageTrail.rectTransform.offsetMax = new Vector2(-4f, -4f);
        damageTrail.sprite = PixelUI.Bar;
        damageTrail.type = Image.Type.Filled;
        damageTrail.fillMethod = Image.FillMethod.Horizontal;
        damageTrail.color = new Color(0.75f, 0.35f, 0.55f, 0.7f);
        damageTrail.raycastTarget = false;

        fill = PixelUI.NewImage("Fill", track.transform);
        PixelUI.Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(4f, 4f);
        fill.rectTransform.offsetMax = new Vector2(-4f, -4f);
        fill.sprite = PixelUI.Bar;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.color = new Color(0.65f, 0.25f, 0.85f);
        fill.raycastTarget = false;

        Hide();
    }

    public bool IsVisible
    {
        get { return gameObject.activeSelf && group != null && group.alpha > 0.01f; }
    }

    public void Show(string bossName)
    {
        if (group == null) { return; }

        if (nameLabel != null) { nameLabel.text = bossName; }

        displayed = 1f;
        target = 1f;
        fill.fillAmount = 1f;
        damageTrail.fillAmount = 1f;

        gameObject.SetActive(true);
        group.alpha = 1f;
    }

    public void Hide()
    {
        if (group == null) { return; }

        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void SetFraction(float fraction)
    {
        target = Mathf.Clamp01(fraction);
        if (fill != null) { fill.fillAmount = target; }
    }

    private void Update()
    {
        if (damageTrail == null) { return; }

        displayed = Mathf.MoveTowards(displayed, target, Time.deltaTime * 0.35f);
        damageTrail.fillAmount = Mathf.Max(displayed, target);
    }
}
