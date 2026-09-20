using UnityEngine;

// The supplied artwork - the title logo and the square key art - loaded once
// and shared. Both are optional: if the files are missing the screens fall
// back to the text they drew before, so a fresh clone still runs.
public static class Branding
{
    private const string LogoResource = "Branding/GameLogo";
    private const string KeyArtResource = "Branding/KeyArt";

    private static Sprite logo;
    private static Sprite keyArt;
    private static bool logoLoaded;
    private static bool keyArtLoaded;

    public static Sprite Logo
    {
        get
        {
            if (!logoLoaded)
            {
                logoLoaded = true;
                logo = Resources.Load<Sprite>(LogoResource);
            }

            return logo;
        }
    }

    public static Sprite KeyArt
    {
        get
        {
            if (!keyArtLoaded)
            {
                keyArtLoaded = true;
                keyArt = Resources.Load<Sprite>(KeyArtResource);
            }

            return keyArt;
        }
    }

    public static bool HasLogo
    {
        get { return Logo != null; }
    }

    public static bool HasKeyArt
    {
        get { return KeyArt != null; }
    }
}
