using UnityEngine;

// Records that outlive a run: best completion time, highest gold, and the
// per-level history shown on the progress screen. Kept in PlayerPrefs
// deliberately - deleting a run must not wipe the player's records.
public static class ProfileStats
{
    private const string BestTimeKey = "stats.bestTime";
    private const string BestGoldKey = "stats.bestGold";
    private const string RunsCompletedKey = "stats.runsCompleted";
    private const string DeathsKey = "stats.deaths";

    // The last run that ended, however it ended. Drives the summary line on the
    // game over and progress screens.
    private const string LastOutcomeKey = "stats.lastOutcome";
    private const string LastLevelKey = "stats.lastLevel";
    private const string LastGoldKey = "stats.lastGold";
    private const string LastTimeKey = "stats.lastTime";

    public const string OutcomeNone = "none";
    public const string OutcomeWin = "win";
    public const string OutcomeDeath = "death";

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

    public static int Deaths
    {
        get { return PlayerPrefs.GetInt(DeathsKey, 0); }
    }

    public static string LastOutcome
    {
        get { return PlayerPrefs.GetString(LastOutcomeKey, OutcomeNone); }
    }

    public static int LastLevel
    {
        get { return PlayerPrefs.GetInt(LastLevelKey, 0); }
    }

    public static int LastGold
    {
        get { return PlayerPrefs.GetInt(LastGoldKey, 0); }
    }

    public static float LastTime
    {
        get { return PlayerPrefs.GetFloat(LastTimeKey, 0f); }
    }

    // ----- per level history ---------------------------------------------

    // How many times this level has ever been cleared, across every run.
    public static int TimesCleared(int levelNumber)
    {
        return PlayerPrefs.GetInt(ClearedKey(levelNumber), 0);
    }

    public static bool HasCleared(int levelNumber)
    {
        return TimesCleared(levelNumber) > 0;
    }

    // Run time on the clock the first time the level was finished fastest, so
    // the progress screen can show a personal best per level.
    public static float BestLevelTime(int levelNumber)
    {
        return PlayerPrefs.GetFloat(BestLevelTimeKey(levelNumber), 0f);
    }

    public static bool HasBestLevelTime(int levelNumber)
    {
        return PlayerPrefs.HasKey(BestLevelTimeKey(levelNumber));
    }

    // Called the moment a level's gate opens for the first time in this run.
    public static void RecordLevelCleared(int levelNumber, float playTime)
    {
        if (levelNumber < 1) { return; }

        PlayerPrefs.SetInt(ClearedKey(levelNumber), TimesCleared(levelNumber) + 1);

        if (!HasBestLevelTime(levelNumber) || playTime < BestLevelTime(levelNumber))
        {
            PlayerPrefs.SetFloat(BestLevelTimeKey(levelNumber), playTime);
        }

        PlayerPrefs.Save();
    }

    // How far the player has ever got, which is what the progress screen calls
    // the "furthest level". Independent of the current run's save file.
    public static int FurthestLevelCleared
    {
        get
        {
            int furthest = 0;
            for (int level = 1; level <= GameScenes.Levels.Length; level++)
            {
                if (HasCleared(level)) { furthest = level; }
            }

            return furthest;
        }
    }

    public static bool AllLevelsCleared
    {
        get
        {
            for (int level = 1; level <= GameScenes.Levels.Length; level++)
            {
                if (!HasCleared(level)) { return false; }
            }

            return true;
        }
    }

    private static string ClearedKey(int levelNumber)
    {
        return "level." + levelNumber + ".cleared";
    }

    private static string BestLevelTimeKey(int levelNumber)
    {
        return "level." + levelNumber + ".bestTime";
    }

    // ----- run outcomes ---------------------------------------------------

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
        SetLastRun(OutcomeWin, GameScenes.Levels.Length, gold, playTime);

        PlayerPrefs.Save();
    }

    // The mirror of RecordVictory: a run that ended at the game over screen.
    public static void RecordDeath(int levelNumber, int gold, float playTime)
    {
        PlayerPrefs.SetInt(DeathsKey, Deaths + 1);
        SetLastRun(OutcomeDeath, levelNumber, gold, playTime);

        PlayerPrefs.Save();
    }

    private static void SetLastRun(string outcome, int levelNumber, int gold, float playTime)
    {
        PlayerPrefs.SetString(LastOutcomeKey, outcome);
        PlayerPrefs.SetInt(LastLevelKey, levelNumber);
        PlayerPrefs.SetInt(LastGoldKey, gold);
        PlayerPrefs.SetFloat(LastTimeKey, playTime);
    }

    // Used by the tests and by nothing else: the game never wipes records.
    public static void ClearAll()
    {
        PlayerPrefs.DeleteKey(BestTimeKey);
        PlayerPrefs.DeleteKey(BestGoldKey);
        PlayerPrefs.DeleteKey(RunsCompletedKey);
        PlayerPrefs.DeleteKey(DeathsKey);
        PlayerPrefs.DeleteKey(LastOutcomeKey);
        PlayerPrefs.DeleteKey(LastLevelKey);
        PlayerPrefs.DeleteKey(LastGoldKey);
        PlayerPrefs.DeleteKey(LastTimeKey);

        for (int level = 1; level <= GameScenes.Levels.Length; level++)
        {
            PlayerPrefs.DeleteKey(ClearedKey(level));
            PlayerPrefs.DeleteKey(BestLevelTimeKey(level));
        }

        PlayerPrefs.Save();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) { seconds = 0f; }

        int total = Mathf.FloorToInt(seconds);
        return string.Format("{0:00}:{1:00}", total / 60, total % 60);
    }
}
