using UnityEngine;

// Icons for the weapon slots. The weapon art comes from the project's own UI
// sprites; the padlock has no artwork in the project, so it is drawn once at
// runtime rather than pulling in an asset from elsewhere.
public static class WeaponIcons
{
    private static Sprite padlock;

    public static Sprite Get(WeaponType weapon)
    {
        GameArtLibrary art = GameArtLibrary.Instance;
        return art != null ? art.WeaponIcon(weapon) : null;
    }

    public static Sprite Padlock
    {
        get
        {
            if (padlock == null) { padlock = BuildPadlock(); }
            return padlock;
        }
    }

    // A body with a shackle above it, drawn on a small grid so it keeps the
    // chunky look of the rest of the pixel UI.
    private static Sprite BuildPadlock()
    {
        const int size = 32;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Color body = new Color(0.92f, 0.86f, 0.68f, 1f);
        Color shadow = new Color(0.35f, 0.30f, 0.22f, 1f);
        Color clear = new Color(0f, 0f, 0f, 0f);

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) { pixels[i] = clear; }

        // Body: a filled rectangle across the lower half.
        for (int y = 4; y <= 17; y++)
        {
            for (int x = 7; x <= 24; x++)
            {
                bool edge = y == 4 || y == 17 || x == 7 || x == 24;
                pixels[y * size + x] = edge ? shadow : body;
            }
        }

        // Keyhole.
        for (int y = 8; y <= 13; y++)
        {
            int width = y >= 11 ? 1 : 2;
            for (int x = 16 - width; x <= 15 + width; x++)
            {
                pixels[y * size + x] = shadow;
            }
        }

        // Shackle: an arc of thickness 2 sitting on top of the body.
        Vector2 centre = new Vector2(16f, 18f);
        for (int y = 17; y < size - 2; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                if (distance >= 6.0f && distance <= 8.0f)
                {
                    pixels[y * size + x] = body;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
