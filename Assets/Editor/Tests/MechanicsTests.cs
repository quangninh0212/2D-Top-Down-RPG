using NUnit.Framework;
using UnityEngine;

// The rules behind the new mechanics that can be checked without a scene:
// skill timing, speed changes, the warning count, and the audio switches.
public class MechanicsTests
{
    // ----- skills ---------------------------------------------------------

    [Test]
    public void Skill_StartsReady_AndGoesOnCooldownWhenUsed()
    {
        SkillCooldown shield = new SkillCooldown(3f, 9f);

        Assert.IsTrue(shield.IsReady(0f));
        Assert.IsTrue(shield.TryActivate(0f));
        Assert.IsTrue(shield.IsActive(1f));
        Assert.IsFalse(shield.IsReady(1f));
        Assert.IsFalse(shield.TryActivate(1f), "A skill must not be usable again while recharging.");
    }

    [Test]
    public void Skill_EffectEndsBeforeCooldown()
    {
        SkillCooldown shield = new SkillCooldown(3f, 9f);
        shield.TryActivate(10f);

        Assert.IsTrue(shield.IsActive(12.9f));
        Assert.IsFalse(shield.IsActive(13f));
        Assert.IsFalse(shield.IsReady(18.9f));
        Assert.IsTrue(shield.IsReady(19f));
    }

    [Test]
    public void Skill_BrokenShieldStopsProtectingButKeepsItsCooldown()
    {
        SkillCooldown shield = new SkillCooldown(3f, 9f);
        shield.TryActivate(0f);
        shield.EndEarly(1f);

        Assert.IsFalse(shield.IsActive(1f));
        Assert.IsFalse(shield.IsReady(1f), "Breaking the shield must not refund the cooldown.");
    }

    [Test]
    public void Skill_CooldownDialRunsFromFullToEmpty()
    {
        SkillCooldown stun = new SkillCooldown(2.5f, 12f);
        stun.TryActivate(0f);

        Assert.AreEqual(1f, stun.CooldownFraction(0f), 0.001f);
        Assert.AreEqual(0.5f, stun.CooldownFraction(6f), 0.001f);
        Assert.AreEqual(0f, stun.CooldownFraction(12f), 0.001f);
    }

    // ----- movement speed -------------------------------------------------

    [Test]
    public void Speed_BoostAndSlowMultiplyAndExpire()
    {
        SpeedModifiers modifiers = new SpeedModifiers();

        Assert.AreEqual(1f, modifiers.Multiplier(0f), 0.001f);

        modifiers.ApplyBoost(1.6f, 5f, 0f);
        Assert.AreEqual(1.6f, modifiers.Multiplier(1f), 0.001f);

        modifiers.ApplySlow(0.5f, 2f, 1f);
        Assert.AreEqual(0.8f, modifiers.Multiplier(2f), 0.001f, "Boost and slow should combine.");

        Assert.AreEqual(1.6f, modifiers.Multiplier(3.5f), 0.001f, "The slow should have worn off.");
        Assert.AreEqual(1f, modifiers.Multiplier(5.5f), 0.001f, "The boost should have worn off.");
    }

    [Test]
    public void Speed_RepeatedBoostsRefreshRatherThanStack()
    {
        SpeedModifiers modifiers = new SpeedModifiers();

        modifiers.ApplyBoost(1.6f, 5f, 0f);
        modifiers.ApplyBoost(1.6f, 5f, 1f);

        Assert.AreEqual(1.6f, modifiers.Multiplier(2f), 0.001f);
        Assert.IsTrue(modifiers.IsBoosted(5.5f), "A second rune should extend the timer.");
    }

    // ----- forbidden zone -------------------------------------------------

    [Test]
    public void ForbiddenZone_WarningCountIsAlwaysBetweenThreeAndSix()
    {
        Assert.AreEqual(3, ForbiddenZone.ClampWarnings(0));
        Assert.AreEqual(3, ForbiddenZone.ClampWarnings(3));
        Assert.AreEqual(5, ForbiddenZone.ClampWarnings(5));
        Assert.AreEqual(6, ForbiddenZone.ClampWarnings(6));
        Assert.AreEqual(6, ForbiddenZone.ClampWarnings(40));
    }

    // ----- audio switches -------------------------------------------------

    [Test]
    public void AudioSwitches_ArePersistedAndRaiseChanged()
    {
        bool originalSfx = AudioToggles.SfxEnabled;
        bool originalMusic = AudioToggles.MusicEnabled;

        int changes = 0;
        System.Action counter = () => changes++;
        AudioToggles.Changed += counter;

        try
        {
            AudioToggles.SfxEnabled = true;
            AudioToggles.MusicEnabled = true;
            changes = 0;

            AudioToggles.SfxEnabled = false;
            AudioToggles.MusicEnabled = false;

            Assert.IsFalse(AudioToggles.SfxEnabled);
            Assert.IsFalse(AudioToggles.MusicEnabled);
            Assert.AreEqual(2, changes);

            AudioToggles.SfxEnabled = false;
            Assert.AreEqual(2, changes, "Setting the same value again should not announce a change.");
        }
        finally
        {
            AudioToggles.Changed -= counter;
            AudioToggles.SfxEnabled = originalSfx;
            AudioToggles.MusicEnabled = originalMusic;
        }
    }
}
