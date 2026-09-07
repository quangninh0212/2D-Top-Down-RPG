using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Draws the walkable area of every level as text and flood-fills it from the
// spawn. A level can look fine in the scene view and still trap the player in a
// pocket; the reachable-cell count is the number that actually matters.
public static class WalkabilityDiagnostics
{
    private const int MinX = -19;
    private const int MaxX = 19;
    private const int MinY = -11;
    private const int MaxY = 11;

    // A level should offer far more room than this; anything less means the
    // player is walled in.
    private const int MinimumReachableCells = 120;

    [MenuItem("Tools/Soulbound Gate/Debug/Map Walkable Area")]
    public static void Map()
    {
        StringBuilder report = new StringBuilder();
        List<string> failures = new List<string>();

        foreach (string sceneName in GameScenes.Levels)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            MapScene(scene, sceneName, report, failures);
        }

        report.AppendLine();
        report.AppendLine(failures.Count == 0
            ? "WALKABILITY RESULT: every level is open enough."
            : "WALKABILITY RESULT: " + failures.Count + " problem(s):");

        foreach (string failure in failures) { report.AppendLine("  FAIL " + failure); }

        Debug.Log("[WALKABILITY]\n" + report);
    }

    private static void MapScene(Scene scene, string sceneName, StringBuilder report, List<string> failures)
    {
        Vector2 spawn = FindSpawn(scene);

        int width = MaxX - MinX + 1;
        int height = MaxY - MinY + 1;

        bool[,] free = new bool[width, height];

        for (int ix = 0; ix < width; ix++)
        {
            for (int iy = 0; iy < height; iy++)
            {
                Vector2 point = new Vector2(MinX + ix, MinY + iy);
                free[ix, iy] = SpawnDiagnostics.Blockers(scene, point).Count == 0;
            }
        }

        bool[,] reached = FloodFill(free, spawn, width, height);

        int reachedCount = 0;
        int freeCount = 0;

        for (int ix = 0; ix < width; ix++)
        {
            for (int iy = 0; iy < height; iy++)
            {
                if (free[ix, iy]) { freeCount++; }
                if (reached[ix, iy]) { reachedCount++; }
            }
        }

        report.AppendLine();
        report.AppendLine(sceneName + "  spawn " + spawn.ToString("0.0") +
                          "  reachable " + reachedCount + " of " + freeCount + " free cells");
        report.AppendLine("  legend: '#' blocked   '.' free but unreachable   '+' reachable   'S' spawn");

        // Printed top row first so the map reads the same way up as the game.
        for (int iy = height - 1; iy >= 0; iy--)
        {
            StringBuilder line = new StringBuilder("  ");

            for (int ix = 0; ix < width; ix++)
            {
                bool isSpawn = Mathf.RoundToInt(spawn.x) == MinX + ix && Mathf.RoundToInt(spawn.y) == MinY + iy;

                if (isSpawn) { line.Append('S'); }
                else if (!free[ix, iy]) { line.Append('#'); }
                else if (reached[ix, iy]) { line.Append('+'); }
                else { line.Append('.'); }
            }

            report.AppendLine(line.ToString());
        }

        if (reachedCount < MinimumReachableCells)
        {
            failures.Add(sceneName + " only lets the player reach " + reachedCount +
                         " cells from the spawn (expected at least " + MinimumReachableCells + ")");
        }

        // Every mandatory enemy has to be somewhere the player can actually get to.
        foreach (EnemyHealth enemy in FindAll<EnemyHealth>(scene))
        {
            if (!enemy.CountsTowardObjective) { continue; }

            if (!IsReachable(reached, enemy.transform.position, width, height))
            {
                failures.Add(sceneName + " has an unreachable enemy at " +
                             ((Vector2)enemy.transform.position).ToString("0.0"));
            }
        }

        // So are the gates, or the level cannot be finished.
        foreach (AreaExit exit in FindAll<AreaExit>(scene))
        {
            if (!IsReachable(reached, exit.transform.position, width, height))
            {
                failures.Add(sceneName + " gate '" + exit.name + "' at " +
                             ((Vector2)exit.transform.position).ToString("0.0") + " is unreachable");
            }
        }
    }

    private static bool IsReachable(bool[,] reached, Vector2 position, int width, int height)
    {
        // Anything within one cell counts, since positions are not grid aligned.
        int cx = Mathf.RoundToInt(position.x) - MinX;
        int cy = Mathf.RoundToInt(position.y) - MinY;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int x = cx + dx;
                int y = cy + dy;

                if (x < 0 || y < 0 || x >= width || y >= height) { continue; }
                if (reached[x, y]) { return true; }
            }
        }

        return false;
    }

    // Reusable by the level builder, so enemies and gates can be checked against
    // the same reachability rule this report uses.
    public static HashSet<Vector2Int> ReachableCells(Scene scene)
    {
        Vector2 spawn = FindSpawn(scene);

        int width = MaxX - MinX + 1;
        int height = MaxY - MinY + 1;

        bool[,] free = new bool[width, height];

        for (int ix = 0; ix < width; ix++)
        {
            for (int iy = 0; iy < height; iy++)
            {
                Vector2 point = new Vector2(MinX + ix, MinY + iy);
                free[ix, iy] = SpawnDiagnostics.Blockers(scene, point).Count == 0;
            }
        }

        bool[,] reached = FloodFill(free, spawn, width, height);

        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

        for (int ix = 0; ix < width; ix++)
        {
            for (int iy = 0; iy < height; iy++)
            {
                if (reached[ix, iy]) { cells.Add(new Vector2Int(MinX + ix, MinY + iy)); }
            }
        }

        return cells;
    }

    // An enemy counts as reachable when the player can get next to it. Testing
    // only the cell it stands on would condemn every enemy tucked against a
    // tree - and moving those is how they ended up drifting a step further
    // across the map on each run of the setup tool.
    public static bool IsCellReachable(HashSet<Vector2Int> cells, Vector2 position)
    {
        int cx = Mathf.RoundToInt(position.x);
        int cy = Mathf.RoundToInt(position.y);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (cells.Contains(new Vector2Int(cx + dx, cy + dy))) { return true; }
            }
        }

        return false;
    }

    private static bool[,] FloodFill(bool[,] free, Vector2 spawn, int width, int height)
    {
        bool[,] reached = new bool[width, height];

        int startX = Mathf.Clamp(Mathf.RoundToInt(spawn.x) - MinX, 0, width - 1);
        int startY = Mathf.Clamp(Mathf.RoundToInt(spawn.y) - MinY, 0, height - 1);

        if (!free[startX, startY]) { return reached; }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        reached[startX, startY] = true;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int x = cell.x + dx[i];
                int y = cell.y + dy[i];

                if (x < 0 || y < 0 || x >= width || y >= height) { continue; }
                if (reached[x, y] || !free[x, y]) { continue; }

                reached[x, y] = true;
                queue.Enqueue(new Vector2Int(x, y));
            }
        }

        return reached;
    }

    private static Vector2 FindSpawn(Scene scene)
    {
        foreach (AreaEntrance entrance in FindAll<AreaEntrance>(scene))
        {
            if (entrance.IsDefaultSpawn) { return entrance.transform.position; }
        }

        foreach (PlayerController player in FindAll<PlayerController>(scene))
        {
            return player.transform.position;
        }

        return Vector2.zero;
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
