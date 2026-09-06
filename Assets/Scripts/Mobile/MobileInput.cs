using UnityEngine;

// Shared touch input state. The on-screen controls write into this; the player,
// aim controller and weapons read from it, so the desktop keyboard and mouse
// path stays untouched.
public static class MobileInput
{
    public static Vector2 MoveInput { get; set; }

    private static Vector2 aimDirection;
    private static bool aimActive;

    // Set once at startup. On a phone there is no mouse to aim with, so the aim
    // controller switches to picking targets automatically.
    public static bool UseAutoTargeting { get; set; }

    // True while the on-screen attack button is held.
    public static bool AttackHeld { get; private set; }

    public static void SetAim(Vector2 direction)
    {
        aimDirection = direction;
        aimActive = true;
    }

    public static void ClearAim()
    {
        aimActive = false;
    }

    public static void SetAttackHeld(bool held)
    {
        AttackHeld = held;
    }

    public static bool TryGetAimDirection(out Vector2 direction)
    {
        direction = aimDirection;
        return aimActive;
    }

    // Static state outlives scene loads, so it has to be cleared explicitly.
    // Without this, dying (or any scene change) while holding a stick leaves the
    // player running and attacking with no touch left to cancel it.
    public static void ResetAll()
    {
        MoveInput = Vector2.zero;
        aimDirection = Vector2.zero;
        aimActive = false;
        AttackHeld = false;
    }
}
