using UnityEngine;

// The single place aiming is decided. Sword, Bow, Staff, Magic Laser and the
// weapon pivot all read from here instead of each reaching for the mouse, so
// desktop and touch behave identically from the weapons' point of view.
//
// Desktop: the mouse, as before.
// Touch:   the nearest living enemy inside the auto-target radius, falling back
//          to the direction the player last moved.
[DisallowMultipleComponent]
public class PlayerAimController : MonoBehaviour
{
    private static PlayerAimController instance;

    // Self-healing on purpose. Every gameplay scene ships its own Player
    // prefab, and the copy that loses the singleton race still runs Awake
    // before it is destroyed. Reading the aim controller back off the live
    // player means no ordering of those events can leave this null.
    public static PlayerAimController Instance
    {
        get
        {
            if (instance == null && PlayerController.Instance != null)
            {
                instance = PlayerController.Instance.GetComponent<PlayerAimController>();
            }

            return instance;
        }
    }

    [SerializeField] private float autoTargetRadius = 9f;

    // How far in front of the player a sword swing will still snap onto a
    // target. Deliberately short: melee should feel aimed, not automatic.
    [SerializeField] private float meleeAssistRadius = 2.5f;
    [SerializeField] private float meleeAssistAngle = 70f;

    private Vector2 aimDirection = Vector2.right;
    private Vector2 lastFacingDirection = Vector2.right;
    private Transform currentTarget;
    private float nextTargetScanTime;

    public Vector2 AimDirection
    {
        get { return aimDirection; }
    }

    public Vector2 LastFacingDirection
    {
        get { return lastFacingDirection; }
    }

    public Transform CurrentTarget
    {
        get { return currentTarget; }
    }

    public bool HasTarget
    {
        get { return currentTarget != null; }
    }

    private void Awake()
    {
        // The scene's own Player is destroyed moments after this when a
        // persistent one already exists. Claiming the singleton here would
        // hand it to the copy that is about to die, and its OnDestroy would
        // then clear it - leaving the surviving player with no aim controller
        // and auto-targeting silently dead until the next trip through the
        // main menu.
        if (instance != null && instance != this) { return; }

        instance = this;
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // The player walks into the next level still holding a target from the last
    // one. That reference dies with its scene, and the scan interval means it
    // would not be replaced for a fraction of a second - long enough to swing at
    // nothing on arrival. Clearing it forces a fresh look immediately.
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                               UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        currentTarget = null;
        nextTargetScanTime = 0f;
    }

    private void OnDestroy()
    {
        if (instance == this) { instance = null; }
    }

    // Fed by PlayerController every frame so the fallback aim always points
    // somewhere sensible even when the player is standing still.
    public void ReportMovement(Vector2 movement)
    {
        if (movement.sqrMagnitude > 0.01f)
        {
            lastFacingDirection = movement.normalized;
        }
    }

    private void Update()
    {
        aimDirection = Resolve();
    }

    private Vector2 Resolve()
    {
        // An explicit touch aim (dragging on the attack side of the screen)
        // always wins - the player is pointing at something on purpose.
        if (MobileInput.TryGetAimDirection(out Vector2 touchAim) && touchAim.sqrMagnitude > 0.001f)
        {
            currentTarget = null;
            lastFacingDirection = touchAim.normalized;
            return touchAim.normalized;
        }

        if (MobileInput.UseAutoTargeting)
        {
            return ResolveAutoTarget();
        }

        return ResolveMouse();
    }

    private Vector2 ResolveMouse()
    {
        currentTarget = null;

        Camera camera = Camera.main;
        if (camera == null) { return lastFacingDirection; }

        Vector3 mouseWorld = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (Vector2)(mouseWorld - transform.position);

        if (direction.sqrMagnitude < 0.0001f) { return lastFacingDirection; }

        direction.Normalize();
        lastFacingDirection = direction;
        return direction;
    }

    private Vector2 ResolveAutoTarget()
    {
        // Scanning every frame with FindObjectsOfType would be wasteful on a
        // phone; a few times a second is more than enough for this pace.
        if (Time.time >= nextTargetScanTime)
        {
            nextTargetScanTime = Time.time + 0.15f;
            currentTarget = FindNearestEnemy(autoTargetRadius);
        }

        if (currentTarget == null) { return lastFacingDirection; }

        Vector2 toTarget = (Vector2)(currentTarget.position - transform.position);
        if (toTarget.sqrMagnitude < 0.0001f) { return lastFacingDirection; }

        return toTarget.normalized;
    }

    // Sword aiming: mostly the player's own facing, nudged onto an enemy that
    // is already almost in front of them.
    public Vector2 GetMeleeDirection()
    {
        Vector2 baseDirection = aimDirection;

        if (!MobileInput.UseAutoTargeting) { return baseDirection; }

        Transform nearby = FindNearestEnemy(meleeAssistRadius);
        if (nearby == null) { return baseDirection; }

        Vector2 toTarget = (Vector2)(nearby.position - transform.position);
        if (toTarget.sqrMagnitude < 0.0001f) { return baseDirection; }

        toTarget.Normalize();

        // Only assist when the target is roughly where the player is already
        // pointing, so the swing never spins around behind them.
        if (Vector2.Angle(baseDirection, toTarget) > meleeAssistAngle) { return baseDirection; }

        return toTarget;
    }

    private Transform FindNearestEnemy(float radius)
    {
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        Transform best = null;
        float bestDistance = radius * radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || enemy.IsDead) { continue; }

            float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (distance > bestDistance) { continue; }

            bestDistance = distance;
            best = enemy.transform;
        }

        return best;
    }

    // Convenience for the weapons, which all need the same "flip when pointing
    // left" treatment on the shared weapon pivot.
    public static Vector2 CurrentAim
    {
        get { return Instance != null ? Instance.AimDirection : Vector2.right; }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, autoTargetRadius);
    }
}
