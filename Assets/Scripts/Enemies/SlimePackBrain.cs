using UnityEngine;

// NPC 1 - Blue Slime: a pack hunter.
//
// On its own a slime is not much, so it plays the numbers. It tells the rest
// of the pack the moment it sees the player, it approaches from its own angle
// instead of queueing up behind the others, and when it is nearly dead it
// breaks off, recovers a little, and comes back in.
public class SlimePackBrain : NpcBrain
{
    [SerializeField] private float surroundRadius = 1.6f;
    [SerializeField] private float lungeRange = 1.9f;
    [SerializeField] private int recoveryHealth = 1;

    private Color normalColour = Color.white;

    protected override void Awake()
    {
        base.Awake();

        if (body != null) { normalColour = body.color; }
    }

    // Each slime owns an approach angle, so three of them arrive at three
    // different sides of the player rather than all from the same one.
    private Vector2 FlankSpot(Vector2 playerPosition)
    {
        float angle = 90f + Slot * 115f;
        Vector2 approach = NpcSenses.Rotate(Vector2.right, angle);

        Vector2 spot = playerPosition + approach * surroundRadius;

        // A spot inside a wall is worse than no plan at all.
        if (!NpcSenses.IsFree(gameObject, spot, 0.3f)) { return playerPosition; }

        return spot;
    }

    protected override void Engage(Vector2 playerPosition, float distance)
    {
        // Close enough to bite: stop being clever and go straight in, which is
        // what actually lands the contact damage.
        if (distance <= lungeRange)
        {
            MoveTowards(playerPosition);
            return;
        }

        MoveTowards(FlankSpot(playerPosition));
    }

    protected override void OnRetreatBegan()
    {
        // Visibly wounded, so the player can read why it turned tail.
        if (body != null) { body.color = new Color(0.65f, 0.75f, 1f, 0.85f); }
    }

    protected override void OnRetreatEnded()
    {
        if (body != null) { body.color = normalColour; }

        // It used the time away to pull itself together.
        if (health != null && !health.IsDead)
        {
            health.RestoreHealth(health.CurrentHealth + recoveryHealth);
        }
    }
}
