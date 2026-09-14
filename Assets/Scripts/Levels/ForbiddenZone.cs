using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An area of the map enemies are not supposed to enter. When one crosses into
// it, an alarm sounds a set number of times - between three and six - and the
// zone flashes. Enemies are not stopped: the warning is for the player.
[RequireComponent(typeof(BoxCollider2D))]
public class ForbiddenZone : MonoBehaviour
{
    public const int MinimumWarnings = 3;
    public const int MaximumWarnings = 6;

    public static event Action<ForbiddenZone, EnemyHealth> OnIntrusion;

    [SerializeField] private Vector2 size = new Vector2(5f, 4f);
    [SerializeField, Range(MinimumWarnings, MaximumWarnings)] private int warningCount = 4;
    [SerializeField] private float warningInterval = 0.35f;

    // After an alarm finishes, another intrusion must wait this long before it
    // can set the alarm off again, so a crowd walking in does not blare forever.
    [SerializeField] private float rearmDelay = 1.5f;

    // Enemies that happen to be standing in the zone when the level loads have
    // not "moved into" it; they are ignored for this long.
    [SerializeField] private float armDelay = 0.75f;

    private readonly HashSet<EnemyHealth> inside = new HashSet<EnemyHealth>();

    private SpriteRenderer visual;
    private Coroutine alarm;
    private float armedAt;
    private float rearmedAt;

    public int WarningCount
    {
        get { return ClampWarnings(warningCount); }
    }

    public int WarningsInLastAlarm { get; private set; }

    public int TotalWarningsPlayed { get; private set; }

    public bool AlarmActive
    {
        get { return alarm != null; }
    }

    public Vector2 Size
    {
        get { return size; }
    }

    public static int ClampWarnings(int requested)
    {
        return Mathf.Clamp(requested, MinimumWarnings, MaximumWarnings);
    }

    private void Awake()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;

        BuildVisual();
    }

    // The striped sprite is one unit across. It is stretched on a child so the
    // zone's own transform - and so its collider - keeps the authored size.
    private void BuildVisual()
    {
        GameObject child = new GameObject("Zone Visual");
        child.transform.SetParent(transform, false);
        child.transform.localScale = new Vector3(size.x, size.y, 1f);

        visual = child.AddComponent<SpriteRenderer>();
        visual.sprite = GameplaySprites.HazardZone;
        visual.sortingOrder = -1;
        visual.color = new Color(1f, 1f, 1f, 0.9f);
    }

    private void Start()
    {
        armedAt = Time.time + armDelay;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.isTrigger) { return; }

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.IsDead) { return; }

        bool alreadyInside = !inside.Add(enemy);
        if (alreadyInside) { return; }

        if (Time.time < armedAt) { return; }

        OnIntrusion?.Invoke(this, enemy);

        if (alarm == null && Time.time >= rearmedAt)
        {
            alarm = StartCoroutine(AlarmRoutine());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) { return; }

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null) { inside.Remove(enemy); }
    }

    private IEnumerator AlarmRoutine()
    {
        GameMessages.Toast("CẢNH BÁO: QUÁI VẬT XÂM NHẬP VÙNG CẤM!");

        WarningsInLastAlarm = 0;
        int count = WarningCount;

        for (int i = 0; i < count; i++)
        {
            AudioManager.PlaySfx(GameSfx.Warning);
            WarningsInLastAlarm++;
            TotalWarningsPlayed++;

            if (visual != null) { visual.color = new Color(1f, 0.55f, 0.55f, 1f); }
            yield return new WaitForSeconds(warningInterval * 0.5f);

            if (visual != null) { visual.color = new Color(1f, 1f, 1f, 0.9f); }
            yield return new WaitForSeconds(warningInterval * 0.5f);
        }

        rearmedAt = Time.time + rearmDelay;
        alarm = null;
    }

    private void OnDisable()
    {
        alarm = null;
        inside.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}
