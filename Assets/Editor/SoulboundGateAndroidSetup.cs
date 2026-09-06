using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One button that takes the project from "PC prototype" to "Android build
// ready". Every step is idempotent: running it twice produces the same project,
// never duplicated objects.
public static class SoulboundGateAndroidSetup
{
    private const string ProductName = "Soulbound Gate";
    private const string CompanyName = "Quang Ninh and Hong Phong";
    private const string BundleIdentifier = "com.quangninhhongphong.soulboundgate";

    [MenuItem("Tools/Soulbound Gate/Complete Android Game Setup", priority = 0)]
    public static void RunAll()
    {
        SoulboundSetupLog.Step("Starting complete setup...");

        GenerateContentAssets();
        ConfigureWeaponInventory();
        // The HUD layout is applied at runtime by HudLayout: the scenes override
        // the prefab, so editing the prefab alone would not take effect.

        LevelSceneBuilder.BuildFrontEndScenes();
        LevelSceneBuilder.BuildAllLevels();

        ConfigureBuildSettings();
        ConfigurePlayerSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SoulboundSetupLog.Step("Setup complete.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Soulbound Gate", "Soulbound Gate setup completed.", "OK");
        }
    }

    // ----- individual steps ----------------------------------------------

    [MenuItem("Tools/Soulbound Gate/Steps/Generate Content Assets")]
    public static void GenerateContentAssets()
    {
        PlaceholderAudioGenerator.GenerateAll();
        GameArtLibraryBuilder.Build();
        AppIconGenerator.Generate();
        BossPrefabBuilder.Build();
        WriteAudioFolderNote();
    }

    // The three weapon assets are wired straight onto ActiveInventory, so the
    // inventory no longer depends on the order of the old five-slot UI strip.
    [MenuItem("Tools/Soulbound Gate/Steps/Configure Weapon Inventory")]
    public static void ConfigureWeaponInventory()
    {
        const string prefabPath = "Assets/Prefabs/Scene Management/UICanvas.prefab";

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            SoulboundSetupLog.Warn("UICanvas prefab not found; weapons not wired.");
            return;
        }

        ActiveInventory inventory = root.GetComponentInChildren<ActiveInventory>(true);

        if (inventory == null)
        {
            PrefabUtility.UnloadPrefabContents(root);
            SoulboundSetupLog.Warn("ActiveInventory not found inside UICanvas; weapons not wired.");
            return;
        }

        SerializedObject serialized = new SerializedObject(inventory);
        SetWeapon(serialized, "swordInfo", "Assets/Scriptable Objects/Sword.asset");
        SetWeapon(serialized, "bowInfo", "Assets/Scriptable Objects/Bow.asset");
        SetWeapon(serialized, "staffInfo", "Assets/Scriptable Objects/Staff.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        SoulboundSetupLog.Step("Weapon assets wired onto ActiveInventory.");
    }

    private static void SetWeapon(SerializedObject serialized, string field, string assetPath)
    {
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) { return; }

        property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponInfo>(assetPath);
    }

    [MenuItem("Tools/Soulbound Gate/Steps/Configure Build Settings")]
    public static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        for (int i = 0; i < GameScenes.BuildOrder.Length; i++)
        {
            string path = "Assets/Scenes/" + GameScenes.BuildOrder[i] + ".unity";

            if (!File.Exists(path))
            {
                SoulboundSetupLog.Warn("Scene missing from disk, skipped in Build Settings: " + path);
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();

        SoulboundSetupLog.Step("Build Settings set to " + scenes.Count + " scenes, splash first.");
    }

    [MenuItem("Tools/Soulbound Gate/Steps/Configure Player Settings")]
    public static void ConfigurePlayerSettings()
    {
        PlayerSettings.productName = ProductName;
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleIdentifier);

        // Landscape both ways, never portrait.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // Play Store requires a 64-bit slice, which in turn requires IL2CPP.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // Draw into the cutout area; the UI keeps itself inside the safe area.
        PlayerSettings.Android.renderOutsideSafeArea = true;

        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
        {
            UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
            UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
        });

        // A coursework build signs with the debug keystore.
        PlayerSettings.Android.useCustomKeystore = false;

        SoulboundSetupLog.Step("Player Settings configured for Android landscape, ARM64 + IL2CPP.");
    }

    private static void WriteAudioFolderNote()
    {
        Directory.CreateDirectory("Assets/Audio/Music");
        Directory.CreateDirectory("Assets/Audio/SFX");

        const string note =
            "The generated audio lives in Assets/Resources/Audio so AudioManager can\n" +
            "load it by name at runtime (Resources.Load). These folders are kept for\n" +
            "any hand-authored clips you want to add later.\n\n" +
            "Generated specifically for this project.\n";

        File.WriteAllText("Assets/Audio/README.txt", note);
    }
}
