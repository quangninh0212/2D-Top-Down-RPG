using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Generates every sound in the game as an original waveform written to WAV.
// Nothing is downloaded and nothing is copied: each clip is synthesised here
// from simple oscillators and noise, so the audio is unambiguously the
// project's own work.
public static class PlaceholderAudioGenerator
{
    private const int SampleRate = 22050;
    private const string SfxFolder = "Assets/Resources/Audio/SFX";
    private const string MusicFolder = "Assets/Resources/Audio/Music";

    // Deterministic noise, so re-running the generator produces identical files
    // rather than a fresh random texture every time.
    private static System.Random random = new System.Random(20240905);

    [MenuItem("Tools/Soulbound Gate/Generate Audio")]
    public static void GenerateAll()
    {
        random = new System.Random(20240905);

        Directory.CreateDirectory(SfxFolder);
        Directory.CreateDirectory(MusicFolder);

        GenerateSfx();
        GenerateMusic();

        WriteAttribution();

        AssetDatabase.Refresh();
        SoulboundSetupLog.Step("Audio generated into Resources/Audio.");
    }

    // ----- effects --------------------------------------------------------

    private static void GenerateSfx()
    {
        Write(SfxFolder, GameSfx.UiClick.ToString(), Blip(1200f, 0.06f, 0.25f));
        Write(SfxFolder, GameSfx.SwordSwing.ToString(), Swish(0.22f));
        Write(SfxFolder, GameSfx.BowShot.ToString(), Twang(0.25f));
        Write(SfxFolder, GameSfx.StaffShot.ToString(), Sweep(320f, 1400f, 0.35f));
        Write(SfxFolder, GameSfx.Projectile.ToString(), Sweep(700f, 400f, 0.18f));
        Write(SfxFolder, GameSfx.PlayerHurt.ToString(), Sweep(520f, 180f, 0.28f));
        Write(SfxFolder, GameSfx.PlayerDeath.ToString(), Sweep(420f, 90f, 0.9f));
        Write(SfxFolder, GameSfx.EnemyHurt.ToString(), Thud(260f, 0.16f));
        Write(SfxFolder, GameSfx.EnemyDeath.ToString(), NoiseBurst(0.35f, 0.35f));
        Write(SfxFolder, GameSfx.CoinPickup.ToString(), Arpeggio(new[] { 1046f, 1568f }, 0.16f));
        Write(SfxFolder, GameSfx.HealthPickup.ToString(), Arpeggio(new[] { 659f, 880f, 1046f }, 0.28f));
        Write(SfxFolder, GameSfx.StaminaPickup.ToString(), Arpeggio(new[] { 587f, 784f }, 0.2f));
        Write(SfxFolder, GameSfx.GateOpen.ToString(), Arpeggio(new[] { 392f, 523f, 659f, 784f }, 0.85f));
        Write(SfxFolder, GameSfx.Dash.ToString(), Swish(0.16f));
        Write(SfxFolder, GameSfx.BossAttack.ToString(), Sweep(180f, 520f, 0.4f));
        Write(SfxFolder, GameSfx.Victory.ToString(), Arpeggio(new[] { 523f, 659f, 784f, 1046f }, 1.1f));
        Write(SfxFolder, GameSfx.Purchase.ToString(), Arpeggio(new[] { 784f, 1046f }, 0.22f));
        Write(SfxFolder, GameSfx.Denied.ToString(), Arpeggio(new[] { 300f, 220f }, 0.22f));
    }

    private static float[] Blip(float frequency, float duration, float volume)
    {
        int count = Samples(duration);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            data[i] = Square(frequency, t) * Envelope(i, count, 0.01f, 0.5f) * volume;
        }

        return data;
    }

    // Filtered noise with a fast attack: reads as a blade cutting the air.
    private static float[] Swish(float duration)
    {
        int count = Samples(duration);
        float[] data = new float[count];
        float previous = 0f;

        for (int i = 0; i < count; i++)
        {
            float progress = i / (float)count;

            // The low-pass opens then closes, which is what gives the sweep.
            float cutoff = Mathf.Lerp(0.05f, 0.65f, Mathf.Sin(progress * Mathf.PI));
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);

            previous = Mathf.Lerp(previous, noise, cutoff);
            data[i] = previous * Envelope(i, count, 0.02f, 0.6f) * 0.35f;
        }

        return data;
    }

    private static float[] Twang(float duration)
    {
        int count = Samples(duration);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float progress = i / (float)count;

            // Pitch falls away quickly, like a released string.
            float frequency = Mathf.Lerp(880f, 300f, progress * progress);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * Envelope(i, count, 0.005f, 0.8f) * 0.4f;
        }

        return data;
    }

    private static float[] Sweep(float from, float to, float duration)
    {
        int count = Samples(duration);
        float[] data = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float progress = i / (float)count;
            float frequency = Mathf.Lerp(from, to, progress);

            phase += 2f * Mathf.PI * frequency / SampleRate;
            data[i] = Mathf.Sin(phase) * Envelope(i, count, 0.02f, 0.5f) * 0.35f;
        }

        return data;
    }

    private static float[] Thud(float frequency, float duration)
    {
        int count = Samples(duration);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0) * 0.3f;

            data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * t) + noise) * Envelope(i, count, 0.005f, 0.9f) * 0.35f;
        }

        return data;
    }

    private static float[] NoiseBurst(float duration, float volume)
    {
        int count = Samples(duration);
        float[] data = new float[count];
        float previous = 0f;

        for (int i = 0; i < count; i++)
        {
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            previous = Mathf.Lerp(previous, noise, 0.35f);

            data[i] = previous * Envelope(i, count, 0.005f, 0.95f) * volume;
        }

        return data;
    }

    private static float[] Arpeggio(float[] notes, float duration)
    {
        int count = Samples(duration);
        float[] data = new float[count];
        int perNote = Mathf.Max(1, count / notes.Length);

        for (int i = 0; i < count; i++)
        {
            int noteIndex = Mathf.Min(notes.Length - 1, i / perNote);
            int localIndex = i - noteIndex * perNote;

            float t = localIndex / (float)SampleRate;
            float value = Square(notes[noteIndex], t) * 0.5f + Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * 0.5f;

            data[i] = value * Envelope(localIndex, perNote, 0.01f, 0.5f) * Envelope(i, count, 0.01f, 0.2f) * 0.3f;
        }

        return data;
    }

    // ----- music ----------------------------------------------------------

    private static void GenerateMusic()
    {
        // A minor / natural minor material throughout: sombre without being grim.
        Write(MusicFolder, GameMusic.Menu.ToString(),
              Loop(new[] { 220f, 261f, 329f, 261f }, new[] { 110f, 110f, 98f, 98f }, 8f, 0.9f));

        Write(MusicFolder, GameMusic.Level.ToString(),
              Loop(new[] { 293f, 349f, 440f, 349f, 329f, 392f, 493f, 392f }, new[] { 146f, 146f, 164f, 164f }, 8f, 0.55f));

        Write(MusicFolder, GameMusic.Boss.ToString(),
              Loop(new[] { 233f, 233f, 277f, 311f, 233f, 349f, 311f, 277f }, new[] { 116f, 116f, 87f, 87f }, 8f, 0.4f));

        Write(MusicFolder, GameMusic.Victory.ToString(),
              Loop(new[] { 523f, 659f, 784f, 1046f, 784f, 659f }, new[] { 130f, 164f, 196f, 261f }, 6f, 0.75f));
    }

    // Two voices: a square-wave melody over a soft triangle bass, with the
    // whole loop crossfaded at the seam so it repeats without a click.
    private static float[] Loop(float[] melody, float[] bass, float duration, float noteLength)
    {
        int count = Samples(duration);
        float[] data = new float[count];

        float melodyStep = duration / melody.Length;
        float bassStep = duration / bass.Length;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;

            int melodyIndex = Mathf.Min(melody.Length - 1, (int)(t / melodyStep));
            int bassIndex = Mathf.Min(bass.Length - 1, (int)(t / bassStep));

            float melodyLocal = t - melodyIndex * melodyStep;
            float bassLocal = t - bassIndex * bassStep;

            float melodyGate = Mathf.Clamp01(1f - melodyLocal / (melodyStep * noteLength));
            float bassGate = Mathf.Clamp01(1f - bassLocal / bassStep);

            float lead = Square(melody[melodyIndex], t) * melodyGate * melodyGate * 0.16f;
            float low = Triangle(bass[bassIndex], t) * bassGate * 0.22f;

            data[i] = lead + low;
        }

        CrossfadeSeam(data, Samples(0.25f));
        return data;
    }

    // Blends the tail into the head so the loop point is inaudible.
    private static void CrossfadeSeam(float[] data, int fadeSamples)
    {
        fadeSamples = Mathf.Min(fadeSamples, data.Length / 4);

        for (int i = 0; i < fadeSamples; i++)
        {
            float blend = i / (float)fadeSamples;
            int tail = data.Length - fadeSamples + i;

            float mixed = data[tail] * (1f - blend) + data[i] * blend;
            data[i] = mixed;
            data[tail] = mixed;
        }
    }

    // ----- helpers --------------------------------------------------------

    private static int Samples(float seconds)
    {
        return Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
    }

    private static float Square(float frequency, float t)
    {
        return Mathf.Sin(2f * Mathf.PI * frequency * t) >= 0f ? 1f : -1f;
    }

    private static float Triangle(float frequency, float t)
    {
        float phase = (t * frequency) % 1f;
        return 4f * Mathf.Abs(phase - 0.5f) - 1f;
    }

    private static float Envelope(int index, int count, float attackFraction, float releaseFraction)
    {
        float progress = index / (float)Mathf.Max(1, count);

        float attack = attackFraction <= 0f ? 1f : Mathf.Clamp01(progress / attackFraction);
        float release = releaseFraction <= 0f ? 1f : Mathf.Clamp01((1f - progress) / releaseFraction);

        return attack * release;
    }

    private static void Write(string folder, string name, float[] samples)
    {
        string path = Path.Combine(folder, name + ".wav");
        File.WriteAllBytes(path, EncodeWav(samples));
    }

    // Minimal 16-bit mono PCM WAV.
    private static byte[] EncodeWav(float[] samples)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int dataBytes = samples.Length * 2;

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });

            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);                       // PCM header size
            writer.Write((short)1);                 // PCM format
            writer.Write((short)1);                 // mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);           // byte rate
            writer.Write((short)2);                 // block align
            writer.Write((short)16);                // bits per sample

            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                writer.Write(value);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }

    private static void WriteAttribution()
    {
        string text =
            "All audio in this folder was generated procedurally by" + Environment.NewLine +
            "Assets/Editor/PlaceholderAudioGenerator.cs." + Environment.NewLine + Environment.NewLine +
            "Generated specifically for this project. No third-party or copyrighted" + Environment.NewLine +
            "material is used. Re-run Tools > Soulbound Gate > Generate Audio to rebuild." + Environment.NewLine;

        File.WriteAllText("Assets/Resources/Audio/AUDIO_SOURCE.txt", text);
    }
}
