using UnityEngine;

// NPC 2 - Grape: a stand-off thrower.
//
// It wants one particular distance: far enough that the player's sword cannot
// reach, close enough to hit. It backs away when the player closes in, strafes
// so it is not a stationary target, refuses to throw at a wall, and walks
// around until it has a clear line again.
public class GrapeThrowerBrain : NpcBrain
{
    [SerializeField] private float preferredRange = 4.2f;
    [SerializeField] private float tooCloseRange = 2.6f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float strafeInterval = 1.5f;

    private IEnemy weapon;
    private float nextAttackTime;
    private float nextStrafeFlip;
    private float strafeSign = 1f;

    // Read by the smoke test: a throw that never had a clear line should not
    // have happened at all.
    public int ThrowsMade { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        weapon = GetComponent<IEnemy>();
    }

    protected override void Engage(Vector2 playerPosition, float distance)
    {
        bool clearLine = NpcSenses.HasLineOfSight(gameObject, transform.position, playerPosition);

        // Nothing to throw at through a wall: move until there is a line.
        if (!clearLine)
        {
            MoveTowards(playerPosition);
            return;
        }

        if (distance < tooCloseRange)
        {
            // Backing off is the whole point of a thrower; it still throws
            // while it retreats.
            MoveAwayFrom(playerPosition);
            TryThrow(playerPosition);
            return;
        }

        if (distance > preferredRange + 1f)
        {
            MoveTowards(playerPosition);
            TryThrow(playerPosition);
            return;
        }

        Strafe(playerPosition);
        TryThrow(playerPosition);
    }

    // Sidesteps around the player at its chosen distance, flipping direction
    // every so often so the movement is not predictable.
    private void Strafe(Vector2 playerPosition)
    {
        if (Time.time >= nextStrafeFlip)
        {
            nextStrafeFlip = Time.time + strafeInterval;
            strafeSign = Random.value < 0.5f ? -1f : 1f;
        }

        Vector2 toPlayer = (playerPosition - (Vector2)transform.position).normalized;
        Move(NpcSenses.Rotate(toPlayer, 90f * strafeSign));
    }

    private void TryThrow(Vector2 playerPosition)
    {
        if (weapon == null || Time.time < nextAttackTime) { return; }
        if (!NpcSenses.HasLineOfSight(gameObject, transform.position, playerPosition)) { return; }

        nextAttackTime = Time.time + attackCooldown;
        ThrowsMade++;

        weapon.Attack();
    }

    // Hurt: open the distance right up rather than simply walking backwards.
    protected override void Retreat(Vector2 playerPosition)
    {
        MoveAwayFrom(playerPosition);

        if (Vector2.Distance(transform.position, playerPosition) > preferredRange)
        {
            TryThrow(playerPosition);
        }
    }
}
