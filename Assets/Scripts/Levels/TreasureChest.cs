using System;
using UnityEngine;

// Collision object Z. Touching it bursts the chest open: it explodes in a flash,
// pays out gold, throws out a new item for the player to collect, and is gone.
// Opened chests stay opened for the rest of the run.
[RequireComponent(typeof(BoxCollider2D))]
public class TreasureChest : MonoBehaviour
{
    public static event Action<TreasureChest> OnOpened;

    [SerializeField] private int goldReward = 10;

    // The item that pops out - a health pickup by default.
    [SerializeField] private GameObject bonusItemPrefab;

    [SerializeField] private GameObject burstEffectPrefab;

    private PersistentObjectId persistentId;
    private bool opened;

    public string PersistentId
    {
        get { return persistentId != null ? persistentId.Id : null; }
    }

    public int GoldReward
    {
        get { return goldReward; }
    }

    private void Awake()
    {
        persistentId = GetComponent<PersistentObjectId>();
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.GetComponent<PlayerController>() != null) { Open(); }
    }

    public bool Open()
    {
        if (opened) { return false; }
        opened = true;

        Vector3 position = transform.position;

        // Effect 6: it blows open.
        AudioManager.PlaySfx(GameSfx.Explosion);
        AudioManager.PlaySfx(GameSfx.ChestOpen);

        if (burstEffectPrefab != null) { Instantiate(burstEffectPrefab, position, Quaternion.identity); }
        ExpandingRing.Spawn(position, new Color(1f, 0.8f, 0.35f, 0.9f), 3f, 0.4f);

        if (ScreenShakeManager.Instance != null) { ScreenShakeManager.Instance.ShakeScreen(); }

        // Effect 7: gold.
        if (EconomyManager.Instance != null) { EconomyManager.Instance.AddGold(goldReward); }
        GameMessages.Toast("RƯƠNG BÁU! +" + goldReward + " VÀNG");

        // Effect 8: a new item appears.
        if (bonusItemPrefab != null) { Instantiate(bonusItemPrefab, position, Quaternion.identity); }

        OnOpened?.Invoke(this);
        Destroy(gameObject);

        return true;
    }
}
