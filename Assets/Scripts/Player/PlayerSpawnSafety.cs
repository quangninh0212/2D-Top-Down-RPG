using UnityEngine;

// Last line of defence against the player starting a level inside a wall.
//
// A spawn point can be correct in the scene and still go wrong at runtime: an
// old save file holding coordinates from a different level, a transition name
// that matches no entrance, a map edited after the fact. Whatever the cause the
// symptom is the same and the player cannot recover from it - they simply
// cannot move. So rather than trusting the placement, the result is checked and
// corrected.
public static class PlayerSpawnSafety
{
    private const float SearchStep = 0.5f;
    private const float MaxSearchRadius = 12f;
    private const int DirectionsPerRing = 24;

    public static void EnsureNotStuck(PlayerController player)
    {
        if (player == null) { return; }

        Collider2D body = player.GetComponent<Collider2D>();
        if (body == null) { return; }

        Vector2 size = body.bounds.size;
        Vector2 offset = (Vector2)body.bounds.center - (Vector2)player.transform.position;

        if (!IsBlocked(player, player.transform.position, offset, size)) { return; }

        Vector2 original = player.transform.position;
        Vector2 rescued = FindFreeSpot(player, original, offset, size);

        // Brushing against a wall is not being stuck in one; only report when
        // the player actually had to be moved.
        if ((rescued - original).sqrMagnitude < 0.0001f) { return; }

        Debug.LogWarning("[PlayerSpawnSafety] Player started inside geometry at " + original.ToString("0.00") +
                         "; moved to " + rescued.ToString("0.00") + ".");

        player.transform.position = new Vector3(rescued.x, rescued.y, player.transform.position.z);
    }

    private static Vector2 FindFreeSpot(PlayerController player, Vector2 from, Vector2 offset, Vector2 size)
    {
        // Somewhere the level itself says is a sensible place to stand, in
        // preference order: the default spawn, then any area entrance.
        AreaEntrance[] entrances = Object.FindObjectsOfType<AreaEntrance>();

        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < entrances.Length; i++)
            {
                AreaEntrance entrance = entrances[i];
                if (entrance == null) { continue; }
                if (pass == 0 && !entrance.IsDefaultSpawn) { continue; }
                if (IsBlocked(player, entrance.transform.position, offset, size)) { continue; }

                return entrance.transform.position;
            }
        }

        // Otherwise the nearest clear point, so the player keeps as much of
        // their intended position as possible.
        for (float radius = SearchStep; radius <= MaxSearchRadius; radius += SearchStep)
        {
            for (int i = 0; i < DirectionsPerRing; i++)
            {
                float angle = i * Mathf.PI * 2f / DirectionsPerRing;
                Vector2 candidate = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                // Outside the arena is free space, but it is off the map - the
                // camera does not show it and there is no way back in.
                if (!InsideArena(candidate)) { continue; }
                if (IsBlocked(player, candidate, offset, size)) { continue; }

                return candidate;
            }
        }

        return from;
    }

    private static bool InsideArena(Vector2 point)
    {
        LevelCameraAnchor anchor = Object.FindObjectOfType<LevelCameraAnchor>();
        if (anchor == null) { return true; }

        Vector2 centre = anchor.transform.position;

        // A margin in from the wall, so the rescue never lands flush against it.
        return Mathf.Abs(point.x - centre.x) <= anchor.ArenaWidth * 0.5f - 1.5f
               && Mathf.Abs(point.y - centre.y) <= anchor.ArenaHeight * 0.5f - 1.5f;
    }

    private static bool IsBlocked(PlayerController player, Vector2 position, Vector2 offset, Vector2 size)
    {
        // Well under the real size: brushing a wall must not read as being
        // stuck inside one.
        Collider2D[] hits = Physics2D.OverlapBoxAll(position + offset, size * 0.8f, 0f);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || hit.isTrigger) { continue; }
            if (hit.transform.IsChildOf(player.transform)) { continue; }
            if (hit.GetComponentInParent<PlayerController>() != null) { continue; }

            // Enemies move out of the way on their own.
            if (hit.GetComponentInParent<EnemyHealth>() != null) { continue; }

            return true;
        }

        return false;
    }
}
