using System;
using System.Collections.Generic;

// Everything a run consists of. Plain serialisable classes so JsonUtility can
// round-trip them; no Unity types, which keeps the file readable and stable.
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int saveVersion = CurrentVersion;

    // A run only counts as continuable while the player is alive and has not
    // already finished the game.
    public bool runActive = true;
    public bool runCompleted;

    public string currentScene = GameScenes.Scene1;
    public string lastTransitionName = "";
    public float playerX;
    public float playerY;
    public bool hasPlayerPosition;

    public int currentHealth = 3;
    public int maxHealth = 3;
    public int currentStamina = 3;
    public int maxStamina = 3;

    public int gold;
    public float playTime;

    public int currentWeapon = (int)WeaponType.Sword;
    public bool ownsSword = true;
    public bool ownsBow;
    public bool ownsStaff;
    public bool shopBowUnlocked;
    public bool shopStaffUnlocked;

    public int highestUnlockedLevel = 1;
    public bool bossDefeated;

    // -1 means "boss untouched"; anything else restores an in-progress fight.
    public int bossCurrentHealth = -1;
    public int bossPhase;

    public List<SceneStateData> scenes = new List<SceneStateData>();

    public SceneStateData GetOrCreateScene(string sceneName)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i] != null && scenes[i].sceneName == sceneName) { return scenes[i]; }
        }

        SceneStateData state = new SceneStateData { sceneName = sceneName };
        scenes.Add(state);
        return state;
    }

    public SceneStateData FindScene(string sceneName)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i] != null && scenes[i].sceneName == sceneName) { return scenes[i]; }
        }

        return null;
    }

    public bool IsLevelComplete(int levelNumber)
    {
        SceneStateData state = FindScene(GameScenes.LevelScene(levelNumber));
        return state != null && state.completed;
    }

    public bool OwnsWeapon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Bow: return ownsBow;
            case WeaponType.Staff: return ownsStaff;
            default: return ownsSword;
        }
    }

    public void SetOwnsWeapon(WeaponType weapon, bool owned)
    {
        switch (weapon)
        {
            case WeaponType.Bow: ownsBow = owned; break;
            case WeaponType.Staff: ownsStaff = owned; break;
            default: ownsSword = owned; break;
        }
    }
}

[Serializable]
public class SceneStateData
{
    public string sceneName;
    public bool completed;
    public bool rewardClaimed;
    public bool gateOpen;
    public bool visited;

    // Persistent ids of enemies / destructibles that must not come back.
    public List<string> removedObjects = new List<string>();

    // Health of enemies that are still alive, so a half-fought room stays fought.
    public List<EnemyHealthEntry> enemyHealth = new List<EnemyHealthEntry>();

    public bool IsRemoved(string id)
    {
        return !string.IsNullOrEmpty(id) && removedObjects.Contains(id);
    }

    public void MarkRemoved(string id)
    {
        if (string.IsNullOrEmpty(id) || removedObjects.Contains(id)) { return; }

        removedObjects.Add(id);
        RemoveHealthEntry(id);
    }

    public int GetHealth(string id, int fallback)
    {
        for (int i = 0; i < enemyHealth.Count; i++)
        {
            if (enemyHealth[i] != null && enemyHealth[i].id == id) { return enemyHealth[i].health; }
        }

        return fallback;
    }

    public void SetHealth(string id, int health)
    {
        if (string.IsNullOrEmpty(id)) { return; }

        for (int i = 0; i < enemyHealth.Count; i++)
        {
            if (enemyHealth[i] != null && enemyHealth[i].id == id)
            {
                enemyHealth[i].health = health;
                return;
            }
        }

        enemyHealth.Add(new EnemyHealthEntry { id = id, health = health });
    }

    private void RemoveHealthEntry(string id)
    {
        for (int i = enemyHealth.Count - 1; i >= 0; i--)
        {
            if (enemyHealth[i] != null && enemyHealth[i].id == id) { enemyHealth.RemoveAt(i); }
        }
    }
}

[Serializable]
public class EnemyHealthEntry
{
    public string id;
    public int health;
}
