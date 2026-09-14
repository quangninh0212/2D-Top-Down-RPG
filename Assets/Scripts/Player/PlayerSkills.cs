using System;
using UnityEngine;

// The player's two defensive skills.
//
// Shield (Q, or the KHIÊN button): for a few seconds every incoming hit is
//   absorbed - arrows, grape splashes, boss projectiles and contact damage alike.
// Stun   (E, or the CHOÁNG button): a blast that disables every enemy nearby for
//   a short time. They stop moving, stop attacking and deal no contact damage.
//
// Lives on the player and is added at runtime, like the aim controller.
[DisallowMultipleComponent]
public class PlayerSkills : MonoBehaviour
{
    public static event Action<string> OnSkillUsed;

    [Header("Shield")]
    [SerializeField] private SkillCooldown shield = new SkillCooldown(3f, 9f);

    [Header("Stun")]
    [SerializeField] private SkillCooldown stun = new SkillCooldown(2.5f, 12f);
    [SerializeField] private float stunRadius = 5f;

    // The boss is tougher: it recovers in half the time.
    [SerializeField] private float bossStunFactor = 0.5f;

    private static PlayerSkills instance;

    private SpriteRenderer shieldBubble;
    private Vector3 bubbleScale = Vector3.one * 1.6f;

    // Same self-healing pattern as PlayerAimController: the copy of the player
    // that every scene ships must not be able to leave this null.
    public static PlayerSkills Instance
    {
        get
        {
            if (instance == null && PlayerController.Instance != null)
            {
                instance = PlayerController.Instance.GetComponent<PlayerSkills>();
            }

            return instance;
        }
    }

    public SkillCooldown Shield
    {
        get { return shield; }
    }

    public SkillCooldown StunSkill
    {
        get { return stun; }
    }

    public float StunRadius
    {
        get { return stunRadius; }
    }

    public bool ShieldActive
    {
        get { return shield.IsActive(Time.time); }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { return; }

        instance = this;
        BuildShieldBubble();
    }

    private void OnDestroy()
    {
        if (instance == this) { instance = null; }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) { TryShield(); }
        if (Input.GetKeyDown(KeyCode.E)) { TryStun(); }

        UpdateShieldBubble();
    }

    private bool CanUseSkills()
    {
        if (PauseMenuUI.IsPaused) { return false; }

        PlayerHealth health = PlayerHealth.Instance;
        return health == null || !health.isDead;
    }

    // ----- shield ---------------------------------------------------------

    public bool TryShield()
    {
        if (!CanUseSkills()) { return false; }

        if (!shield.TryActivate(Time.time))
        {
            AudioManager.PlaySfx(GameSfx.Denied);
            return false;
        }

        AudioManager.PlaySfx(GameSfx.ShieldUp);
        OnSkillUsed?.Invoke("shield");
        return true;
    }

    // Called by PlayerHealth before any damage is applied. Returns true when the
    // hit was soaked up by the shield.
    public bool AbsorbHit()
    {
        if (!ShieldActive) { return false; }

        AudioManager.PlaySfx(GameSfx.ShieldBlock);

        if (shieldBubble != null) { shieldBubble.transform.localScale = bubbleScale * 1.25f; }

        return true;
    }

    // A spike trap pops the shield instead of hurting the player.
    public bool BreakShield()
    {
        if (!ShieldActive) { return false; }

        shield.EndEarly(Time.time);
        AudioManager.PlaySfx(GameSfx.ShieldBreak);
        ExpandingRing.Spawn(transform.position, new Color(0.45f, 0.85f, 1f, 0.9f), 2.4f, 0.3f);
        GameMessages.Toast("KHIÊN ĐÃ BỊ PHÁ!");
        return true;
    }

    // ----- stun -----------------------------------------------------------

    public int TryStun()
    {
        if (!CanUseSkills()) { return 0; }

        if (!stun.TryActivate(Time.time))
        {
            AudioManager.PlaySfx(GameSfx.Denied);
            return 0;
        }

        AudioManager.PlaySfx(GameSfx.Stun);
        ExpandingRing.Spawn(transform.position, new Color(0.55f, 0.85f, 1f, 0.85f), stunRadius * 2f, 0.4f);

        int affected = 0;

        foreach (EnemyHealth enemy in FindObjectsOfType<EnemyHealth>())
        {
            if (enemy == null || enemy.IsDead) { continue; }
            if (Vector2.Distance(enemy.transform.position, transform.position) > stunRadius) { continue; }

            float duration = enemy is BossHealth ? stun.Duration * bossStunFactor : stun.Duration;
            EnemyStun.Apply(enemy.gameObject, duration);
            affected++;
        }

        GameMessages.Toast(affected > 0 ? "CHOÁNG " + affected + " QUÁI VẬT!" : "KHÔNG CÓ QUÁI TRONG TẦM");
        OnSkillUsed?.Invoke("stun");
        return affected;
    }

    // ----- visuals --------------------------------------------------------

    private void BuildShieldBubble()
    {
        GameObject bubble = new GameObject("Shield Bubble");
        bubble.transform.SetParent(transform, false);
        bubble.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        bubble.transform.localScale = bubbleScale;

        shieldBubble = bubble.AddComponent<SpriteRenderer>();
        shieldBubble.sprite = GameplaySprites.Ring;
        shieldBubble.color = new Color(0.45f, 0.85f, 1f, 0.55f);
        shieldBubble.sortingOrder = 10;
        shieldBubble.enabled = false;
    }

    private void UpdateShieldBubble()
    {
        if (shieldBubble == null) { return; }

        bool active = ShieldActive;
        shieldBubble.enabled = active;

        if (!active) { return; }

        // Flickers in its last second so the player knows it is about to drop.
        float remaining = shield.RemainingActive(Time.time);
        float alpha = remaining < 1f && Mathf.Repeat(Time.time * 10f, 1f) < 0.5f ? 0.2f : 0.55f;

        Color colour = shieldBubble.color;
        colour.a = alpha;
        shieldBubble.color = colour;

        shieldBubble.transform.localScale = Vector3.Lerp(shieldBubble.transform.localScale,
                                                         bubbleScale * (1f + Mathf.Sin(Time.time * 6f) * 0.04f),
                                                         Time.deltaTime * 10f);
    }
}
