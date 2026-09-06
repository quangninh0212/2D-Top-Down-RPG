using UnityEngine;

// Records that outlive a run: best completion time and highest gold. Kept in
// PlayerPrefs deliberately - deleting a run must not wipe the player's records.
public static class ProfileStats
{
    private const string BestTimeKey = "stats.bestTime";
    private const string BestGoldKey = "stats.bestGold";
    private const string RunsCompletedKey = "stats.runsCompleted";

    public static bool HasBestTime
    {
        get { return PlayerPrefs.HasKey(BestTimeKey); }
    }

    public static float BestTime
    {
        get { return PlayerPrefs.GetFloat(BestTimeKey, 0f); }
    }

    public static int BestGold
    {
        get { return PlayerPrefs.GetInt(BestGoldKey, 0); }
    }

    public static int RunsCompleted
    {
        get { return PlayerPrefs.GetInt(RunsCompletedKey, 0); }
    }

    public static void RecordVictory(float playTime, int gold)
    {
        if (!HasBestTime || playTime < BestTime)
        {
            PlayerPrefs.SetFloat(BestTimeKey, playTime);
        }

        if (gold > BestGold)
        {
            PlayerPrefs.SetInt(BestGoldKey, gold);
        }

        PlayerPrefs.SetInt(RunsCompletedKey, RunsCompleted + 1);
        PlayerPrefs.Save();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) { seconds = 0f; }

        int total = Mathf.FloorToInt(seconds);
        return string.Format("{0:00}:{1:00}", total / 60, total % 60);
    }
}
