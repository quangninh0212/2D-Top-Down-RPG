using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private WeaponInfo weaponInfo;

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }

    // Lets touch devices switch weapons by tapping a slot instead of pressing 1-5.
    public void OnPointerClick(PointerEventData eventData)
    {
        ActiveInventory activeInventory = GetComponentInParent<ActiveInventory>();
        if (activeInventory == null)
        {
            return;
        }

        Transform slotContainer = transform;
        while (slotContainer.parent != null && slotContainer.parent != activeInventory.transform)
        {
            slotContainer = slotContainer.parent;
        }

        activeInventory.SelectSlot(slotContainer.GetSiblingIndex());
    }
}
