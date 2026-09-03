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
