// The three weapons the player can own. Magic Laser is the Staff's projectile,
// not a weapon of its own.
public enum WeaponType
{
    Sword = 0,
    Bow = 1,
    Staff = 2
}

public static class WeaponTypeInfo
{
    public const int Count = 3;

    public static string DisplayName(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Bow: return "BOW";
            case WeaponType.Staff: return "STAFF";
            default: return "SWORD";
        }
    }

    public static int ShopPrice(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Bow: return 10;
            case WeaponType.Staff: return 20;
            default: return 0;
        }
    }

    // Level that has to be cleared before the weapon appears in the shop.
    public static int UnlockedByLevel(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Bow: return 1;
            case WeaponType.Staff: return 2;
            default: return 0;
        }
    }

    public static bool IsUnlockedInShop(SaveData data, WeaponType weapon)
    {
        if (data == null) { return weapon == WeaponType.Sword; }

        switch (weapon)
        {
            case WeaponType.Bow: return data.shopBowUnlocked || data.IsLevelComplete(1);
            case WeaponType.Staff: return data.shopStaffUnlocked || data.IsLevelComplete(2);
            default: return true;
        }
    }
}
