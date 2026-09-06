// Design data for the five levels: what they are called, what clearing them
// pays, and what clearing them unlocks in the shop.
public struct LevelInfo
{
    public int Number;
    public string Scene;
    public string DisplayName;
    public string Objective;
    public int RewardGold;

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
            RewardGold = 10, ShopUnlock = WeaponType.Bow, HasShopUnlock = true
        },
        new LevelInfo
        {
            Number = 2, Scene = GameScenes.Scene2,
            DisplayName = "SHADOW GROVE", Objective = "Tiêu diệt 5 Grape",
            RewardGold = 20, ShopUnlock = WeaponType.Staff, HasShopUnlock = true
        },
        new LevelInfo
        {
            Number = 3, Scene = GameScenes.Scene3,
            DisplayName = "WHISPERING CROSSROADS", Objective = "Tiêu diệt 3 Blue Slime và 4 Grape",
            RewardGold = 25
        },
        new LevelInfo
        {
            Number = 4, Scene = GameScenes.Scene4,
            DisplayName = "HAUNTED MARSH", Objective = "Tiêu diệt 4 Ghost",
            RewardGold = 30
        },
        new LevelInfo
        {
            Number = 5, Scene = GameScenes.Scene5,
            DisplayName = "GATE OF SOULS", Objective = "Đánh bại Soul Warden",
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
