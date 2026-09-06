using UnityEngine;

// The boss shares the normal enemy damage pipeline but reports its health to
// the on-screen bar and ends the run rather than just dropping loot.
public class BossHealth : EnemyHealth
{
    [SerializeField] private string bossName = "SOUL WARDEN";

    private BossController controller;
    private bool registered;

    public string BossName
    {
        get { return bossName; }
    }

    public float HealthFraction
    {
        get { return MaxHealth <= 0 ? 0f : Mathf.Clamp01(CurrentHealth / (float)MaxHealth); }
    }

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<BossController>();
    }

    protected override void Start()
    {
        base.Start();

        // Restore a fight the player saved partway through.
        GameSaveManager save = GameSaveManager.Instance;
        if (save != null && save.Data.bossCurrentHealth > 0)
        {
            RestoreHealth(save.Data.bossCurrentHealth);
        }

        ShowBar();
        PushHealthToBar();
    }

    private void ShowBar()
    {
        if (registered) { return; }
        registered = true;

        BossHealthBarUI bar = Bar;
        if (bar != null) { bar.Show(bossName); }
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        PushHealthToBar();

        if (controller != null) { controller.OnHealthChanged(HealthFraction); }

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null && !IsDead) { save.Data.bossCurrentHealth = CurrentHealth; }
    }

    private void PushHealthToBar()
    {
        BossHealthBarUI bar = Bar;
        if (bar != null) { bar.SetFraction(HealthFraction); }
    }

    protected override void Die()
    {
        BossHealthBarUI bar = Bar;
        if (bar != null) { bar.Hide(); }

        if (controller != null) { controller.OnBossDefeated(); }

        base.Die();
    }

    private static BossHealthBarUI Bar
    {
        get { return GameplayRuntime.Instance != null ? GameplayRuntime.Instance.BossBar : null; }
    }
}
