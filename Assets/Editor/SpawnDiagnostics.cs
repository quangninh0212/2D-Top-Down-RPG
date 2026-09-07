using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Reports whether every place the player can be put down is actually standing
// room. A spawn inside a wall tile or a prop collider leaves the player wedged
// and unable to walk out, which is invisible when reading the scene file.
public static class SpawnDiagnostics
{
    // Roughly the player's capsule; a spawn needs this much clearance.
    private const float PlayerRadius = 0.4f;

    [MenuItem("Tools/Soulbound Gate/Debug/Check Spawn Points")]
    public static void Check()
    {
        StringBuilder report = new StringBuilder();
        List<string> blocked = new List<string>();

        foreach (string sceneName in GameScenes.Levels)
        {
            string path = "Assets/Scenes/" + sceneName + ".unity";
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            report.AppendLine();
            report.AppendLine(sceneName + ":");

            foreach (AreaEntrance entrance in FindAll<AreaEntrance>(scene))
            {
                string label = entrance.IsDefaultSpawn
                    ? "default spawn"
                    : "arrival '" + entrance.TransitionName + "'";

                Report(scene, report, blocked, sceneName, label, entrance.transform.position);
            }

            foreach (PlayerController player in FindAll<PlayerController>(scene))
            {
                Report(scene, report, blocked, sceneName, "Player prefab start", player.transform.position);
            }
        }

        report.AppendLine();
        report.AppendLine(blocked.Count == 0
            ? "SPAWN RESULT: every spawn point is clear."
            : "SPAWN RESULT: " + blocked.Count + " blocked spawn(s):");

        foreach (string entry in blocked) { report.AppendLine("  BLOCKED " + entry); }

        Debug.Log("[SPAWN CHECK]\n" + report);
    }

    private static void Report(Scene scene, StringBuilder report, List<string> blocked,
                               string sceneName, string label, Vector3 position)
    {
        List<string> reasons = Blockers(scene, position);

        report.AppendLine("  " + label + " at " + ((Vector2)position).ToString("0.0") + " -> " +
                          (reasons.Count == 0 ? "clear" : "BLOCKED by " + string.Join(", ", reasons)));

        if (reasons.Count > 0)
        {
            blocked.Add(sceneName + " " + label + " at " + ((Vector2)position).ToString("0.0") +
                        " (" + string.Join(", ", reasons) + ")");
        }
    }

    // Two independent sources of obstruction: solid tiles, and prop colliders.
    public static List<string> Blockers(Scene scene, Vector3 position)
    {
        List<string> reasons = new List<string>();

        foreach (Tilemap tilemap in AllTilemaps(scene))
        {
            TilemapCollider2D tileCollider = tilemap.GetComponent<TilemapCollider2D>();

            // A trigger tilemap is the tree-canopy overlay, which the player is
            // meant to walk under.
            if (tileCollider == null || !tileCollider.enabled || tileCollider.isTrigger) { continue; }

            // Check the cell under the point and its neighbours, because the
            // player is wider than a single cell.
            Vector3Int centre = tilemap.WorldToCell(position);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 && dy != 0) { continue; }

                    Vector3Int cell = new Vector3Int(centre.x + dx, centre.y + dy, 0);
                    if (tilemap.GetTile(cell) == null) { continue; }

                    Vector3 cellCentre = tilemap.GetCellCenterWorld(cell);
                    if (Vector2.Distance(cellCentre, position) > 0.5f + PlayerRadius) { continue; }

                    reasons.Add("tile in '" + tilemap.name + "'");
                    dx = 2;
                    break;
                }
            }
        }

        foreach (Collider2D collider in AllColliders(scene))
        {
            if (collider == null || collider.isTrigger || !collider.enabled) { continue; }
            if (collider.GetComponentInParent<PlayerController>() != null) { continue; }

            // The Cinemachine confiner is a polygon covering the whole view; the
            // player is meant to be inside it, so it would match everywhere.
            if (collider.gameObject.layer == LayerMask.NameToLayer("Camera")) { continue; }
            if (collider.GetComponentInParent<Cinemachine.CinemachineVirtualCamera>() != null) { continue; }
            if (collider.transform.root.name == "Camera") { continue; }

            // Wall geometry is checked by containment only. Using a clearance
            // radius on a large enclosing shape would report every point inside
            // the arena as blocked.
            bool largeShape = collider is TilemapCollider2D || collider is CompositeCollider2D
                              || collider.transform.root.name == "Play Area Bounds";

            bool blocked = largeShape
                ? collider.OverlapPoint(position)
                : collider.OverlapPoint(position)
                  || Vector2.Distance(collider.ClosestPoint(position), position) < PlayerRadius;

            if (blocked)
            {
                // Naming the object rather than just its root, so "Environment"
                // does not stand in for whichever prop is actually in the way.
                reasons.Add(collider is CompositeCollider2D || collider is TilemapCollider2D
                    ? "collider on '" + collider.transform.name + "'"
                    : "'" + collider.transform.name + "' at " +
                      ((Vector2)collider.transform.position).ToString("0.0"));
            }
        }

        return reasons;
    }

    private static List<Tilemap> AllTilemaps(Scene scene)
    {
        List<Tilemap> results = new List<Tilemap>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<Tilemap>(true));
        }

        return results;
    }

    private static List<Collider2D> AllColliders(Scene scene)
    {
        List<Collider2D> results = new List<Collider2D>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<Collider2D>(true));
        }

        return results;
    }

    private static List<T> FindAll<T>(Scene scene) where T : Component
    {
        List<T> results = new List<T>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<T>(true));
        }

        return results;
    }
}
