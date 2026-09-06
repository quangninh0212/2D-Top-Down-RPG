using NUnit.Framework;

// The progression rules the coursework brief is specific about: what each level
// unlocks, what things cost, and that a cleared level stays cleared.
public class ProgressionTests
{
    [Test]
    public void NewRun_StartsWithSwordOnly()
    {
        SaveData data = new SaveData();

        Assert.IsTrue(data.OwnsWeapon(WeaponType.Sword));
        Assert.IsFalse(data.OwnsWeapon(WeaponType.Bow));
        Assert.IsFalse(data.OwnsWeapon(WeaponType.Staff));
        Assert.AreEqual(0, data.gold);
        Assert.AreEqual(1, data.highestUnlockedLevel);
    }

    [Test]
    public void BowUnlocksInShop_OnlyAfterLevelOne()
    {
        SaveData data = new SaveData();
        Assert.IsFalse(WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Bow));

        data.GetOrCreateScene(GameScenes.Scene1).completed = true;
        Assert.IsTrue(WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Bow));
    }

    [Test]
    public void StaffUnlocksInShop_OnlyAfterLevelTwo()
    {
        SaveData data = new SaveData();
        data.GetOrCreateScene(GameScenes.Scene1).completed = true;

        Assert.IsFalse(WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Staff));

        data.GetOrCreateScene(GameScenes.Scene2).completed = true;
        Assert.IsTrue(WeaponTypeInfo.IsUnlockedInShop(data, WeaponType.Staff));
    }

    [Test]
    public void WeaponPrices_MatchTheBrief()
    {
        Assert.AreEqual(0, WeaponTypeInfo.ShopPrice(WeaponType.Sword));
        Assert.AreEqual(10, WeaponTypeInfo.ShopPrice(WeaponType.Bow));
        Assert.AreEqual(20, WeaponTypeInfo.ShopPrice(WeaponType.Staff));
    }

    [Test]
    public void LevelRewards_MatchTheBrief()
    {
        Assert.AreEqual(10, LevelCatalog.Get(1).RewardGold);
        Assert.AreEqual(20, LevelCatalog.Get(2).RewardGold);
        Assert.AreEqual(25, LevelCatalog.Get(3).RewardGold);
        Assert.AreEqual(30, LevelCatalog.Get(4).RewardGold);
    }

    [Test]
    public void EachLevelSceneMapsToItsCatalogEntry()
    {
        for (int level = 1; level <= LevelCatalog.Count; level++)
        {
            string scene = GameScenes.LevelScene(level);

            Assert.IsTrue(LevelCatalog.TryGet(scene, out LevelInfo info), "No catalog entry for " + scene);
            Assert.AreEqual(level, info.Number);
            Assert.AreEqual(level, GameScenes.LevelNumberOf(scene));
        }
    }

    [Test]
    public void MenuScenesAreNotTreatedAsGameplay()
    {
        Assert.IsFalse(GameScenes.IsGameplayScene(GameScenes.MainMenu));
        Assert.IsFalse(GameScenes.IsGameplayScene(GameScenes.Loading));
        Assert.IsFalse(GameScenes.IsGameplayScene(GameScenes.Splash));
        Assert.IsFalse(GameScenes.IsGameplayScene(GameScenes.Victory));
        Assert.IsTrue(GameScenes.IsGameplayScene(GameScenes.Scene1));
    }

    [Test]
    public void ClearedSceneState_SurvivesRevisiting()
    {
        SaveData data = new SaveData();

        SceneStateData first = data.GetOrCreateScene(GameScenes.Scene1);
        first.completed = true;
        first.gateOpen = true;
        first.rewardClaimed = true;
        first.MarkRemoved("Scene1:enemy:0");

        // Asking for it again must return the same record, not a fresh one.
        SceneStateData again = data.GetOrCreateScene(GameScenes.Scene1);

        Assert.AreSame(first, again);
        Assert.IsTrue(again.completed);
        Assert.IsTrue(again.gateOpen);
        Assert.IsTrue(again.rewardClaimed, "The completion reward must never be paid twice.");
        Assert.IsTrue(again.IsRemoved("Scene1:enemy:0"), "A killed enemy must not respawn on a revisit.");
        Assert.AreEqual(1, data.scenes.Count);
    }

    [Test]
    public void EnemyHealthEntries_AreStoredPerObjectAndOverwritten()
    {
        SceneStateData state = new SceneStateData { sceneName = GameScenes.Scene2 };

        state.SetHealth("a", 3);
        state.SetHealth("b", 1);
        state.SetHealth("a", 2);

        Assert.AreEqual(2, state.GetHealth("a", 99));
        Assert.AreEqual(1, state.GetHealth("b", 99));
        Assert.AreEqual(99, state.GetHealth("missing", 99));
        Assert.AreEqual(2, state.enemyHealth.Count);
    }
}
