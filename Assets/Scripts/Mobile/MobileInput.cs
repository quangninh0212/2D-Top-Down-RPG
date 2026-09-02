using UnityEngine;

// Shared touch input state. OnScreenJoystick writes into this; PlayerController,
// MouseFollow, Sword and Staff read from it instead of Input.mousePosition when
// a touch aim stick is active, so desktop mouse/keyboard controls keep working unchanged.
public static class MobileInput
{
    public static Vector2 MoveInput { get; set; }

    private static Vector2 aimDirection;
    private static bool aimActive;

    public static void SetAim(Vector2 direction)
    {
        aimDirection = direction;
        aimActive = true;
    }

    public static void ClearAim()
    {
        aimActive = false;
    }

    public static bool TryGetAimDirection(out Vector2 direction)
    {
        direction = aimDirection;
        return aimActive;
    }
}
