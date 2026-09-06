using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owns the in-memory run and is the only thing that touches SaveSystem.
// Created once by GameBootstrap and kept alive for the whole session.
public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    public static event Action OnGoldOrProgressChanged;

    // The run currently being played. Never null: a fresh SaveData stands in
    // for "no run yet" so menus can read it without null checks everywhere.
    public SaveData Data { get; private set; } = new SaveData();

    // Set the moment the player dies. Every autosave path checks it, so a
    // coroutine finishing after death cannot resurrect the file we just deleted.
    public bool RunEnded { get; private set; }

    private bool runLoaded;

    public static void EnsureExists()
    {
        if (Instance != null) { return; }

        GameObject go = new GameObject("GameSaveManager");
        DontDestroyOnLoad(go);
        go.AddComponent<GameSaveManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    private void Update()
    {
        // Play time only accumulates while actually in a level and alive.
        if (RunEnded || !runLoaded) { return; }
        if (!GameScenes.IsGameplayScene(SceneManager.GetActiveScene().name)) { return; }

        Data.playTime += Time.unscaledDeltaTime;
    }

    // ----- run lifecycle --------------------------------------------------

    public bool HasValidSave()
    {
        return SaveSystem.HasValidSave();
    }

    public void NewRun()
    {
        Data = new SaveData();
        runLoaded = true;
        RunEnded = false;

        SaveSystem.Delete();
        SaveSystem.Save(Data);
        RaiseChanged();
    }

    public bool LoadRun()
    {
        SaveData loaded = SaveSystem.Load();

        if (loaded == null || !loaded.runActive || loaded.runCompleted)
        {
            return false;
        }

        Data = loaded;
        runLoaded = true;
        RunEnded = false;
        RaiseChanged();
        return true;
    }

    public void DeleteRun()
    {
        SaveSystem.Delete();
        Data = new SaveData();
        runLoaded = false;
        RaiseChanged();
    }

    // Player died: the run is over, the file goes, and nothing may write again
    // until a new run starts.
    public void EndRunByDeath()
    {
        RunEnded = true;
        runLoaded = false;
        SaveSystem.Delete();
        Data.runActive = false;
        RaiseChanged();
    }

    public void CompleteRun()
    {
        Data.runCompleted = true;
        Data.runActive = false;
        Data.bossDefeated = true;

        ProfileStats.RecordVictory(Data.playTime, Data.gold);

        // A finished run must not appear behind Continue.
        RunEnded = true;
        runLoaded = false;
        SaveSystem.Delete();
        RaiseChanged();
    }

    // ----- saving ---------------------------------------------------------

    public bool CanSave()
    {
        if (RunEnded || !runLoaded) { return false; }

        PlayerHealth health = PlayerHealth.Instance;
        if (health == null) { return false; }

        return !health.isDead && health.CurrentHealth > 0;
    }

    // Writes the run to disk. Returns false when saving is not allowed right
    // now (dead player, no run, or a menu scene) rather than throwing.
    public bool SaveRun()
    {
        if (!CanSave()) { return false; }

        CaptureLiveState();
        return SaveSystem.Save(Data);
    }

    // Writes what is already in Data without re-reading the live scene. A scene
    // transition has to use this: capturing again would overwrite the
    // destination scene with the one being left, and stamp the player's position
    // in the old level onto the new one - which dropped them inside a wall.
    public bool SaveWithoutCapture()
    {
        if (RunEnded || !runLoaded) { return false; }

        return SaveSystem.Save(Data);
    }

    // Pulls the live scene's values into Data without writing anything, so a
    // scene transition can snapshot state before the objects are destroyed.
    public void CaptureLiveState()
    {
        if (RunEnded) { return; }

        string sceneName = SceneManager.GetActiveScene().name;
        if (GameScenes.IsGameplayScene(sceneName)) { Data.currentScene = sceneName; }

        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            Data.playerX = player.transform.position.x;
            Data.playerY = player.transform.position.y;
            Data.hasPlayerPosition = true;
        }

        PlayerHealth health = PlayerHealth.Instance;
        if (health != null)
        {
            Data.currentHealth = health.CurrentHealth;
            Data.maxHealth = health.MaxHealth;
        }

        Stamina stamina = Stamina.Instance;
        if (stamina != null)
        {
            Data.currentStamina = stamina.CurrentStamina;
            Data.maxStamina = stamina.MaxStamina;
        }

        EconomyManager economy = EconomyManager.Instance;
        if (economy != null) { Data.gold = economy.CurrentGold; }

        ActiveInventory inventory = ActiveInventory.Instance;
        if (inventory != null) { Data.currentWeapon = (int)inventory.CurrentWeapon; }

        LevelManager level = LevelManager.Instance;
        if (level != null) { level.CaptureState(); }
    }

    // Called after a gameplay scene finishes loading, so the live objects take
    // on the saved values.
    public void ApplyToLiveScene(bool restorePosition)
    {
        PlayerHealth health = PlayerHealth.Instance;
        if (health != null) { health.ApplyLoadedHealth(Data.currentHealth, Data.maxHealth); }

        Stamina stamina = Stamina.Instance;
        if (stamina != null) { stamina.ApplyLoadedStamina(Data.currentStamina, Data.maxStamina); }

        EconomyManager economy = EconomyManager.Instance;
        if (economy != null) { economy.SetGold(Data.gold); }

        ActiveInventory inventory = ActiveInventory.Instance;
        if (inventory != null) { inventory.ApplyLoadedWeapon((WeaponType)Data.currentWeapon); }

        PlayerController player = PlayerController.Instance;

        // A stored position only means anything in the scene it was recorded in.
        // Applying one from another level puts the player inside whatever
        // happens to be at those coordinates here.
        bool positionBelongsHere = Data.currentScene == SceneManager.GetActiveScene().name;

        if (restorePosition && Data.hasPlayerPosition && positionBelongsHere && player != null)
        {
            player.transform.position = new Vector3(Data.playerX, Data.playerY, player.transform.position.z);
        }
    }

    // ----- progression helpers -------------------------------------------

    public SceneStateData SceneState(string sceneName)
    {
        return Data.GetOrCreateScene(sceneName);
    }

    public void MarkRunStartedAt(string sceneName)
    {
        runLoaded = true;
        RunEnded = false;
        Data.runActive = true;
        Data.currentScene = sceneName;
    }

    public void RaiseChanged()
    {
        OnGoldOrProgressChanged?.Invoke();
    }

    // ----- app lifecycle --------------------------------------------------

    private void OnApplicationPause(bool paused)
    {
        if (paused) { SaveRun(); }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) { SaveRun(); }
    }

    private void OnApplicationQuit()
    {
        SaveRun();
    }
}
