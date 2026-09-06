using UnityEngine;

public class Staff : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject magicLaser;
    [SerializeField] private Transform magicLaserSpawnPoint;

    private Animator myAnimator;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // The direction the shot was aimed at when the animation started, so a
    // target moving during the wind-up does not drag the laser around.
    private Vector2 queuedAim = Vector2.right;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    public void Attack()
    {
        queuedAim = PlayerAimController.CurrentAim;

        if (myAnimator != null) { myAnimator.SetTrigger(AttackHash); }

        AudioManager.PlaySfx(GameSfx.StaffShot);
    }

    public void SpawnStaffProjectileAnimEvent()
    {
        if (magicLaser == null || magicLaserSpawnPoint == null) { return; }

        GameObject newLaser = Instantiate(magicLaser, magicLaserSpawnPoint.position, Quaternion.identity);

        MagicLaser laser = newLaser.GetComponent<MagicLaser>();
        if (laser == null) { return; }

        laser.SetDirection(queuedAim);
        laser.UpdateLaserRange(weaponInfo != null ? weaponInfo.weaponRange : 8f);
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }
}
