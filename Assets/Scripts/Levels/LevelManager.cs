using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One per gameplay scene. Restores what the save file remembers about this
// level, counts the mandatory enemies, and opens the forward gate when the
// last one dies.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private float introDelay = 0.35f;

    private readonly List<EnemyHealth> mandatoryEnemies = new List<EnemyHealth>();
    private LevelInfo info;
    private bool hasInfo;
    private SceneStateData state;
    private bool gateOpen;
    private bool restored;

    public bool GateOpen
    {
        get { return gateOpen; }
    }

    public int RemainingMandatoryEnemies
    {
        get
        {
            int alive = 0;
            for (int i = 0; i < mandatoryEnemies.Count; i++)
            {
                if (mandatoryEnemies[i] != null && !mandatoryEnemies[i].IsDead) { alive++; }
            }

            return alive;
        }
    }

    public static bool IsCurrentLevelClear
    {
        get { return Instance == null || Instance.gateOpen; }
    }

    private void Awake()
    {
        Instance = this;
        hasInfo = LevelCatalog.TryGet(gameObject.scene.name, out info);
    }

    private void OnDestroy()
    {
        EnemyHealth.OnEnemyDied -= HandleEnemyDied;
        Destructible.OnDestructibleDestroyed -= HandleDestructibleDestroyed;

        if (Instance == this) { Instance = null; }
    }

    private void Start()
    {
        GameSaveManager.EnsureExists();

        state = GameSaveManager.Instance != null
            ? GameSaveManager.Instance.SceneState(gameObject.scene.name)
            : new SceneStateData { sceneName = gameObject.scene.name };

        RestoreSceneState();
        RestorePlayerState();
        CollectMandatoryEnemies();

        EnemyHealth.OnEnemyDied += HandleEnemyDied;
        Destructible.OnDestructibleDestroyed += HandleDestructibleDestroyed;

        // A level that was already finished stays finished, even on a revisit.
        if (state.completed || state.gateOpen || RemainingMandatoryEnemies == 0)
        {
            OpenGate(false);
        }
        else
        {
            SetGateVisuals(false);
        }

        state.visited = true;
        StartCoroutine(IntroRoutine());
        StartCoroutine(UnstickPlayerRoutine());
    }

    // ----- restore --------------------------------------------------------

    private void RestoreSceneState()
    {
        if (state == null || restored) { return; }
        restored = true;

        // Anything the save file says is gone must not exist this time either.
        PersistentObjectId[] tracked = FindObjectsOfType<PersistentObjectId>(true);

        for (int i = 0; i < tracked.Length; i++)
        {
            PersistentObjectId id = tracked[i];
            if (id == null || !id.HasId) { continue; }

            if (state.IsRemoved(id.Id))
            {
                Destroy(id.gameObject);
                continue;
            }

            EnemyHealth enemy = id.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.RestoreHealth(state.GetHealth(id.Id, enemy.MaxHealth));
            }
        }
    }

    // Health, stamina, gold and the equipped weapon come from the run, not from
    // whatever the prefabs happen to default to.
    private void RestorePlayerState()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save == null) { return; }

        save.MarkRunStartedAt(gameObject.scene.name);

        bool restorePosition = SceneFlow.RestoreSavedPosition;
        save.ApplyToLiveScene(restorePosition);

        if (restorePosition)
        {
            // Continuing a save drops the player straight in, so nothing else
            // will clear the fade for us.
            SceneFlow.ConsumeSavedPosition();

            UIFade fade = UIFade.Instance;
            if (fade != null) { fade.FadeToClear(); }
        }

        if (CameraController.Instance != null) { CameraController.Instance.ApplyLevelCamera(); }
    }

    private void CollectMandatoryEnemies()
    {
        mandatoryEnemies.Clear();

        EnemyHealth[] all = FindObjectsOfType<EnemyHealth>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].CountsTowardObjective) { mandatoryEnemies.Add(all[i]); }
        }
    }

    // Area entrances place the player in their own Start, and script order is
    // not guaranteed, so the final position is only known once the frame has
    // finished. Physics also needs a step before overlap queries are accurate.
    private IEnumerator UnstickPlayerRoutine()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        PlayerSpawnSafety.EnsureNotStuck(PlayerController.Instance);
    }

    private IEnumerator IntroRoutine()
    {
        if (!hasInfo) { yield break; }

        yield return new WaitForSeconds(introDelay);

        string subtitle = state != null && state.completed
            ? "Khu vực đã hoàn thành"
            : info.Objective;

        GameMessages.Banner(info.DisplayName, subtitle);
    }

    // ----- events ---------------------------------------------------------

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (enemy == null || enemy.gameObject.scene != gameObject.scene) { return; }

        if (state != null && !string.IsNullOrEmpty(enemy.PersistentId))
        {
            state.MarkRemoved(enemy.PersistentId);
        }

        if (!enemy.CountsTowardObjective || gateOpen) { return; }

        if (RemainingMandatoryEnemies == 0)
        {
            OpenGate(true);
        }
    }

    private void HandleDestructibleDestroyed(Destructible destructible)
    {
        if (destructible == null || state == null) { return; }
        if (destructible.gameObject.scene != gameObject.scene) { return; }

        state.MarkRemoved(destructible.PersistentId);
    }

    // ----- gate -----------------------------------------------------------

    private void OpenGate(bool celebrate)
    {
        gateOpen = true;

        if (state != null)
        {
            state.gateOpen = true;
            state.completed = true;
        }

        SetGateVisuals(true);

        if (!celebrate) { return; }

        AudioManager.PlaySfx(GameSfx.GateOpen);
        GameMessages.Banner("CỔNG ĐÃ MỞ!", "Tiến tới khu vực tiếp theo");

        GrantCompletionRewards();

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null) { save.SaveRun(); }
    }

    private void GrantCompletionRewards()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save == null || state == null || !hasInfo) { return; }

        if (state.rewardClaimed) { return; }
        state.rewardClaimed = true;

        if (info.RewardGold > 0 && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(info.RewardGold);
            GameMessages.Toast("+" + info.RewardGold + " VÀNG");
        }

        int nextLevel = info.Number + 1;
        if (nextLevel <= LevelCatalog.Count && save.Data.highestUnlockedLevel < nextLevel)
        {
            save.Data.highestUnlockedLevel = nextLevel;
        }

        if (info.HasShopUnlock)
        {
            UnlockShopItem(save, info.ShopUnlock);
        }
    }

    private static void UnlockShopItem(GameSaveManager save, WeaponType weapon)
    {
        if (weapon == WeaponType.Bow && !save.Data.shopBowUnlocked)
        {
            save.Data.shopBowUnlocked = true;
            GameMessages.Toast("BOW ĐÃ ĐƯỢC MỞ BÁN TRONG CỬA HÀNG!");
        }
        else if (weapon == WeaponType.Staff && !save.Data.shopStaffUnlocked)
        {
            save.Data.shopStaffUnlocked = true;
            GameMessages.Toast("STAFF ĐÃ ĐƯỢC MỞ BÁN TRONG CỬA HÀNG!");
        }

        save.RaiseChanged();
    }

    private void SetGateVisuals(bool open)
    {
        AreaExit[] exits = FindObjectsOfType<AreaExit>();

        for (int i = 0; i < exits.Length; i++)
        {
            if (exits[i] != null) { exits[i].SetLocked(!open && exits[i].IsForwardGate); }
        }
    }

    // ----- persistence ----------------------------------------------------

    // Writes the live enemy health values back into the save data. Called
    // before a scene change or a manual save.
    public void CaptureState()
    {
        if (state == null) { return; }

        state.gateOpen = gateOpen;

        EnemyHealth[] all = FindObjectsOfType<EnemyHealth>();
        for (int i = 0; i < all.Length; i++)
        {
            EnemyHealth enemy = all[i];
            if (enemy == null || enemy.IsDead || string.IsNullOrEmpty(enemy.PersistentId)) { continue; }

            state.SetHealth(enemy.PersistentId, enemy.CurrentHealth);
        }
    }

    // Called by the boss when it dies, so Scene5 finishes the run.
    public void NotifyBossDefeated()
    {
        gateOpen = true;

        if (state != null)
        {
            state.completed = true;
            state.gateOpen = true;
        }
    }
}
