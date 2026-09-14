using UnityEngine;

// Temporary changes to how fast the player walks: a boost from the speed rune,
// a slow from the spike trap. Both can be in effect at once and multiply
// together. Plain C# with the clock passed in so the arithmetic can be tested.
public class SpeedModifiers
{
    private float boostUntil;
    private float boostMultiplier = 1f;

    private float slowUntil;
    private float slowMultiplier = 1f;

    // Picking up a second boost refreshes the timer rather than stacking, so a
    // cluster of runes cannot send the player through walls.
    public void ApplyBoost(float multiplier, float duration, float now)
    {
        boostMultiplier = Mathf.Max(1f, multiplier);
        boostUntil = Mathf.Max(boostUntil, now + Mathf.Max(0f, duration));
    }

    public void ApplySlow(float multiplier, float duration, float now)
    {
        slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        slowUntil = Mathf.Max(slowUntil, now + Mathf.Max(0f, duration));
    }

    public bool IsBoosted(float now)
    {
        return now < boostUntil;
    }

    public bool IsSlowed(float now)
    {
        return now < slowUntil;
    }

    public float BoostRemaining(float now)
    {
        return Mathf.Max(0f, boostUntil - now);
    }

    public float SlowRemaining(float now)
    {
        return Mathf.Max(0f, slowUntil - now);
    }

    public float Multiplier(float now)
    {
        float value = 1f;

        if (IsBoosted(now)) { value *= boostMultiplier; }
        if (IsSlowed(now)) { value *= slowMultiplier; }

        return value;
    }

    public void Clear()
    {
        boostUntil = 0f;
        slowUntil = 0f;
    }
}
