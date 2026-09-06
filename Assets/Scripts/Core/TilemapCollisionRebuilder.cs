using UnityEngine;
using UnityEngine.Tilemaps;

// Rebuilds tilemap wall collision from the tiles that are actually there.
//
// A CompositeCollider2D bakes its outline into the scene file. The generated
// levels were built by copying Scene1's grid and repainting it, so they shipped
// carrying Scene1's baked outline: the walls you could see were the new map's,
// but the walls you collided with were the old one's - invisible, and enough to
// pen the player into a small pocket.
//
// Regenerating on load makes the collision follow the tiles no matter what was
// saved, so the two can never disagree again.
public static class TilemapCollisionRebuilder
{
    public static void Rebuild()
    {
        CompositeCollider2D[] composites = Object.FindObjectsOfType<CompositeCollider2D>();

        for (int i = 0; i < composites.Length; i++)
        {
            CompositeCollider2D composite = composites[i];
            if (composite == null) { continue; }

            // A composite only merges colliders that opt in. Without this the
            // regeneration would produce nothing at all and the level would have
            // no walls - the opposite failure, and a worse one.
            TilemapCollider2D tilemapCollider = composite.GetComponent<TilemapCollider2D>();

            if (tilemapCollider != null && !tilemapCollider.usedByComposite)
            {
                tilemapCollider.usedByComposite = true;
            }

            // Regenerating the composite alone is not enough: it merges whatever
            // shapes the TilemapCollider2D is currently holding, and those are
            // themselves cached from the tiles that were present when the
            // component last built them. Toggling it forces that rebuild first,
            // so the composite merges the real map rather than a stale copy.
            int before = composite.pathCount;

            if (tilemapCollider != null)
            {
                tilemapCollider.enabled = false;
                tilemapCollider.enabled = true;
            }

            composite.GenerateGeometry();

            Debug.Log("[TilemapCollisionRebuilder] Rebuilt '" + composite.name + "' collision: " +
                      before + " -> " + composite.pathCount + " outlines, bounds " +
                      composite.bounds.size.ToString("0.0") + ".");
        }

        // Overlap queries made later in the same frame - placing the player,
        // checking they are not stuck - must see the new shapes.
        Physics2D.SyncTransforms();
    }
}
