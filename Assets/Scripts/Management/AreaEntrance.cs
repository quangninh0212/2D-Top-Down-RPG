using UnityEngine;

// Where the player appears when arriving from a particular gate. The name has
// to match the transition name set by the AreaExit on the other side.
public class AreaEntrance : MonoBehaviour
{
    [SerializeField] private string transitionName;

    // Used when a level is entered with no transition at all - a fresh run, or
    // a save that stored no position.
    [SerializeField] private bool isDefaultSpawn;

    public string TransitionName
    {
        get { return transitionName; }
    }

    public bool IsDefaultSpawn
    {
        get { return isDefaultSpawn; }
    }

    private void Start()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null) { return; }

        if (!ShouldPlacePlayer()) { return; }

        player.transform.position = transform.position;

        if (CameraController.Instance != null) { CameraController.Instance.ApplyLevelCamera(); }

        UIFade fade = UIFade.Instance;
        if (fade != null) { fade.FadeToClear(); }
    }

    private bool ShouldPlacePlayer()
    {
        // Continuing a save puts the player back exactly where they stood.
        if (SceneFlow.RestoreSavedPosition) { return false; }

        SceneManagement management = SceneManagement.Instance;
        string incoming = management != null ? management.SceneTransitionName : null;

        if (!string.IsNullOrEmpty(incoming))
        {
            return incoming == transitionName;
        }

        return isDefaultSpawn;
    }
}
