using System;
using System.Collections;
using UnityEngine;

// Health for every enemy, including the boss (BossHealth derives from this).
// Death is announced through an event so LevelManager never has to poll.
public class EnemyHealth : MonoBehaviour
{
    // Raised the moment an enemy's health reaches zero, before the object is
    // destroyed, so listeners can still read its id.
    public static event Action<EnemyHealth> OnEnemyDied;

    [SerializeField] protected int startingHealth = 3;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private float knockBackThrust = 15f;

    [Tooltip("Uncheck for enemies the player does not have to kill to open the gate.")]
    [SerializeField] private bool countsTowardObjective = true;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    private PersistentObjectId persistentId;
    private bool isDead;

    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    public int MaxHealth
    {
        get { return startingHealth; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    public bool CountsTowardObjective
    {
        get { return countsTowardObjective; }
    }

    public string PersistentId
    {
        get { return persistentId != null ? persistentId.Id : null; }
    }

    protected virtual void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        persistentId = GetComponent<PersistentObjectId>();
        currentHealth = startingHealth;
    }

    protected virtual void Start()
    {
        // Awake already seeded this; Start only matters when the value was not
        // overwritten by LevelManager restoring a saved fight.
        if (currentHealth <= 0) { currentHealth = startingHealth; }
    }

    // Boss summons are pressure, not part of the level objective, and they have
    // no place in the save file either.
    public void MarkAsSummon()
    {
        countsTowardObjective = false;
    }

    // Used by LevelManager when re-entering a level the player half-cleared.
    public void RestoreHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 1, startingHealth);
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead) { return; }

        currentHealth -= damage;

        if (knockback != null && PlayerController.Instance != null)
        {
            knockback.GetKnockedBack(PlayerController.Instance.transform, knockBackThrust);
        }

        if (flash != null) { StartCoroutine(flash.FlashRoutine()); }

        AudioManager.PlaySfx(GameSfx.EnemyHurt);

        StartCoroutine(CheckDetectDeathRoutine());
    }

    private IEnumerator CheckDetectDeathRoutine()
    {
        float wait = flash != null ? flash.GetRestoreMatTime() : 0.2f;
        yield return new WaitForSeconds(wait);
        DetectDeath();
    }

    public void DetectDeath()
    {
        if (isDead || currentHealth > 0) { return; }

        Die();
    }

    protected virtual void Die()
    {
        isDead = true;

        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        AudioManager.PlaySfx(GameSfx.EnemyDeath);

        PickUpSpawner spawner = GetComponent<PickUpSpawner>();
        if (spawner != null) { spawner.DropItems(); }

        OnEnemyDied?.Invoke(this);

        Destroy(gameObject);
    }
}
