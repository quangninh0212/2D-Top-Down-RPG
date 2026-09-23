// What finishing a level actually asks of the player. Four of the five used to
// be "kill everything"; these give each region its own shape.
public enum LevelGoal
{
    ClearEnemies,   // the gate opens when the last marked enemy falls
    Survive,        // hold out for a while - killing them all also works
    FindKey,        // the gate is locked until the key is found
    DefeatBoss      // the boss room ends the run rather than opening a gate
}

// Design data for the five levels: what they are called, what they ask for,
// what clearing them pays, and what clearing them unlocks in the shop.
public struct LevelInfo
{
    public int Number;
    public string Scene;
    public string DisplayName;
    public string Objective;
    public int RewardGold;

    public LevelGoal Goal;

    // Seconds to hold out, for a Survive level.
    public float SurviveSeconds;

    // Shop item this level unlocks on completion, or None.
    public WeaponType ShopUnlock;
    public bool HasShopUnlock;
}

public static class LevelCatalog
{
    private static readonly LevelInfo[] Levels =
    {
        new LevelInfo
        {
            Number = 1, Scene = GameScenes.Scene1,
            DisplayName = "FORGOTTEN MEADOW", Objective = "Tiêu diệt 5 Blue Slime",
            Goal = LevelGoal.ClearEnemies,
            RewardGold = 10, ShopUnlock = WeaponType.Bow, HasShopUnlock = true
        },
        new LevelInfo
        {
            Number = 2, Scene = GameScenes.Scene2,
            DisplayName = "SHADOW GROVE", Objective = "Tiêu diệt 5 Grape",
            Goal = LevelGoal.ClearEnemies,
            RewardGold = 20, ShopUnlock = WeaponType.Staff, HasShopUnlock = true
        },
        new LevelInfo
        {
            Number = 3, Scene = GameScenes.Scene3,
            DisplayName = "WHISPERING CROSSROADS", Objective = "Sống sót 45 giây giữa ngã tư",
            Goal = LevelGoal.Survive, SurviveSeconds = 45f,
            RewardGold = 25
        },
        new LevelInfo
        {
            Number = 4, Scene = GameScenes.Scene4,
            DisplayName = "HAUNTED MARSH", Objective = "Tìm chìa khoá mở cổng",
            Goal = LevelGoal.FindKey,
            RewardGold = 30
        },
        new LevelInfo
        {
            Number = 5, Scene = GameScenes.Scene5,
            DisplayName = "GATE OF SOULS", Objective = "Đánh bại Soul Warden",
            Goal = LevelGoal.DefeatBoss,
            RewardGold = 0
        }
    };

    public static bool TryGet(string sceneName, out LevelInfo info)
    {
        for (int i = 0; i < Levels.Length; i++)
        {
            if (Levels[i].Scene == sceneName)
            {
                info = Levels[i];
                return true;
            }
        }

        info = default;
        return false;
    }

    public static LevelInfo Get(int levelNumber)
    {
        int index = levelNumber - 1;
        return index >= 0 && index < Levels.Length ? Levels[index] : Levels[0];
    }

    public static int Count
    {
        get { return Levels.Length; }
    }
}
