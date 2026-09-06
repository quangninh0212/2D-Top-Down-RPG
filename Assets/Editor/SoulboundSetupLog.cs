using UnityEngine;

// Shared logging for the setup tools, so every step is traceable in the console
// under one prefix.
public static class SoulboundSetupLog
{
    public const string Prefix = "[SOULBOUND SETUP] ";

    public static void Step(string message)
    {
        Debug.Log(Prefix + message);
    }

    public static void Warn(string message)
    {
        Debug.LogWarning(Prefix + message);
    }
}
