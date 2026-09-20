using UnityEngine;

// The thinking half of an NPC. The base class handles what every one of them
// shares - seeing the player, remembering where it last saw them, telling its
// neighbours, and knowing when it is hurt enough to back off - and leaves the
// actual tactics to the subclass.
//
// It replaces the old EnemyAI, which only ever roamed at random and charged
// whatever was in range. That component is switched off rather than removed, so
// everything already keyed to it (contact damage, the stun skill) still works.
[RequireComponent(typeof(EnemyPathfinding))]
public abstract class NpcBrain : MonoBehaviour
{
    public enum BrainState
    {
        Patrol,      // has not seen anything; walks its own area
        Investigate, // lost sight, or was told about a sighting; goes to look
        Engage,      // can see the player and is acting on it
        Retreat      // hurt: breaks off, then comes back
    }

    [SerializeField] protected float sightRange = 7f;
    [SerializeField] protected float hearingRange = 10f;
    [SerializeField] protected float memoryDuration = 4f;
    [SerializeField] protected float patrolRadius = 2.5f;
    [SerializeField] protected float retreatHealthFraction = 0.34f;
    [SerializeField] protected float retreatDuration = 3f;

    protected EnemyPathfinding pathfinding;
    protected EnemyHealth health;
    protected SpriteRenderer body;

    private BrainState state = BrainState.Patrol;
    private Vector2 lastKnownPlayerPosition;
    private float lastSeenTime = -999f;
    private float retreatUntil;
    private bool hasRetreated;
    private Vector2 home;
    private Vector2 patrolDirection;
    private float nextPatrolTurn;
    private GameObject alertMarker;
    private float alertMarkerUntil;
    private float investigateUntil;

    // Read by the smoke test and by the subclasses.
    public BrainState State
    {
        get { return state; }
    }

    public bool RemembersPlayer
    {
        get { return Time.time - lastSeenTime <= memoryDuration; }
    }

    public Vector2 LastKnownPlayerPosition
    {
        get { return lastKnownPlayerPosition; }
    }

    public int Slot
    {
        get { return NpcAlertNetwork.SlotFor(gameObject); }
    }

    protected virtual void Awake()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
        health = GetComponent<EnemyHealth>();
        body = GetComponent<SpriteRenderer>();

        // The old brain and the new one must not both drive the same body.
        EnemyAI legacy = GetComponent<EnemyAI>();
        if (legacy != null) { legacy.enabled = false; }
    }

    protected virtual void Start()
    {
        home = transform.position;
        patrolDirection = Random.insideUnitCircle.normalized;
    }

    protected virtual void OnEnable()
    {
        NpcAlertNetwork.OnSighting += OnAllySighting;
    }

    protected virtual void OnDisable()
    {
        NpcAlertNetwork.OnSighting -= OnAllySighting;
    }

    protected virtual void OnDestroy()
    {
        NpcAlertNetwork.Release(gameObject);
    }

    private void Update()
    {
        // The stun skill takes precedence over anything the brain wants.
        if (EnemyStun.IsStunned(this))
        {
            pathfinding.StopMoving();
            return;
        }

        PlayerController player = PlayerController.Instance;
        PlayerHealth playerHealth = PlayerHealth.Instance;

        if (player == null || (playerHealth != null && playerHealth.isDead))
        {
            state = BrainState.Patrol;
            Patrol();
            UpdateAlertMarker();
            return;
        }

        Vector2 playerPosition = player.transform.position;
        bool canSee = CanSee(playerPosition);

        if (canSee)
        {
            bool firstContact = !RemembersPlayer;

            lastSeenTime = Time.time;
            lastKnownPlayerPosition = playerPosition;

            if (firstContact) { NoticePlayer(playerPosition); }
        }

        ChooseState(canSee, playerPosition);
        Act(playerPosition, Vector2.Distance(transform.position, playerPosition));

        UpdateAlertMarker();
    }

    // ----- perception -----------------------------------------------------

    protected virtual bool CanSee(Vector2 playerPosition)
    {
        if (Vector2.Distance(transform.position, playerPosition) > sightRange) { return false; }

        return NpcSenses.HasLineOfSight(gameObject, transform.position, playerPosition);
    }

    // First sight after losing track: shout, and flag it on screen.
    protected virtual void NoticePlayer(Vector2 playerPosition)
    {
        ShowAlertMarker();
        NpcAlertNetwork.Report(playerPosition, gameObject);
    }

    // An ally saw something. Near enough to have heard it, and not already busy
    // with the player, means going to look.
    private void OnAllySighting(Vector2 position, GameObject reporter)
    {
        if (reporter == gameObject || this == null) { return; }
        if (Vector2.Distance(transform.position, position) > hearingRange) { return; }
        if (state == BrainState.Retreat || RemembersPlayer) { return; }

        lastKnownPlayerPosition = position;
        state = BrainState.Investigate;
        investigateUntil = Time.time + memoryDuration;
    }

    // ----- state ----------------------------------------------------------

    private void ChooseState(bool canSee, Vector2 playerPosition)
    {
        // Being hurt overrides everything else, but only once: an NPC that
        // retreated, healed a little and came back does not keep running.
        if (state == BrainState.Retreat)
        {
            if (Time.time < retreatUntil) { return; }

            state = RemembersPlayer ? BrainState.Engage : BrainState.Patrol;
            OnRetreatEnded();
            return;
        }

        if (!hasRetreated && ShouldRetreat())
        {
            hasRetreated = true;
            retreatUntil = Time.time + retreatDuration;
            state = BrainState.Retreat;
            OnRetreatBegan();
            return;
        }

        if (canSee)
        {
            state = BrainState.Engage;
            return;
        }

        if (RemembersPlayer || Time.time < investigateUntil)
        {
            state = BrainState.Investigate;
            return;
        }

        state = BrainState.Patrol;
    }

    protected virtual bool ShouldRetreat()
    {
        if (health == null || health.MaxHealth <= 0) { return false; }

        return health.CurrentHealth > 0 &&
               health.CurrentHealth <= Mathf.CeilToInt(health.MaxHealth * retreatHealthFraction);
    }

    protected virtual void OnRetreatBegan()
    {
    }

    protected virtual void OnRetreatEnded()
    {
    }

    private void Act(Vector2 playerPosition, float distance)
    {
        switch (state)
        {
            case BrainState.Engage:
                Engage(playerPosition, distance);
                break;

            case BrainState.Investigate:
                Investigate(lastKnownPlayerPosition);
                break;

            case BrainState.Retreat:
                Retreat(playerPosition);
                break;

            default:
                Patrol();
                break;
        }
    }

    // ----- behaviour ------------------------------------------------------

    protected abstract void Engage(Vector2 playerPosition, float distance);

    // Walk to where the player was last seen; give up once there.
    protected virtual void Investigate(Vector2 target)
    {
        if (Vector2.Distance(transform.position, target) < 0.6f)
        {
            investigateUntil = 0f;
            lastSeenTime = -999f;
            pathfinding.StopMoving();
            return;
        }

        MoveTowards(target);
    }

    // A slow wander that stays near where the NPC started, so a level's
    // enemies do not all drift into one corner over time.
    protected virtual void Patrol()
    {
        if (Time.time >= nextPatrolTurn)
        {
            nextPatrolTurn = Time.time + Random.Range(1.5f, 3f);

            Vector2 offset = (Vector2)transform.position - home;
            patrolDirection = offset.magnitude > patrolRadius
                ? -offset.normalized                        // turn back towards home
                : Random.insideUnitCircle.normalized;
        }

        Move(patrolDirection);
    }

    protected virtual void Retreat(Vector2 playerPosition)
    {
        MoveAwayFrom(playerPosition);
    }

    // ----- movement -------------------------------------------------------

    protected void MoveTowards(Vector2 target)
    {
        Move(target - (Vector2)transform.position);
    }

    protected void MoveAwayFrom(Vector2 target)
    {
        Move((Vector2)transform.position - target);
    }

    // EnemyPathfinding takes a direction, not a destination, and it walks
    // straight into walls, so the direction is steered around them first.
    protected void Move(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            pathfinding.StopMoving();
            return;
        }

        pathfinding.MoveTo(NpcSenses.Avoid(gameObject, transform.position, direction, 1.1f));
    }

    protected void Hold()
    {
        pathfinding.StopMoving();
    }

    // ----- the "!" marker -------------------------------------------------

    protected void ShowAlertMarker()
    {
        if (alertMarker == null)
        {
            alertMarker = new GameObject("Alert");
            alertMarker.transform.SetParent(transform, false);
            alertMarker.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            SpriteRenderer renderer = alertMarker.AddComponent<SpriteRenderer>();
            renderer.sprite = GameplaySprites.Alert;
            renderer.sortingOrder = 50;
        }

        alertMarker.SetActive(true);
        alertMarkerUntil = Time.time + 1.2f;
    }

    private void UpdateAlertMarker()
    {
        if (alertMarker == null || !alertMarker.activeSelf) { return; }

        if (Time.time >= alertMarkerUntil)
        {
            alertMarker.SetActive(false);
            return;
        }

        // Bobs, so it catches the eye against a busy tilemap.
        alertMarker.transform.localPosition =
            new Vector3(0f, 0.9f + Mathf.Sin(Time.time * 9f) * 0.06f, 0f);
    }
}
