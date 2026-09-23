using NUnit.Framework;

// The writing and the level goals, checked without a scene: both are plain
// data, and both are easy to break by editing one entry and not the rest.
public class StoryAndGoalTests
{
    [Test]
    public void TheStoryHasAnOpeningAndAnEnding()
    {
        Assert.GreaterOrEqual(StoryContent.Prologue.Length, 3, "The opening needs enough to set the scene.");
        Assert.GreaterOrEqual(StoryContent.Epilogue.Length, 2);

        foreach (string line in StoryContent.Prologue) { Assert.IsNotEmpty(line); }
        foreach (string line in StoryContent.Epilogue) { Assert.IsNotEmpty(line); }

        Assert.IsNotEmpty(StoryContent.Summary);
    }

    [Test]
    public void EveryLevelIsIntroducedByItsOwnLine()
    {
        for (int level = 1; level <= LevelCatalog.Count; level++)
        {
            Assert.IsNotEmpty(StoryContent.LoreFor(level), "Level " + level + " has no line of its own.");
        }

        Assert.IsEmpty(StoryContent.LoreFor(0), "There is no level 0 to introduce.");
        Assert.IsEmpty(StoryContent.LoreFor(LevelCatalog.Count + 1));
    }

    [Test]
    public void TheLevelsDoNotAllAskForTheSameThing()
    {
        System.Collections.Generic.HashSet<LevelGoal> goals = new System.Collections.Generic.HashSet<LevelGoal>();

        for (int level = 1; level <= LevelCatalog.Count; level++)
        {
            goals.Add(LevelCatalog.Get(level).Goal);
        }

        Assert.GreaterOrEqual(goals.Count, 3, "Five levels sharing one goal is one level five times.");
    }

    [Test]
    public void EachGoalIsSetUpToBeFinishable()
    {
        for (int level = 1; level <= LevelCatalog.Count; level++)
        {
            LevelInfo info = LevelCatalog.Get(level);

            Assert.IsNotEmpty(info.Objective, "Level " + level + " never says what it wants.");
            Assert.IsNotEmpty(info.DisplayName);

            if (info.Goal == LevelGoal.Survive)
            {
                Assert.Greater(info.SurviveSeconds, 0f, "A survival level with no clock cannot be finished.");
            }
        }
    }

    [Test]
    public void TheCrossroadsIsSurvivalAndTheMarshIsAKeyHunt()
    {
        Assert.AreEqual(LevelGoal.Survive, LevelCatalog.Get(3).Goal);
        Assert.AreEqual(45f, LevelCatalog.Get(3).SurviveSeconds, 0.01f);

        Assert.AreEqual(LevelGoal.FindKey, LevelCatalog.Get(4).Goal);
        Assert.AreEqual(LevelGoal.DefeatBoss, LevelCatalog.Get(5).Goal);
    }

    [Test]
    public void TheStorySceneIsInTheBuild()
    {
        bool found = false;

        for (int i = 0; i < GameScenes.BuildOrder.Length; i++)
        {
            if (GameScenes.BuildOrder[i] == GameScenes.Story) { found = true; }
        }

        Assert.IsTrue(found, "The opening cannot be shown if its scene is not in the build.");
        Assert.IsFalse(GameScenes.IsGameplayScene(GameScenes.Story));
    }
}
