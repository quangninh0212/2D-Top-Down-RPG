using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One persistent audio hub: a looping music source and a pooled set of one-shot
// sources for effects. Clips are loaded by name from Resources, so a missing
// clip is a silent no-op rather than a null reference.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MasterVolumeKey = "settings.volume.master";
    private const string MusicVolumeKey = "settings.volume.music";
    private const string SfxVolumeKey = "settings.volume.sfx";

    // Effects fired many times in one frame (a burst of projectiles) should not
    // stack into a wall of noise.
    private const float SameSfxCooldown = 0.05f;
    private const int SfxSourceCount = 6;

    private AudioSource musicSource;
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private int nextSfxSource;

    private readonly Dictionary<GameSfx, AudioClip> sfxClips = new Dictionary<GameSfx, AudioClip>();
    private readonly Dictionary<GameMusic, AudioClip> musicClips = new Dictionary<GameMusic, AudioClip>();
    private readonly Dictionary<GameSfx, float> lastPlayed = new Dictionary<GameSfx, float>();

    private GameMusic currentMusic = GameMusic.None;
    private Coroutine musicFade;

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 0.6f;
    public float SfxVolume { get; private set; } = 0.9f;

    public static void EnsureExists()
    {
        if (Instance != null) { return; }

        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
        BuildSources();
        LoadClips();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    private void BuildSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;

        for (int i = 0; i < SfxSourceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sfxSources.Add(source);
        }

        ApplyVolumes();
    }

    // Clips live in Resources/Audio so they can be added later without touching
    // a prefab; anything missing simply stays silent.
    private void LoadClips()
    {
        foreach (GameSfx sfx in System.Enum.GetValues(typeof(GameSfx)))
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/" + sfx);
            if (clip != null) { sfxClips[sfx] = clip; }
        }

        foreach (GameMusic music in System.Enum.GetValues(typeof(GameMusic)))
        {
            if (music == GameMusic.None) { continue; }

            AudioClip clip = Resources.Load<AudioClip>("Audio/Music/" + music);
            if (clip != null) { musicClips[music] = clip; }
        }
    }

    // ----- playback -------------------------------------------------------

    public static void PlaySfx(GameSfx sfx)
    {
        if (Instance != null) { Instance.PlaySfxInternal(sfx); }
    }

    public static void PlayMusic(GameMusic music)
    {
        if (Instance != null) { Instance.PlayMusicInternal(music); }
    }

    private void PlaySfxInternal(GameSfx sfx)
    {
        if (!sfxClips.TryGetValue(sfx, out AudioClip clip) || clip == null) { return; }

        if (lastPlayed.TryGetValue(sfx, out float last) && Time.unscaledTime - last < SameSfxCooldown)
        {
            return;
        }

        lastPlayed[sfx] = Time.unscaledTime;

        AudioSource source = sfxSources[nextSfxSource];
        nextSfxSource = (nextSfxSource + 1) % sfxSources.Count;

        source.pitch = Random.Range(0.96f, 1.04f);
        source.PlayOneShot(clip, MasterVolume * SfxVolume);
    }

    private void PlayMusicInternal(GameMusic music)
    {
        if (music == currentMusic) { return; }

        currentMusic = music;

        if (music == GameMusic.None || !musicClips.TryGetValue(music, out AudioClip clip) || clip == null)
        {
            if (musicFade != null) { StopCoroutine(musicFade); }
            musicFade = StartCoroutine(FadeOutRoutine());
            return;
        }

        if (musicFade != null) { StopCoroutine(musicFade); }
        musicFade = StartCoroutine(CrossfadeRoutine(clip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip clip)
    {
        yield return FadeOutRoutine();

        musicSource.clip = clip;
        musicSource.Play();

        float target = MasterVolume * MusicVolume;
        float elapsed = 0f;

        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, target, elapsed / 0.4f);
            yield return null;
        }

        musicSource.volume = target;
        musicFade = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float start = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < 0.3f && start > 0f)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, elapsed / 0.3f);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    // ----- volume ---------------------------------------------------------

    private void LoadVolumes()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.9f);
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        if (musicSource == null) { return; }

        // Only touch the music level directly when no fade is running, or the
        // fade and the slider would fight each other.
        if (musicFade == null && musicSource.isPlaying)
        {
            musicSource.volume = MasterVolume * MusicVolume;
        }
    }

    // Music for a scene is chosen in one place so no scene has to remember.
    public static void PlayMusicForScene(string sceneName)
    {
        if (sceneName == GameScenes.MainMenu || sceneName == GameScenes.Splash)
        {
            PlayMusic(GameMusic.Menu);
        }
        else if (sceneName == GameScenes.Scene5)
        {
            PlayMusic(GameMusic.Boss);
        }
        else if (sceneName == GameScenes.Victory)
        {
            PlayMusic(GameMusic.Victory);
        }
        else if (GameScenes.IsGameplayScene(sceneName))
        {
            PlayMusic(GameMusic.Level);
        }
    }
}
