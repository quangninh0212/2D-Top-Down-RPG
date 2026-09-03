using UnityEngine;
using UnityEngine.EventSystems;

// Tap the screen to attack towards that spot. The aim direction runs from the
// player to wherever the finger is, and holding keeps attacking while sliding
// re-aims. This replaces a second stick, which asked the player to think in
// directions when what they actually want is to hit a particular place.
public class TouchAimZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RectTransform zoneRect;
    private RectTransform marker;

    public void Init(RectTransform zone, RectTransform markerRect)
    {
        zoneRect = zone;
        marker = markerRect;

        if (marker != null) { marker.gameObject.SetActive(false); }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AimAt(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        AimAt(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    // Also called when a scene load or focus loss swallows the release.
    public void Release()
    {
        if (marker != null) { marker.gameObject.SetActive(false); }

        MobileInput.ClearAim();

        // Not ?.  - that would miss a destroyed weapon, which death leaves behind.
        if (ActiveWeapon.Instance != null) { ActiveWeapon.Instance.StopAttackingTouch(); }
    }

    private void AimAt(PointerEventData eventData)
    {
        if (PlayerController.Instance == null || Camera.main == null) { return; }

        Vector2 playerOnScreen = Camera.main.WorldToScreenPoint(PlayerController.Instance.transform.position);
        Vector2 direction = eventData.position - playerOnScreen;

        // Finger resting on the player itself gives no usable direction.
        if (direction.sqrMagnitude < 1f) { return; }

        MobileInput.SetAim(direction.normalized);

        if (ActiveWeapon.Instance != null) { ActiveWeapon.Instance.StartAttackingTouch(); }

        ShowMarker(eventData);
    }

    private void ShowMarker(PointerEventData eventData)
    {
        if (marker == null) { return; }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                zoneRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            marker.anchoredPosition = localPoint;
            marker.gameObject.SetActive(true);
        }
    }
}
