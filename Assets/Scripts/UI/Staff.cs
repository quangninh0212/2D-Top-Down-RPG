using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Staff : MonoBehaviour, IWeapon
{
    private void Update()
    {
        MouseFollowWithOffset();
    }


    public void Attack()
    {
        Debug.Log("Staff Attack");
        ActiveWeapon.Instance.ToggleIsAttacking(false);
    }

    private void MouseFollowWithOffset()
    {
        bool pointingLeft;
        float angle;

        if (MobileInput.TryGetAimDirection(out Vector2 aimDirection))
        {
            pointingLeft = aimDirection.x < 0;
            angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        }
        else
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 playerScreenPoint = Camera.main.WorldToScreenPoint(PlayerController.Instance.transform.position);
            pointingLeft = mousePos.x < playerScreenPoint.x;
            angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
        }

        if (pointingLeft)
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, -180, angle);
        }
        else
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
