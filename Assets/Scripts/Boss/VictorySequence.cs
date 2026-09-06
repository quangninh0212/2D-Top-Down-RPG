using System.Collections;
using UnityEngine;

// Runs the short pause between the boss dying and the victory screen. It lives
// on its own object because the boss is destroyed the moment it dies and cannot
// finish a coroutine of its own.
public class VictorySequence : MonoBehaviour
{
    private const float DelayBeforeVictory = 2.2f;

    private static bool running;

    public static void Begin()
    {
        if (running) { return; }
        running = true;

        GameObject go = new GameObject("VictorySequence");
        DontDestroyOnLoad(go);
        go.AddComponent<VictorySequence>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(DelayBeforeVictory);

        AudioManager.PlaySfx(GameSfx.Victory);

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null)
        {
            // Pull the live gold and play time across first: CompleteRun writes
            // the records and then clears the run file.
            save.CaptureLiveState();
            save.CompleteRun();
        }

        UIFade fade = UIFade.Instance;
        if (fade != null) { fade.FadeToBlack(); }

        yield return new WaitForSeconds(0.8f);

        running = false;
        SceneFlow.GoToVictory();

        Destroy(gameObject);
    }
}
