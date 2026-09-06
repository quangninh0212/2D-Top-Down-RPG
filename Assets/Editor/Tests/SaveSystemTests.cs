using System.IO;
using NUnit.Framework;
using UnityEngine;

// Covers the rules that decide whether a run can be continued, which is where a
// mistake would silently cost the player their progress.
public class SaveSystemTests
{
    [SetUp]
    [TearDown]
    public void ClearSave()
    {
        SaveSystem.Delete();
    }

    [Test]
    public void NoSaveFile_MeansNothingToContinue()
    {
        Assert.IsFalse(SaveSystem.HasValidSave());
        Assert.IsNull(SaveSystem.Load());
    }

    [Test]
    public void SavedRun_RoundTripsThroughDisk()
    {
        SaveData data = new SaveData
        {
            gold = 42,
            currentScene = GameScenes.Scene3,
            currentHealth = 2,
            maxHealth = 3,
            currentStamina = 1,
            playTime = 123.5f,
            currentWeapon = (int)WeaponType.Bow
        };

        data.SetOwnsWeapon(WeaponType.Bow, true);
        data.GetOrCreateScene(GameScenes.Scene1).completed = true;

        Assert.IsTrue(SaveSystem.Save(data));

        SaveData loaded = SaveSystem.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(42, loaded.gold);
        Assert.AreEqual(GameScenes.Scene3, loaded.currentScene);
        Assert.AreEqual(2, loaded.currentHealth);
        Assert.AreEqual((int)WeaponType.Bow, loaded.currentWeapon);
        Assert.IsTrue(loaded.ownsBow);
        Assert.IsFalse(loaded.ownsStaff);
        Assert.IsTrue(loaded.IsLevelComplete(1));
        Assert.IsFalse(loaded.IsLevelComplete(2));
    }

    [Test]
    public void DeadRun_IsNotContinuable()
    {
        SaveData data = new SaveData { runActive = false };
        SaveSystem.Save(data);

        Assert.IsFalse(SaveSystem.HasValidSave(), "A run the player died in must not offer Continue.");
    }

    [Test]
    public void CompletedRun_IsNotContinuable()
    {
        SaveData data = new SaveData { runCompleted = true };
        SaveSystem.Save(data);

        Assert.IsFalse(SaveSystem.HasValidSave(), "A finished run must not drop the player back into the boss fight.");
    }

    [Test]
    public void DeletingASave_RemovesEveryCopyOfIt()
    {
        SaveSystem.Save(new SaveData());

        // Two saves so the atomic write has produced a backup alongside the file.
        SaveSystem.Save(new SaveData { gold = 5 });
        SaveSystem.Delete();

        Assert.IsFalse(SaveSystem.HasValidSave());
        Assert.IsNull(SaveSystem.Load(), "Death must not leave a recoverable backup behind.");
    }

    [Test]
    public void UnknownSaveVersion_IsRejectedRatherThanHalfApplied()
    {
        SaveData data = new SaveData();
        SaveSystem.Save(data);

        string json = File.ReadAllText(SaveSystem.SavePath);
        File.WriteAllText(SaveSystem.SavePath, json.Replace("\"saveVersion\": 1", "\"saveVersion\": 99"));

        Assert.IsNull(SaveSystem.Load());
    }

    [Test]
    public void CorruptSaveFile_DoesNotThrow()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        File.WriteAllText(SaveSystem.SavePath, "{ this is not json");

        Assert.DoesNotThrow(() => SaveSystem.Load());
        Assert.IsFalse(SaveSystem.HasValidSave());
    }
}
