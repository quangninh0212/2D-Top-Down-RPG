using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Checks that the project actually matches what the setup tool was asked to
// produce. Run after a setup so problems surface here rather than on a phone.
public static class ProjectValidator
{
    private struct Expectation
    {
        public string Scene;
        public int MandatoryEnemies;

        // The boss room is checked on its boss, not on a headcount. Victory
        // there comes from the Soul Warden dying, so extra enemies placed in it
        // are a design choice rather than a mistake.
        public bool BossLevel;

        public Expectation(string scene, int enemies, bool bossLevel = false)
        {
            Scene = scene;
            MandatoryEnemies = enemies;
            BossLevel = bossLevel;
        }
    }

    private static readonly Expectation[] Expected =
    {
        new Expectation(GameScenes.Scene1, 5),
        new Expectation(GameScenes.Scene2, 5),
        new Expectation(GameScenes.Scene3, 7),
        new Expectation(GameScenes.Scene4, 4),
        new Expectation(GameScenes.Scene5, 1, bossLevel: true)
    };

    [MenuItem("Tools/Soulbound Gate/Validate Project")]
    public static void Validate()
    {
        StringBuilder report = new StringBuilder();
        List<string> failures = new List<string>();

        ValidateBuildSettings(report, failures);
        ValidateSaveDataRoundTrip(report, failures);
        ValidateEconomyRules(report, failures);
        ValidateWeaponUnlockRules(report, failures);
        ValidateGeneratedAssets(report, failures);

        foreach (Expectation expectation in Expected)
        {
            ValidateLevel(expectation, report, failures);
        }

        report.AppendLine();
        report.AppendLine(failures.Count == 0
            ? "RESULT: all checks passed."
            : "RESULT: " + failures.Count + " problem(s) found.");

        foreach (string failure in failures) { report.AppendLine("  FAIL " + failure); }

        Debug.Log("[SOULBOUND VALIDATE]\n" + report);

        if (failures.Count > 0)
        {
            Debug.LogWarning("[SOULBOUND VALIDATE] " + failures.Count + " problem(s); see the report above.");
        }
    }

    // ----- project-level checks ------------------------------------------

    private static void ValidateBuildSettings(StringBuilder report, List<string> failures)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        report.AppendLine("Build Settings (" + scenes.Length + " scenes):");

        for (int i = 0; i < scenes.Length; i++)
        {
            report.AppendLine("  " + i + "  " + Path.GetFileNameWithoutExtension(scenes[i].path));
        }

        if (scenes.Length != GameScenes.BuildOrder.Length)
        {
            failures.Add("Build Settings has " + scenes.Length + " scenes, expected " + GameScenes.BuildOrder.Length);
            return;
        }

        for (int i = 0; i < GameScenes.BuildOrder.Length; i++)
        {
            string actual = Path.GetFileNameWithoutExtension(scenes[i].path);

            if (actual != GameScenes.BuildOrder[i])
            {
                failures.Add("Build index " + i + " is " + actual + ", expected " + GameScenes.BuildOrder[i]);
            }
        }
    }

    // Exercises the save format end to end without entering play mode.
    private static void ValidateSaveDataRoundTrip(StringBuilder report, List<string> failures)
    {
        SaveData data = new SaveData
        {
            gold = 137,
            currentScene = GameScenes.Scene3,
            currentHealth = 2,
            playTime = 91.5f
        };

        data.SetOwnsWeapon(WeaponType.Bow, true);

        SceneStateData state = data.GetOrCreateScene(GameScenes.Scene1);
        state.completed = true;
        state.MarkRemoved("Scene1:enemy:0");
        state.SetHealth("Scene1:enemy:1", 2);

        SaveData copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));

        bool ok = copy != null
                  && copy.gold == 137
                  && copy.currentScene == GameScenes.Scene3
                  && copy.ownsBow
                  && !copy.ownsStaff
                  && copy.IsLevelComplete(1)
                  && copy.FindScene(GameScenes.Scene1).IsRemoved("Scene1:enemy:0")
                  && copy.FindScene(GameScenes.Scene1).GetHealth("Scene1:enemy:1", 9) == 2;

        report.AppendLine("SaveData round-trip: " + (ok ? "ok" : "FAILED"));

        if (!ok) { failures.Add("SaveData did not survive a JSON round-trip"); }

        // Removing an object must also drop its stored health, or a dead enemy
        // would come back with a remembered health value.
        bool healthCleared = state.GetHealth("Scene1:enemy:0", -1) == -1;
        report.AppendLine("Removed objects drop their health entry: " + (healthCleared ? "ok" : "FAILED"));

        if (!healthCleared) { failures.Add("MarkRemoved left a stale health entry behind"); }
    }

    private static void ValidateEconomyRules(StringBuilder report, List<string> failures)
    {
        SaveData data = new SaveData { gold = 10 };

        // The shop's arithmetic, verified without needing a live scene.
        bool canAffordBow = data.gold >= WeaponTypeInfo.ShopPrice(WeaponType.Bow);
        bool canAffordStaff = data.gold >= WeaponTypeInfo.ShopPrice(WeaponType.Staff);

        report.AppendLine("Economy: 10 gold buys Bow (" + canAffordBow + "), not Staff (" + !canAffordStaff + ")");

        if (!canAffordBow || canAffordStaff)
        {
            failures.Add("Weapon prices are not Bow 10 / Staff 20");
        }
    }

    private static void ValidateWeaponUnlockRules(StringBuilder report, List<string> failures)
    {
        SaveData data = new SaveData();

        bool bowLockedAtStart = !WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Bow);
        data.GetOrCreateScene(GameScenes.Scene1).completed = true;
        bool bowUnlockedAfterLevel1 = WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Bow);

        bool staffLocked = !WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Staff);
        data.GetOrCreateScene(GameScenes.Scene2).completed = true;
        bool staffUnlocked = WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Staff);

        report.AppendLine("Shop unlocks: Bow after Scene1 (" + (bowLockedAtStart && bowUnlockedAfterLevel1) +
                          "), Staff after Scene2 (" + (staffLocked && staffUnlocked) + ")");

        if (!bowLockedAtStart || !bowUnlockedAfterLevel1) { failures.Add("Bow shop unlock is not gated on Scene1"); }
        if (!staffLocked || !staffUnlocked) { failures.Add("Staff shop unlock is not gated on Scene2"); }

        // Sword is free and always owned.
        if (!new SaveData().OwnsWeapon(WeaponType.Sword)) { failures.Add("Sword is not owned by default"); }
    }

    private static void ValidateGeneratedAssets(StringBuilder report, List<string> failures)
    {
        CheckAsset("Assets/Resources/UI/GameArtLibrary.asset", report, failures);
        CheckAsset("Assets/Generated/UI/SoulboundGate_AppIcon.png", report, failures);
        CheckAsset("Assets/Prefabs/Enemies/Boss/Soul Warden.prefab", report, failures);

        int sfxCount = Directory.Exists("Assets/Resources/Audio/SFX")
            ? Directory.GetFiles("Assets/Resources/Audio/SFX", "*.wav").Length : 0;

        int musicCount = Directory.Exists("Assets/Resources/Audio/Music")
            ? Directory.GetFiles("Assets/Resources/Audio/Music", "*.wav").Length : 0;

        report.AppendLine("Audio: " + sfxCount + " sfx, " + musicCount + " music tracks");

        if (sfxCount == 0) { failures.Add("No generated sound effects found"); }
        if (musicCount == 0) { failures.Add("No generated music found"); }

        GameArtLibrary library = AssetDatabase.LoadAssetAtPath<GameArtLibrary>("Assets/Resources/UI/GameArtLibrary.asset");

        if (library != null)
        {
            report.AppendLine("Art library: sword=" + (library.swordIcon != null) +
                              " bow=" + (library.bowIcon != null) +
                              " staff=" + (library.staffIcon != null) +
                              " playerFrames=" + (library.playerIdleFrames != null ? library.playerIdleFrames.Length : 0) +
                              " monsterFrames=" + (library.menuMonsterFrames != null ? library.menuMonsterFrames.Length : 0));

            if (library.swordIcon == null || library.bowIcon == null || library.staffIcon == null)
            {
                failures.Add("Art library is missing a weapon icon");
            }
        }
    }

    private static void CheckAsset(string path, StringBuilder report, List<string> failures)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<Object>(path) != null;
        report.AppendLine("Asset " + path + ": " + (exists ? "ok" : "MISSING"));

        if (!exists) { failures.Add("Missing asset " + path); }
    }

    // ----- per-level checks ----------------------------------------------

    private static void ValidateLevel(Expectation expectation, StringBuilder report, List<string> failures)
    {
        string path = "Assets/Scenes/" + expectation.Scene + ".unity";

        if (!File.Exists(path))
        {
            failures.Add("Scene file missing: " + path);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        int mandatory = 0;
        int withIds = 0;
        int destructibles = 0;
        bool hasPlayer = false;
        bool hasManagers = false;
        bool hasCanvas = false;
        bool hasCamera = false;
        bool hasLevelManager = false;
        bool hasAnchor = false;
        int groundTiles = 0;
        int wallTiles = 0;

        List<string> gates = new List<string>();
        List<string> entrances = new List<string>();
        HashSet<string> seenIds = new HashSet<string>();
        List<string> duplicateIds = new List<string>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EnemyHealth enemy in root.GetComponentsInChildren<EnemyHealth>(true))
            {
                if (enemy.CountsTowardObjective) { mandatory++; }

                PersistentObjectId id = enemy.GetComponent<PersistentObjectId>();
                if (id != null && id.HasId)
                {
                    withIds++;
                    if (!seenIds.Add(id.Id)) { duplicateIds.Add(id.Id); }
                }
            }

            foreach (Destructible destructible in root.GetComponentsInChildren<Destructible>(true))
            {
                destructibles++;

                PersistentObjectId id = destructible.GetComponent<PersistentObjectId>();
                if (id != null && id.HasId && !seenIds.Add(id.Id)) { duplicateIds.Add(id.Id); }
            }

            if (root.GetComponentInChildren<PlayerController>(true) != null) { hasPlayer = true; }
            if (root.GetComponentInChildren<EconomyManager>(true) != null) { hasManagers = true; }
            if (root.GetComponentInChildren<UIFade>(true) != null) { hasCanvas = true; }
            if (root.GetComponentInChildren<Camera>(true) != null) { hasCamera = true; }
            if (root.GetComponentInChildren<LevelManager>(true) != null) { hasLevelManager = true; }
            if (root.GetComponentInChildren<LevelCameraAnchor>(true) != null) { hasAnchor = true; }

            foreach (AreaExit exit in root.GetComponentsInChildren<AreaExit>(true))
            {
                gates.Add(exit.name + "->" + exit.SceneToLoad + (exit.IsForwardGate ? " (forward)" : " (back)"));
            }

            foreach (AreaEntrance entrance in root.GetComponentsInChildren<AreaEntrance>(true))
            {
                entrances.Add(entrance.IsDefaultSpawn ? "<default spawn>" : entrance.TransitionName);
            }

            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                int count = CountTiles(tilemap);

                if (tilemap.name == "Grass") { groundTiles += count; }
                if (tilemap.name == "Foreground") { wallTiles += count; }
            }
        }

        report.AppendLine();
        report.AppendLine(expectation.Scene + ":");
        report.AppendLine("  mandatory enemies: " + mandatory + " (expected " + expectation.MandatoryEnemies + ")");
        report.AppendLine("  persistent ids on enemies: " + withIds + ", destructibles: " + destructibles);
        report.AppendLine("  ground tiles: " + groundTiles + ", wall tiles: " + wallTiles);
        report.AppendLine("  gates: " + (gates.Count == 0 ? "none" : string.Join(", ", gates)));
        report.AppendLine("  entrances: " + (entrances.Count == 0 ? "none" : string.Join(", ", entrances)));
        report.AppendLine("  player=" + hasPlayer + " managers=" + hasManagers + " canvas=" + hasCanvas +
                          " camera=" + hasCamera + " levelManager=" + hasLevelManager + " anchor=" + hasAnchor);

        if (expectation.BossLevel)
        {
            int bosses = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bosses += root.GetComponentsInChildren<BossHealth>(true).Length;
            }

            report.AppendLine("  bosses: " + bosses + " (plus " + (mandatory - bosses) + " other enemies)");

            if (bosses != 1)
            {
                failures.Add(expectation.Scene + " has " + bosses + " bosses, expected exactly 1");
            }
        }
        else if (mandatory != expectation.MandatoryEnemies)
        {
            failures.Add(expectation.Scene + " has " + mandatory + " mandatory enemies, expected " + expectation.MandatoryEnemies);
        }

        if (withIds != mandatory) { failures.Add(expectation.Scene + " has enemies without a persistent id"); }
        if (duplicateIds.Count > 0) { failures.Add(expectation.Scene + " has duplicate persistent ids: " + string.Join(", ", duplicateIds)); }
        if (!hasPlayer) { failures.Add(expectation.Scene + " has no Player"); }
        if (!hasManagers) { failures.Add(expectation.Scene + " has no Managers"); }
        if (!hasCanvas) { failures.Add(expectation.Scene + " has no UICanvas"); }
        if (!hasCamera) { failures.Add(expectation.Scene + " has no Camera"); }
        if (!hasLevelManager) { failures.Add(expectation.Scene + " has no LevelManager"); }
        if (!hasAnchor) { failures.Add(expectation.Scene + " has no fixed camera anchor"); }
        if (groundTiles == 0) { failures.Add(expectation.Scene + " has an empty ground tilemap"); }
        if (wallTiles == 0) { failures.Add(expectation.Scene + " has no collision walls"); }
        if (gates.Count == 0) { failures.Add(expectation.Scene + " has no gates"); }

        CheckBakedCollisionMatchesTiles(scene, expectation.Scene, report, failures);
    }

    // A CompositeCollider2D stores its outline in the scene file. If that
    // outline was baked from a different set of tiles - which is what happened
    // when the new levels were built by copying and repainting Scene1's grid -
    // the player collides with walls that are not drawn anywhere. Regenerating
    // and comparing is the only way to see it.
    private static void CheckBakedCollisionMatchesTiles(Scene scene, string sceneName,
                                                        StringBuilder report, List<string> failures)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CompositeCollider2D composite in root.GetComponentsInChildren<CompositeCollider2D>(true))
            {
                // The number of separate outlines is a weak signal - two quite
                // different maps can both produce five loops. The vertex count
                // and the area they cover are what actually differ.
                int savedVertices = CountOutlineVertices(composite);
                Bounds savedBounds = composite.bounds;

                // The composite merges whatever shapes the TilemapCollider2D is
                // holding, and those are cached from the tiles that were present
                // when it last built them. Without forcing that rebuild first,
                // regenerating just reproduces the stale shape and the check
                // would always pass.
                TilemapCollider2D tilemapCollider = composite.GetComponent<TilemapCollider2D>();

                if (tilemapCollider != null)
                {
                    tilemapCollider.enabled = false;
                    tilemapCollider.enabled = true;
                }

                composite.GenerateGeometry();

                int rebuiltVertices = CountOutlineVertices(composite);
                Bounds rebuiltBounds = composite.bounds;

                report.AppendLine("  collision '" + composite.name + "': saved " + savedVertices +
                                  " vertices " + savedBounds.size.ToString("0.0") +
                                  ", rebuilt " + rebuiltVertices + " vertices " + rebuiltBounds.size.ToString("0.0"));

                bool sameShape = savedVertices == rebuiltVertices
                                 && (savedBounds.center - rebuiltBounds.center).sqrMagnitude < 0.01f
                                 && (savedBounds.size - rebuiltBounds.size).sqrMagnitude < 0.01f;

                if (!sameShape)
                {
                    failures.Add(sceneName + ": baked collision on '" + composite.name +
                                 "' does not match its tiles (" + savedVertices + " vertices saved vs " +
                                 rebuiltVertices + " rebuilt) - the player would hit invisible walls");
                }
            }
        }
    }

    private static int CountOutlineVertices(CompositeCollider2D composite)
    {
        int total = 0;
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < composite.pathCount; i++)
        {
            points.Clear();
            total += composite.GetPath(i, points);
        }

        return total;
    }

    private static int CountTiles(Tilemap tilemap)
    {
        int count = 0;

        foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(position) != null) { count++; }
        }

        return count;
    }
}
