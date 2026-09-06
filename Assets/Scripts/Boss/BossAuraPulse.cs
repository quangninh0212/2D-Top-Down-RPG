using UnityEngine;

// Slow breathing glow around the boss. Sine-driven so it never flickers, and
// cheap enough that it costs nothing on a phone.
public class BossAuraPulse : MonoBehaviour
{
    [SerializeField] private float scaleAmount = 0.08f;
    [SerializeField] private float speed = 1.3f;
    [SerializeField] private float alphaAmount = 0.12f;

    private SpriteRenderer spriteRenderer;
    private Vector3 restingScale;
    private float baseAlpha;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        restingScale = transform.localScale;

        if (spriteRenderer != null) { baseAlpha = spriteRenderer.color.a; }
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * speed);

        transform.localScale = restingScale * (1f + wave * scaleAmount);

        if (spriteRenderer == null) { return; }

        Color colour = spriteRenderer.color;
        colour.a = Mathf.Clamp01(baseAlpha + wave * alphaAmount);
        spriteRenderer.color = colour;
    }
}
