using Cinemachine;
using UnityEngine;

// The camera no longer chases the player. Each level places a
// LevelCameraAnchor; the virtual camera parks there and frames the whole arena,
// which is what makes the fixed-screen layout readable on a phone.
public class CameraController : Singleton<CameraController>
{
    private CinemachineVirtualCamera virtualCamera;
    private LevelCameraAnchor anchor;
    private float lastAspect;

    private void Start()
    {
        ApplyLevelCamera();
    }

    // Rotating the device changes the aspect, and with it how much of the arena
    // fits, so the framing is re-checked rather than set once.
    private void Update()
    {
        if (anchor == null || virtualCamera == null) { return; }

        float aspect = CurrentAspect();
        if (Mathf.Abs(aspect - lastAspect) < 0.001f) { return; }

        ApplyFraming(aspect);
    }

    // Called on scene load and whenever an area entrance places the player.
    public void ApplyLevelCamera()
    {
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (virtualCamera == null) { return; }

        anchor = FindObjectOfType<LevelCameraAnchor>();

        // Detaching Follow is what stops Cinemachine from tracking the player.
        virtualCamera.Follow = null;
        virtualCamera.LookAt = null;

        // The confiner exists to clamp a following camera; with a fixed camera
        // it only fights the anchor, so it is switched off.
        CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner != null) { confiner.enabled = false; }

        // Snap rather than glide, so entering a level never shows the camera
        // sliding in from the previous level's framing.
        CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain != null) { brain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut; }

        ApplyFraming(CurrentAspect());
    }

    private void ApplyFraming(float aspect)
    {
        lastAspect = aspect;

        if (anchor == null) { return; }

        virtualCamera.transform.position = anchor.CameraPosition;
        virtualCamera.m_Lens.OrthographicSize = anchor.OrthographicSizeFor(aspect);

        // The brain copies the virtual camera across on its own update, which
        // is a frame late on a scene load; setting it directly avoids a flash
        // of the previous level's framing.
        Camera main = Camera.main;
        if (main != null && main.orthographic)
        {
            main.transform.position = anchor.CameraPosition;
            main.orthographicSize = virtualCamera.m_Lens.OrthographicSize;
        }
    }

    private static float CurrentAspect()
    {
        if (Screen.height <= 0) { return 16f / 9f; }

        return Screen.width / (float)Screen.height;
    }

    // Kept for compatibility with older calls; the camera is fixed now, so this
    // just re-applies the level framing.
    public void SetPlayerCameraFollow()
    {
        ApplyLevelCamera();
    }
}
