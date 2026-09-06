using System;
using UnityEngine;

// Owns which weapons the player has and which one is in their hand. The old
// five-slot UI in UICanvas.prefab is no longer the source of truth: the three
// WeaponInfo assets are referenced directly, so nothing depends on child order.
public class ActiveInventory : Singleton<ActiveInventory>
{
    public static event Action<WeaponType> OnWeaponChanged;

    [Header("Weapons, in WeaponType order")]
    [SerializeField] private WeaponInfo swordInfo;
    [SerializeField] private WeaponInfo bowInfo;
    [SerializeField] private WeaponInfo staffInfo;

    private PlayerControls playerControls;
    private WeaponType currentWeapon = WeaponType.Sword;

    public WeaponType CurrentWeapon
    {
        get { return currentWeapon; }
    }

    protected override void Awake()
    {
        base.Awake();

        playerControls = new PlayerControls();

        // The legacy slot strip is kept in the prefab for its artwork, but it no
        // longer drives anything, so it is faded out rather than deleted.
        HideLegacySlots();
    }

    private void Start()
    {
        playerControls.Inventory.Keyboard.performed += OnNumberKey;
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Inventory.Keyboard.performed -= OnNumberKey;
            playerControls.Dispose();
        }
    }

    // ----- ownership ------------------------------------------------------

    public bool IsOwned(WeaponType weapon)
    {
        if (weapon == WeaponType.Sword) { return true; }

        GameSaveManager save = GameSaveManager.Instance;
        return save != null && save.Data.OwnsWeapon(weapon);
    }

    public WeaponInfo GetWeaponInfo(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Bow: return bowInfo;
            case WeaponType.Staff: return staffInfo;
            default: return swordInfo;
        }
    }

    // ----- equipping ------------------------------------------------------

    public void EquipStartingWeapon()
    {
        WeaponType wanted = currentWeapon;

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null) { wanted = (WeaponType)save.Data.currentWeapon; }

        if (!IsOwned(wanted)) { wanted = WeaponType.Sword; }

        EquipWeapon(wanted, false);
    }

    public void ApplyLoadedWeapon(WeaponType weapon)
    {
        EquipWeapon(IsOwned(weapon) ? weapon : WeaponType.Sword, false);
    }

    // Steps to the next weapon the player actually owns. Locked weapons are
    // skipped rather than equipping an empty hand.
    public void CycleWeapon()
    {
        for (int step = 1; step <= WeaponTypeInfo.Count; step++)
        {
            WeaponType candidate = (WeaponType)(((int)currentWeapon + step) % WeaponTypeInfo.Count);

            if (IsOwned(candidate))
            {
                EquipWeapon(candidate, true);
                return;
            }
        }
    }

    public void EquipWeapon(WeaponType weapon, bool playSound)
    {
        if (!IsOwned(weapon))
        {
            GameMessages.Toast(WeaponTypeInfo.DisplayName(weapon) + " CHƯA ĐƯỢC MỞ KHÓA");
            return;
        }

        WeaponInfo info = GetWeaponInfo(weapon);
        if (info == null || info.weaponPrefab == null)
        {
            Debug.LogWarning("[ActiveInventory] No weapon prefab assigned for " + weapon);
            return;
        }

        currentWeapon = weapon;

        ActiveWeapon activeWeapon = ActiveWeapon.Instance;
        if (activeWeapon == null) { return; }

        if (activeWeapon.CurrentActiveWeapon != null)
        {
            Destroy(activeWeapon.CurrentActiveWeapon.gameObject);
        }

        GameObject spawned = Instantiate(info.weaponPrefab, activeWeapon.transform);
        activeWeapon.NewWeapon(spawned.GetComponent<MonoBehaviour>());

        GameSaveManager save = GameSaveManager.Instance;
        if (save != null) { save.Data.currentWeapon = (int)weapon; }

        if (playSound) { AudioManager.PlaySfx(GameSfx.UiClick); }

        OnWeaponChanged?.Invoke(weapon);
    }

    // Called from the on-screen weapon button.
    public void TouchCycleWeapon() => CycleWeapon();

    // Kept so the InventorySlot components in the old prefab still compile.
    public void SelectSlot(int indexNum)
    {
        if (indexNum < 0 || indexNum >= WeaponTypeInfo.Count) { return; }

        EquipWeapon((WeaponType)indexNum, true);
    }

    private void OnNumberKey(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        int pressed = Mathf.RoundToInt(context.ReadValue<float>()) - 1;
        SelectSlot(pressed);
    }

    private void HideLegacySlots()
    {
        // Disabling the object outright would stop this component's own Start
        // from running, so the strip is made invisible and non-interactive
        // instead.
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) { group = gameObject.AddComponent<CanvasGroup>(); }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
