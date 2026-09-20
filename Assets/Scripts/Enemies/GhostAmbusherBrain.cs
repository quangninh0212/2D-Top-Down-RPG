using UnityEngine;

// NPC 3 - Ghost: an ambusher.
//
// It does not patrol and it does not chase across the map. It hangs half
// faded where it is until the player walks into its reach, then materialises
// and opens fire. If the player runs, or breaks the line with a wall, it
// blinks out and reappears behind them instead of trailing along in the open.
public class GhostAmbusherBrain : NpcBrain
{
    [SerializeField] private float ambushRange = 5f;
    [SerializeField] private float standOffRange = 3.6f;

    // Same reach the old EnemyAI had: a ghost never shoots at something it
    // could not have reached before.
    [SerializeField] private float attackRange = 5f;

    [SerializeField] private float attackCooldown = 2.4f;
    [SerializeField] private float blinkCooldown = 7f;
    [SerializeField] private float blinkTriggerRange = 4.6f;
    [SerializeField] private float blinkBehindDistance = 2.2f;

    // Faded, but plainly there: it drifts around in this state, so it has to be
    // visible enough to be read as an enemy rather than as scenery.
    private const float DormantAlpha = 0.55f;

    private IEnemy weapon;
    private float nextAttackTime;
    private float nextBlinkTime;
    private bool materialised;

    public int Blinks { get; private set; }

    public bool Materialised
    {
        get { return materialised; }
    }

    protected override void Awake()
    {
        base.Awake();
        weapon = GetComponent<IEnemy>();
    }

    protected override void Start()
    {
        base.Start();
        SetAlpha(DormantAlpha);
    }

    // It only "sees" what walks into its ambush; the rest of the room could be
    // in plain view and it would not stir.
    protected override bool CanSee(Vector2 playerPosition)
    {
        if (Vector2.Distance(transform.position, playerPosition) > ambushRange) { return false; }

        return NpcSenses.HasLineOfSight(gameObject, transform.position, playerPosition);
    }

    protected override void NoticePlayer(Vector2 playerPosition)
    {
        Materialise();
        base.NoticePlayer(playerPosition);
    }

    // It drifts around its haunt while it waits, half faded, the way it always
    // did. Standing perfectly still until touched read as a broken enemy.
    protected override void Patrol()
    {
        base.Patrol();
    }

    // It lost the player. Rather than jog after them in the open, it steps
    // through and comes out at their back - which is how it gets a second
    // ambush out of the same encounter.
    protected override void Investigate(Vector2 target)
    {
        PlayerController player = PlayerController.Instance;
        Vector2 hunted = player != null ? (Vector2)player.transform.position : target;

        if (TryBlinkBehind(hunted)) { return; }

        base.Investigate(target);
    }

    protected override void Engage(Vector2 playerPosition, float distance)
    {
        bool clearLine = NpcSenses.HasLineOfSight(gameObject, transform.position, playerPosition);

        // Too far, or cut off: reappear behind the player rather than walk.
        if ((distance > blinkTriggerRange || !clearLine) && TryBlinkBehind(playerPosition)) { return; }

        if (distance < standOffRange - 0.6f)
        {
            MoveAwayFrom(playerPosition);
        }
        else if (distance > standOffRange + 0.6f)
        {
            MoveTowards(playerPosition);
        }
        else
        {
            Hold();
        }

        if (clearLine) { TryShoot(distance); }
    }

    private void TryShoot(float distance)
    {
        if (weapon == null || Time.time < nextAttackTime) { return; }
        if (distance > attackRange) { return; }

        nextAttackTime = Time.time + attackCooldown;
        weapon.Attack();
    }

    // Steps out of the world and back in at the player's back. The spot has to
    // be clear, so it never lands inside a wall; if none of the candidates
    // work, the blink simply does not happen.
    private bool TryBlinkBehind(Vector2 playerPosition)
    {
        if (Time.time < nextBlinkTime) { return false; }

        PlayerController player = PlayerController.Instance;
        Vector2 facing = player != null && player.MoveDirection.sqrMagnitude > 0.01f
            ? player.MoveDirection.normalized
            : ((Vector2)transform.position - playerPosition).normalized;

        // Twelve directions at two distances: behind the player is the first
        // choice, but a blink that finds nowhere to land at all would leave the
        // ghost trudging along in the open, which is the thing it must not do.
        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector2 direction = NpcSenses.Rotate(-facing, (attempt % 12) * 30f);
            float reach = attempt < 12 ? blinkBehindDistance : blinkBehindDistance * 0.7f;
            Vector2 candidate = playerPosition + direction * reach;

            if (!NpcSenses.IsFree(gameObject, candidate, 0.35f)) { continue; }
            if (!NpcSenses.HasLineOfSight(gameObject, candidate, playerPosition)) { continue; }

            nextBlinkTime = Time.time + blinkCooldown;
            Blinks++;

            ExpandingRing.Spawn(transform.position, new Color(0.6f, 0.5f, 0.95f, 0.8f), 1.4f, 0.3f);
            transform.position = candidate;
            ExpandingRing.Spawn(candidate, new Color(0.6f, 0.5f, 0.95f, 0.8f), 1.4f, 0.3f);

            AudioManager.PlaySfx(GameSfx.Dash);
            Hold();

            return true;
        }

        return false;
    }

    // Hurt: fades most of the way out and drifts off, which makes it hard to
    // finish but also stops it shooting.
    protected override void OnRetreatBegan()
    {
        SetAlpha(DormantAlpha);
    }

    protected override void OnRetreatEnded()
    {
        Materialise();
    }

    private void Materialise()
    {
        materialised = true;
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (body == null) { return; }

        Color colour = body.color;
        body.color = new Color(colour.r, colour.g, colour.b, alpha);
    }
}
