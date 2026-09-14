using UnityEngine;

// Collision object Y. Stepping on it springs the spikes: the player loses
// health and is slowed down for a moment. An active shield takes the blow
// instead and is destroyed. The trap stays on the map and re-arms.
[RequireComponent(typeof(BoxCollider2D))]
public class SpikeTrap : MonoBehaviour
{
    public enum Outcome
    {
        None,
        Damaged,
        ShieldBroken
    }

    [SerializeField] private int damage = 1;
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 2.5f;
    [SerializeField] private float rearmTime = 1.2f;

    private SpriteRenderer spriteRenderer;
    private float readyAt;
    private float lowerAt;

    public float SlowMultiplier
    {
        get { return slowMultiplier; }
    }

    private void Awake()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.8f, 0.8f);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) { spriteRenderer = gameObject.AddComponent<SpriteRenderer>(); }

        spriteRenderer.sprite = GameplaySprites.SpikesDown;
        spriteRenderer.sortingOrder = -1;
    }

    private void Update()
    {
        if (lowerAt > 0f && Time.time >= lowerAt)
        {
            lowerAt = 0f;
            spriteRenderer.sprite = GameplaySprites.SpikesDown;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrySpring(other);
    }

    // Standing still on the plate springs it again once it has re-armed.
    private void OnTriggerStay2D(Collider2D other)
    {
        TrySpring(other);
    }

    private void TrySpring(Collider2D other)
    {
        if (Time.time < readyAt) { return; }

        PlayerController player = other != null ? other.GetComponent<PlayerController>() : null;
        if (player != null) { Spring(player); }
    }

    // Generated sprite: drawn as a gizmo so it can be seen and moved in the editor.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.9f, 0.25f, 0.25f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 0.8f, 0f));
    }

    public Outcome Spring(PlayerController player)
    {
        if (player == null) { return Outcome.None; }

        PlayerHealth health = PlayerHealth.Instance;
        if (health != null && health.isDead) { return Outcome.None; }

        readyAt = Time.time + rearmTime;
        lowerAt = Time.time + 0.6f;
        spriteRenderer.sprite = GameplaySprites.SpikesUp;

        AudioManager.PlaySfx(GameSfx.TrapHit);

        Outcome outcome;
        PlayerSkills skills = PlayerSkills.Instance;

        // Effect 5: the shield is lost instead of health.
        if (skills != null && skills.BreakShield())
        {
            outcome = Outcome.ShieldBroken;
        }
        else
        {
            // Effect 3: health goes down.
            if (health != null) { health.TakeDamage(damage, transform); }
            outcome = Outcome.Damaged;
        }

        // Effect 4: slowed down either way - the spikes still catch the feet.
        player.ApplySlow(slowMultiplier, slowDuration);

        if (outcome == Outcome.Damaged) { GameMessages.Toast("BẪY GAI! BỊ LÀM CHẬM"); }

        return outcome;
    }
}
