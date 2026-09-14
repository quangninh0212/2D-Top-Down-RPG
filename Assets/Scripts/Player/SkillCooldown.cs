using System;
using UnityEngine;

// Timing for one skill: how long it lasts once used, and how long before it can
// be used again. Plain C# with the clock passed in, so the rules can be tested
// without a running scene.
[Serializable]
public class SkillCooldown
{
    [SerializeField] private float duration;
    [SerializeField] private float cooldown;

    private float activeUntil;
    private float readyAt;

    public SkillCooldown(float duration, float cooldown)
    {
        this.duration = Mathf.Max(0f, duration);
        this.cooldown = Mathf.Max(0f, cooldown);
    }

    public float Duration
    {
        get { return duration; }
    }

    public float Cooldown
    {
        get { return cooldown; }
    }

    public bool IsReady(float now)
    {
        return now >= readyAt;
    }

    public bool IsActive(float now)
    {
        return now < activeUntil;
    }

    // Starts the skill if it is off cooldown. The cooldown runs from the moment
    // of use, not from when the effect ends.
    public bool TryActivate(float now)
    {
        if (!IsReady(now)) { return false; }

        activeUntil = now + duration;
        readyAt = now + cooldown;
        return true;
    }

    // A shield broken by a trap stops protecting immediately, but its cooldown
    // is not refunded.
    public void EndEarly(float now)
    {
        if (activeUntil > now) { activeUntil = now; }
    }

    public float RemainingActive(float now)
    {
        return Mathf.Max(0f, activeUntil - now);
    }

    public float RemainingCooldown(float now)
    {
        return Mathf.Max(0f, readyAt - now);
    }

    // 1 right after use, 0 once ready - drives the dial over the button.
    public float CooldownFraction(float now)
    {
        if (cooldown <= 0f) { return 0f; }

        return Mathf.Clamp01(RemainingCooldown(now) / cooldown);
    }

    public void Reset()
    {
        activeUntil = 0f;
        readyAt = 0f;
    }
}
