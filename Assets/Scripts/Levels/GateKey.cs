using System;
using UnityEngine;

// The key to the marsh gate. Picking it up is the only thing that opens the
// way out of a FindKey level - killing every ghost in the room will not.
// Once taken it stays taken for the rest of the run.
[RequireComponent(typeof(CircleCollider2D))]
public class GateKey : MonoBehaviour
{
    public static event Action<GateKey> OnTaken;

    private PersistentObjectId persistentId;
    private SpriteRenderer spriteRenderer;
    private Vector3 restingPosition;
    private bool taken;

    public string PersistentId
    {
        get { return persistentId != null ? persistentId.Id : null; }
    }

    public bool Taken
    {
        get { return taken; }
    }

    private void Awake()
    {
        persistentId = GetComponent<PersistentObjectId>();

        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.5f;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) { spriteRenderer = gameObject.AddComponent<SpriteRenderer>(); }
        if (spriteRenderer.sprite == null) { spriteRenderer.sprite = GameplaySprites.GateKey; }

        spriteRenderer.sortingOrder = 1;
    }

    private void Start()
    {
        restingPosition = transform.position;
    }

    // Hovers and glows: in a dim marsh it has to be findable.
    private void Update()
    {
        transform.position = restingPosition + new Vector3(0f, Mathf.Sin(Time.time * 2.4f) * 0.1f, 0f);

        float glow = 0.85f + Mathf.Sin(Time.time * 4f) * 0.15f;
        spriteRenderer.color = new Color(1f, glow, 0.55f, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other != null ? other.GetComponent<PlayerController>() : null;
        if (player != null) { Take(); }
    }

    // Drawn as a gizmo as well, because the sprite is generated at runtime and
    // the object would otherwise be invisible while placing it in the editor.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.35f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    public bool Take()
    {
        if (taken) { return false; }
        taken = true;

        AudioManager.PlaySfx(GameSfx.GateOpen);
        GameMessages.Toast("ĐÃ TÌM THẤY CHÌA KHOÁ!");

        ExpandingRing.Spawn(transform.position, new Color(1f, 0.85f, 0.35f, 0.9f), 2.2f, 0.4f);

        OnTaken?.Invoke(this);
        Destroy(gameObject);

        return true;
    }
}
