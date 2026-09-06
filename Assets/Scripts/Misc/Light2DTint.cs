using UnityEngine;
using UnityEngine.Rendering.Universal;

// Small helper for recolouring the URP 2D lights that sit under portals and
// torches. Kept in one place so callers do not each have to know about the
// render pipeline types.
public static class Light2DTint
{
    public static void Apply(GameObject root, Color colour, float intensityScale)
    {
        if (root == null) { return; }

        Light2D[] lights = root.GetComponentsInChildren<Light2D>(true);

        for (int i = 0; i < lights.Length; i++)
        {
            Light2D light = lights[i];
            if (light == null) { continue; }

            light.color = colour;
            light.intensity = Mathf.Max(0.05f, intensityScale);
        }
    }
}
