using System.IO;
using UnityEditor;
using UnityEngine;

// Turns the two pieces of supplied artwork - the title logo and the square key
// art - into sprites the game can use.
//
// The originals are JPEGs on a black background, which would show as a black
// box over the menu's gradient. So the black is keyed out to transparency, the
// empty margin is cropped away, and the result is written as a PNG into
// Resources where the runtime can find it by name.
public static class BrandingImporter
{
    private const string SourceFolder = "Docs/Branding";
    private const string OutputFolder = "Assets/Resources/Branding";

    private const string LogoSource = SourceFolder + "/logo-source.jpg";
    private const string KeyArtSource = SourceFolder + "/keyart-source.jpg";

    public const string LogoPath = OutputFolder + "/GameLogo.png";
    public const string KeyArtPath = OutputFolder + "/KeyArt.png";

    // JPEG leaves a little noise around true black, so anything below the first
    // value is dropped outright and the band up to the second fades in. Kept
    // low on purpose: the artwork's own shadows must survive.
    private const float TransparentBelow = 0.035f;
    private const float OpaqueAbove = 0.11f;

    [MenuItem("Tools/Soulbound Gate/Steps/Import Branding Art")]
    public static void ImportAll()
    {
        Directory.CreateDirectory(OutputFolder);

        bool logo = Convert(LogoSource, LogoPath);
        bool keyArt = Convert(KeyArtSource, KeyArtPath);

        AssetDatabase.Refresh();

        if (logo) { ConfigureAsSprite(LogoPath); }
        if (keyArt) { ConfigureAsSprite(KeyArtPath); }

        SoulboundSetupLog.Step("Branding art imported (logo: " + logo + ", key art: " + keyArt + ").");
    }

    private static bool Convert(string sourcePath, string outputPath)
    {
        if (!File.Exists(sourcePath))
        {
            SoulboundSetupLog.Warn("Branding source missing, skipped: " + sourcePath);
            return false;
        }

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!source.LoadImage(File.ReadAllBytes(sourcePath)))
        {
            SoulboundSetupLog.Warn("Could not read branding source: " + sourcePath);
            Object.DestroyImmediate(source);
            return false;
        }

        Color32[] keyed = KeyOutBlack(source);
        RectInt bounds = OpaqueBounds(keyed, source.width, source.height);

        Color32[] cropped = Crop(keyed, source.width, bounds);
        WritePng(outputPath, cropped, bounds.width, bounds.height);

        SoulboundSetupLog.Step("  " + Path.GetFileName(outputPath) + ": " + source.width + "x" + source.height +
                               " -> " + bounds.width + "x" + bounds.height);

        Object.DestroyImmediate(source);
        return true;
    }

    // The background is black; the artwork is not. Alpha follows the brightest
    // channel so a blue glow on black keeps its glow instead of a grey halo.
    private static Color32[] KeyOutBlack(Texture2D source)
    {
        Color[] pixels = source.GetPixels();
        Color32[] result = new Color32[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            float brightest = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));

            float alpha = Mathf.InverseLerp(TransparentBelow, OpaqueAbove, brightest);
            result[i] = new Color(pixel.r, pixel.g, pixel.b, Mathf.Clamp01(alpha));
        }

        return result;
    }

    // The supplied logo sits in a wide black field; cropping to what is
    // actually drawn means the layout can position it without guessing at the
    // margin.
    private static RectInt OpaqueBounds(Color32[] pixels, int width, int height)
    {
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a < 26) { continue; }   // ~10%

                if (x < minX) { minX = x; }
                if (x > maxX) { maxX = x; }
                if (y < minY) { minY = y; }
                if (y > maxY) { maxY = y; }
            }
        }

        if (maxX < minX || maxY < minY) { return new RectInt(0, 0, width, height); }

        // A few pixels of breathing room, so a glow is not clipped flat.
        const int margin = 4;

        minX = Mathf.Max(0, minX - margin);
        minY = Mathf.Max(0, minY - margin);
        maxX = Mathf.Min(width - 1, maxX + margin);
        maxY = Mathf.Min(height - 1, maxY + margin);

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static Color32[] Crop(Color32[] pixels, int sourceWidth, RectInt bounds)
    {
        Color32[] result = new Color32[bounds.width * bounds.height];

        for (int y = 0; y < bounds.height; y++)
        {
            for (int x = 0; x < bounds.width; x++)
            {
                result[y * bounds.width + x] = pixels[(bounds.y + y) * sourceWidth + bounds.x + x];
            }
        }

        return result;
    }

    private static void WritePng(string path, Color32[] pixels, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();

        File.WriteAllBytes(path, texture.EncodeToPNG());

        Object.DestroyImmediate(texture);
    }

    private static void ConfigureAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        // Full colour artwork, not pixel art on a grid: it is drawn at many
        // sizes, so it is filtered rather than snapped to pixels.
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        importer.SaveAndReimport();
    }
}
