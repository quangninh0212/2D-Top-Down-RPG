using System;

// Thin event bus between gameplay systems and whatever UI happens to be alive.
// Keeps LevelManager and the boss from holding references to canvases that are
// rebuilt on every scene load.
public static class GameMessages
{
    // Large centred banner: title plus a smaller line under it.
    public static event Action<string, string> OnBanner;

    // Short line that slides in near the top and fades out.
    public static event Action<string> OnToast;

    public static void Banner(string title, string subtitle)
    {
        OnBanner?.Invoke(title, subtitle);
    }

    public static void Toast(string message)
    {
        OnToast?.Invoke(message);
    }
}
