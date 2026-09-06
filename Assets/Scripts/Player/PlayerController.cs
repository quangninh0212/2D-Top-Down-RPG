using System.Collections;
using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    public bool FacingLeft { get { return facingLeft; } }

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float dashSpeed = 4f;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] private Transform weaponCollider;

    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private Knockback knockback;
    private PlayerAimController aimController;
    private float startingMoveSpeed;

    private bool facingLeft = false;
    private bool isDashing = false;
    private bool controlsEnabled = true;

    protected override void Awake()
    {
        base.Awake();

        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();

        // The aim controller lives on the player and is added here rather than
        // on the prefab, so an older prefab still gets one.
        aimController = GetComponent<PlayerAimController>();
        if (aimController == null) { aimController = gameObject.AddComponent<PlayerAimController>(); }
    }

    private void Start()
    {
        playerControls.Combat.Dash.performed += OnDashPerformed;

        startingMoveSpeed = moveSpeed;

        if (ActiveInventory.Instance != null) { ActiveInventory.Instance.EquipStartingWeapon(); }
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    protected void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Combat.Dash.performed -= OnDashPerformed;
            playerControls.Dispose();
        }
    }

    private void Update()
    {
        PlayerInput();
    }

    private void FixedUpdate()
    {
        AdjustPlayerFacingDirection();
        Move();
    }

    public Transform GetWeaponCollider()
    {
        return weaponCollider;
    }

    // Death and the pause menu both need the player to stop responding without
    // disabling the object, which would take the singleton with it.
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (!enabled)
        {
            movement = Vector2.zero;
            if (myAnimator != null)
            {
                myAnimator.SetFloat("moveX", 0f);
                myAnimator.SetFloat("moveY", 0f);
            }
        }
    }

    private void PlayerInput()
    {
        if (!controlsEnabled)
        {
            movement = Vector2.zero;
            return;
        }

        Vector2 keyboardMove = playerControls.Movement.Move.ReadValue<Vector2>();
        movement = keyboardMove.sqrMagnitude > 0.01f ? keyboardMove : MobileInput.MoveInput;

        if (aimController != null) { aimController.ReportMovement(movement); }

        myAnimator.SetFloat("moveX", movement.x);
        myAnimator.SetFloat("moveY", movement.y);
    }

    private void Move()
    {
        if (!controlsEnabled) { return; }
        if (knockback != null && knockback.GettingKnockedBack) { return; }
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.isDead) { return; }

        rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
    }

    // The sprite faces wherever the weapon is aimed, which on touch means the
    // auto-target or the last movement direction.
    private void AdjustPlayerFacingDirection()
    {
        if (mySpriteRender == null) { return; }

        float facingX = aimController != null ? aimController.AimDirection.x : 1f;

        // Almost-vertical aim should not flip the sprite back and forth.
        if (Mathf.Abs(facingX) < 0.05f) { return; }

        facingLeft = facingX < 0f;
        mySpriteRender.flipX = facingLeft;
    }

    // Called from the on-screen Dash button on touch devices.
    public void TouchDash() => Dash();

    private void OnDashPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context) => Dash();

    private void Dash()
    {
        if (!controlsEnabled) { return; }
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.isDead) { return; }
        if (isDashing) { return; }
        if (Stamina.Instance == null || Stamina.Instance.CurrentStamina <= 0) { return; }

        Stamina.Instance.UseStamina();
        isDashing = true;
        moveSpeed *= dashSpeed;

        if (myTrailRenderer != null) { myTrailRenderer.emitting = true; }

        AudioManager.PlaySfx(GameSfx.Dash);

        StartCoroutine(EndDashRoutine());
    }

    private IEnumerator EndDashRoutine()
    {
        float dashTime = .2f;
        float dashCD = .25f;

        yield return new WaitForSeconds(dashTime);

        moveSpeed = startingMoveSpeed;
        if (myTrailRenderer != null) { myTrailRenderer.emitting = false; }

        yield return new WaitForSeconds(dashCD);

        isDashing = false;
    }
}
