using System.Collections;
using UnityEngine;

public class MagicLaser : MonoBehaviour
{
    [SerializeField] private float laserGrowTime = 2f;

    private bool isGrowing = true;
    private bool directionSet;
    private float laserRange;
    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D capsuleCollider2D;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        // Staff normally sets the direction explicitly; this covers a laser
        // spawned by anything that does not.
        if (!directionSet) { SetDirection(PlayerAimController.CurrentAim); }
    }

    // Aimed once at spawn: a growing beam that re-aimed every frame would sweep
    // across the screen and hit everything.
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) { direction = Vector2.right; }

        transform.right = direction.normalized;
        directionSet = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<Indestructible>() && !other.isTrigger)
        {
            isGrowing = false;
        }
    }

    public void UpdateLaserRange(float laserRange)
    {
        this.laserRange = laserRange;
        StartCoroutine(IncreaseLaserLengthRoutine());
    }

    private IEnumerator IncreaseLaserLengthRoutine()
    {
        float timePassed = 0f;

        while (spriteRenderer != null && spriteRenderer.size.x < laserRange && isGrowing)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / laserGrowTime;
            float length = Mathf.Lerp(1f, laserRange, linearT);

            spriteRenderer.size = new Vector2(length, 1f);

            if (capsuleCollider2D != null)
            {
                capsuleCollider2D.size = new Vector2(length, capsuleCollider2D.size.y);
                capsuleCollider2D.offset = new Vector2(length / 2f, capsuleCollider2D.offset.y);
            }

            yield return null;
        }

        SpriteFade fade = GetComponent<SpriteFade>();
        if (fade != null) { StartCoroutine(fade.SlowFadeRoutine()); }
    }
}
