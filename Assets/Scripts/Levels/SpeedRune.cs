using System;
using UnityEngine;

// Collision object X. Touching it makes the player move faster for a few
// seconds, and the rune vanishes in a burst of light. Once taken it stays gone
// for the rest of the run.
[RequireComponent(typeof(CircleCollider2D))]
public class SpeedRune : MonoBehaviour
{
    public static event Action<SpeedRune> OnConsumed;

    [SerializeField] private float speedMultiplier = 1.6f;
    [SerializeField] private float duration = 5f;

    private PersistentObjectId persistentId;
    private SpriteRenderer spriteRenderer;
    private Vector3 restingPosition;
    private bool consumed;

    public string PersistentId
    {
        get { return persistentId != null ? persistentId.Id : null; }
    }

    public float SpeedMultiplier
    {
        get { return speedMultiplier; }
    }

    public float Duration
    {
        get { return duration; }
    }

    private void Awake()
    {
        persistentId = GetComponent<PersistentObjectId>();

        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.45f;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) { spriteRenderer = gameObject.AddComponent<SpriteRenderer>(); }
        if (spriteRenderer.sprite == null) { spriteRenderer.sprite = GameplaySprites.SpeedRune; }
    }

    private void Start()
    {
        restingPosition = transform.position;
    }

    // Hovers and pulses so it reads as something to pick up.
    private void Update()
    {
        transform.position = restingPosition + new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.08f, 0f);

        float glow = 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
        spriteRenderer.color = new Color(glow, 1f, 1f, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other != null ? other.GetComponent<PlayerController>() : null;
        if (player != null) { Apply(player); }
    }

    // The sprite is generated at runtime, so without this the rune would be
    // invisible in the Scene view while placing it by hand.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Vector3 p = transform.position;
        Gizmos.DrawLine(p + Vector3.up * 0.45f, p + Vector3.right * 0.45f);
        Gizmos.DrawLine(p + Vector3.right * 0.45f, p + Vector3.down * 0.45f);
        Gizmos.DrawLine(p + Vector3.down * 0.45f, p + Vector3.left * 0.45f);
        Gizmos.DrawLine(p + Vector3.left * 0.45f, p + Vector3.up * 0.45f);
    }

    public bool Apply(PlayerController player)
    {
        if (consumed || player == null) { return false; }
        consumed = true;

        // Effect 1: faster movement.
        player.ApplySpeedBoost(speedMultiplier, duration);

        AudioManager.PlaySfx(GameSfx.SpeedUp);
        GameMessages.Toast("TĂNG TỐC " + Mathf.RoundToInt(duration) + " GIÂY!");

        // Effect 2: the rune disappears.
        ExpandingRing.Spawn(transform.position, new Color(0.4f, 0.9f, 1f, 0.9f), 2f, 0.35f);
        OnConsumed?.Invoke(this);
        Destroy(gameObject);

        return true;
    }
}
