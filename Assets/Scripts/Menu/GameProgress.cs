using UnityEngine;
using UnityEngine.SceneManagement;

// Remembers which area the player was last in, so Continue has something to
// return to. Deliberately small: the tutorial project has no save system, and
// inventing one would change gameplay rather than the front end.
public static class GameProgress
{
    private const string LastSceneKey = "progress.lastScene";

    public static bool HasSave
    {
        get { return PlayerPrefs.HasKey(LastSceneKey); }
    }

    public static string LastScene
    {
        get { return PlayerPrefs.GetString(LastSceneKey, "Scene1"); }
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(LastSceneKey);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartRecording()
    {
        SceneManager.sceneLoaded += (scene, mode) => Record(scene.name);

        // The first scene has already loaded by the time this runs, so it never
        // raises the event - covers pressing Play straight into a gameplay scene.
        Record(SceneManager.GetActiveScene().name);
    }

    private static void Record(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName == MainMenu.MenuSceneName) { return; }

        PlayerPrefs.SetString(LastSceneKey, sceneName);
        PlayerPrefs.Save();
    }
}
