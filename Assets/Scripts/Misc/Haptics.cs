using UnityEngine;

// Short vibrations on Android. Unity's Handheld.Vibrate is a fixed ~500 ms
// buzz, which is far too long for a hit reaction, so the Android vibrator is
// driven directly through JNI when it is available and Handheld.Vibrate is only
// the fallback.
public static class Haptics
{
    private const string EnabledKey = "settings.vibration";
    private const float MinimumInterval = 0.08f;

    private static float lastVibration;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static bool vibratorResolved;
#endif

    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt(EnabledKey, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void LightTap()
    {
        Vibrate(25);
    }

    public static void HeavyTap()
    {
        Vibrate(60);
    }

    private static void Vibrate(long milliseconds)
    {
        if (!Enabled) { return; }
        if (!Application.isMobilePlatform) { return; }

        // Rapid hits would otherwise leave the phone buzzing continuously.
        if (Time.unscaledTime - lastVibration < MinimumInterval) { return; }
        lastVibration = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaObject device = ResolveVibrator();
            if (device != null)
            {
                device.Call("vibrate", milliseconds);
                return;
            }
        }
        catch (System.Exception)
        {
            // Some devices refuse the call; falling through is enough.
        }
#endif

        Handheld.Vibrate();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject ResolveVibrator()
    {
        if (vibratorResolved) { return vibrator; }
        vibratorResolved = true;

        using (AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }

        return vibrator;
    }
#endif
}
