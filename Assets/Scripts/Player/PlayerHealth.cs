using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : Singleton<PlayerHealth>
{
    // Raised once, when the player dies. The game over screen listens for it.
    public static event Action OnPlayerDied;
    public static event Action<int, int> OnHealthChanged;

    public bool isDead { get; private set; }

    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockBackThrustAmount = 10f;
    [SerializeField] private float damageRecoveryTime = 1f;

    private const string HealthSliderName = "Health Slider";
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private Slider healthSlider;
    private int currentHealth;
    private bool canTakeDamage = true;
    private Knockback knockback;
    private Flash flash;

    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    public int MaxHealth
    {
        get { return maxHealth; }
    }

    protected override void Awake()
    {
        base.Awake();

        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();

        currentHealth = maxHealth;
    }

    private void Start()
    {
        isDead = false;

        // A scene load leaves the old slider behind.
        healthSlider = null;
        UpdateHealthSlider();
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (isDead) { return; }

        EnemyAI enemy = other.gameObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            TakeDamage(1, other.transform);
        }
    }

    public void HealPlayer()
    {
        HealPlayer(1);
    }

    public void HealPlayer(int amount)
    {
        if (isDead || amount <= 0) { return; }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthSlider();
        AudioManager.PlaySfx(GameSfx.HealthPickup);
    }

    // Used by the save system when a run is restored or the shop heals the run.
    public void ApplyLoadedHealth(int current, int max)
    {
        maxHealth = Mathf.Max(1, max);
        currentHealth = Mathf.Clamp(current, 0, maxHealth);
        isDead = currentHealth <= 0;
        UpdateHealthSlider();
    }

    public void TakeDamage(int damageAmount, Transform hitTransform)
    {
        if (!canTakeDamage || isDead) { return; }

        if (ScreenShakeManager.Instance != null) { ScreenShakeManager.Instance.ShakeScreen(); }
        if (knockback != null && hitTransform != null) { knockback.GetKnockedBack(hitTransform, knockBackThrustAmount); }
        if (flash != null) { StartCoroutine(flash.FlashRoutine()); }

        canTakeDamage = false;
        currentHealth -= damageAmount;

        AudioManager.PlaySfx(GameSfx.PlayerHurt);
        Haptics.LightTap();

        StartCoroutine(DamageRecoveryRoutine());
        UpdateHealthSlider();
        CheckIfPlayerDeath();
    }

    private void CheckIfPlayerDeath()
    {
        if (currentHealth > 0 || isDead) { return; }

        isDead = true;
        currentHealth = 0;
        UpdateHealthSlider();

        // The run is over the instant the player dies: the save goes now, before
        // any queued autosave can write the dead state back to disk.
        if (GameSaveManager.Instance != null) { GameSaveManager.Instance.EndRunByDeath(); }

        MobileInput.ResetAll();

        PlayerController controller = PlayerController.Instance;
        if (controller != null) { controller.SetControlsEnabled(false); }

        if (ActiveWeapon.Instance != null) { ActiveWeapon.Instance.DisableForDeath(); }

        Animator animator = GetComponent<Animator>();
        if (animator != null) { animator.SetTrigger(DeathHash); }

        AudioManager.PlaySfx(GameSfx.PlayerDeath);

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(1.6f);

        OnPlayerDied?.Invoke();
    }

    private IEnumerator DamageRecoveryRoutine()
    {
        yield return new WaitForSeconds(damageRecoveryTime);
        canTakeDamage = true;
    }

    private void UpdateHealthSlider()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (healthSlider == null)
        {
            GameObject sliderGO = GameObject.Find(HealthSliderName);
            if (sliderGO == null) { return; }

            healthSlider = sliderGO.GetComponent<Slider>();
            if (healthSlider == null) { return; }
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }
}
