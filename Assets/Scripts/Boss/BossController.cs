using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Soul Warden. Three phases that get busier as its health drops, every
// dangerous attack preceded by a visible tell so the player can read it and
// dash out of the way.
[RequireComponent(typeof(BossHealth))]
public class BossController : MonoBehaviour
{
    [Header("Projectiles")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 6.5f;
    [SerializeField] private float spawnDistance = 0.9f;

    [Header("Summons")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private GameObject grapePrefab;
    [SerializeField] private int maxLiveSummons = 4;

    [Header("Movement")]
    [SerializeField] private float driftSpeed = 0.9f;
    [SerializeField] private float preferredRange = 5.5f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 0.45f;
    [SerializeField] private Color telegraphColour = new Color(1f, 0.55f, 0.95f);

    private BossHealth health;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Color baseColour;

    private int phase = 1;
    private bool defeated;
    private bool acting;

    private readonly List<GameObject> summons = new List<GameObject>();

    // A tiny pool keeps a burst of projectiles from allocating on a phone.
    private readonly Queue<GameObject> projectilePool = new Queue<GameObject>();

    private void Awake()
    {
        health = GetComponent<BossHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null) { baseColour = spriteRenderer.color; }
    }

    private void Start()
    {
        GameSaveManager save = GameSaveManager.Instance;
        if (save != null && save.Data.bossPhase > 0) { phase = save.Data.bossPhase; }

        GameMessages.Banner("SOUL WARDEN", "Người gác Cổng Linh Hồn đã thức tỉnh");

        StartCoroutine(BehaviourRoutine());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // ----- phases ---------------------------------------------------------

    public void OnHealthChanged(float fraction)
    {
        int wanted = fraction > 0.65f ? 1 : fraction > 0.30f ? 2 : 3;

        if (wanted == phase) { return; }

        phase = wanted;

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null) { save.Data.bossPhase = phase; }

        GameMessages.Toast("SOUL WARDEN - GIAI ĐOẠN " + phase);
        StartCoroutine(PhaseFlashRoutine());
    }

    private IEnumerator PhaseFlashRoutine()
    {
        if (spriteRenderer == null) { yield break; }

        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = baseColour;
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ----- behaviour ------------------------------------------------------

    private IEnumerator BehaviourRoutine()
    {
        yield return new WaitForSeconds(1.2f);

        while (!defeated)
        {
            switch (phase)
            {
                case 1:
                    yield return ConeBurst(5, 55f);
                    yield return Rest(2.2f);
                    break;

                case 2:
                    yield return ConeBurst(7, 80f);
                    yield return Rest(1.2f);
                    yield return RadialBurst(10);
                    yield return TrySummon(slimePrefab, 2);
                    yield return Rest(1.6f);
                    break;

                default:
                    yield return ConeBurst(9, 100f);
                    yield return Rest(0.9f);
                    yield return RadialBurst(14);
                    yield return Rest(0.9f);
                    yield return TrySummon(grapePrefab, 1);
                    yield return TrySummon(slimePrefab, 1);
                    yield return Rest(1.2f);
                    break;
            }
        }
    }

    private IEnumerator Rest(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds && !defeated)
        {
            elapsed += Time.deltaTime;
            Drift();
            yield return null;
        }
    }

    // Hovers towards a comfortable distance rather than charging the player, so
    // there is always somewhere to dodge to.
    private void Drift()
    {
        if (body == null || PlayerController.Instance == null) { return; }

        Vector2 toPlayer = (Vector2)(PlayerController.Instance.transform.position - transform.position);
        float distance = toPlayer.magnitude;
        if (distance < 0.05f) { return; }

        Vector2 direction = toPlayer / distance;
        float speed = driftSpeed * (phase >= 2 ? 1.35f : 1f);

        // Back off when too close, close in when too far, hold otherwise.
        float sign = distance > preferredRange + 1f ? 1f : distance < preferredRange - 1f ? -1f : 0f;

        body.MovePosition(body.position + direction * (sign * speed * Time.deltaTime));

        if (spriteRenderer != null) { spriteRenderer.flipX = direction.x < 0f; }
    }

    private IEnumerator Telegraph()
    {
        if (spriteRenderer == null)
        {
            yield return new WaitForSeconds(telegraphTime);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < telegraphTime && !defeated)
        {
            elapsed += Time.deltaTime;

            // Pulses towards the tell colour and back, twice.
            float t = Mathf.PingPong(elapsed * 6f, 1f);
            spriteRenderer.color = Color.Lerp(baseColour, telegraphColour, t);

            yield return null;
        }

        spriteRenderer.color = baseColour;
    }

    private IEnumerator ConeBurst(int count, float spread)
    {
        yield return Telegraph();

        if (defeated || PlayerController.Instance == null) { yield break; }

        Vector2 toPlayer = (Vector2)(PlayerController.Instance.transform.position - transform.position);
        float centre = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        float step = count > 1 ? spread / (count - 1) : 0f;
        float angle = centre - spread * 0.5f;

        AudioManager.PlaySfx(GameSfx.BossAttack);

        for (int i = 0; i < count; i++)
        {
            FireProjectile(angle);
            angle += step;
        }
    }

    private IEnumerator RadialBurst(int count)
    {
        yield return Telegraph();

        if (defeated) { yield break; }

        AudioManager.PlaySfx(GameSfx.BossAttack);

        float step = 360f / count;

        // Offset each ring so consecutive bursts never leave the same gap.
        float offset = Random.Range(0f, step);

        for (int i = 0; i < count; i++)
        {
            FireProjectile(offset + i * step);
        }
    }

    private void FireProjectile(float angleDegrees)
    {
        if (projectilePrefab == null) { return; }

        float radians = angleDegrees * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        Vector3 position = transform.position + direction * spawnDistance;

        GameObject projectile = Rent(position, direction);
        if (projectile == null) { return; }

        Projectile move = projectile.GetComponent<Projectile>();
        if (move != null) { move.UpdateMoveSpeed(projectileSpeed); }
    }

    private GameObject Rent(Vector3 position, Vector3 direction)
    {
        GameObject projectile = null;

        while (projectilePool.Count > 0 && projectile == null)
        {
            projectile = projectilePool.Dequeue();
        }

        if (projectile == null)
        {
            projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
        }
        else
        {
            projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
            projectile.SetActive(true);
        }

        projectile.transform.right = direction;
        return projectile;
    }

    private IEnumerator TrySummon(GameObject prefab, int count)
    {
        if (prefab == null) { yield break; }

        CleanSummonList();

        if (summons.Count >= maxLiveSummons) { yield break; }

        yield return Telegraph();

        for (int i = 0; i < count && summons.Count < maxLiveSummons; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(2f, 3.5f);
            GameObject spawned = Instantiate(prefab, transform.position + (Vector3)offset, Quaternion.identity);

            // Summons are a pressure tool, not part of the objective - Scene5's
            // gate is opened by the boss dying, not by clearing them.
            EnemyHealth spawnedHealth = spawned.GetComponent<EnemyHealth>();
            if (spawnedHealth != null) { spawnedHealth.MarkAsSummon(); }

            summons.Add(spawned);
        }
    }

    private void CleanSummonList()
    {
        for (int i = summons.Count - 1; i >= 0; i--)
        {
            if (summons[i] == null) { summons.RemoveAt(i); }
        }
    }

    // ----- death ----------------------------------------------------------

    public void OnBossDefeated()
    {
        if (defeated) { return; }
        defeated = true;

        StopAllCoroutines();
        DespawnSummons();

        if (LevelManager.Instance != null) { LevelManager.Instance.NotifyBossDefeated(); }

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null)
        {
            save.Data.bossDefeated = true;
            save.Data.bossCurrentHealth = -1;
        }

        GameMessages.Banner("SOUL WARDEN ĐÃ BỊ ĐÁNH BẠI", "Cổng Linh Hồn đã mở");

        // Runs on a helper because this object is about to be destroyed.
        VictorySequence.Begin();
    }

    private void DespawnSummons()
    {
        CleanSummonList();

        for (int i = 0; i < summons.Count; i++)
        {
            if (summons[i] != null) { Destroy(summons[i]); }
        }

        summons.Clear();
    }
}
