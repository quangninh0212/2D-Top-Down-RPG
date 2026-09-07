using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Builds the gameplay scenes. Rather than guessing at tile names, the builder
// samples Scene1 - the map the project already ships - for its grid layout and
// its actual ground and wall tiles, then paints new arenas with the same
// material. That keeps every level visually part of the same world and means a
// tile being renamed cannot break the tool.
public static class LevelSceneBuilder
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string BackupFolder = "Assets/Scenes/Backup";

    private const string PlayerPrefab = "Assets/Prefabs/Scene Management/Player.prefab";
    private const string CameraPrefab = "Assets/Prefabs/Scene Management/Camera.prefab";
    private const string ManagersPrefab = "Assets/Prefabs/Scene Management/Managers.prefab";
    private const string UICanvasPrefab = "Assets/Prefabs/Scene Management/UICanvas.prefab";

    private const string SlimePrefab = "Assets/Prefabs/Enemies/Blue Slime.prefab";
    private const string GrapePrefab = "Assets/Prefabs/Enemies/Enemie1.prefab";
    private const string GhostPrefab = "Assets/Prefabs/Enemies/Ghost.prefab";
    private const string BossPrefab = "Assets/Prefabs/Enemies/Boss/Soul Warden.prefab";
    private const string AreaExitPrefab = "Assets/Prefabs/AreaExit.prefab";
    private const string PortalVfxPrefab = "Assets/Prefabs/VFX/Portal VFX.prefab";

    // The playable arena the camera has to show in full.
    private const float ArenaWidth = 32f;
    private const float ArenaHeight = 18f;

    private const int HalfWidth = 16;
    private const int HalfHeight = 9;
    private const int WallThickness = 2;

    // A 20:9 phone showing an 18-unit-tall arena sees 40 units across, so the
    // ground is painted well past the walls. Without this the extra width would
    // be empty space outside the map.
    private const int GroundMarginX = 10;
    private const int GroundMarginY = 5;

    // ----- sampled material ----------------------------------------------

    private class GridTemplate
    {
        public GameObject GridRoot;
        public TileBase GroundTile;
        public TileBase WallTile;
        public TileBase CosmeticTile;
        public GameObject AreaExitTemplate;
        public GameObject GlobalLightTemplate;
    }

    // ----- public entry points -------------------------------------------

    public static void BuildFrontEndScenes()
    {
        BuildSimpleScene(GameScenes.Splash, "SplashScreen", typeof(SplashScreenController));
        BuildSimpleScene(GameScenes.Loading, "LoadingScreen", typeof(LoadingScreenController));
        BuildSimpleScene(GameScenes.Victory, "VictoryScreen", typeof(VictoryScreenController));
        EnsureMainMenuScene();
    }

    public static void BuildAllLevels()
    {
        BackupExistingScene(GameScenes.Scene1);
        BackupExistingScene(GameScenes.Scene2);

        PatchScene1();
        PatchScene2();
        BuildScene3();
        BuildScene4();
        BuildScene5();
    }

    // ----- front end ------------------------------------------------------

    private static void BuildSimpleScene(string sceneName, string objectName, System.Type controller)
    {
        string path = ScenesFolder + "/" + sceneName + ".unity";

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject host = new GameObject(objectName);
        host.AddComponent(controller);

        EditorSceneManager.SaveScene(scene, path);
        SoulboundSetupLog.Step("Scene " + sceneName + " ready.");
    }

    private static void EnsureMainMenuScene()
    {
        string path = ScenesFolder + "/" + GameScenes.MainMenu + ".unity";

        if (File.Exists(path))
        {
            // The menu scene already holds nothing but the MainMenu component;
            // it only needs checking, not rebuilding.
            Scene existing = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            if (Object.FindObjectOfType<MainMenu>() == null)
            {
                new GameObject("MainMenu").AddComponent<MainMenu>();
                EditorSceneManager.SaveScene(existing, path);
            }

            SoulboundSetupLog.Step("Scene MainMenu verified.");
            return;
        }

        BuildSimpleScene(GameScenes.MainMenu, "MainMenu", typeof(MainMenu));
    }

    // ----- existing levels ------------------------------------------------

    private static void PatchScene1()
    {
        string path = ScenesFolder + "/" + GameScenes.Scene1 + ".unity";
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        Transform enemies = EnsureContainer(scene, "Enemies");

        // Exactly five Blue Slime: the level's stated objective.
        TrimEnemies(enemies, SlimePrefab, 5, new[]
        {
            new Vector2(8.8f, 1.9f), new Vector2(-4.5f, -1.2f), new Vector2(-2.7f, 5.0f),
            new Vector2(7.8f, -4.0f), new Vector2(-9.5f, 2.4f)
        });

        RemoveEnemiesOtherThan(enemies, SlimePrefab);

        FinishLevel(scene, path, GameScenes.Scene1, new Vector2(0f, 0f),
                    exitEast: GameScenes.Scene2, exitWest: null,
                    eastGatePosition: new Vector2(14.8f, -1.0f), westGatePosition: Vector2.zero,
                    defaultSpawn: new Vector2(-11f, 0f));
    }

    private static void PatchScene2()
    {
        string path = ScenesFolder + "/" + GameScenes.Scene2 + ".unity";
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        Transform enemies = EnsureContainer(scene, "Enemies");

        // Scene2's objective is five Grape; the Ghosts move on to Scene4.
        ClearChildren(enemies);
        SpawnEnemies(enemies, GrapePrefab, new[]
        {
            new Vector2(-8f, 2.5f), new Vector2(-3f, -3.5f), new Vector2(2.5f, 3.5f),
            new Vector2(7f, -2f), new Vector2(10f, 3f)
        });

        FinishLevel(scene, path, GameScenes.Scene2, new Vector2(0f, 0f),
                    exitEast: GameScenes.Scene3, exitWest: GameScenes.Scene1,
                    eastGatePosition: new Vector2(14.6f, 0f), westGatePosition: new Vector2(-14.9f, 0f),
                    defaultSpawn: new Vector2(-12f, 0f));
    }

    // ----- new levels -----------------------------------------------------

    private static void BuildScene3()
    {
        Scene scene = NewLevelScene(GameScenes.Scene3, out GridTemplate template);

        // Two fighting areas separated by a broken wall with two gaps, so the
        // player can pull enemies through a choke point instead of taking all
        // seven at once.
        PaintArena(template, ArenaStyle.Crossroads);

        Transform environment = EnsureContainer(scene, "Environment");
        ScatterProps(environment, new[]
        {
            ("Assets/Prefabs/Tree.prefab", new Vector2(-12f, 5.5f)),
            ("Assets/Prefabs/Tree (1).prefab", new Vector2(-12.5f, -5.5f)),
            ("Assets/Prefabs/Tree.prefab", new Vector2(12.5f, 5.5f)),
            ("Assets/Prefabs/Tree (1).prefab", new Vector2(12f, -5.5f)),
            ("Assets/Prefabs/Bush.prefab", new Vector2(-6.5f, 3f)),
            ("Assets/Prefabs/Bush.prefab", new Vector2(-6f, -3.5f)),
            ("Assets/Prefabs/Bush.prefab", new Vector2(6.5f, 3.5f)),
            ("Assets/Prefabs/Crate.prefab", new Vector2(-9f, 0.5f)),
            ("Assets/Prefabs/Crate 1.prefab", new Vector2(9f, -0.5f)),
            ("Assets/Prefabs/Barrel.prefab", new Vector2(4f, 5f)),
            ("Assets/Prefabs/Barrel.prefab", new Vector2(-3.5f, -5.5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(-0.5f, 6.5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(-0.5f, -6.5f))
        });

        Transform enemies = EnsureContainer(scene, "Enemies");

        SpawnEnemies(enemies, SlimePrefab, new[]
        {
            new Vector2(-9.5f, 3f), new Vector2(-8f, -3f), new Vector2(-4.5f, 0f)
        });

        SpawnEnemies(enemies, GrapePrefab, new[]
        {
            new Vector2(6f, 4f), new Vector2(9.5f, 1f), new Vector2(7.5f, -3.5f), new Vector2(11.5f, -1.5f)
        });

        FinishLevel(scene, ScenesFolder + "/" + GameScenes.Scene3 + ".unity", GameScenes.Scene3,
                    Vector2.zero,
                    exitEast: GameScenes.Scene4, exitWest: GameScenes.Scene2,
                    eastGatePosition: new Vector2(14.5f, 0f), westGatePosition: new Vector2(-14.5f, 0f),
                    defaultSpawn: new Vector2(-12f, 0f));
    }

    private static void BuildScene4()
    {
        Scene scene = NewLevelScene(GameScenes.Scene4, out GridTemplate template);

        // Marsh: open water pools with wide dry lanes, because the Ghosts fire
        // spreads of projectiles and the player needs room to dodge on a phone.
        PaintArena(template, ArenaStyle.Marsh);

        Transform environment = EnsureContainer(scene, "Environment");
        ScatterProps(environment, new[]
        {
            ("Assets/Prefabs/Tree.prefab", new Vector2(-13f, 6f)),
            ("Assets/Prefabs/Tree.prefab", new Vector2(13f, -6f)),
            ("Assets/Prefabs/Tree (1).prefab", new Vector2(-13f, -6f)),
            ("Assets/Prefabs/Tree (1).prefab", new Vector2(13f, 6f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(-8f, 6.5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(0f, 6.5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(8f, 6.5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(-8f, -6.5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(8f, -6.5f)),
            ("Assets/Prefabs/Barrel.prefab", new Vector2(-5f, 0f)),
            ("Assets/Prefabs/Crate.prefab", new Vector2(5f, 0f)),
            ("Assets/Prefabs/Bush.prefab", new Vector2(-2f, 4.5f)),
            ("Assets/Prefabs/Bush.prefab", new Vector2(2f, -4.5f))
        });

        Transform enemies = EnsureContainer(scene, "Enemies");
        SpawnEnemies(enemies, GhostPrefab, new[]
        {
            new Vector2(-7f, 3.5f), new Vector2(7f, 3.5f), new Vector2(-7f, -3.5f), new Vector2(7f, -3.5f)
        });

        FinishLevel(scene, ScenesFolder + "/" + GameScenes.Scene4 + ".unity", GameScenes.Scene4,
                    Vector2.zero,
                    exitEast: GameScenes.Scene5, exitWest: GameScenes.Scene3,
                    eastGatePosition: new Vector2(14.5f, 0f), westGatePosition: new Vector2(-14.5f, 0f),
                    defaultSpawn: new Vector2(-12f, 0f));
    }

    private static void BuildScene5()
    {
        Scene scene = NewLevelScene(GameScenes.Scene5, out GridTemplate template);

        // A clean circular arena. The boss fills it with projectile patterns,
        // so nothing is placed that could trap the player against a wall.
        PaintArena(template, ArenaStyle.BossRoom);

        Transform environment = EnsureContainer(scene, "Environment");
        ScatterProps(environment, new[]
        {
            ("Assets/Prefabs/Torche.prefab", new Vector2(-10f, 5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(10f, 5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(-10f, -5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(10f, -5f)),
            ("Assets/Prefabs/Torche.prefab", new Vector2(0f, 6.5f))
        });

        Transform enemies = EnsureContainer(scene, "Enemies");

        GameObject boss = InstantiateAt(BossPrefab, enemies, new Vector2(0f, 3.5f));
        if (boss == null)
        {
            SoulboundSetupLog.Warn("Boss prefab missing; Scene5 built without it.");
        }

        FinishLevel(scene, ScenesFolder + "/" + GameScenes.Scene5 + ".unity", GameScenes.Scene5,
                    Vector2.zero,
                    exitEast: null, exitWest: GameScenes.Scene4,
                    eastGatePosition: Vector2.zero, westGatePosition: new Vector2(-14.5f, -0.5f),
                    defaultSpawn: new Vector2(0f, -6f));
    }

    // ----- scene scaffolding ---------------------------------------------

    // Creates an empty level and moves a copy of Scene1's grid, camera and
    // lighting into it, so every component setting - colliders, sorting orders,
    // parallax, the URP light - matches the shipped levels exactly.
    private static Scene NewLevelScene(string sceneName, out GridTemplate template)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        template = CopyTemplateFromScene1(scene);

        InstantiateAt(CameraPrefab, null, Vector2.zero);
        InstantiateAt(ManagersPrefab, null, Vector2.zero);
        InstantiateAt(UICanvasPrefab, null, Vector2.zero);
        InstantiateAt(PlayerPrefab, null, new Vector2(-12f, 0f));

        return scene;
    }

    private static GridTemplate CopyTemplateFromScene1(Scene destination)
    {
        GridTemplate template = new GridTemplate();

        string sourcePath = ScenesFolder + "/" + GameScenes.Scene1 + ".unity";
        Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);

        foreach (GameObject root in source.GetRootGameObjects())
        {
            if (root.name == "Grid")
            {
                template.GridRoot = Object.Instantiate(root);
                template.GridRoot.name = "Grid";
                SceneManager.MoveGameObjectToScene(template.GridRoot, destination);
            }
            else if (root.name == "Global Light 2D")
            {
                template.GlobalLightTemplate = Object.Instantiate(root);
                template.GlobalLightTemplate.name = "Global Light 2D";
                SceneManager.MoveGameObjectToScene(template.GlobalLightTemplate, destination);
            }
            else if (root.name == "AreaExit")
            {
                template.AreaExitTemplate = Object.Instantiate(root);
                template.AreaExitTemplate.name = "AreaExitTemplate";
                template.AreaExitTemplate.SetActive(false);
                SceneManager.MoveGameObjectToScene(template.AreaExitTemplate, destination);
            }
        }

        SampleTiles(template);
        ClearTilemaps(template.GridRoot);

        EditorSceneManager.CloseScene(source, true);
        return template;
    }

    // Picks the most-used tile in each layer, which is the ground fill and the
    // wall body respectively.
    private static void SampleTiles(GridTemplate template)
    {
        if (template.GridRoot == null) { return; }

        template.GroundTile = MostCommonTile(FindTilemap(template.GridRoot, "Grass"));
        template.WallTile = MostCommonTile(FindTilemap(template.GridRoot, "Foreground"));
        template.CosmeticTile = MostCommonTile(FindTilemap(template.GridRoot, "Cosmetic"));

        if (template.GroundTile == null || template.WallTile == null)
        {
            SoulboundSetupLog.Warn("Could not sample ground/wall tiles from Scene1; new maps may be empty.");
        }
    }

    private static TileBase MostCommonTile(Tilemap tilemap)
    {
        if (tilemap == null) { return null; }

        Dictionary<TileBase, int> counts = new Dictionary<TileBase, int>();

        foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(position);
            if (tile == null) { continue; }

            counts.TryGetValue(tile, out int count);
            counts[tile] = count + 1;
        }

        TileBase best = null;
        int bestCount = 0;

        foreach (KeyValuePair<TileBase, int> pair in counts)
        {
            if (pair.Value <= bestCount) { continue; }

            best = pair.Key;
            bestCount = pair.Value;
        }

        return best;
    }

    private static Tilemap FindTilemap(GameObject gridRoot, string layerName)
    {
        if (gridRoot == null) { return null; }

        Transform layer = gridRoot.transform.Find(layerName);
        return layer != null ? layer.GetComponent<Tilemap>() : null;
    }

    private static void ClearTilemaps(GameObject gridRoot)
    {
        if (gridRoot == null) { return; }

        foreach (Tilemap tilemap in gridRoot.GetComponentsInChildren<Tilemap>(true))
        {
            tilemap.ClearAllTiles();
        }
    }

    // ----- painting -------------------------------------------------------

    private enum ArenaStyle
    {
        Crossroads,
        Marsh,
        BossRoom
    }

    private static void PaintArena(GridTemplate template, ArenaStyle style)
    {
        Tilemap ground = FindTilemap(template.GridRoot, "Grass");
        Tilemap walls = FindTilemap(template.GridRoot, "Foreground");
        Tilemap cosmetic = FindTilemap(template.GridRoot, "Cosmetic");

        if (ground == null || walls == null) { return; }

        // Floor everywhere inside the frame.
        for (int x = -HalfWidth; x <= HalfWidth; x++)
        {
            for (int y = -HalfHeight; y <= HalfHeight; y++)
            {
                ground.SetTile(new Vector3Int(x, y, 0), template.GroundTile);
            }
        }

        // A solid band of wall around the edge. It is several tiles thick so a
        // dashing player cannot clip through the boundary.
        for (int x = -HalfWidth; x <= HalfWidth; x++)
        {
            for (int y = -HalfHeight; y <= HalfHeight; y++)
            {
                bool onEdge = x <= -HalfWidth + WallThickness - 1 || x >= HalfWidth - WallThickness + 1
                              || y <= -HalfHeight + WallThickness - 1 || y >= HalfHeight - WallThickness + 1;

                if (onEdge) { walls.SetTile(new Vector3Int(x, y, 0), template.WallTile); }
            }
        }

        switch (style)
        {
            case ArenaStyle.Crossroads:
                PaintCrossroads(walls, cosmetic, template);
                break;

            case ArenaStyle.Marsh:
                PaintMarsh(cosmetic, template);
                break;

            case ArenaStyle.BossRoom:
                PaintBossRoom(walls, cosmetic, template);
                break;
        }

        CarveGateOpenings(walls);
    }

    // A dividing wall down the middle with two doorways.
    private static void PaintCrossroads(Tilemap walls, Tilemap cosmetic, GridTemplate template)
    {
        for (int y = -HalfHeight + WallThickness; y <= HalfHeight - WallThickness; y++)
        {
            // Two gaps, north and south of centre.
            bool doorway = (y >= 2 && y <= 4) || (y >= -4 && y <= -2);
            if (doorway) { continue; }

            walls.SetTile(new Vector3Int(-1, y, 0), template.WallTile);
            walls.SetTile(new Vector3Int(0, y, 0), template.WallTile);
        }

        SprinkleCosmetic(cosmetic, template, 26, 20240301);
    }

    // Cosmetic-only pools: they read as marsh water without adding colliders
    // the player could get stuck against.
    private static void PaintMarsh(Tilemap cosmetic, GridTemplate template)
    {
        if (cosmetic == null || template.CosmeticTile == null) { return; }

        Vector2Int[] pools = { new Vector2Int(-9, 1), new Vector2Int(9, -1), new Vector2Int(0, 5), new Vector2Int(0, -5) };

        for (int i = 0; i < pools.Length; i++)
        {
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    if (x * x * 0.35f + y * y > 4.2f) { continue; }

                    cosmetic.SetTile(new Vector3Int(pools[i].x + x, pools[i].y + y, 0), template.CosmeticTile);
                }
            }
        }
    }

    // Rounded corners so the boss arena reads as a chamber rather than a box.
    private static void PaintBossRoom(Tilemap walls, Tilemap cosmetic, GridTemplate template)
    {
        int inner = HalfWidth - WallThickness;

        for (int x = -HalfWidth; x <= HalfWidth; x++)
        {
            for (int y = -HalfHeight; y <= HalfHeight; y++)
            {
                float normalisedX = x / (float)inner;
                float normalisedY = y / (float)(HalfHeight - WallThickness);

                // Outside the ellipse becomes wall, which rounds the corners off.
                if (normalisedX * normalisedX + normalisedY * normalisedY > 1.05f)
                {
                    walls.SetTile(new Vector3Int(x, y, 0), template.WallTile);
                }
            }
        }

        SprinkleCosmetic(cosmetic, template, 18, 20240505);
    }

    private static void SprinkleCosmetic(Tilemap cosmetic, GridTemplate template, int count, int seed)
    {
        if (cosmetic == null || template.CosmeticTile == null) { return; }

        // Seeded, so rebuilding produces the same map rather than a new one.
        System.Random random = new System.Random(seed);

        for (int i = 0; i < count; i++)
        {
            int x = random.Next(-HalfWidth + WallThickness + 1, HalfWidth - WallThickness);
            int y = random.Next(-HalfHeight + WallThickness + 1, HalfHeight - WallThickness);

            cosmetic.SetTile(new Vector3Int(x, y, 0), template.CosmeticTile);
        }
    }

    // Doorways in the side walls, lined up with where the gates are placed.
    private static void CarveGateOpenings(Tilemap walls)
    {
        for (int y = -2; y <= 2; y++)
        {
            for (int x = HalfWidth - WallThickness; x <= HalfWidth; x++)
            {
                walls.SetTile(new Vector3Int(x, y, 0), null);
            }

            for (int x = -HalfWidth; x <= -HalfWidth + WallThickness; x++)
            {
                walls.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }

    // ----- shared level finishing ----------------------------------------

    private static void FinishLevel(Scene scene, string path, string sceneName, Vector2 cameraCentre,
                                    string exitEast, string exitWest,
                                    Vector2 eastGatePosition, Vector2 westGatePosition, Vector2 defaultSpawn)
    {
        EnsureLevelManager(scene);
        EnsureCameraAnchor(scene, cameraCentre);

        ConfigureGates(scene, sceneName, exitEast, exitWest, eastGatePosition, westGatePosition);

        // Order matters: the gates must exist before their approaches can be
        // cleared, and the ground margin is painted last so it fills whatever
        // the carving left bare.
        if (exitEast != null) { CarveGateApproach(scene, eastGatePosition); }
        if (exitWest != null) { CarveGateApproach(scene, westGatePosition); }

        ExtendGroundMargin(scene);
        ThinTopLayer(scene);
        RefreshTilemapColliders(scene);
        EnsurePlayAreaBounds(scene, cameraCentre);

        // Spawns are placed last, once the map geometry is final, and are
        // checked against it rather than trusted.
        EnsureDefaultSpawn(scene, defaultSpawn);
        NudgeBlockedEntrances(scene);
        NudgeUnreachableEnemies(scene);

        AssignPersistentIds(scene);
        RemoveTemplateLeftovers(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);

        SoulboundSetupLog.Step("Scene " + sceneName + " built (" + CountEnemies(scene) + " mandatory enemies).");
    }

    private static int CountEnemies(Scene scene)
    {
        int count = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EnemyHealth enemy in root.GetComponentsInChildren<EnemyHealth>(true))
            {
                if (enemy.CountsTowardObjective) { count++; }
            }
        }

        return count;
    }

    private static void EnsureLevelManager(Scene scene)
    {
        if (FindInScene<LevelManager>(scene) != null) { return; }

        GameObject go = new GameObject("Level Manager");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<LevelManager>();
    }

    private static void EnsureCameraAnchor(Scene scene, Vector2 centre)
    {
        LevelCameraAnchor anchor = FindInScene<LevelCameraAnchor>(scene);

        if (anchor == null)
        {
            GameObject go = new GameObject("Fixed Camera Anchor");
            SceneManager.MoveGameObjectToScene(go, scene);
            anchor = go.AddComponent<LevelCameraAnchor>();
        }

        anchor.transform.position = new Vector3(centre.x, centre.y, 0f);

        SerializedObject serialized = new SerializedObject(anchor);
        serialized.FindProperty("arenaWidth").floatValue = ArenaWidth;
        serialized.FindProperty("arenaHeight").floatValue = ArenaHeight;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // A closed collider ring around the playable arena. The tilemap walls do
    // most of the work, but the hand-built levels were drawn for a camera that
    // followed the player, so their edges cannot be trusted to be sealed.
    private static void EnsurePlayAreaBounds(Scene scene, Vector2 centre)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Play Area Bounds") { Object.DestroyImmediate(root); }
        }

        GameObject bounds = new GameObject("Play Area Bounds");
        SceneManager.MoveGameObjectToScene(bounds, scene);
        bounds.transform.position = centre;

        // Indestructible so projectiles stop at the boundary instead of
        // sailing off screen.
        bounds.AddComponent<Indestructible>();

        float halfWidth = ArenaWidth * 0.5f;
        float halfHeight = ArenaHeight * 0.5f;
        const float thickness = 2f;

        AddBoundsWall(bounds, new Vector2(0f, halfHeight + thickness * 0.5f), new Vector2(ArenaWidth + thickness * 2f, thickness));
        AddBoundsWall(bounds, new Vector2(0f, -halfHeight - thickness * 0.5f), new Vector2(ArenaWidth + thickness * 2f, thickness));
        AddBoundsWall(bounds, new Vector2(-halfWidth - thickness * 0.5f, 0f), new Vector2(thickness, ArenaHeight + thickness * 2f));
        AddBoundsWall(bounds, new Vector2(halfWidth + thickness * 0.5f, 0f), new Vector2(thickness, ArenaHeight + thickness * 2f));
    }

    private static void AddBoundsWall(GameObject parent, Vector2 offset, Vector2 size)
    {
        BoxCollider2D collider = parent.AddComponent<BoxCollider2D>();
        collider.offset = offset;
        collider.size = size;
    }

    // Clears wall tiles in front of a gate and floors the opening, so a gate
    // added to an existing map is never walled in.
    private static void CarveGateApproach(Scene scene, Vector2 gatePosition)
    {
        Tilemap ground = FindTilemapInScene(scene, "Grass");
        Tilemap walls = FindTilemapInScene(scene, "Foreground");
        Tilemap top = FindTilemapInScene(scene, "Top");

        if (walls == null) { return; }

        TileBase groundTile = ground != null ? MostCommonTile(ground) : null;

        int centreX = Mathf.RoundToInt(gatePosition.x);
        int centreY = Mathf.RoundToInt(gatePosition.y);

        // Wide enough to walk through comfortably with a thumbstick, and deep
        // enough to reach past the thickest wall.
        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector3Int cell = new Vector3Int(centreX + dx, centreY + dy, 0);

                walls.SetTile(cell, null);
                if (top != null) { top.SetTile(cell, null); }
                if (ground != null && groundTile != null) { ground.SetTile(cell, groundTile); }
            }
        }
    }

    // Paints ground well past the arena walls. A 20:9 phone sees far more width
    // than a 16:9 one, and without this the extra view would show empty space
    // beyond the edge of the map.
    private static void ExtendGroundMargin(Scene scene)
    {
        Tilemap ground = FindTilemapInScene(scene, "Grass");
        if (ground == null) { return; }

        TileBase groundTile = MostCommonTile(ground);
        if (groundTile == null) { return; }

        for (int x = -HalfWidth - GroundMarginX; x <= HalfWidth + GroundMarginX; x++)
        {
            for (int y = -HalfHeight - GroundMarginY; y <= HalfHeight + GroundMarginY; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                // Only fills gaps: whatever the level already painted stays.
                if (ground.GetTile(cell) == null) { ground.SetTile(cell, groundTile); }
            }
        }
    }

    // Collision shapes are cached from the tiles that were there when they were
    // last built. After repainting a map they have to be rebuilt, or the level
    // is saved with the previous map's walls - invisible, but solid.
    private static void RefreshTilemapColliders(Scene scene)
    {
        // Two passes, in this order. A composite merges whatever shapes its
        // TilemapCollider2D is holding, and those are cached from the tiles that
        // were there when it last built them - which, for a grid copied out of
        // Scene1, is Scene1's map. Every tilemap collider has to re-read its
        // tiles, and physics has to catch up, before any composite is merged.
        List<GameObject> targets = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (TilemapCollider2D collider in root.GetComponentsInChildren<TilemapCollider2D>(true))
            {
                targets.Add(collider.gameObject);
            }
        }

        for (int i = 0; i < targets.Count; i++)
        {
            RecreateColliders(targets[i]);
        }

        Physics2D.SyncTransforms();

        // The regenerated outline lives on the component, so the scene itself
        // has to be marked dirty or the old geometry is what gets written out.
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // Toggling a TilemapCollider2D asks it to re-read its tiles, but in batch
    // mode that rebuild is deferred and the scene gets saved with the old shape
    // still attached. Replacing the components outright leaves no cache to carry
    // the previous map's outline across.
    private static void RecreateColliders(GameObject target)
    {
        TilemapCollider2D oldTilemapCollider = target.GetComponent<TilemapCollider2D>();
        CompositeCollider2D oldComposite = target.GetComponent<CompositeCollider2D>();

        if (oldTilemapCollider == null) { return; }

        bool isTrigger = oldTilemapCollider.isTrigger;
        bool usedByComposite = oldTilemapCollider.usedByComposite;

        bool hadComposite = oldComposite != null;
        CompositeCollider2D.GeometryType geometryType = hadComposite
            ? oldComposite.geometryType : CompositeCollider2D.GeometryType.Outlines;
        bool compositeIsTrigger = hadComposite && oldComposite.isTrigger;

        // The composite depends on the tilemap collider, so it goes first.
        if (hadComposite) { Object.DestroyImmediate(oldComposite); }
        Object.DestroyImmediate(oldTilemapCollider);

        TilemapCollider2D tilemapCollider = target.AddComponent<TilemapCollider2D>();
        tilemapCollider.isTrigger = isTrigger;
        tilemapCollider.usedByComposite = usedByComposite;

        if (!hadComposite)
        {
            EditorUtility.SetDirty(target);
            return;
        }

        CompositeCollider2D composite = target.AddComponent<CompositeCollider2D>();
        composite.geometryType = geometryType;
        composite.isTrigger = compositeIsTrigger;
        composite.GenerateGeometry();

        EditorUtility.SetDirty(target);

        SoulboundSetupLog.Step("Rebuilt collision on '" + target.name + "': " +
                               composite.pathCount + " outlines, bounds " + composite.bounds.size.ToString("0.0") + ".");
    }

    // The 'Top' layer is tree canopy drawn above the player. It was authored for
    // a camera that followed the player closely; with the whole arena on screen
    // at once it covers most of the playfield and hides the fight. This clears
    // the canopy over the play area and keeps the band around the edges, which
    // still frames the level.
    private static void ThinTopLayer(Scene scene)
    {
        Tilemap top = FindTilemapInScene(scene, "Top");
        if (top == null) { return; }

        // TransparentDetection fades the canopy using OnTriggerEnter2D, so this
        // collider is meant to detect the player, not stop them.
        TilemapCollider2D collider = top.GetComponent<TilemapCollider2D>();
        if (collider != null && !collider.isTrigger)
        {
            collider.isTrigger = true;
            SoulboundSetupLog.Step("Top canopy collider in " + scene.name + " set to trigger so it no longer blocks movement.");
        }

        const int keepOutsideX = 12;
        const int keepOutsideY = 6;

        int removed = 0;

        foreach (Vector3Int cell in top.cellBounds.allPositionsWithin)
        {
            if (top.GetTile(cell) == null) { continue; }
            if (Mathf.Abs(cell.x) > keepOutsideX || Mathf.Abs(cell.y) > keepOutsideY) { continue; }

            top.SetTile(cell, null);
            removed++;
        }

        SoulboundSetupLog.Step("Thinned " + removed + " canopy tiles from the play area in " + scene.name + ".");
    }

    private static Tilemap FindTilemapInScene(Scene scene, string layerName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap.name == layerName) { return tilemap; }
            }
        }

        return null;
    }

    // Where the player appears on a brand new run, or when a save stored no
    // position and no gate was used.
    private static void EnsureDefaultSpawn(Scene scene, Vector2 position)
    {
        // A spawn left over from an earlier run is re-checked, not trusted: the
        // map around it may have changed, and it may have been wrong to start
        // with.
        foreach (AreaEntrance existing in FindAllInScene<AreaEntrance>(scene))
        {
            if (!existing.IsDefaultSpawn) { continue; }

            existing.transform.position = FindClearSpawn(scene, existing.transform.position);
            return;
        }

        GameObject go = new GameObject("Default Spawn");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = FindClearSpawn(scene, position);

        AreaEntrance entrance = go.AddComponent<AreaEntrance>();

        SerializedObject serialized = new SerializedObject(entrance);
        serialized.FindProperty("transitionName").stringValue = "";
        serialized.FindProperty("isDefaultSpawn").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // A hand-placed coordinate is a guess about a map the builder did not draw.
    // This checks it against the finished geometry and, if the player would be
    // wedged in a wall, walks outward to the nearest spot that is genuinely
    // standing room.
    private static Vector3 FindClearSpawn(Scene scene, Vector2 preferred)
    {
        if (SpawnDiagnostics.Blockers(scene, preferred).Count == 0) { return preferred; }

        // The authored Player position is a playtested fallback before resorting
        // to a search.
        PlayerController player = FindInScene<PlayerController>(scene);
        if (player != null && SpawnDiagnostics.Blockers(scene, player.transform.position).Count == 0)
        {
            SoulboundSetupLog.Step("Spawn " + preferred.ToString("0.0") + " in " + scene.name +
                                   " was blocked; using the Player's authored position instead.");
            return player.transform.position;
        }

        // Rings outward in half-cell steps, taking the first clear point, which
        // keeps the spawn as close to the intended spot as possible.
        for (float radius = 0.5f; radius <= 10f; radius += 0.5f)
        {
            for (int step = 0; step < 16; step++)
            {
                float angle = step * Mathf.PI * 2f / 16f;
                Vector2 candidate = preferred + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (Mathf.Abs(candidate.x) > ArenaWidth * 0.5f - 1f) { continue; }
                if (Mathf.Abs(candidate.y) > ArenaHeight * 0.5f - 1f) { continue; }

                if (SpawnDiagnostics.Blockers(scene, candidate).Count != 0) { continue; }

                SoulboundSetupLog.Step("Spawn " + preferred.ToString("0.0") + " in " + scene.name +
                                       " was blocked; moved to " + candidate.ToString("0.0") + ".");
                return candidate;
            }
        }

        SoulboundSetupLog.Warn("No clear spawn found near " + preferred.ToString("0.0") + " in " + scene.name + ".");
        return preferred;
    }

    // Arrival points sit just inside a gate, which is a doorway cut through a
    // wall - a small placement error puts them in the wall itself.
    private static void NudgeBlockedEntrances(Scene scene)
    {
        foreach (AreaEntrance entrance in FindAllInScene<AreaEntrance>(scene))
        {
            if (entrance.IsDefaultSpawn) { continue; }
            if (SpawnDiagnostics.Blockers(scene, entrance.transform.position).Count == 0) { continue; }

            Vector3 moved = FindClearSpawn(scene, entrance.transform.position);
            entrance.transform.position = moved;
        }
    }

    // An enemy the player cannot walk to can never be killed, and the gate opens
    // only when every mandatory enemy is dead - so one enemy dropped behind a
    // wall makes the level impossible to finish.
    private static void NudgeUnreachableEnemies(Scene scene)
    {
        HashSet<Vector2Int> reachable = WalkabilityDiagnostics.ReachableCells(scene);
        if (reachable.Count == 0) { return; }

        foreach (EnemyHealth enemy in FindAllInScene<EnemyHealth>(scene))
        {
            if (!enemy.CountsTowardObjective) { continue; }
            if (WalkabilityDiagnostics.IsCellReachable(reachable, enemy.transform.position)) { continue; }

            Vector3 moved = NearestReachablePoint(reachable, enemy.transform.position);

            SoulboundSetupLog.Step("Enemy in " + scene.name + " at " +
                                   ((Vector2)enemy.transform.position).ToString("0.0") +
                                   " was unreachable; moved to " + ((Vector2)moved).ToString("0.0") + ".");

            enemy.transform.position = moved;
        }
    }

    private static Vector3 NearestReachablePoint(HashSet<Vector2Int> reachable, Vector2 from)
    {
        Vector2Int best = Vector2Int.zero;
        float bestDistance = float.MaxValue;

        foreach (Vector2Int cell in reachable)
        {
            float distance = ((Vector2)cell - from).sqrMagnitude;
            if (distance >= bestDistance) { continue; }

            bestDistance = distance;
            best = cell;
        }

        return new Vector3(best.x, best.y, 0f);
    }

    // ----- gates ----------------------------------------------------------

    private static void ConfigureGates(Scene scene, string sceneName, string exitEast, string exitWest,
                                       Vector2 eastPosition, Vector2 westPosition)
    {
        List<AreaExit> existing = FindAllInScene<AreaExit>(scene);
        GameObject template = FindTemplate(scene);

        // Existing scenes already carry one gate; it is reused and re-aimed
        // rather than replaced, so its portal art and collider survive.
        AreaExit east = exitEast != null ? TakeOrCreateGate(scene, existing, template, "Gate East", eastPosition) : null;
        AreaExit west = exitWest != null ? TakeOrCreateGate(scene, existing, template, "Gate West", westPosition) : null;

        if (east != null) { ConfigureGate(east, sceneName + "->" + exitEast, exitEast, sceneName, "East"); }
        if (west != null) { ConfigureGate(west, sceneName + "->" + exitWest, exitWest, sceneName, "West"); }

        // Any gate left over from the old layout is removed.
        for (int i = 0; i < existing.Count; i++)
        {
            if (existing[i] != null) { Object.DestroyImmediate(existing[i].gameObject); }
        }
    }

    private static AreaExit TakeOrCreateGate(Scene scene, List<AreaExit> pool, GameObject template,
                                             string name, Vector2 position)
    {
        AreaExit gate = null;

        if (pool.Count > 0)
        {
            gate = pool[0];
            pool.RemoveAt(0);
        }
        else if (template != null)
        {
            GameObject copy = Object.Instantiate(template);
            copy.SetActive(true);
            SceneManager.MoveGameObjectToScene(copy, scene);
            gate = copy.GetComponent<AreaExit>();
        }
        else
        {
            // The patched scenes carry no template, so a second gate comes from
            // the project's own AreaExit prefab.
            GameObject spawned = InstantiateAt(AreaExitPrefab, null, position);
            if (spawned != null)
            {
                gate = spawned.GetComponent<AreaExit>();
                AddPortalEffect(spawned);
            }
        }

        if (gate == null) { return null; }

        gate.name = name;
        gate.transform.position = position;

        return gate;
    }

    // The transition name is the contract between the two sides of a doorway:
    // the exit sets it, and the matching entrance on the far side answers to it.
    private static void ConfigureGate(AreaExit gate, string transitionName, string targetScene,
                                      string ownScene, string side)
    {
        SerializedObject serialized = new SerializedObject(gate);
        serialized.FindProperty("sceneToLoad").stringValue = targetScene;
        serialized.FindProperty("sceneTransitionName").stringValue = transitionName;
        serialized.FindProperty("direction").enumValueIndex = 0;   // Auto
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // The entrance sits just inside the gate so arriving does not
        // immediately retrigger it.
        AreaEntrance entrance = gate.GetComponentInChildren<AreaEntrance>(true);

        if (entrance == null)
        {
            GameObject go = new GameObject("Arrival");
            go.transform.SetParent(gate.transform, false);
            entrance = go.AddComponent<AreaEntrance>();
        }

        float inset = side == "East" ? -2.2f : 2.2f;
        entrance.transform.localPosition = new Vector3(inset, 0f, 0f);

        SerializedObject entranceSerialized = new SerializedObject(entrance);
        entranceSerialized.FindProperty("transitionName").stringValue = targetScene + "->" + ownScene;
        entranceSerialized.FindProperty("isDefaultSpawn").boolValue = false;
        entranceSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddPortalEffect(GameObject gate)
    {
        if (gate.GetComponentInChildren<ParticleSystem>(true) != null) { return; }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalVfxPrefab);
        if (prefab == null) { return; }

        GameObject vfx = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        vfx.transform.SetParent(gate.transform, false);
        vfx.transform.localPosition = Vector3.zero;
    }

    private static GameObject FindTemplate(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "AreaExitTemplate") { return root; }
        }

        // Falls back to Scene1's gate, which every level's gate is a copy of.
        return null;
    }

    private static void RemoveTemplateLeftovers(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "AreaExitTemplate") { Object.DestroyImmediate(root); }
        }
    }

    // ----- persistence ids ------------------------------------------------

    // Every enemy and destructible needs a stable id so the save file can
    // remember it. Ids are derived from the scene name and a running index, so
    // rebuilding a level reproduces exactly the same ids.
    private static void AssignPersistentIds(Scene scene)
    {
        // Ids have to be unique inside a scene: the save file records "this one
        // is dead" by id, so two objects sharing one would die together.
        // Duplicating a GameObject in the editor copies its id, so uniqueness is
        // enforced here rather than assumed.
        HashSet<string> used = new HashSet<string>();
        int index = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EnemyHealth enemy in root.GetComponentsInChildren<EnemyHealth>(true))
            {
                AssignId(enemy.gameObject, scene.name + ":enemy:" + index++, used);
            }
        }

        index = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Destructible destructible in root.GetComponentsInChildren<Destructible>(true))
            {
                AssignId(destructible.gameObject, scene.name + ":prop:" + index++, used);
            }
        }
    }

    private static void AssignId(GameObject target, string fallbackId, HashSet<string> used)
    {
        PersistentObjectId component = target.GetComponent<PersistentObjectId>();
        if (component == null) { component = target.AddComponent<PersistentObjectId>(); }

        // An id that is present and unique is kept, so re-running the setup tool
        // does not renumber objects a player's save file already refers to.
        if (component.HasId && used.Add(component.Id)) { return; }

        string id = fallbackId;
        int suffix = 1;

        while (!used.Add(id)) { id = fallbackId + "-" + suffix++; }

        if (component.HasId)
        {
            SoulboundSetupLog.Warn("Duplicate persistent id on '" + target.name + "' in " +
                                   target.scene.name + "; reassigned to " + id + ".");
        }

        component.AssignId(id);
        EditorUtility.SetDirty(component);
    }

    // ----- small helpers --------------------------------------------------

    private static Transform EnsureContainer(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) { return root.transform; }
        }

        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void SpawnEnemies(Transform parent, string prefabPath, Vector2[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            InstantiateAt(prefabPath, parent, positions[i]);
        }
    }

    // Keeps the first `keep` instances of the given prefab and drops the rest,
    // repositioning what survives.
    private static void TrimEnemies(Transform parent, string prefabPath, int keep, Vector2[] positions)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { return; }

        List<Transform> matching = new List<Transform>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject);

            if (source == prefab) { matching.Add(child); }
        }

        for (int i = matching.Count - 1; i >= keep; i--)
        {
            Object.DestroyImmediate(matching[i].gameObject);
            matching.RemoveAt(i);
        }

        while (matching.Count < keep)
        {
            GameObject spawned = InstantiateAt(prefabPath, parent, Vector2.zero);
            if (spawned == null) { break; }

            matching.Add(spawned.transform);
        }

        for (int i = 0; i < matching.Count && i < positions.Length; i++)
        {
            matching[i].position = positions[i];
        }
    }

    private static void RemoveEnemiesOtherThan(Transform parent, string keepPrefabPath)
    {
        GameObject keep = AssetDatabase.LoadAssetAtPath<GameObject>(keepPrefabPath);

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child.GetComponentInChildren<EnemyHealth>(true) == null) { continue; }
            if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject) == keep) { continue; }

            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void ScatterProps(Transform parent, (string path, Vector2 position)[] props)
    {
        for (int i = 0; i < props.Length; i++)
        {
            InstantiateAt(props[i].path, parent, props[i].position);
        }
    }

    private static GameObject InstantiateAt(string prefabPath, Transform parent, Vector2 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { return null; }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (parent != null) { instance.transform.SetParent(parent, false); }

        instance.transform.position = position;
        return instance;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        List<T> found = FindAllInScene<T>(scene);
        return found.Count > 0 ? found[0] : null;
    }

    private static List<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        List<T> results = new List<T>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<T>(true));
        }

        return results;
    }

    private static void BackupExistingScene(string sceneName)
    {
        string path = ScenesFolder + "/" + sceneName + ".unity";
        if (!File.Exists(path)) { return; }

        Directory.CreateDirectory(BackupFolder);

        string backupPath = BackupFolder + "/" + sceneName + "_backup.unity";

        // Only the first backup is kept: re-running the tool must not overwrite
        // the player's original level with an already-modified copy.
        if (File.Exists(backupPath)) { return; }

        AssetDatabase.CopyAsset(path, backupPath);
        SoulboundSetupLog.Step("Backed up " + sceneName + " to " + backupPath + ".");
    }
}
