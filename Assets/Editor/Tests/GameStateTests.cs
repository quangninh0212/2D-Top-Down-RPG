using NUnit.Framework;
using UnityEngine;

// The rules behind the game over screen, the win screen, the saved history and
// the NPC geometry - everything about them that can be checked without a scene.
public class GameStateTests
{
    [SetUp]
    public void ClearRecords()
    {
        ProfileStats.ClearAll();
    }

    [TearDown]
    public void TidyUp()
    {
        ProfileStats.ClearAll();
    }

    // ----- navigation -----------------------------------------------------

    [Test]
    public void GameOver_OffersThreeSeparateDestinations()
    {
        string[] destinations = GameOverUI.Destinations;

        Assert.GreaterOrEqual(destinations.Length, 3, "The brief asks for at least three buttons.");
        Assert.AreEqual(destinations.Length, Distinct(destinations),
            "Two buttons leading to the same screen would not be two destinations.");
    }

    [Test]
    public void Victory_OffersThreeSeparateDestinations()
    {
        string[] destinations = VictoryScreenController.Destinations;

        Assert.GreaterOrEqual(destinations.Length, 3);
        Assert.AreEqual(destinations.Length, Distinct(destinations));
    }

    [Test]
    public void EveryDestinationIsASceneInTheBuild()
    {
        AssertAllInBuild(GameOverUI.Destinations);
        AssertAllInBuild(VictoryScreenController.Destinations);
    }

    [Test]
    public void TheThreeScreensAreScenesOfTheirOwn()
    {
        Assert.AreEqual(3, GameScenes.Screens.Length);

        for (int i = 0; i < GameScenes.Screens.Length; i++)
        {
            string screen = GameScenes.Screens[i];

            Assert.IsTrue(GameScenes.IsScreen(screen));
            Assert.IsFalse(GameScenes.IsGameplayScene(screen), "A screen is not a level.");
        }
    }

    // ----- the saved history ---------------------------------------------

    [Test]
    public void ClearingALevelIsRemembered()
    {
        Assert.IsFalse(ProfileStats.HasCleared(1));

        ProfileStats.RecordLevelCleared(1, 70f);

        Assert.IsTrue(ProfileStats.HasCleared(1));
        Assert.AreEqual(1, ProfileStats.TimesCleared(1));
        Assert.AreEqual(70f, ProfileStats.BestLevelTime(1), 0.01f);
        Assert.AreEqual(1, ProfileStats.FurthestLevelCleared);
    }

    [Test]
    public void OnlyAFasterClearReplacesTheRecord()
    {
        ProfileStats.RecordLevelCleared(2, 90f);
        ProfileStats.RecordLevelCleared(2, 120f);

        Assert.AreEqual(90f, ProfileStats.BestLevelTime(2), 0.01f, "A slower run is still a slower run.");
        Assert.AreEqual(2, ProfileStats.TimesCleared(2));

        ProfileStats.RecordLevelCleared(2, 45f);
        Assert.AreEqual(45f, ProfileStats.BestLevelTime(2), 0.01f);
    }

    [Test]
    public void FurthestLevelIgnoresGaps()
    {
        ProfileStats.RecordLevelCleared(1, 10f);
        ProfileStats.RecordLevelCleared(3, 30f);

        Assert.AreEqual(3, ProfileStats.FurthestLevelCleared);
        Assert.IsFalse(ProfileStats.AllLevelsCleared);

        for (int level = 1; level <= GameScenes.Levels.Length; level++)
        {
            ProfileStats.RecordLevelCleared(level, 10f * level);
        }

        Assert.IsTrue(ProfileStats.AllLevelsCleared);
    }

    [Test]
    public void ALostRunIsRecordedWithoutTouchingTheRecords()
    {
        ProfileStats.RecordVictory(300f, 80);

        int runs = ProfileStats.RunsCompleted;
        float bestTime = ProfileStats.BestTime;

        ProfileStats.RecordDeath(3, 25, 140f);

        Assert.AreEqual(1, ProfileStats.Deaths);
        Assert.AreEqual(ProfileStats.OutcomeDeath, ProfileStats.LastOutcome);
        Assert.AreEqual(3, ProfileStats.LastLevel);
        Assert.AreEqual(25, ProfileStats.LastGold);

        Assert.AreEqual(runs, ProfileStats.RunsCompleted, "Dying is not finishing.");
        Assert.AreEqual(bestTime, ProfileStats.BestTime, 0.01f, "A death must not overwrite a best time.");
    }

    [Test]
    public void WinningRecordsBothTheRunAndTheRecords()
    {
        ProfileStats.RecordVictory(500f, 40);
        ProfileStats.RecordVictory(420f, 30);

        Assert.AreEqual(2, ProfileStats.RunsCompleted);
        Assert.AreEqual(420f, ProfileStats.BestTime, 0.01f);
        Assert.AreEqual(40, ProfileStats.BestGold, "The best gold is not the latest gold.");
        Assert.AreEqual(ProfileStats.OutcomeWin, ProfileStats.LastOutcome);
    }

    // ----- achievements ---------------------------------------------------

    [Test]
    public void MilestonesUnlockFromTheRecordedHistory()
    {
        Assert.AreEqual(0, AchievementsScreenController.UnlockedCount(), "Nothing is earned up front.");

        ProfileStats.RecordLevelCleared(1, 60f);
        Assert.AreEqual(1, AchievementsScreenController.UnlockedCount());

        ProfileStats.RecordVictory(500f, 120);

        // That one run finished the game, beat ten minutes and passed 100 gold,
        // so only "clear every level" is still outstanding.
        Assert.AreEqual(4, AchievementsScreenController.UnlockedCount());
        Assert.AreEqual(5, AchievementsScreenController.TotalCount);
    }

    // ----- NPC geometry ---------------------------------------------------

    [Test]
    public void RotateTurnsADirectionWithoutStretchingIt()
    {
        Vector2 turned = NpcSenses.Rotate(Vector2.right, 90f);

        Assert.AreEqual(0f, turned.x, 0.001f);
        Assert.AreEqual(1f, turned.y, 0.001f);
        Assert.AreEqual(1f, turned.magnitude, 0.001f);

        Vector2 back = NpcSenses.Rotate(turned, -90f);
        Assert.AreEqual(1f, back.x, 0.001f);
        Assert.AreEqual(0f, back.y, 0.001f);
    }

    [Test]
    public void FlankAnglesSpreadThePackOut()
    {
        // The angle each pack member approaches from, as SlimePackBrain works
        // it out: distinct slots must not end up on top of each other.
        Vector2 first = NpcSenses.Rotate(Vector2.right, 90f);
        Vector2 second = NpcSenses.Rotate(Vector2.right, 90f + 115f);
        Vector2 third = NpcSenses.Rotate(Vector2.right, 90f + 230f);

        Assert.Less(Vector2.Dot(first, second), 0.9f);
        Assert.Less(Vector2.Dot(second, third), 0.9f);
        Assert.Less(Vector2.Dot(first, third), 0.9f);
    }

    // ----- plumbing -------------------------------------------------------

    private static int Distinct(string[] values)
    {
        System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < values.Length; i++) { seen.Add(values[i]); }

        return seen.Count;
    }

    private static void AssertAllInBuild(string[] destinations)
    {
        for (int i = 0; i < destinations.Length; i++)
        {
            bool found = false;

            for (int j = 0; j < GameScenes.BuildOrder.Length; j++)
            {
                if (GameScenes.BuildOrder[j] == destinations[i]) { found = true; }
            }

            Assert.IsTrue(found, destinations[i] + " is not in the build order.");
        }
    }
}
