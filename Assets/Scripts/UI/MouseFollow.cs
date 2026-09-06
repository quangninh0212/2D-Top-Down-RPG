using UnityEngine;

// Aims the weapon pivot. Named for what it used to do; it now reads the shared
// aim controller, so a mouse, a thumb and auto-targeting all drive it the same
// way.
//
// This is the only script that rotates the pivot. The weapons themselves used
// to do it too, which meant two scripts fighting over one transform every frame.
public class MouseFollow : MonoBehaviour
{
    private void LateUpdate()
    {
        Vector2 aim = ResolveAim();
        if (aim.sqrMagnitude < 0.0001f) { return; }

        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        // Flipping 180 degrees about Y mirrors the weapon sprite for left-facing
        // attacks, and mirrors its local X axis with it - hence 180 - angle.
        transform.rotation = aim.x < 0f
            ? Quaternion.Euler(0f, -180f, 180f - angle)
            : Quaternion.Euler(0f, 0f, angle);
    }

    private static Vector2 ResolveAim()
    {
        PlayerAimController aim = PlayerAimController.Instance;
        if (aim == null) { return Vector2.right; }

        ActiveWeapon activeWeapon = ActiveWeapon.Instance;
        bool melee = activeWeapon != null && activeWeapon.CurrentActiveWeapon is Sword;

        return melee ? aim.GetMeleeDirection() : aim.AimDirection;
    }
}
