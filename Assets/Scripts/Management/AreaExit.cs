using UnityEngine;

// A doorway between levels. Forward gates stay locked until every mandatory
// enemy in the level is dead; gates leading back to an earlier level are always
// open, so the player can always retreat.
public class AreaExit : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string sceneTransitionName;

    [Tooltip("Leave on Auto to decide from the level numbers of the two scenes.")]
    [SerializeField] private GateDirection direction = GateDirection.Auto;

    private enum GateDirection
    {
        Auto,
        Forward,
        Backward
    }

    private bool locked;
    private float nextLockedMessageTime;

    public string SceneToLoad
    {
        get { return sceneToLoad; }
    }

    // The name the far side's AreaEntrance answers to. Exposed so tooling can
    // check both halves of a doorway agree.
    public string SceneTransitionName
    {
        get { return sceneTransitionName; }
    }

    // A gate counts as forward when it leads to a higher-numbered level; those
    // are the ones the objective has to unlock.
    public bool IsForwardGate
    {
        get
        {
            if (direction == GateDirection.Forward) { return true; }
            if (direction == GateDirection.Backward) { return false; }

            int here = GameScenes.LevelNumberOf(gameObject.scene.name);
            int there = GameScenes.LevelNumberOf(sceneToLoad);

            return here > 0 && there > here;
        }
    }

    public void SetLocked(bool value)
    {
        locked = value;
        ApplyLockedVisuals();
    }

    private void Start()
    {
        ApplyLockedVisuals();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryUse(other);
    }

    // Standing in a locked gate and then clearing the level should let the
    // player through without stepping out and back in.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (locked) { TryUse(other); }
    }

    private void TryUse(Collider2D other)
    {
        if (other == null || other.GetComponent<PlayerController>() == null) { return; }
        if (SceneFlow.IsTransitioning) { return; }
        if (string.IsNullOrEmpty(sceneToLoad)) { return; }

        if (locked)
        {
            ShowLockedMessage();
            return;
        }

        UIFade fade = UIFade.Instance;
        if (fade != null) { fade.FadeToBlack(); }

        SceneFlow.GoToLevel(sceneToLoad, sceneTransitionName);
    }

    private void ShowLockedMessage()
    {
        if (Time.time < nextLockedMessageTime) { return; }
        nextLockedMessageTime = Time.time + 2.5f;

        int remaining = LevelManager.Instance != null ? LevelManager.Instance.RemainingMandatoryEnemies : 0;

        GameMessages.Toast(remaining > 0
            ? "CỔNG BỊ KHÓA - CÒN " + remaining + " QUÁI VẬT"
            : "CỔNG BỊ KHÓA");
    }

    // Locked gates read as dormant: the portal particles dim to a cold blue and
    // the open gate glows warm.
    private void ApplyLockedVisuals()
    {
        Color tint = locked ? new Color(0.35f, 0.45f, 0.75f) : new Color(1f, 0.82f, 0.45f);

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.startColor = tint;
        }

        Light2DTint.Apply(gameObject, tint, locked ? 0.5f : 1f);
    }
}
