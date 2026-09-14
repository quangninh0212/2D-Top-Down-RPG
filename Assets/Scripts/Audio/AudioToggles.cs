using System;
using UnityEngine;

// The two on/off switches the player can flip from the gameplay screen: short
// sound effects, and background music. Separate from the volume sliders - a
// switch that is off stays off whatever the slider says, and turning it back on
// restores the volume the player had chosen.
public static class AudioToggles
{
    private const string SfxKey = "settings.sfxEnabled";
    private const string MusicKey = "settings.musicEnabled";

    public static event Action Changed;

    public static bool SfxEnabled
    {
        get { return PlayerPrefs.GetInt(SfxKey, 1) == 1; }
        set { Store(SfxKey, value); }
    }

    public static bool MusicEnabled
    {
        get { return PlayerPrefs.GetInt(MusicKey, 1) == 1; }
        set { Store(MusicKey, value); }
    }

    private static void Store(string key, bool value)
    {
        int stored = value ? 1 : 0;
        if (PlayerPrefs.GetInt(key, 1) == stored && PlayerPrefs.HasKey(key)) { return; }

        PlayerPrefs.SetInt(key, stored);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
