using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the Soul Warden prefab from the Ghost artwork. It is deliberately not
// "a ghost with more health": it is much larger, tinted with its own colour,
// wrapped in an aura, given a heavier shadow, and driven by BossController
// instead of the generic enemy AI.
public static class BossPrefabBuilder
{
    private const string GhostPath = "Assets/Prefabs/Enemies/Ghost.prefab";
    private const string BulletPath = "Assets/Prefabs/Weapons/Bullet.prefab";
    private const string SlimePath = "Assets/Prefabs/Enemies/Blue Slime.prefab";
    private const string GrapePath = "Assets/Prefabs/Enemies/Enemie1.prefab";
    private const string DeathVfxPath = "Assets/Prefabs/VFX/Ghost Death VFX.prefab";

    private const string BossFolder = "Assets/Prefabs/Enemies/Boss";
    private const string BossPath = BossFolder + "/Soul Warden.prefab";
    private const string AuraTexturePath = "Assets/Generated/UI/SoulWardenAura.png";

    private const int BossHealth = 42;

    [MenuItem("Tools/Soulbound Gate/Rebuild Boss Prefab")]
    public static GameObject Build()
    {
        GameObject ghost = AssetDatabase.LoadAssetAtPath<GameObject>(GhostPath);
        if (ghost == null)
        {
            SoulboundSetupLog.Warn("Ghost prefab not found; boss not built.");
            return null;
        }

        Directory.CreateDirectory(BossFolder);
        Sprite aura = EnsureAuraSprite();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(ghost);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        instance.name = "Soul Warden";
        instance.transform.localScale = Vector3.one * 2.3f;

        StripGenericEnemyBehaviour(instance);
        TintBoss(instance);
        AddAura(instance, aura);
        AddShadow(instance, aura);

        ConfigureHealth(instance);
        ConfigureController(instance);

        // Persistent id so a boss killed before a save/reload stays dead.
        if (instance.GetComponent<PersistentObjectId>() == null)
        {
            instance.AddComponent<PersistentObjectId>();
        }

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, BossPath);
        Object.DestroyImmediate(instance);

        SoulboundSetupLog.Step("Boss prefab built at " + BossPath + " (" + BossHealth + " HP).");
        return saved;
    }

    // The boss does not roam or use the shared Shooter; BossController owns both.
    private static void StripGenericEnemyBehaviour(GameObject instance)
    {
        RemoveComponent<EnemyAI>(instance);
        RemoveComponent<Shooter>(instance);
        RemoveComponent<EnemyPathfinding>(instance);
        RemoveComponent<RandomIdleAnimation>(instance);

        // The knockback thrust of a normal enemy would fling a boss across the
        // arena; it keeps the component but is made far heavier below.
        Rigidbody2D body = instance.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.mass = 12f;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private static void TintBoss(GameObject instance)
    {
        SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
        if (renderer == null) { return; }

        renderer.color = new Color(0.72f, 0.45f, 1f, 1f);
        renderer.sortingOrder = 2;
    }

    private static void AddAura(GameObject instance, Sprite aura)
    {
        GameObject auraGO = new GameObject("Aura");
        auraGO.transform.SetParent(instance.transform, false);
        auraGO.transform.localScale = Vector3.one * 2.6f;

        SpriteRenderer renderer = auraGO.AddComponent<SpriteRenderer>();
        renderer.sprite = aura;
        renderer.color = new Color(0.65f, 0.30f, 1f, 0.42f);
        renderer.sortingOrder = 1;

        auraGO.AddComponent<BossAuraPulse>();
    }

    private static void AddShadow(GameObject instance, Sprite aura)
    {
        GameObject shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(instance.transform, false);
        shadowGO.transform.localPosition = new Vector3(0f, -0.45f, 0f);
        shadowGO.transform.localScale = new Vector3(1.5f, 0.55f, 1f);

        SpriteRenderer renderer = shadowGO.AddComponent<SpriteRenderer>();
        renderer.sprite = aura;
        renderer.color = new Color(0f, 0f, 0f, 0.35f);
        renderer.sortingOrder = 0;
    }

    private static void ConfigureHealth(GameObject instance)
    {
        EnemyHealth existing = instance.GetComponent<EnemyHealth>();
        GameObject deathVfx = AssetDatabase.LoadAssetAtPath<GameObject>(DeathVfxPath);

        if (existing != null) { Object.DestroyImmediate(existing, true); }

        BossHealth health = instance.AddComponent<BossHealth>();

        SerializedObject serialized = new SerializedObject(health);
        serialized.FindProperty("startingHealth").intValue = BossHealth;
        serialized.FindProperty("knockBackThrust").floatValue = 2f;
        serialized.FindProperty("countsTowardObjective").boolValue = true;
        serialized.FindProperty("bossName").stringValue = "SOUL WARDEN";

        if (deathVfx != null)
        {
            serialized.FindProperty("deathVFXPrefab").objectReferenceValue = deathVfx;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureController(GameObject instance)
    {
        BossController controller = instance.GetComponent<BossController>();
        if (controller == null) { controller = instance.AddComponent<BossController>(); }

        SerializedObject serialized = new SerializedObject(controller);

        Assign(serialized, "projectilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath));
        Assign(serialized, "slimePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(SlimePath));
        Assign(serialized, "grapePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(GrapePath));

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Assign(SerializedObject serialized, string property, Object value)
    {
        SerializedProperty found = serialized.FindProperty(property);
        if (found != null) { found.objectReferenceValue = value; }
    }

    private static void RemoveComponent<T>(GameObject instance) where T : Component
    {
        T component = instance.GetComponent<T>();
        if (component != null) { Object.DestroyImmediate(component, true); }
    }

    // A soft radial disc, written once and reused for both the aura and the
    // shadow so no external art is needed.
    private static Sprite EnsureAuraSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(AuraTexturePath);
        if (existing != null) { return existing; }

        Directory.CreateDirectory(Path.GetDirectoryName(AuraTexturePath));

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 centre = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / radius;
                float falloff = Mathf.Clamp01(1f - distance);

                pixels[y * size + x] = new Color(1f, 1f, 1f, falloff * falloff);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        File.WriteAllBytes(AuraTexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(AuraTexturePath);

        TextureImporter importer = AssetImporter.GetAtPath(AuraTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(AuraTexturePath);
    }
}
