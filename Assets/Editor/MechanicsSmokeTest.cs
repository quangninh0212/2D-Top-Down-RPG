using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Plays Scene1 and exercises every mechanic added for the extra requirements:
// the sound and music switches, the shield, the stun, the forbidden zone alarm,
// the three collision objects and the HUD lines. Each check drives the real
// component in a running scene rather than re-implementing its rules.
public static class MechanicsSmokeTest
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly List<string> Failures = new List<string>();

    private static int step;
    private static float waitUntil;
    private static int frames;

    private static bool originalSfx;
    private static bool originalMusic;

    private static EnemyHealth stunnedEnemy;
    private static Vector2 stunnedEnemyPosition;
    private static ForbiddenZone zone;
    private static int pickupsBeforeChest;

    [MenuItem("Tools/Soulbound Gate/Debug/Mechanics Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + GameScenes.Scene1 + ".unity", OpenSceneMode.Single);
        PlayModeTestSettings.ApplyForTest();

        originalSfx = AudioToggles.SfxEnabled;
        originalMusic = AudioToggles.MusicEnabled;
        AudioToggles.SfxEnabled = true;
        AudioToggles.MusicEnabled = true;

        Report.Clear();
        Failures.Clear();
        step = 0;
        frames = 0;
        waitUntil = 0f;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { return; }

        frames++;
        if (Time.time < waitUntil) { return; }

        switch (step)
        {
            case 0:
                // Let the level load, place the player and arm the zone.
                if (frames < 60 || Time.time < 1.5f) { return; }
                step++;
                break;

            case 1: CheckAudioSwitches(); step++; break;
            // Destroy() only takes effect at the end of the frame, so the check
            // that the rune and chest are gone waits a moment first.
            case 2: CheckShieldAndCollisionObjects(); step++; Wait(0.3f); break;
            case 3: CheckConsumedObjectsAreGone(); step++; break;
            case 4: StartStun(); step++; Wait(0.8f); break;
            case 5: CheckStunHeld(); step++; break;
            case 6: StartIntrusion(); step++; Wait(3f); break;
            case 7: CheckAlarm(); step++; break;
            case 8: CheckHud(); step++; break;

            default:
                Finish();
                break;
        }
    }

    private static void Wait(float seconds)
    {
        waitUntil = Time.time + seconds;
    }

    // ----- 2. sound and music switches -----------------------------------

    private static void CheckAudioSwitches()
    {
        Report.AppendLine("Audio switches:");

        GameObject soundOff = FindAny("SoundOff");
        GameObject soundOn = FindAny("SoundOn");
        GameObject musicOn = FindAny("MusicOn");
        GameObject musicOff = FindAny("MusicOff");

        if (soundOff == null || soundOn == null || musicOn == null || musicOff == null)
        {
            Failures.Add("one of SoundOff / SoundOn / MusicOn / MusicOff is missing");
            return;
        }

        CheckPair("SoundOff/SoundOn", soundOff, soundOn);
        CheckPair("MusicOn/MusicOff", musicOn, musicOff);

        // Effects on: SoundOff is the one showing. Tapping it silences effects.
        Expect(soundOff.activeSelf && !soundOn.activeSelf, "with effects on, SoundOff is shown and SoundOn hidden");

        int before = AudioManager.SfxPlayedCount;
        soundOff.GetComponent<Button>().onClick.Invoke();

        Expect(!AudioToggles.SfxEnabled, "tapping SoundOff turns effects off");
        Expect(soundOn.activeSelf && !soundOff.activeSelf, "SoundOn replaces SoundOff");

        AudioManager.PlaySfx(GameSfx.Purchase);
        Expect(AudioManager.SfxPlayedCount == before, "no effect is played while effects are off");

        soundOn.GetComponent<Button>().onClick.Invoke();
        Expect(AudioToggles.SfxEnabled, "tapping SoundOn turns effects back on");
        Expect(soundOff.activeSelf && !soundOn.activeSelf, "SoundOff replaces SoundOn");

        int beforeOn = AudioManager.SfxPlayedCount;
        AudioManager.PlaySfx(GameSfx.Denied);
        Expect(AudioManager.SfxPlayedCount > beforeOn, "effects are heard again once switched back on");

        // Music on: MusicOff is the one showing.
        Expect(musicOff.activeSelf && !musicOn.activeSelf, "with music on, MusicOff is shown and MusicOn hidden");

        musicOff.GetComponent<Button>().onClick.Invoke();
        Expect(!AudioToggles.MusicEnabled, "tapping MusicOff turns music off");
        Expect(musicOn.activeSelf && !musicOff.activeSelf, "MusicOn replaces MusicOff");

        musicOn.GetComponent<Button>().onClick.Invoke();
        Expect(AudioToggles.MusicEnabled, "tapping MusicOn turns music back on");
        Expect(musicOff.activeSelf && !musicOn.activeSelf, "MusicOff replaces MusicOn");
    }

    private static void CheckPair(string label, GameObject a, GameObject b)
    {
        RectTransform ra = (RectTransform)a.transform;
        RectTransform rb = (RectTransform)b.transform;

        bool sameSize = ra.sizeDelta == rb.sizeDelta;
        bool samePlace = ra.anchoredPosition == rb.anchoredPosition && ra.anchorMin == rb.anchorMin;

        Report.AppendLine("  " + label + ": size " + ra.sizeDelta + " / " + rb.sizeDelta +
                          ", position " + ra.anchoredPosition + " / " + rb.anchoredPosition);

        Expect(sameSize, label + " have the same size");
        Expect(samePlace, label + " occupy the same position");
        Expect(a.activeSelf != b.activeSelf, label + " show exactly one at a time");
    }

    // ----- 3. defence and collision objects ------------------------------

    private static void CheckShieldAndCollisionObjects()
    {
        Report.AppendLine("Shield, spike trap, speed rune, treasure chest:");

        PlayerController player = PlayerController.Instance;
        PlayerHealth health = PlayerHealth.Instance;
        PlayerSkills skills = PlayerSkills.Instance;

        if (player == null || health == null || skills == null)
        {
            Failures.Add("player, health or skills missing");
            return;
        }

        int startHealth = health.CurrentHealth;

        // Shield absorbs damage.
        skills.Shield.Reset();
        Expect(skills.TryShield(), "shield activates");
        health.TakeDamage(1, null);
        Expect(health.CurrentHealth == startHealth, "a hit while shielded costs no health (" + health.CurrentHealth + ")");

        // Y: the trap breaks the shield rather than hurting, and slows.
        SpikeTrap trap = Object.FindObjectOfType<SpikeTrap>();
        if (trap == null) { Failures.Add("no spike trap in the level"); }
        else
        {
            SpikeTrap.Outcome outcome = trap.Spring(player);
            Report.AppendLine("  trap on a shielded player -> " + outcome);

            Expect(outcome == SpikeTrap.Outcome.ShieldBroken, "the trap destroys the shield");
            Expect(!skills.ShieldActive, "the shield is gone");
            Expect(health.CurrentHealth == startHealth, "the broken shield took the damage");
            Expect(player.Modifiers.IsSlowed(Time.time), "the trap slows the player");
        }

        // X: the rune speeds the player up and disappears.
        SpeedRune rune = Object.FindObjectOfType<SpeedRune>();
        if (rune == null) { Failures.Add("no speed rune in the level"); }
        else
        {
            Expect(rune.Apply(player), "the rune can be taken");
            Expect(player.Modifiers.IsBoosted(Time.time), "the rune speeds the player up");

            float expected = rune.SpeedMultiplier * (trap != null ? trap.SlowMultiplier : 1f);
            Report.AppendLine("  speed multiplier now x" + player.CurrentSpeedMultiplier.ToString("0.00") +
                              " (expected x" + expected.ToString("0.00") + ")");
            Expect(Mathf.Abs(player.CurrentSpeedMultiplier - expected) < 0.01f, "boost and slow combine");
        }

        // Z: the chest pays gold and drops an item.
        TreasureChest chest = Object.FindObjectOfType<TreasureChest>();
        EconomyManager economy = EconomyManager.Instance;

        if (chest == null) { Failures.Add("no treasure chest in the level"); }
        else if (economy == null) { Failures.Add("no economy manager"); }
        else
        {
            int goldBefore = economy.CurrentGold;
            pickupsBeforeChest = Object.FindObjectsOfType<Pickup>().Length;

            Expect(chest.Open(), "the chest opens");
            Expect(economy.CurrentGold == goldBefore + chest.GoldReward,
                   "the chest pays " + chest.GoldReward + " gold (" + goldBefore + " -> " + economy.CurrentGold + ")");
        }

        // Without a shield the trap does hurt.
        if (trap != null)
        {
            System.Reflection.FieldInfo ready = typeof(SpikeTrap).GetField("readyAt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ready != null) { ready.SetValue(trap, 0f); }

            int beforeTrap = health.CurrentHealth;
            SpikeTrap.Outcome second = trap.Spring(player);

            Report.AppendLine("  trap on an unshielded player -> " + second + ", health " + beforeTrap + " -> " + health.CurrentHealth);
            Expect(second == SpikeTrap.Outcome.Damaged && health.CurrentHealth == beforeTrap - 1, "the trap costs health");
        }
    }

    private static void CheckConsumedObjectsAreGone()
    {
        Expect(Object.FindObjectOfType<SpeedRune>() == null, "the taken rune has disappeared");
        Expect(Object.FindObjectOfType<TreasureChest>() == null, "the opened chest has disappeared");

        int pickups = Object.FindObjectsOfType<Pickup>().Length;
        Report.AppendLine("  pickups before chest " + pickupsBeforeChest + ", after " + pickups);
        Expect(pickups > pickupsBeforeChest, "the chest dropped a new item");
    }

    // ----- 3. stun --------------------------------------------------------

    private static void StartStun()
    {
        Report.AppendLine("Stun:");

        PlayerController player = PlayerController.Instance;
        PlayerSkills skills = PlayerSkills.Instance;
        if (player == null || skills == null) { Failures.Add("no player for the stun check"); return; }

        stunnedEnemy = null;
        foreach (EnemyHealth enemy in Object.FindObjectsOfType<EnemyHealth>())
        {
            if (enemy != null && !enemy.IsDead && !(enemy is BossHealth)) { stunnedEnemy = enemy; break; }
        }

        if (stunnedEnemy == null) { Failures.Add("no enemy to stun"); return; }

        // Bring the player alongside, well inside the stun radius.
        player.transform.position = stunnedEnemy.transform.position + new Vector3(1.5f, 0f, 0f);
        Physics2D.SyncTransforms();

        skills.StunSkill.Reset();
        int affected = skills.TryStun();

        stunnedEnemyPosition = stunnedEnemy.transform.position;
        Report.AppendLine("  stunned " + affected + " enemies");

        Expect(affected >= 1, "the stun reaches at least one enemy");
        Expect(EnemyStun.IsStunned(stunnedEnemy), "the enemy is stunned");
    }

    private static void CheckStunHeld()
    {
        if (stunnedEnemy == null) { return; }

        float moved = Vector2.Distance(stunnedEnemy.transform.position, stunnedEnemyPosition);
        Report.AppendLine("  enemy moved " + moved.ToString("0.000") + " units while stunned");

        Expect(EnemyStun.IsStunned(stunnedEnemy), "still stunned 0.8 seconds later");
        Expect(moved < 0.05f, "a stunned enemy does not move");
    }

    // ----- 1. forbidden zone ---------------------------------------------

    private static void StartIntrusion()
    {
        Report.AppendLine("Forbidden zone:");

        zone = Object.FindObjectOfType<ForbiddenZone>();
        if (zone == null) { Failures.Add("no forbidden zone in the level"); return; }

        EnemyHealth intruder = null;
        foreach (EnemyHealth enemy in Object.FindObjectsOfType<EnemyHealth>())
        {
            if (enemy != null && !enemy.IsDead && enemy != stunnedEnemy) { intruder = enemy; break; }
        }

        if (intruder == null) { intruder = stunnedEnemy; }
        if (intruder == null) { Failures.Add("no enemy to walk into the zone"); return; }

        // Walk the enemy in from outside, so it genuinely enters.
        intruder.transform.position = zone.transform.position + new Vector3(zone.Size.x, 0f, 0f);
        Physics2D.SyncTransforms();
        intruder.transform.position = zone.transform.position;
        Physics2D.SyncTransforms();

        Report.AppendLine("  moved '" + intruder.name + "' into the zone at " + ((Vector2)zone.transform.position).ToString("0.0"));
    }

    private static void CheckAlarm()
    {
        if (zone == null) { return; }

        Report.AppendLine("  warnings in the alarm: " + zone.WarningsInLastAlarm + " (configured " + zone.WarningCount + ")");

        Expect(zone.WarningsInLastAlarm >= ForbiddenZone.MinimumWarnings &&
               zone.WarningsInLastAlarm <= ForbiddenZone.MaximumWarnings,
               "the alarm sounds between 3 and 6 times");
        Expect(zone.WarningsInLastAlarm == zone.WarningCount, "the alarm plays its full configured count");
    }

    // ----- HUD ------------------------------------------------------------

    private static void CheckHud()
    {
        Report.AppendLine("HUD:");

        foreach (string name in new[] { "Health Slider", "Stamina Container", "Gold Amount Text",
                                        "ShieldButton", "StunButton", "ObjectiveText", "EffectsText", "SkillStatusText" })
        {
            GameObject found = FindAny(name);
            Expect(found != null, "HUD has '" + name + "'");
        }

        Text objective = FindAny("ObjectiveText") != null ? FindAny("ObjectiveText").GetComponent<Text>() : null;
        Text skills = FindAny("SkillStatusText") != null ? FindAny("SkillStatusText").GetComponent<Text>() : null;

        if (objective != null) { Report.AppendLine("  objective: " + objective.text); }
        if (skills != null) { Report.AppendLine("  skills: " + skills.text); }

        Expect(objective != null && objective.text.Length > 0, "the objective line has text");
        Expect(skills != null && skills.text.Contains("KHIÊN") && skills.text.Contains("CHOÁNG"), "the skill line lists both skills");
    }

    // ----- plumbing -------------------------------------------------------

    private static void Expect(bool condition, string description)
    {
        Report.AppendLine("  " + (condition ? "ok   " : "FAIL ") + description);
        if (!condition) { Failures.Add(description); }
    }

    private static GameObject FindAny(string name)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>(true))
        {
            if (t.name == name) { return t.gameObject; }
        }

        return null;
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;

        Report.AppendLine();
        Report.AppendLine(Failures.Count == 0
            ? "MECHANICS RESULT: all checks passed."
            : "MECHANICS RESULT: " + Failures.Count + " problem(s):");

        foreach (string failure in Failures) { Report.AppendLine("  FAIL " + failure); }

        Debug.Log("[MECHANICS SMOKE]\n" + Report);

        EditorApplication.ExitPlaymode();
        EditorApplication.update += QuitWhenStopped;
    }

    private static void QuitWhenStopped()
    {
        if (EditorApplication.isPlaying) { return; }

        EditorApplication.update -= QuitWhenStopped;

        AudioToggles.SfxEnabled = originalSfx;
        AudioToggles.MusicEnabled = originalMusic;
        PlayModeTestSettings.Restore();

        EditorApplication.Exit(0);
    }
}
