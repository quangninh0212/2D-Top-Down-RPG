using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the Android launcher icon out of the game's own player sprite: one
// idle frame, scaled up with nearest-neighbour so the pixels stay crisp, over a
// dark fantasy gradient with a soft rune ring behind it.
public static class AppIconGenerator
{
    private const string PlayerIdlePath = "Assets/Sprites/Player/Side animations/spr_player_right_idle.png";
    private const string OutputFolder = "Assets/Generated/UI";
    private const string IconPath = OutputFolder + "/SoulboundGate_AppIcon.png";
    private const string ForegroundPath = OutputFolder + "/SoulboundGate_AppIcon_Foreground.png";
    private const string BackgroundPath = OutputFolder + "/SoulboundGate_AppIcon_Background.png";

    private const int Size = 512;

    [MenuItem("Tools/Soulbound Gate/Generate App Icon")]
    public static void Generate()
    {
        Texture2D source = LoadReadable(PlayerIdlePath);
        if (source == null)
        {
            SoulboundSetupLog.Warn("Player idle sprite not found at " + PlayerIdlePath + "; icon not generated.");
            return;
        }

        Directory.CreateDirectory(OutputFolder);

        // The idle sheet is a strip of square frames; the first one is a clean
        // standing pose.
        int frameSize = source.height;
        RectInt frame = new RectInt(0, 0, Mathf.Min(frameSize, source.width), frameSize);

        Color32[] background = BuildBackground();

        Color32[] composed = (Color32[])background.Clone();
        DrawSprite(composed, source, frame, 0.62f, 0f);

        WritePng(IconPath, composed);
        WritePng(BackgroundPath, background);

        // Adaptive icons crop the foreground hard, so the character is drawn
        // smaller and centred on transparency.
        Color32[] foreground = new Color32[Size * Size];
        DrawSprite(foreground, source, frame, 0.42f, 0f);
        WritePng(ForegroundPath, foreground);

        AssetDatabase.Refresh();

        ConfigureAsSprite(IconPath);
        ConfigureAsSprite(ForegroundPath);
        ConfigureAsSprite(BackgroundPath);

        ApplyToPlayerSettings();

        SoulboundSetupLog.Step("App icon generated at " + IconPath + " and applied to Android Player Settings.");
    }

    // Dark blue-violet gradient with a warm rune ring - reads as "fantasy gate"
    // at launcher size without any imported art.
    private static Color32[] BuildBackground()
    {
        Color32[] pixels = new Color32[Size * Size];

        Color deep = new Color(0.05f, 0.05f, 0.11f);
        Color lift = new Color(0.17f, 0.11f, 0.26f);
        Color ring = new Color(1f, 0.78f, 0.38f);

        Vector2 centre = new Vector2(Size * 0.5f, Size * 0.5f);
        float ringRadius = Size * 0.36f;
        float ringWidth = Size * 0.018f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float vertical = y / (float)(Size - 1);
                Color colour = Color.Lerp(deep, lift, vertical);

                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);

                // Glow inside the ring, then the ring itself on top.
                float glow = Mathf.Clamp01(1f - distance / (ringRadius * 1.25f));
                colour += ring * (glow * glow * 0.22f);

                float onRing = Mathf.Clamp01(1f - Mathf.Abs(distance - ringRadius) / ringWidth);
                colour = Color.Lerp(colour, ring, onRing * 0.85f);

                colour.a = 1f;
                pixels[y * Size + x] = colour;
            }
        }

        return pixels;
    }

    // Nearest-neighbour scaling: a pixel-art character must never be smoothed.
    private static void DrawSprite(Color32[] target, Texture2D source, RectInt frame, float coverage, float yOffsetFraction)
    {
        int drawSize = Mathf.RoundToInt(Size * coverage);
        int originX = (Size - drawSize) / 2;
        int originY = (Size - drawSize) / 2 + Mathf.RoundToInt(Size * yOffsetFraction);

        for (int y = 0; y < drawSize; y++)
        {
            int destY = originY + y;
            if (destY < 0 || destY >= Size) { continue; }

            int sourceY = frame.y + Mathf.Clamp(y * frame.height / drawSize, 0, frame.height - 1);

            for (int x = 0; x < drawSize; x++)
            {
                int destX = originX + x;
                if (destX < 0 || destX >= Size) { continue; }

                int sourceX = frame.x + Mathf.Clamp(x * frame.width / drawSize, 0, frame.width - 1);

                Color pixel = source.GetPixel(sourceX, sourceY);
                if (pixel.a < 0.02f) { continue; }

                Color under = target[destY * Size + destX];
                Color blended = Color.Lerp(under, pixel, pixel.a);
                blended.a = Mathf.Max(under.a, pixel.a);

                target[destY * Size + destX] = blended;
            }
        }
    }

    private static void WritePng(string path, Color32[] pixels)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    // Temporarily makes a texture readable so its pixels can be sampled, then
    // puts the import settings back exactly as they were.
    private static Texture2D LoadReadable(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { return null; }

        bool wasReadable = importer.isReadable;

        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (!wasReadable)
        {
            // Copy the pixels out before locking the texture again.
            Texture2D copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            copy.SetPixels(texture.GetPixels());
            copy.Apply();

            importer.isReadable = false;
            importer.SaveAndReimport();

            return copy;
        }

        return texture;
    }

    private static void ConfigureAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;      // keeps the pixel art crisp
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void ApplyToPlayerSettings()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        Texture2D foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
        Texture2D background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);

        if (icon == null) { return; }

        // The Android icon kinds (Legacy, Round, Adaptive) live in the Android
        // editor extension, so they are discovered rather than named directly -
        // that keeps this file compiling with or without the module installed.
        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android))
        {
            bool adaptive = kind.ToString().IndexOf("Adaptive", System.StringComparison.OrdinalIgnoreCase) >= 0;

            Texture2D[] layers = adaptive && foreground != null && background != null
                ? new[] { background, foreground }
                : new[] { icon };

            AssignKind(kind, layers);
        }
    }

    private static void AssignKind(PlatformIconKind kind, Texture2D[] layers)
    {
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);

        for (int i = 0; i < icons.Length; i++)
        {
            int layerCount = Mathf.Min(icons[i].maxLayerCount, layers.Length);
            Texture2D[] assigned = new Texture2D[Mathf.Max(1, layerCount)];

            for (int layer = 0; layer < assigned.Length; layer++)
            {
                assigned[layer] = layers[Mathf.Min(layer, layers.Length - 1)];
            }

            icons[i].SetTextures(assigned);
        }

        PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, icons);
    }
}
