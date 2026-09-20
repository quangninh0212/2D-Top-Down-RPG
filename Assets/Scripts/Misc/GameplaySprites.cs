using UnityEngine;

// Pixel art for the new gameplay pieces - the shield bubble, the forbidden
// zone, the rune, the spikes, and the sound and music switch icons. The project
// has no artwork for any of these, so they are drawn once at runtime on a small
// grid with point filtering, which keeps them in the same chunky style as the
// rest of the game instead of pulling in outside assets.
public static class GameplaySprites
{
    private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

    private static Sprite ring;
    private static Sprite speedRune;
    private static Sprite spikesDown;
    private static Sprite spikesUp;
    private static Sprite hazardZone;
    private static Sprite alert;
    private static Sprite speakerOn;
    private static Sprite speakerOff;
    private static Sprite musicOn;
    private static Sprite musicOff;
    private static Sprite shieldIcon;
    private static Sprite stunIcon;

    // ----- world sprites (1 unit across) ---------------------------------

    public static Sprite Ring
    {
        get { return ring != null ? ring : (ring = BuildRing()); }
    }

    public static Sprite SpeedRune
    {
        get { return speedRune != null ? speedRune : (speedRune = BuildSpeedRune()); }
    }

    public static Sprite SpikesDown
    {
        get { return spikesDown != null ? spikesDown : (spikesDown = BuildSpikes(false)); }
    }

    public static Sprite SpikesUp
    {
        get { return spikesUp != null ? spikesUp : (spikesUp = BuildSpikes(true)); }
    }

    public static Sprite HazardZone
    {
        get { return hazardZone != null ? hazardZone : (hazardZone = BuildHazardZone()); }
    }

    // The "!" an NPC shows the moment it notices the player, so its state is
    // readable on screen instead of only in the code.
    public static Sprite Alert
    {
        get { return alert != null ? alert : (alert = BuildAlert()); }
    }

    // ----- HUD icons ------------------------------------------------------

    public static Sprite SpeakerOn
    {
        get { return speakerOn != null ? speakerOn : (speakerOn = BuildSpeaker(true)); }
    }

    public static Sprite SpeakerOff
    {
        get { return speakerOff != null ? speakerOff : (speakerOff = BuildSpeaker(false)); }
    }

    public static Sprite MusicOn
    {
        get { return musicOn != null ? musicOn : (musicOn = BuildNote(true)); }
    }

    public static Sprite MusicOff
    {
        get { return musicOff != null ? musicOff : (musicOff = BuildNote(false)); }
    }

    public static Sprite ShieldIcon
    {
        get { return shieldIcon != null ? shieldIcon : (shieldIcon = BuildShieldIcon()); }
    }

    public static Sprite StunIcon
    {
        get { return stunIcon != null ? stunIcon : (stunIcon = BuildStunIcon()); }
    }

    // ----- drawing --------------------------------------------------------

    private static Sprite BuildRing()
    {
        const int size = 48;
        Color[] pixels = Blank(size);
        Color edge = Color.white;
        Color fill = new Color(1f, 1f, 1f, 0.25f);

        Vector2 centre = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);

                if (d <= 23f && d >= 20f) { pixels[y * size + x] = edge; }
                else if (d < 20f) { pixels[y * size + x] = fill; }
            }
        }

        return Finish(pixels, size, size, size);
    }

    // A glowing blue diamond with two chevrons pointing forward.
    private static Sprite BuildSpeedRune()
    {
        const int size = 16;
        Color[] pixels = Blank(size);
        Color outline = new Color(0.10f, 0.25f, 0.55f);
        Color body = new Color(0.30f, 0.80f, 1f);
        Color glint = new Color(0.90f, 1f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int manhattan = Mathf.Abs(x - 8) + Mathf.Abs(y - 8);

                if (manhattan == 7) { Set(pixels, size, x, y, outline); }
                else if (manhattan < 7) { Set(pixels, size, x, y, body); }
            }
        }

        // Chevrons: ">>"
        for (int i = 0; i < 3; i++)
        {
            Set(pixels, size, 5 + i, 8 + i, glint);
            Set(pixels, size, 5 + i, 8 - i, glint);
            Set(pixels, size, 8 + i, 8 + i, glint);
            Set(pixels, size, 8 + i, 8 - i, glint);
        }

        return Finish(pixels, size, size, size);
    }

    // A dark plate with a 3x3 grid of spikes, either retracted or sprung.
    private static Sprite BuildSpikes(bool raised)
    {
        const int size = 16;
        Color[] pixels = Blank(size);
        Color plate = new Color(0.22f, 0.20f, 0.22f);
        Color plateEdge = new Color(0.10f, 0.09f, 0.10f);
        Color hole = new Color(0.05f, 0.05f, 0.06f);
        Color steel = new Color(0.82f, 0.84f, 0.88f);
        Color blood = new Color(0.70f, 0.12f, 0.14f);

        for (int y = 1; y < 15; y++)
        {
            for (int x = 1; x < 15; x++)
            {
                bool edge = x == 1 || x == 14 || y == 1 || y == 14;
                Set(pixels, size, x, y, edge ? plateEdge : plate);
            }
        }

        for (int gy = 0; gy < 3; gy++)
        {
            for (int gx = 0; gx < 3; gx++)
            {
                int cx = 4 + gx * 4;
                int cy = 4 + gy * 4;

                if (!raised)
                {
                    Set(pixels, size, cx, cy, hole);
                    continue;
                }

                // A small upward triangle.
                Set(pixels, size, cx - 1, cy - 1, steel);
                Set(pixels, size, cx, cy - 1, steel);
                Set(pixels, size, cx + 1, cy - 1, steel);
                Set(pixels, size, cx, cy, steel);
                Set(pixels, size, cx, cy + 1, blood);
            }
        }

        return Finish(pixels, size, size, size);
    }

    // Translucent red with diagonal warning stripes and a solid border.
    private static Sprite BuildHazardZone()
    {
        const int size = 32;
        Color[] pixels = Blank(size);
        Color fill = new Color(0.85f, 0.10f, 0.10f, 0.18f);
        Color stripe = new Color(0.95f, 0.20f, 0.15f, 0.34f);
        Color border = new Color(1f, 0.25f, 0.20f, 0.85f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                bool onStripe = ((x + y) / 4) % 2 == 0;

                Set(pixels, size, x, y, edge ? border : (onStripe ? stripe : fill));
            }
        }

        return Finish(pixels, size, size, size);
    }

    private static Sprite BuildSpeaker(bool on)
    {
        const int size = 32;
        Color[] pixels = Blank(size);
        Color ink = new Color(1f, 0.96f, 0.88f);
        Color cross = new Color(1f, 0.35f, 0.30f);

        // Box and cone.
        FillRect(pixels, size, 5, 12, 9, 20, ink);
        for (int i = 0; i < 7; i++)
        {
            FillRect(pixels, size, 10 + i, 12 - i, 10 + i, 20 + i, ink);
        }

        if (on)
        {
            Arc(pixels, size, 16, 16, 5, ink);
            Arc(pixels, size, 16, 16, 9, ink);
        }
        else
        {
            Line(pixels, size, 20, 11, 28, 21, cross);
            Line(pixels, size, 20, 21, 28, 11, cross);
            Line(pixels, size, 21, 11, 29, 21, cross);
            Line(pixels, size, 21, 21, 29, 11, cross);
        }

        return Finish(pixels, size, size, size);
    }

    private static Sprite BuildNote(bool on)
    {
        const int size = 32;
        Color[] pixels = Blank(size);
        Color ink = new Color(1f, 0.96f, 0.88f);
        Color cross = new Color(1f, 0.35f, 0.30f);

        // Two note heads joined by a beam.
        FillCircle(pixels, size, 10, 9, 4, ink);
        FillCircle(pixels, size, 22, 12, 4, ink);
        FillRect(pixels, size, 13, 9, 14, 26, ink);
        FillRect(pixels, size, 25, 12, 26, 28, ink);
        for (int x = 13; x <= 26; x++)
        {
            int y = 26 + (x - 13) * 2 / 13;
            FillRect(pixels, size, x, y, x, y + 2, ink);
        }

        if (!on)
        {
            Line(pixels, size, 3, 3, 29, 29, cross);
            Line(pixels, size, 4, 3, 30, 29, cross);
            Line(pixels, size, 3, 4, 29, 30, cross);
        }

        return Finish(pixels, size, size, size);
    }

    private static Sprite BuildShieldIcon()
    {
        const int size = 32;
        Color[] pixels = Blank(size);
        Color rim = new Color(1f, 0.96f, 0.88f);
        Color face = new Color(0.35f, 0.75f, 1f);

        for (int y = 4; y < 28; y++)
        {
            // Straight sides at the top, tapering to a point at the bottom.
            float taper = y < 14 ? 11f : Mathf.Lerp(11f, 0f, (14f - y) / -14f);
            int half = Mathf.RoundToInt(taper);

            for (int x = 16 - half; x <= 16 + half; x++)
            {
                bool edge = x == 16 - half || x == 16 + half || y == 27;
                Set(pixels, size, x, 31 - y, edge ? rim : face);
            }
        }

        return Finish(pixels, size, size, size);
    }

    private static Sprite BuildStunIcon()
    {
        const int size = 32;
        Color[] pixels = Blank(size);
        Color bolt = new Color(1f, 0.90f, 0.35f);

        int[,] points = { { 19, 29 }, { 9, 15 }, { 16, 15 }, { 12, 3 }, { 24, 18 }, { 17, 18 }, { 19, 29 } };

        for (int i = 0; i < points.GetLength(0) - 1; i++)
        {
            Line(pixels, size, points[i, 0], points[i, 1], points[i + 1, 0], points[i + 1, 1], bolt);
        }

        FillRect(pixels, size, 12, 15, 17, 18, bolt);
        FillRect(pixels, size, 14, 8, 16, 15, bolt);
        FillRect(pixels, size, 16, 18, 19, 24, bolt);

        return Finish(pixels, size, size, size);
    }

    // ----- primitives -----------------------------------------------------

    // A fat exclamation mark with a dark outline, so it reads over any tile.
    private static Sprite BuildAlert()
    {
        const int size = 32;
        Color[] pixels = Blank(size);

        Color body = new Color(1f, 0.85f, 0.25f, 1f);
        Color edge = new Color(0.12f, 0.08f, 0.04f, 1f);

        FillRect(pixels, size, 11, 6, 20, 31, edge);
        FillRect(pixels, size, 13, 22, 18, 29, body);
        FillRect(pixels, size, 13, 12, 18, 20, body);
        FillRect(pixels, size, 13, 8, 18, 10, body);

        return Finish(pixels, size, size, 48f);
    }

    private static Color[] Blank(int size)
    {
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) { pixels[i] = Clear; }
        return pixels;
    }

    private static void Set(Color[] pixels, int size, int x, int y, Color colour)
    {
        if (x < 0 || y < 0 || x >= size || y >= size) { return; }
        pixels[y * size + x] = colour;
    }

    private static void FillRect(Color[] pixels, int size, int x0, int y0, int x1, int y1, Color colour)
    {
        for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
        {
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            {
                Set(pixels, size, x, y, colour);
            }
        }
    }

    private static void FillCircle(Color[] pixels, int size, int cx, int cy, int radius, Color colour)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius) { Set(pixels, size, cx + x, cy + y, colour); }
            }
        }
    }

    // The right-hand half of a circle outline: a sound wave.
    private static void Arc(Color[] pixels, int size, int cx, int cy, int radius, Color colour)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = 1; x <= radius; x++)
            {
                int d = x * x + y * y;
                if (d <= radius * radius && d > (radius - 2) * (radius - 2))
                {
                    Set(pixels, size, cx + x, cy + y, colour);
                }
            }
        }
    }

    private static void Line(Color[] pixels, int size, int x0, int y0, int x1, int y1, Color colour)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            Set(pixels, size, x0, y0, colour);
            if (x0 == x1 && y0 == y1) { break; }

            int e2 = 2 * error;
            if (e2 >= dy) { error += dy; x0 += sx; }
            if (e2 <= dx) { error += dx; y0 += sy; }
        }
    }

    private static Sprite Finish(Color[] pixels, int width, int height, float pixelsPerUnit)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
