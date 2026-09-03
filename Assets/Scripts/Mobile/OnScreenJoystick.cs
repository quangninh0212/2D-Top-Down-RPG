using UnityEngine;
using UnityEngine.EventSystems;

// Floating virtual joystick. The whole touch zone accepts input: wherever the thumb
// lands, the stick re-centres itself there, so the player always has room to drag in
// every direction - even if part of the zone sits under a system bar or off-screen.
// Which stick it drives is picked with joystickRole.
public class OnScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum Role { Move, Aim }

    [SerializeField] private Role joystickRole = Role.Move;
    [SerializeField] private RectTransform touchZone;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 90f;
    [SerializeField] private float deadZone = 0.15f;

    private Vector2 restingPosition;
    private Vector2 originLocalPoint;

    // For controls built at runtime (see MobileControlsBootstrap) where the fields
    // can't be wired up in the inspector.
    public void Init(Role role, RectTransform zoneRect, RectTransform backgroundRect, RectTransform handleRect, float range)
    {
        joystickRole = role;
        touchZone = zoneRect;
        background = backgroundRect;
        handle = handleRect;
        handleRange = range;
        restingPosition = backgroundRect.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchZone, eventData.position, eventData.pressEventCamera, out originLocalPoint))
        {
            return;
        }

        // Re-centre the stick under the thumb.
        background.anchoredPosition = originLocalPoint;
        handle.anchoredPosition = Vector2.zero;
        Apply(Vector2.zero);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchZone, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 inputVector = Vector2.ClampMagnitude((localPoint - originLocalPoint) / handleRange, 1f);
        handle.anchoredPosition = inputVector * handleRange;

        Apply(inputVector.magnitude < deadZone ? Vector2.zero : inputVector);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        background.anchoredPosition = restingPosition;
        handle.anchoredPosition = Vector2.zero;
        Apply(Vector2.zero);
    }

    // Clears the stick without needing a touch to end - used when the scene
    // changes or the app loses focus, where OnPointerUp never arrives.
    public void ResetStick()
    {
        if (background != null) { background.anchoredPosition = restingPosition; }
        if (handle != null) { handle.anchoredPosition = Vector2.zero; }

        Apply(Vector2.zero);
    }

    private void Apply(Vector2 value)
    {
        if (joystickRole == Role.Move)
        {
            MobileInput.MoveInput = value;
            return;
        }

        // Note: no null-conditional operator here. It bypasses Unity's overloaded
        // equality, so a destroyed ActiveWeapon (the player's death destroys it)
        // would not be treated as null.
        bool weaponAlive = ActiveWeapon.Instance != null;

        if (value == Vector2.zero)
        {
            MobileInput.ClearAim();
            if (weaponAlive) { ActiveWeapon.Instance.StopAttackingTouch(); }
        }
        else
        {
            MobileInput.SetAim(value);
            if (weaponAlive) { ActiveWeapon.Instance.StartAttackingTouch(); }
        }
    }
}
