using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The win message, shown over the gameplay screen the moment the boss dies and
// before the victory screen takes over. It is deliberately short-lived: the
// buttons belong on the victory screen, this is the celebration in place.
public class WinOverlayUI : MonoBehaviour
{
    private CanvasGroup group;
    private Image flash;
    private Image rays;
    private Text title;

    public bool IsVisible
    {
        get { return group != null && group.gameObject.activeSelf; }
    }

    public void Build()
    {
        PixelUI.Stretch((RectTransform)transform);

        group = PixelUI.NewFullScreenGroup("WinRoot", transform);

        // Nothing here is clickable; the player is watching, not choosing.
        group.blocksRaycasts = false;
        group.interactable = false;

        flash = PixelUI.NewImage("Flash", group.transform);
        PixelUI.Stretch(flash.rectTransform);
        flash.color = new Color(1f, 0.92f, 0.62f, 0f);
        flash.raycastTarget = false;

        rays = PixelUI.NewImage("Rays", group.transform);
        PixelUI.Stretch(rays.rectTransform);
        rays.rectTransform.offsetMin = new Vector2(-300f, -300f);
        rays.rectTransform.offsetMax = new Vector2(300f, 300f);
        rays.sprite = PixelUI.Glow;
        rays.color = new Color(1f, 0.82f, 0.35f, 0.3f);
        rays.raycastTarget = false;

        title = PixelUI.NewTitle("Title", group.transform, "CHIẾN THẮNG!", 118);
        title.color = new Color(1f, 0.87f, 0.45f);
        title.rectTransform.sizeDelta = new Vector2(1600f, 160f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 60f);

        Text subtitle = PixelUI.NewBody("Subtitle", group.transform, "SOUL WARDEN ĐÃ BỊ ĐÁNH BẠI", 38);
        subtitle.color = PixelUI.Cream;
        subtitle.rectTransform.sizeDelta = new Vector2(1600f, 60f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -40f);

        Hide();
    }

    public void Show()
    {
        if (group == null) { return; }

        MobileInput.ResetAll();
        PixelUI.SetGroupVisible(group, true);
        transform.SetAsLastSibling();

        // SetGroupVisible turns raycasts back on for every panel it shows; this
        // one is a celebration, not a dialog, and must not eat touches.
        group.blocksRaycasts = false;
        group.interactable = false;

        StopAllCoroutines();
        StartCoroutine(CelebrationRoutine());
    }

    public void Hide()
    {
        StopAllCoroutines();
        PixelUI.SetGroupVisible(group, false);
    }

    // A white-gold flash, then the title swells in while the glow behind it
    // breathes. Unscaled time: the world may already be slowing down.
    private IEnumerator CelebrationRoutine()
    {
        const float flashTime = 0.35f;
        float elapsed = 0f;

        while (elapsed < flashTime)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / flashTime);
            flash.color = new Color(1f, 0.92f, 0.62f, 0.75f * (1f - t));
            title.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t);
            group.alpha = Mathf.Clamp01(t * 2f);

            yield return null;
        }

        flash.color = new Color(1f, 0.92f, 0.62f, 0f);
        title.transform.localScale = Vector3.one;
        group.alpha = 1f;

        while (true)
        {
            float pulse = 0.26f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f)) * 0.2f;
            rays.color = new Color(1f, 0.82f, 0.35f, pulse);

            yield return null;
        }
    }
}
