using UnityEngine;
using UnityEngine.SceneManagement;

// Every scene change in the game goes through here. It parks the destination
// in a static field, then loads the loading screen, which does the real async
// load. Nothing else is allowed to call SceneManager.LoadScene for gameplay.
public static class SceneFlow
{
    public static string PendingScene { get; private set; }
    public static string PendingTransitionName { get; private set; }

    // True when the player is resuming a save and should be dropped at the
    // stored coordinates rather than at an area entrance.
    public static bool RestoreSavedPosition { get; private set; }

    public static bool IsTransitioning { get; private set; }

    // Walking through a door: snapshot the level we are leaving, then load.
    public static void GoToLevel(string sceneName, string transitionName)
    {
        if (IsTransitioning) { return; }

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null && save.CanSave())
        {
            // Snapshot the level being left first - enemy health, gate state,
            // gold - then point the save at the destination. Order matters:
            // capturing after this would put the old scene's name and the old
            // scene's player position back into the save.
            save.CaptureLiveState();

            save.Data.currentScene = sceneName;
            save.Data.lastTransitionName = transitionName;
            save.Data.hasPlayerPosition = false;   // the area entrance decides where we land

            save.SaveWithoutCapture();
        }

        Load(sceneName, transitionName, false);
    }

    public static void StartNewRun()
    {
        // Explicitly, rather than relying on the story screen to do it on the
        // way past: a new run must never inherit the last one's player, least
        // of all the dead one left behind by the game over screen.
        LeaveGameplay();

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null)
        {
            save.NewRun();
            save.MarkRunStartedAt(GameScenes.Scene1);
        }

        // A new run opens with the story, which then drops the player into the
        // first level. Continuing a saved run goes straight back to the level.
        GoToStory(false);
    }

    // ----- story ----------------------------------------------------------

    // True while the story screen is showing the ending rather than the
    // opening; it decides both the text and where the screen leads next.
    public static bool StoryIsEpilogue { get; private set; }

    public static void GoToStory(bool epilogue)
    {
        StoryIsEpilogue = epilogue;

        ResetTimeScale();
        MobileInput.ResetAll();
        LeaveGameplay();

        SceneManager.LoadScene(GameScenes.Story);
    }

    // Where the story screen hands over to: into the game at the start, on to
    // the victory screen at the end.
    public static void ContinueFromStory()
    {
        if (StoryIsEpilogue)
        {
            StoryIsEpilogue = false;
            GoToVictory();
            return;
        }

        Load(GameScenes.Scene1, "", false);
    }

    // Returns false when there was nothing valid to continue from.
    public static bool ContinueRun()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save == null || !save.LoadRun()) { return false; }

        Load(save.Data.currentScene, save.Data.lastTransitionName, save.Data.hasPlayerPosition);
        return true;
    }

    public static void GoToMainMenu()
    {
        LeaveGameplay();
        SceneManager.LoadScene(GameScenes.MainMenu);
    }

    public static void GoToVictory()
    {
        LeaveGameplay();
        SceneManager.LoadScene(GameScenes.Victory);
    }

    // Where a screen's back button leads. Stored rather than inferred: the same
    // progress screen is opened from the menu, from game over and from victory,
    // and each of those wants the player handed back to where they came from.
    public static string ScreenReturnScene { get; private set; } = GameScenes.MainMenu;

    public static void GoToScreen(string screenName, string returnScene)
    {
        if (string.IsNullOrEmpty(screenName)) { return; }

        // Gameplay is never a safe place to come back to: the run that was
        // being played has already ended by the time a screen is opened.
        ScreenReturnScene = string.IsNullOrEmpty(returnScene) || GameScenes.IsGameplayScene(returnScene)
            ? GameScenes.MainMenu
            : returnScene;

        LeaveGameplay();
        SceneManager.LoadScene(screenName);
    }

    public static void ReturnFromScreen()
    {
        string target = ScreenReturnScene;
        ScreenReturnScene = GameScenes.MainMenu;

        ResetTimeScale();
        SceneManager.LoadScene(string.IsNullOrEmpty(target) ? GameScenes.MainMenu : target);
    }

    // The player, the gameplay canvas and the managers all live in
    // DontDestroyOnLoad so they can walk between levels. Leaving gameplay has to
    // clear them out, or the menu would end up with a controllable player
    // standing on top of it and a second set of singletons next run.
    private static void LeaveGameplay()
    {
        ResetTimeScale();
        MobileInput.ResetAll();
        IsTransitioning = false;
        PendingScene = null;

        DestroyRootOf(PlayerController.Instance);
        DestroyRootOf(UIFade.Instance);
        DestroyRootOf(BaseSingleton.Instance);
        DestroyRootOf(CameraController.Instance);
        DestroyRootOf(EconomyManager.Instance);
        DestroyRootOf(ScreenShakeManager.Instance);
    }

    private static void DestroyRootOf(Component component)
    {
        if (component == null) { return; }

        Object.Destroy(component.transform.root.gameObject);
    }

    // Restart after a death: wipe the run and drop into a brand new Scene1.
    public static void RestartFromScratch()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save != null) { save.DeleteRun(); }

        StartNewRun();
    }

    // Cleared once the level has placed the player, so the next door transition
    // uses its area entrance instead of the stale saved coordinates.
    public static void ConsumeSavedPosition()
    {
        RestoreSavedPosition = false;
    }

    public static void ClearPending()
    {
        PendingScene = null;
        IsTransitioning = false;
    }

    private static void Load(string sceneName, string transitionName, bool restorePosition)
    {
        ResetTimeScale();
        MobileInput.ResetAll();

        PendingScene = sceneName;
        PendingTransitionName = transitionName ?? "";
        RestoreSavedPosition = restorePosition;
        IsTransitioning = true;

        SceneManagement sceneManagement = SceneManagement.Instance;
        if (sceneManagement != null) { sceneManagement.SetTransitionName(PendingTransitionName); }

        SceneManager.LoadScene(GameScenes.Loading);
    }

    // Pause leaves the game frozen; every scene change has to undo that or the
    // next scene loads into a stopped world.
    public static void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }
}
