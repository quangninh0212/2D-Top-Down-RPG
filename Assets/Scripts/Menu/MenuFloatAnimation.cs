using UnityEngine;
using UnityEngine.UI;

// Gives the menu characters a little life: a slow vertical bob, a barely
// perceptible breath and a gentle tilt. All driven by sine waves, never random
// jitter, so it reads as calm rather than shaky.
public class MenuFloatAnimation : MonoBehaviour
{
    [SerializeField] private float bobHeight = 12f;
    [SerializeField] private float bobSpeed = 1.1f;
    [SerializeField] private float scaleAmount = 0.015f;
    [SerializeField] private float scaleSpeed = 0.8f;
    [SerializeField] private float tiltDegrees = 1.4f;
    [SerializeField] private float tiltSpeed = 0.55f;
    [SerializeField] private float phase;

    private RectTransform rect;
    private Vector2 restingPosition;
    private Vector3 restingScale;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        restingPosition = rect.anchoredPosition;
        restingScale = rect.localScale;
    }

    // Offsetting the phase per character stops them moving in lockstep.
    public void SetPhase(float value)
    {
        phase = value;
    }

    private void Update()
    {
        if (rect == null) { return; }

        float t = Time.unscaledTime;

        rect.anchoredPosition = restingPosition + new Vector2(0f, Mathf.Sin(t * bobSpeed + phase) * bobHeight);

        float scale = 1f + Mathf.Sin(t * scaleSpeed + phase * 1.7f) * scaleAmount;
        rect.localScale = new Vector3(restingScale.x * scale, restingScale.y * scale, restingScale.z);

        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * tiltSpeed + phase * 0.6f) * tiltDegrees);
    }
}
