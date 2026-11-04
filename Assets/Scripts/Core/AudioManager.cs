using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent audio hub. It creates itself before the first scene loads, so any scene can be
/// played on its own. Clips come from Resources/AudioLibrary.
/// Usage from any script: AudioManager.PlaySfx(AudioManager.Lib?.enemyHit);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public const string MusicVolumeKey = "MusicVolume";
    public const string SfxVolumeKey = "SfxVolume";

    public static AudioManager Instance { get; private set; }

    /// <summary>The library, or null when the asset does not exist yet.</summary>
    public static AudioLibrary Lib => Instance != null ? Instance.library : null;

    private AudioLibrary library;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip currentMusic;

    public float MusicVolume { get; private set; } = 0.7f;
    public float SfxVolume { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

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
        library = Resources.Load<AudioLibrary>("AudioLibrary");

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.ignoreListenerPause = true;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.ignoreListenerPause = true;

        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        musicSource.volume = MusicVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic(MusicForScene(scene.name));
        AttachButtonSounds();
    }

    private AudioClip MusicForScene(string sceneName)
    {
        if (library == null)
            return null;

        switch (sceneName)
        {
            case "SampleScene": return library.level1Music;
            case "Nivel2": return library.level2Music;
            default: return library.menuMusic;
        }
    }

    /// <summary>Adds the click sound to every Button of the loaded scene (active or not).</summary>
    private static void AttachButtonSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
        {
            if (button.GetComponent<UIButtonSound>() == null)
                button.gameObject.AddComponent<UIButtonSound>();
        }
    }

    // ---------- Music ----------

    public void PlayMusic(AudioClip clip)
    {
        if (clip == currentMusic && musicSource.isPlaying)
            return;

        currentMusic = clip;
        musicSource.Stop();
        musicSource.clip = clip;

        if (clip != null)
            musicSource.Play();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        musicSource.volume = MusicVolume;
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    // ---------- Sound effects ----------

    /// <summary>Plays a one-shot effect with the SFX volume. Null clips and a missing manager are ignored.</summary>
    public static void PlaySfx(AudioClip clip)
    {
        if (clip == null || Instance == null)
            return;

        Instance.sfxSource.PlayOneShot(clip, Instance.SfxVolume);
    }
}
