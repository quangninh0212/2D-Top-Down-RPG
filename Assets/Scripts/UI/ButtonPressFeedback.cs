using UnityEngine;
using UnityEngine.EventSystems;

// A touchscreen has no hover state, so a button needs to visibly react the
// instant a finger lands on it. Scales down slightly on press and springs back.
public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float speed = 14f;

    private Vector3 restingScale;
    private float target = 1f;

    private void Awake()
    {
        restingScale = transform.localScale;
    }

    private void OnDisable()
    {
        target = 1f;
        transform.localScale = restingScale;
    }

    private void Update()
    {
        // Unscaled, so the pause menu's buttons still animate while frozen.
        float current = transform.localScale.x / Mathf.Max(0.0001f, restingScale.x);
        float next = Mathf.Lerp(current, target, Time.unscaledDeltaTime * speed);

        transform.localScale = restingScale * next;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        target = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        target = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target = 1f;
    }
}
