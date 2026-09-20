using UnityEngine;

// The physics an NPC brain needs to look around: can it see the player from
// here, and is a spot worth moving to. Everything in this game sits on the
// default layer, so the queries filter by component rather than by layer mask.
public static class NpcSenses
{
    // A clear line between two points: walls and props block it, triggers,
    // the player and other NPCs do not.
    public static bool HasLineOfSight(GameObject self, Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;

        if (distance <= 0.05f) { return true; }

        // A ray that starts inside a collider reports that collider by default,
        // which would have every NPC blinded by the arena-sized volumes it is
        // standing in. Only what the line actually crosses counts.
        bool queriesStartInColliders = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(from, delta / distance, distance);

        Physics2D.queriesStartInColliders = queriesStartInColliders;

        for (int i = 0; i < hits.Length; i++)
        {
            if (Blocks(hits[i].collider, self)) { return false; }
        }

        return true;
    }

    // Somewhere the NPC could stand: used before a ghost blinks and before a
    // slime commits to a flanking spot.
    public static bool IsFree(GameObject self, Vector2 point, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (Blocks(hits[i], self)) { return false; }
        }

        return true;
    }

    private static bool Blocks(Collider2D collider, GameObject self)
    {
        if (collider == null || collider.isTrigger) { return false; }

        // Its own body, and anything else alive, is not scenery.
        if (self != null && collider.transform.IsChildOf(self.transform)) { return false; }
        if (collider.GetComponentInParent<PlayerController>() != null) { return false; }
        if (collider.GetComponentInParent<EnemyHealth>() != null) { return false; }

        // The camera rig carries a map-sized polygon that Cinemachine uses to
        // clamp the view. It is hollow, it is not part of the level, and
        // standing inside it is the normal state of affairs.
        if (collider.GetComponentInParent<CameraController>() != null) { return false; }
        if (collider.GetComponentInParent<Camera>() != null) { return false; }

        return true;
    }

    // Steers a desired direction around whatever is in front of the NPC, by
    // trying progressively wider turns to each side. Without this a brain that
    // chases in a straight line simply grinds into the nearest wall.
    public static Vector2 Avoid(GameObject self, Vector2 origin, Vector2 desired, float probeDistance)
    {
        if (desired.sqrMagnitude < 0.0001f) { return desired; }

        Vector2 direction = desired.normalized;

        if (HasLineOfSight(self, origin, origin + direction * probeDistance)) { return direction; }

        for (int step = 1; step <= 4; step++)
        {
            float angle = step * 25f;

            Vector2 left = Rotate(direction, angle);
            if (HasLineOfSight(self, origin, origin + left * probeDistance)) { return left; }

            Vector2 right = Rotate(direction, -angle);
            if (HasLineOfSight(self, origin, origin + right * probeDistance)) { return right; }
        }

        return direction;
    }

    public static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(direction.x * cos - direction.y * sin,
                           direction.x * sin + direction.y * cos);
    }
}
