using UnityEngine;

// Sits on anything that should hurt enemies on contact: the sword's collider,
// the arrow, the magic laser.
public class DamageSource : MonoBehaviour
{
    [Tooltip("Used when there is no equipped weapon to read a damage value from.")]
    [SerializeField] private int fallbackDamage = 1;

    private int damageAmount;

    private void Start()
    {
        damageAmount = fallbackDamage;

        ActiveWeapon activeWeapon = ActiveWeapon.Instance;
        IWeapon weapon = activeWeapon != null ? activeWeapon.CurrentActiveWeapon as IWeapon : null;

        if (weapon != null && weapon.GetWeaponInfo() != null)
        {
            damageAmount = weapon.GetWeaponInfo().weaponDamage;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null) { enemyHealth.TakeDamage(damageAmount); }
    }
}
