using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Walks the whole level chain in play mode the way the gates do, and reports
// where the player actually ends up on each arrival.
//
// A wrong arrival is invisible in the scene files: the entrance is placed
// correctly, but if nothing matches the incoming transition name the player
// simply keeps the position they had in the previous level - which lands them
// inside whatever happens to be at those coordinates in the new one.
public static class TransitionSmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private static int step;
    private static int framesInStep;
    private static string awaitingScene;
    private static bool leftBossRoom;

    [MenuItem("Tools/Soulbound Gate/Debug/Transition Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + GameScenes.Scene1 + ".unity", OpenSceneMode.Single);

        PlayModeTestSettings.ApplyForTest();

        Report.Clear();
        Failures.Clear();
        step = 0;
        framesInStep = 0;
        awaitingScene = null;
        leftBossRoom = false;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { return; }

        framesInStep++;

        string active = SceneManager.GetActiveScene().name;

        // Still travelling: the loading screen sits between every pair of levels.
        if (awaitingScene != null)
        {
            // The loading screen paces itself against LoadSceneAsync progress,
            // which does not advance in batch mode. This test is about where the
            // player lands, so once the loading screen has taken over - proving
            // the gate handed off correctly - it is skipped straight to the
            // destination, keeping all the transition state the gate set up.
            if (active == GameScenes.Loading && framesInStep > 10)
            {
                SceneManager.LoadScene(awaitingScene);
                return;
            }

            if (active != awaitingScene)
            {
                if (framesInStep > 6000)
                {
                    Failures.Add("Transition to " + awaitingScene + " never completed; stuck in '" + active +
                                 "' (pending='" + SceneFlow.PendingScene + "', transitioning=" +
                                 SceneFlow.IsTransitioning + ")");
                    Finish();
                }

                return;
            }

            // Give the arriving scene a few frames to place the player.
            if (framesInStep < 30) { return; }

            awaitingScene = null;
            framesInStep = 0;
        }

        if (framesInStep < 30) { return; }

        InspectArrival(active);

        if (step >= GameScenes.Levels.Length - 1)
        {
            // One more hop: walk back out of the boss room with the boss still
            // alive. That is the case that used to leave its health bar stuck
            // across the top of every later screen.
            if (!leftBossRoom)
            {
                leftBossRoom = true;

                if (StartTransitionTo(active, GameScenes.Scene4))
                {
                    framesInStep = 0;
                    return;
                }
            }

            Finish();
            return;
        }

        if (!StartNextTransition(active))
        {
            Finish();
            return;
        }

        step++;
        framesInStep = 0;
    }

    private static bool StartNextTransition(string fromScene)
    {
        return StartTransitionTo(fromScene, GameScenes.LevelScene(GameScenes.LevelNumberOf(fromScene) + 1));
    }

    private static bool StartTransitionTo(string fromScene, string target)
    {
        AreaExit gate = null;

        foreach (AreaExit exit in Object.FindObjectsOfType<AreaExit>())
        {
            if (exit.SceneToLoad == target) { gate = exit; break; }
        }

        if (gate == null)
        {
            Failures.Add(fromScene + " has no gate leading to " + target);
            return false;
        }

        Report.AppendLine("  -> using gate '" + gate.name + "' transition '" + gate.SceneTransitionName + "'");

        // The same call the gate makes once it is unlocked. The lock itself is
        // covered by other checks; this is about where the player lands.
        SceneFlow.GoToLevel(gate.SceneToLoad, gate.SceneTransitionName);

        awaitingScene = target;
        return true;
    }

    private static void InspectArrival(string sceneName)
    {
        Report.AppendLine();
        Report.AppendLine(sceneName + ":");

        PlayerController player = PlayerController.Instance;

        if (player == null)
        {
            Report.AppendLine("  no player");
            Failures.Add(sceneName + ": no player after arriving");
            return;
        }

        Vector2 position = player.transform.position;
        bool blocked = IsBlocked(position);
        int room = FreeNeighbourhood(position);

        Report.AppendLine("  player at " + position.ToString("0.0") +
                          "  blocked=" + blocked +
                          "  free cells within 4 units=" + room);

        // Names every entrance so a mismatched transition name is obvious.
        SceneManagement management = SceneManagement.Instance;
        Report.AppendLine("  incoming transition '" +
                          (management != null ? management.SceneTransitionName : "<none>") + "'");

        foreach (AreaEntrance entrance in Object.FindObjectsOfType<AreaEntrance>())
        {
            Report.AppendLine("    entrance '" + entrance.TransitionName + "'" +
                              (entrance.IsDefaultSpawn ? " (default spawn)" : "") +
                              " at " + ((Vector2)entrance.transform.position).ToString("0.0"));
        }

        if (blocked) { Failures.Add(sceneName + ": player arrives inside a collider at " + position.ToString("0.0")); }

        CheckBossBar(sceneName);

        MapRuntimeReachability(sceneName, position);

        VerifyUnstick(sceneName, player, position);

        // A player with almost nowhere to walk is trapped even if not literally
        // overlapping something.
        if (room < 12)
        {
            Failures.Add(sceneName + ": player arrives boxed in at " + position.ToString("0.0") +
                         " with only " + room + " free cells nearby");
        }
    }

    // The boss health bar belongs to the persistent overlay, so it has to be
    // cleared by whoever leaves the boss room. It used to sit across the top of
    // every later screen until the app was restarted.
    private static void CheckBossBar(string sceneName)
    {
        GameplayRuntime runtime = Object.FindObjectOfType<GameplayRuntime>();
        BossHealthBarUI bar = runtime != null ? runtime.BossBar : null;

        if (bar == null)
        {
            Report.AppendLine("  boss bar: not built");
            return;
        }

        bool bossHere = Object.FindObjectOfType<BossHealth>() != null;

        Report.AppendLine("  boss bar visible=" + bar.IsVisible + "  boss in scene=" + bossHere);

        if (bar.IsVisible && !bossHere)
        {
            Failures.Add(sceneName + ": the boss health bar is still on screen with no boss in the level");
        }
    }

    // Flood-fills the level from where the player is standing, using the live
    // physics colliders. The edit-time map is built from tile data and saved
    // collider shapes; this is what the player actually collides with at run
    // time, so a difference between the two points straight at the cause.
    private static void MapRuntimeReachability(string sceneName, Vector2 origin)
    {
        const int minX = -19;
        const int maxX = 19;
        const int minY = -11;
        const int maxY = 11;

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        bool[,] free = new bool[width, height];

        for (int ix = 0; ix < width; ix++)
        {
            for (int iy = 0; iy < height; iy++)
            {
                free[ix, iy] = !IsBlocked(new Vector2(minX + ix, minY + iy));
            }
        }

        bool[,] reached = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        int startX = Mathf.Clamp(Mathf.RoundToInt(origin.x) - minX, 0, width - 1);
        int startY = Mathf.Clamp(Mathf.RoundToInt(origin.y) - minY, 0, height - 1);

        if (free[startX, startY])
        {
            reached[startX, startY] = true;
            queue.Enqueue(new Vector2Int(startX, startY));
        }

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

        int reachedCount = 0;
        for (int ix = 0; ix < width; ix++)
        {
            for (int iy = 0; iy < height; iy++)
            {
                if (reached[ix, iy]) { reachedCount++; }
            }
        }

        Report.AppendLine("  runtime reachable cells: " + reachedCount);

        for (int iy = height - 1; iy >= 0; iy--)
        {
            StringBuilder line = new StringBuilder("    ");

            for (int ix = 0; ix < width; ix++)
            {
                line.Append(reached[ix, iy] ? '+' : (free[ix, iy] ? '.' : '#'));
            }

            Report.AppendLine(line.ToString());
        }

        if (reachedCount < 120)
        {
            Failures.Add(sceneName + ": at run time the player can only reach " + reachedCount + " cells");
            NameBlockersAround(origin);
        }
    }

    // Says what is actually in the way. A map of '#' shows where the player is
    // stopped; this says by what.
    private static void NameBlockersAround(Vector2 origin)
    {
        Report.AppendLine("  what is blocking, sampled around the player:");

        int reported = 0;

        for (int radius = 1; radius <= 6 && reported < 8; radius++)
        {
            for (int i = 0; i < 8 && reported < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                string names = BlockerNames(point);
                if (names == null) { continue; }

                // If the collider stops the player where no tile is drawn, the
                // collision shape and the map have gone out of step.
                Report.AppendLine("    " + point.ToString("0.0") + " blocked by " + names +
                                  "  | Foreground tile here: " + HasForegroundTile(point));
                reported++;
            }
        }

        if (reported == 0) { Report.AppendLine("    (nothing found)"); }
    }

    private static string HasForegroundTile(Vector2 position)
    {
        foreach (UnityEngine.Tilemaps.Tilemap tilemap in Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>())
        {
            if (tilemap.name != "Foreground") { continue; }

            Vector3Int cell = tilemap.WorldToCell(position);
            bool here = tilemap.GetTile(cell) != null;

            // The player is wider than a cell, so a neighbour can be the real
            // reason - report both.
            bool neighbour = tilemap.GetTile(cell + Vector3Int.right) != null
                             || tilemap.GetTile(cell + Vector3Int.left) != null
                             || tilemap.GetTile(cell + Vector3Int.up) != null
                             || tilemap.GetTile(cell + Vector3Int.down) != null;

            return here ? "yes" : (neighbour ? "no (neighbour has one)" : "NO - collider disagrees with the map");
        }

        return "no Foreground tilemap";
    }

    private static string BlockerNames(Vector2 position)
    {
        PlayerController player = PlayerController.Instance;
        Collider2D body = player != null ? player.GetComponent<Collider2D>() : null;

        Vector2 size = body != null ? (Vector2)body.bounds.size : new Vector2(0.6f, 0.5f);
        Vector2 offset = body != null
            ? (Vector2)body.bounds.center - (Vector2)player.transform.position
            : Vector2.zero;

        Collider2D[] hits = Physics2D.OverlapBoxAll(position + offset, size * 0.9f, 0f);
        List<string> names = new List<string>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.isTrigger) { continue; }
            if (player != null && hit.transform.IsChildOf(player.transform)) { continue; }
            if (hit.GetComponentInParent<PlayerController>() != null) { continue; }
            if (hit.GetComponentInParent<EnemyHealth>() != null) { continue; }
            if (hit.transform.root.name == "Camera") { continue; }

            names.Add(hit.GetType().Name + " on '" + hit.transform.name +
                      "' (root '" + hit.transform.root.name + "')");
        }

        return names.Count == 0 ? null : string.Join(", ", names);
    }

    // Deliberately wedges the player into a wall and checks the runtime rescue
    // gets them out. That safety net is what protects against a save file from
    // an older build holding coordinates that land inside geometry, which is
    // otherwise unrecoverable for the player.
    private static void VerifyUnstick(string sceneName, PlayerController player, Vector2 original)
    {
        Vector2 wall = FindBlockedPointNear(original);

        if (wall == original)
        {
            Report.AppendLine("  unstick check: skipped, no wall found nearby");
            return;
        }

        player.transform.position = wall;
        PlayerSpawnSafety.EnsureNotStuck(player);

        Vector2 rescued = player.transform.position;
        bool freed = !IsBlocked(rescued);

        Report.AppendLine("  unstick check: placed in wall at " + wall.ToString("0.0") +
                          " -> ended at " + rescued.ToString("0.0") + "  free=" + freed);

        if (!freed)
        {
            Failures.Add(sceneName + ": a player stuck in a wall at " + wall.ToString("0.0") + " was not rescued");
        }

        player.transform.position = original;
    }

    private static Vector2 FindBlockedPointNear(Vector2 from)
    {
        for (float radius = 1f; radius <= 14f; radius += 0.5f)
        {
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI * 2f / 24f;
                Vector2 candidate = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (IsBlocked(candidate)) { return candidate; }
            }
        }

        return from;
    }

    // Probes with the player's own collider shape rather than a guessed radius,
    // so "blocked" here means what it means to the physics engine.
    private static bool IsBlocked(Vector2 position)
    {
        PlayerController player = PlayerController.Instance;
        Collider2D body = player != null ? player.GetComponent<Collider2D>() : null;

        Vector2 size = body != null ? (Vector2)body.bounds.size : new Vector2(0.6f, 0.5f);
        Vector2 offset = body != null
            ? (Vector2)body.bounds.center - (Vector2)player.transform.position
            : Vector2.zero;

        Collider2D[] hits = Physics2D.OverlapBoxAll(position + offset, size * 0.9f, 0f);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.isTrigger) { continue; }
            if (player != null && hit.transform.IsChildOf(player.transform)) { continue; }
            if (hit.GetComponentInParent<PlayerController>() != null) { continue; }
            if (hit.GetComponentInParent<EnemyHealth>() != null) { continue; }
            if (hit.transform.root.name == "Camera") { continue; }

            return true;
        }

        return false;
    }

    // Counts standing room in the immediate area, which is what "can I actually
    // walk anywhere" means in practice.
    private static int FreeNeighbourhood(Vector2 centre)
    {
        int free = 0;

        for (int dx = -4; dx <= 4; dx++)
        {
            for (int dy = -4; dy <= 4; dy++)
            {
                if (!IsBlocked(centre + new Vector2(dx, dy))) { free++; }
            }
        }

        return free;
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? "TRANSITION RESULT: every arrival is clear."
            : "TRANSITION RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[TRANSITION SMOKE]\n" + Report);

        EditorApplication.ExitPlaymode();
        EditorApplication.update += QuitWhenStopped;
    }

    private static void QuitWhenStopped()
    {
        if (EditorApplication.isPlaying) { return; }

        EditorApplication.update -= QuitWhenStopped;

        // Put the project-wide play mode setting back before leaving.
        PlayModeTestSettings.Restore();

        EditorApplication.Exit(0);
    }
}
