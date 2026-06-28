using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class SoundData
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio List")]
    [SerializeField] private List<SoundData> musicList = new List<SoundData>();
    [SerializeField] private List<SoundData> sfxList = new List<SoundData>();

    private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
    private readonly Dictionary<string, SoundData> musicByName =
        new Dictionary<string, SoundData>(StringComparer.Ordinal);
    private readonly Dictionary<string, SoundData> sfxByName =
        new Dictionary<string, SoundData>(StringComparer.Ordinal);

    private string currentMusicName;
    private SoundData currentMusic;

    public float MusicVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    public bool IsMusicMuted { get; private set; }
    public bool IsSFXMuted { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            BuildSoundLookup(musicList, musicByName);
            BuildSoundLookup(sfxList, sfxByName);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        foreach (Button button in registeredButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayButtonClickSFX);
        }

        registeredButtons.Clear();
        Instance = null;
    }

    public void PlayMusic(MusicName musicName)
    {
        PlayMusic(musicName.ToString());
    }

    public void PlaySFX(SFXName sfxName)
    {
        PlaySFX(sfxName.ToString());
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || IsSFXMuted)
            return;

        sfxSource.PlayOneShot(clip, SFXVolume);
    }

    public void PlayMusic(string musicName)
    {
        if (!musicByName.TryGetValue(musicName, out SoundData music) ||
            music.clip == null)
            return;

        if (currentMusicName == musicName && musicSource.isPlaying)
            return;

        currentMusicName = musicName;
        currentMusic = music;
        musicSource.clip = music.clip;
        musicSource.loop = music.loop;
        ApplyMusicVolume();
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(string sfxName)
    {
        if (!sfxByName.TryGetValue(sfxName, out SoundData sfx) ||
            sfx.clip == null)
            return;

        if (IsSFXMuted) return;

        sfxSource.PlayOneShot(sfx.clip, sfx.volume * SFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        ApplyMusicVolume();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
    }

    public void ToggleMusicMute()
    {
        IsMusicMuted = !IsMusicMuted;
        ApplyMusicVolume();
    }

    public void ToggleSFXMute()
    {
        IsSFXMuted = !IsSFXMuted;
    }

    public void RegisterButton(Button button)
    {
        if (button == null || !registeredButtons.Add(button))
            return;

        button.onClick.AddListener(PlayButtonClickSFX);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case GameScenes.Gameplay:
            case GameScenes.MainMenu:
                PlayMusic(MusicName.Gameplay);
                break;
            case GameScenes.Battle:
                PlayMusic(MusicName.Battle);
                break;
            case GameScenes.Interior:
                PlayMusic(MusicName.Interior);
                break;
        }

        RegisterSceneButtons();
    }

    private void RegisterSceneButtons()
    {
        registeredButtons.RemoveWhere(button => button == null);

        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
            RegisterButton(button);
    }

    private void PlayButtonClickSFX()
    {
        if (SceneManager.GetActiveScene().name == GameScenes.Battle)
            return;

        PlaySFX(SFXName.Click);
    }

    private void ApplyMusicVolume()
    {
        if (musicSource == null)
            return;

        float trackVolume = currentMusic != null
            ? currentMusic.volume
            : 1f;

        musicSource.volume = IsMusicMuted
            ? 0f
            : trackVolume * MusicVolume;
    }

    private static void BuildSoundLookup(
        IEnumerable<SoundData> sounds,
        IDictionary<string, SoundData> destination)
    {
        destination.Clear();

        foreach (SoundData sound in sounds)
        {
            if (sound == null || string.IsNullOrWhiteSpace(sound.name))
                continue;

            destination[sound.name.Trim()] = sound;
        }
    }
}
