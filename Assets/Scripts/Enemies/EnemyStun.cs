using UnityEngine;

// Puts an enemy out of action for a short time: it stops moving, stops
// attacking, deals no contact damage, and its animation freezes under a cold
// tint. Added on demand by the player's stun skill, so no enemy prefab needs it.
public class EnemyStun : MonoBehaviour
{
    private static readonly Color StunTint = new Color(0.55f, 0.85f, 1f);

    private float stunnedUntil;
    private bool effectsApplied;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D body;
    private EnemyPathfinding pathfinding;
    private Color baseColour = Color.white;

    public bool Stunned
    {
        get { return Time.time < stunnedUntil; }
    }

    public float Remaining
    {
        get { return Mathf.Max(0f, stunnedUntil - Time.time); }
    }

    public static EnemyStun Apply(GameObject target, float duration)
    {
        if (target == null) { return null; }

        EnemyStun stun = target.GetComponent<EnemyStun>();
        if (stun == null) { stun = target.AddComponent<EnemyStun>(); }

        stun.Stun(duration);
        return stun;
    }

    // Safe to call from anything attached to an enemy, stunned or not.
    public static bool IsStunned(Component source)
    {
        if (source == null) { return false; }

        EnemyStun stun = source.GetComponentInParent<EnemyStun>();
        return stun != null && stun.Stunned;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        pathfinding = GetComponent<EnemyPathfinding>();

        if (spriteRenderer != null) { baseColour = spriteRenderer.color; }
    }

    public void Stun(float duration)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + Mathf.Max(0f, duration));

        if (pathfinding != null) { pathfinding.StopMoving(); }
        if (body != null) { body.velocity = Vector2.zero; }
        if (animator != null) { animator.speed = 0f; }

        effectsApplied = true;
    }

    // LateUpdate, so the tint wins over anything else recolouring the sprite
    // this frame - the boss flashes itself during its attack tells.
    private void LateUpdate()
    {
        if (!effectsApplied) { return; }

        if (Stunned)
        {
            if (spriteRenderer != null)
            {
                float pulse = 0.55f + Mathf.Sin(Time.time * 12f) * 0.15f;
                spriteRenderer.color = Color.Lerp(baseColour, StunTint, pulse);
            }

            if (body != null) { body.velocity = Vector2.zero; }
            return;
        }

        effectsApplied = false;

        if (spriteRenderer != null) { spriteRenderer.color = baseColour; }
        if (animator != null) { animator.speed = 1f; }
    }
}
