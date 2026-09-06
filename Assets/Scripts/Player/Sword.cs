using UnityEngine;

public class Sword : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private float swordAttackCD = .5f;
    [SerializeField] private WeaponInfo weaponInfo;

    private Transform weaponCollider;
    private Animator myAnimator;

    private GameObject slashAnim;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            weaponCollider = PlayerController.Instance.GetWeaponCollider();
        }

        if (slashAnimSpawnPoint == null)
        {
            GameObject spawnPoint = GameObject.Find("SlashSpawnPoint");
            if (spawnPoint != null) { slashAnimSpawnPoint = spawnPoint.transform; }
        }
    }

    private void Update()
    {
        AimWeapon();
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }

    public void Attack()
    {
        if (myAnimator != null) { myAnimator.SetTrigger("Attack"); }
        if (weaponCollider != null) { weaponCollider.gameObject.SetActive(true); }

        AudioManager.PlaySfx(GameSfx.SwordSwing);

        if (slashAnimPrefab == null || slashAnimSpawnPoint == null) { return; }

        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.parent = transform.parent;
    }

    public void DoneAttackingAnimEvent()
    {
        if (weaponCollider != null) { weaponCollider.gameObject.SetActive(false); }
    }

    public void SwingUpFlipAnimEvent()
    {
        if (slashAnim == null) { return; }

        slashAnim.transform.rotation = Quaternion.Euler(-180, 0, 0);
        FlipSlashIfFacingLeft();
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim == null) { return; }

        slashAnim.transform.rotation = Quaternion.Euler(0, 0, 0);
        FlipSlashIfFacingLeft();
    }

    private void FlipSlashIfFacingLeft()
    {
        if (PlayerController.Instance == null || !PlayerController.Instance.FacingLeft) { return; }

        SpriteRenderer renderer = slashAnim.GetComponent<SpriteRenderer>();
        if (renderer != null) { renderer.flipX = true; }
    }

    // The pivot itself is aimed by MouseFollow; the sword only has to keep its
    // damage collider on the correct side of the player.
    private void AimWeapon()
    {
        if (weaponCollider == null) { return; }

        PlayerAimController aim = PlayerAimController.Instance;
        if (aim == null) { return; }

        bool pointingLeft = aim.GetMeleeDirection().x < 0f;

        weaponCollider.rotation = pointingLeft
            ? Quaternion.Euler(0, -180, 0)
            : Quaternion.Euler(0, 0, 0);
    }
}
