using UnityEngine;

public class Bow : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    private static readonly int FireHash = Animator.StringToHash("Fire");

    private Animator myAnimator;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    public void Attack()
    {
        if (myAnimator != null) { myAnimator.SetTrigger(FireHash); }

        AudioManager.PlaySfx(GameSfx.BowShot);

        if (arrowPrefab == null || arrowSpawnPoint == null || ActiveWeapon.Instance == null) { return; }

        // The pivot is already aimed by MouseFollow, so the arrow inherits the
        // aim direction - auto-target on a phone, mouse on a desktop.
        GameObject newArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, ActiveWeapon.Instance.transform.rotation);

        Projectile projectile = newArrow.GetComponent<Projectile>();
        if (projectile != null && weaponInfo != null)
        {
            projectile.UpdateProjectileRange(weaponInfo.weaponRange);
        }
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }
}
