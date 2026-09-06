using System;
using UnityEngine;

// Bushes, crates and barrels. Breaking one is remembered for the rest of the
// run, so walking back into a cleared area does not repopulate it with loot.
public class Destructible : MonoBehaviour
{
    public static event Action<Destructible> OnDestructibleDestroyed;

    [SerializeField] private GameObject destroyVFX;

    private PersistentObjectId persistentId;
    private bool broken;

    public string PersistentId
    {
        get { return persistentId != null ? persistentId.Id : null; }
    }

    private void Awake()
    {
        persistentId = GetComponent<PersistentObjectId>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (broken) { return; }

        bool hitByWeapon = other.gameObject.GetComponent<DamageSource>() != null
                           || other.gameObject.GetComponent<Projectile>() != null;

        if (!hitByWeapon) { return; }

        broken = true;

        PickUpSpawner spawner = GetComponent<PickUpSpawner>();
        if (spawner != null) { spawner.DropItems(); }

        if (destroyVFX != null)
        {
            Instantiate(destroyVFX, transform.position, Quaternion.identity);
        }

        OnDestructibleDestroyed?.Invoke(this);

        Destroy(gameObject);
    }
}
