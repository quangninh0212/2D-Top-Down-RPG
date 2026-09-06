using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Installs the systems that have to exist regardless of which scene the game
// starts in - pressing Play on Scene3 in the editor has to work as well as
// launching the app properly.
public static class GameBootstrap
{
    private static bool installed;
    private static EventSystem persistentEventSystem;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (installed) { return; }
        installed = true;

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // A phone has no mouse to aim with, so weapons pick their own targets.
        MobileInput.UseAutoTargeting = Application.isMobilePlatform;

        EnsureEventSystem();
        GameSaveManager.EnsureExists();
        AudioManager.EnsureExists();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Only one EventSystem exists for the whole session, and it outlives scene
    // loads. Previously each screen made its own, which died with its scene -
    // so the second gameplay level a player entered had no EventSystem at all
    // and none of the touch controls responded.
    public static void EnsureEventSystem()
    {
        if (persistentEventSystem != null) { return; }

        GameObject go = new GameObject("EventSystem (Persistent)");
        Object.DontDestroyOnLoad(go);

        persistentEventSystem = go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    // Scenes authored before this existed still carry their own; two active
    // EventSystems fight over input, so the scene-owned ones are removed.
    private static void RemoveDuplicateEventSystems()
    {
        EventSystem[] all = Object.FindObjectsOfType<EventSystem>();

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i] == persistentEventSystem) { continue; }

            Object.Destroy(all[i].gameObject);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Pause freezes time; every scene has to start running again.
        SceneFlow.ResetTimeScale();

        EnsureEventSystem();
        RemoveDuplicateEventSystems();

        AudioManager.PlayMusicForScene(scene.name);

        // Arriving anywhere means the journey is over. Relying on the loading
        // screen alone to clear this left every gate permanently dead if that
        // screen ever failed to finish its own coroutine.
        if (scene.name != GameScenes.Loading)
        {
            SceneFlow.ClearPending();
        }

        if (GameScenes.IsGameplayScene(scene.name))
        {
            // Runs before any Start, so the player is placed against the real
            // walls rather than whatever outline the scene file had baked in.
            TilemapCollisionRebuilder.Rebuild();

            GameplayRuntime.EnsureExists();
        }
    }
}
