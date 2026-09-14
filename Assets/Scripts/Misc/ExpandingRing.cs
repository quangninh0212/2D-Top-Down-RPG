using UnityEngine;

// A ring that grows and fades out once, then removes itself. Used as the visual
// for the stun blast and for objects that burst when touched.
public class ExpandingRing : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color colour;
    private float startScale;
    private float endScale;
    private float duration;
    private float elapsed;

    public static void Spawn(Vector3 position, Color colour, float endDiameter, float duration)
    {
        GameObject go = new GameObject("Ring Effect");
        go.transform.position = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GameplaySprites.Ring;
        renderer.color = colour;
        renderer.sortingOrder = 20;

        ExpandingRing ring = go.AddComponent<ExpandingRing>();
        ring.spriteRenderer = renderer;
        ring.colour = colour;
        ring.startScale = 0.3f;
        ring.endScale = Mathf.Max(0.3f, endDiameter);
        ring.duration = Mathf.Max(0.05f, duration);

        go.transform.localScale = Vector3.one * ring.startScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, 1f - (1f - t) * (1f - t));

        Color faded = colour;
        faded.a *= 1f - t;
        spriteRenderer.color = faded;

        if (t >= 1f) { Destroy(gameObject); }
    }
}
