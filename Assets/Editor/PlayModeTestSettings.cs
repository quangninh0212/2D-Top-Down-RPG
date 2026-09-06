using UnityEditor;

// The smoke tests need the domain to survive entering play mode, otherwise the
// callback driving them is wiped out before it can inspect anything.
//
// That is a project-wide editor setting, so it is put back afterwards. Leaving
// it on would change how Play Mode behaves for everyone: static fields would no
// longer reset between sessions, and GameBootstrap - which guards its setup with
// a static flag - would skip installing on the second Play.
public static class PlayModeTestSettings
{
    private static bool savedEnabled;
    private static EnterPlayModeOptions savedOptions;
    private static bool held;

    public static void ApplyForTest()
    {
        if (!held)
        {
            savedEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            savedOptions = EditorSettings.enterPlayModeOptions;
            held = true;
        }

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
    }

    public static void Restore()
    {
        if (!held) { return; }

        EditorSettings.enterPlayModeOptionsEnabled = savedEnabled;
        EditorSettings.enterPlayModeOptions = savedOptions;
        held = false;
    }
}
