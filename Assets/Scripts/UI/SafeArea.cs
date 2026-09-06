using UnityEngine;

// Keeps its RectTransform inside the device's safe area, so nothing important
// ends up under a notch, a punch-hole or the gesture bar. Re-applies whenever
// the reported area changes, which is what happens on rotation.
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    [Tooltip("Extra padding in canvas-reference pixels, applied on top of the safe area.")]
    [SerializeField] private float extraPadding = 12f;

    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            Apply();
        }
    }

    public void Apply()
    {
        if (rect == null) { rect = GetComponent<RectTransform>(); }
        if (Screen.width <= 0 || Screen.height <= 0) { return; }

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        rect.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        rect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

        rect.offsetMin = new Vector2(extraPadding, extraPadding);
        rect.offsetMax = new Vector2(-extraPadding, -extraPadding);
    }
}
