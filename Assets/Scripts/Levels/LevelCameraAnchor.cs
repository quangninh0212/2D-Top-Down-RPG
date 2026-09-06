using UnityEngine;

// Marks where the fixed camera sits for this level and how much of the world it
// has to show. Phones are much wider than 16:9, so the camera size is worked
// out from the device's aspect rather than baked in - otherwise the arena would
// be cropped on some screens and swim in empty space on others.
public class LevelCameraAnchor : MonoBehaviour
{
    [Tooltip("Width of the playable arena in world units. The camera always shows at least this much.")]
    [SerializeField] private float arenaWidth = 32f;

    [Tooltip("Height of the playable arena in world units.")]
    [SerializeField] private float arenaHeight = 18f;

    public float ArenaWidth
    {
        get { return arenaWidth; }
    }

    public float ArenaHeight
    {
        get { return arenaHeight; }
    }

    public Vector3 CameraPosition
    {
        get { return new Vector3(transform.position.x, transform.position.y, -10f); }
    }

    // Orthographic size is a half-height, so covering the width means dividing
    // by the aspect. Taking the larger of the two guarantees the whole arena is
    // on screen whatever shape the display is.
    public float OrthographicSizeFor(float aspect)
    {
        if (aspect <= 0.01f) { aspect = 16f / 9f; }

        float forHeight = arenaHeight * 0.5f;
        float forWidth = arenaWidth * 0.5f / aspect;

        return Mathf.Max(forHeight, forWidth);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.35f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(arenaWidth, arenaHeight, 0f));
    }
}
