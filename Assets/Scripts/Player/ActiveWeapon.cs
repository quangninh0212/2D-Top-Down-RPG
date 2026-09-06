using System.Collections;
using UnityEngine;

// The pivot the equipped weapon hangs from, and the attack loop. Holding the
// attack button (mouse or on-screen) keeps swinging on the weapon's cooldown.
public class ActiveWeapon : Singleton<ActiveWeapon>
{
    public MonoBehaviour CurrentActiveWeapon { get; private set; }

    private PlayerControls playerControls;
    private float timeBetweenAttacks;

    private bool attackButtonDown, isAttacking = false;
    private bool disabled;

    protected override void Awake()
    {
        base.Awake();

        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    private void Start()
    {
        playerControls.Combat.Attack.started += OnAttackStarted;
        playerControls.Combat.Attack.canceled += OnAttackCanceled;

        AttackCooldown();
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Combat.Attack.started -= OnAttackStarted;
            playerControls.Combat.Attack.canceled -= OnAttackCanceled;
            playerControls.Dispose();
        }
    }

    private void Update()
    {
        Attack();
    }

    public void NewWeapon(MonoBehaviour newWeapon)
    {
        CurrentActiveWeapon = newWeapon;

        AttackCooldown();

        IWeapon weapon = CurrentActiveWeapon as IWeapon;
        if (weapon != null && weapon.GetWeaponInfo() != null)
        {
            timeBetweenAttacks = weapon.GetWeaponInfo().weaponCooldown;
        }
    }

    public void WeaponNull()
    {
        CurrentActiveWeapon = null;
    }

    // Death removes the weapon but must leave this singleton alive, or the next
    // scene finds no pivot to hang a weapon from.
    public void DisableForDeath()
    {
        disabled = true;
        attackButtonDown = false;

        if (CurrentActiveWeapon != null)
        {
            Destroy(CurrentActiveWeapon.gameObject);
            CurrentActiveWeapon = null;
        }
    }

    private void AttackCooldown()
    {
        isAttacking = true;
        StopAllCoroutines();
        StartCoroutine(TimeBetweenAttacksRoutine());
    }

    private IEnumerator TimeBetweenAttacksRoutine()
    {
        yield return new WaitForSeconds(timeBetweenAttacks);
        isAttacking = false;
    }

    private void OnAttackStarted(UnityEngine.InputSystem.InputAction.CallbackContext context) => StartAttacking();
    private void OnAttackCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context) => StopAttacking();

    private void StartAttacking()
    {
        attackButtonDown = true;
    }

    private void StopAttacking()
    {
        attackButtonDown = false;
    }

    // Called from the on-screen attack button / aim zone on touch devices.
    public void StartAttackingTouch() => StartAttacking();
    public void StopAttackingTouch() => StopAttacking();

    private void Attack()
    {
        if (disabled) { return; }
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.isDead) { return; }
        if (PauseMenuUI.IsPaused) { return; }

        bool held = attackButtonDown || MobileInput.AttackHeld;

        if (!held || isAttacking || CurrentActiveWeapon == null) { return; }

        AttackCooldown();
        (CurrentActiveWeapon as IWeapon)?.Attack();
    }
}
