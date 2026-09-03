using UnityEngine;

// Every graphic in the front end is drawn at runtime, so the menu needs no
// imported art and renders identically on every device. Edges are worked out
// from a signed distance and faded across a pixel, which is what keeps the
// curves smooth however far the UI is scaled up.
public static class MenuArt
{
    public static Sprite Disc(int size, float outlineWidth, Color fill, Color outline)
    {
        Texture2D texture = NewTexture(size, size);
        Color[] pixels = new Color[size * size];

        float radius = size * 0.5f - 1f;
        float innerEdge = radius - outlineWidth;
        Vector2 centre = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);

                float coverage = Mathf.Clamp01(radius - distance + 0.5f);
                float onOutline = Mathf.Clamp01(distance - innerEdge + 0.5f);

                Color colour = Color.Lerp(fill, outline, onOutline);
                colour.a *= coverage;
                pixels[y * size + x] = colour;
            }
        }

        return Finish(texture, pixels, Vector4.zero);
    }

    // Sliced so panels and bars can stretch without smearing their corners.
    public static Sprite RoundedRect(int width, int height, float radius, Color fill)
    {
        Texture2D texture = NewTexture(width, height);
        Color[] pixels = new Color[width * height];

        Vector2 centre = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 half = new Vector2(width * 0.5f - radius - 1f, height * 0.5f - radius - 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 offset = new Vector2(Mathf.Abs(x + 0.5f - centre.x), Mathf.Abs(y + 0.5f - centre.y));
                Vector2 outside = Vector2.Max(offset - half, Vector2.zero);
                float distance = outside.magnitude - radius;

                Color colour = fill;
                colour.a *= Mathf.Clamp01(0.5f - distance);
                pixels[y * width + x] = colour;
            }
        }

        float border = radius + 1f;
        return Finish(texture, pixels, new Vector4(border, border, border, border));
    }

    // Soft dot with no hard edge at all, used for the drifting motes.
    public static Sprite SoftGlow(int size, Color colour)
    {
        Texture2D texture = NewTexture(size, size);
        Color[] pixels = new Color[size * size];

        float radius = size * 0.5f;
        Vector2 centre = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / radius;
                float falloff = Mathf.Clamp01(1f - distance);

                Color pixel = colour;
                pixel.a *= falloff * falloff * falloff;
                pixels[y * size + x] = pixel;
            }
        }

        return Finish(texture, pixels, Vector4.zero);
    }

    public static Sprite VerticalGradient(int height, Color bottom, Color top)
    {
        Texture2D texture = NewTexture(1, height);
        Color[] pixels = new Color[height];

        for (int y = 0; y < height; y++)
        {
            pixels[y] = Color.Lerp(bottom, top, y / (height - 1f));
        }

        return Finish(texture, pixels, Vector4.zero);
    }

    // A doorway: straight sides with a domed top, drawn as a frame with the
    // opening left clear so whatever sits behind it shows through.
    public static Sprite Arch(int width, int height, float frameThickness, Color frame)
    {
        Texture2D texture = NewTexture(width, height);
        Color[] pixels = new Color[width * height];

        float centreX = width * 0.5f;
        float halfWidth = width * 0.5f - 2f;
        float springLine = height - halfWidth - 2f;   // where the sides meet the dome

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                // Distance to the outline: negative inside, positive outside.
                float distance = py <= springLine
                    ? Mathf.Abs(px - centreX) - halfWidth
                    : Vector2.Distance(new Vector2(px, py), new Vector2(centreX, springLine)) - halfWidth;

                float insideShape = Mathf.Clamp01(0.5f - distance);
                float insideOpening = Mathf.Clamp01(0.5f - (distance + frameThickness));

                Color colour = frame;
                colour.a *= insideShape * (1f - insideOpening);
                pixels[y * width + x] = colour;
            }
        }

        return Finish(texture, pixels, Vector4.zero);
    }

    // Rolling silhouette for the parallax layers. The waves complete a whole
    // number of cycles across the texture, so copies sit side by side seamlessly.
    public static Sprite HillBand(int width, int height, float baseHeight, float amplitude, float phase, Color colour)
    {
        Texture2D texture = NewTexture(width, height);
        Color[] pixels = new Color[width * height];

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)width;
            float crest = baseHeight + amplitude * (
                0.60f * Mathf.Sin(Mathf.PI * 2f * t + phase) +
                0.28f * Mathf.Sin(Mathf.PI * 4f * t + phase * 1.7f) +
                0.12f * Mathf.Sin(Mathf.PI * 6f * t + phase * 2.3f));

            for (int y = 0; y < height; y++)
            {
                Color pixel = colour;
                pixel.a *= Mathf.Clamp01(crest - y + 0.5f);
                pixels[y * width + x] = pixel;
            }
        }

        return Finish(texture, pixels, Vector4.zero);
    }

    public static Sprite Solid(Color colour)
    {
        Texture2D texture = NewTexture(4, 4);
        Color[] pixels = new Color[16];

        for (int i = 0; i < pixels.Length; i++) { pixels[i] = colour; }

        return Finish(texture, pixels, Vector4.zero);
    }

    private static Texture2D NewTexture(int width, int height)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
    }

    private static Sprite Finish(Texture2D texture, Color[] pixels, Vector4 border)
    {
        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                             new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
