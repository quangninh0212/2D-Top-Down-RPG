using UnityEngine;
using UnityEngine.EventSystems;

// The attack button repeats while held, on the equipped weapon's own cooldown.
// A plain Button only fires on release, which would make every weapon feel slow.
public class HoldToAttack : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public void Bind()
    {
        // Nothing to wire up: the pointer callbacks drive MobileInput directly.
        // Kept as an explicit call so the construction code reads in order.
    }

    private void OnDisable()
    {
        MobileInput.SetAttackHeld(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (PauseMenuUI.IsPaused) { return; }
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.isDead) { return; }

        MobileInput.SetAttackHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        MobileInput.SetAttackHeld(false);
    }

    // A finger that slides off the button should stop the attack, otherwise it
    // sticks on until the next press.
    public void OnPointerExit(PointerEventData eventData)
    {
        MobileInput.SetAttackHeld(false);
    }
}
